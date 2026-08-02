namespace CalradiaForge.UI.Lifecycle;

using System.Windows;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Eula;
using CalradiaForge.Core.Infra.GamePlatform;
using CalradiaForge.Core.Infra.Localization;
using CalradiaForge.Core.Infra.Modpacks;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Models;
using CalradiaForge.UI.Composition;
using CalradiaForge.UI.Dialogs;
using CalradiaForge.UI.Toasts;
using CalradiaForge.UI.Views;

using Serilog;

/// <summary>
/// Owns the ordered startup gates and defers shell resolution until they pass.
/// </summary>
public sealed class ApplicationStartupCoordinator {
	private readonly ILogger _logger;
	private readonly TranslationManager _translationManager;
	private readonly TranslationService _translationService;
	private readonly ConfigFileManager _configFileManager;
	private readonly AppSettings _appSettings;
	private readonly EulaService _eulaService;
	private readonly ILanguageSelectionDialogService _languageDialog;
	private readonly IEulaDialogService _eulaDialog;
	private readonly GameDetectionService _gameDetectionService;
	private readonly ModPipelineManager _modPipeline;
	private readonly ModpackService _modpackService;
	private readonly StartupNotificationQueue _startupNotifications;
	private readonly StartupNotificationDrainCoordinator _notificationDrain;
	private readonly IInstallNotificationPresenter _installNotifications;
	private readonly IMainWindowProvider _mainWindowProvider;
	private bool _configurationWarningPublished;
	private bool _shellDisplayed;

	public ApplicationStartupCoordinator(
		ILogger logger,
		TranslationManager translationManager,
		TranslationService translationService,
		ConfigFileManager configFileManager,
		AppSettings appSettings,
		EulaService eulaService,
		ILanguageSelectionDialogService languageDialog,
		IEulaDialogService eulaDialog,
		GameDetectionService gameDetectionService,
		ModPipelineManager modPipeline,
		ModpackService modpackService,
		StartupNotificationQueue startupNotifications,
		StartupNotificationDrainCoordinator notificationDrain,
		IInstallNotificationPresenter installNotifications,
		IMainWindowProvider mainWindowProvider) {
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_translationManager = translationManager ?? throw new ArgumentNullException(nameof(translationManager));
		_translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
		_configFileManager = configFileManager ?? throw new ArgumentNullException(nameof(configFileManager));
		_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
		_eulaService = eulaService ?? throw new ArgumentNullException(nameof(eulaService));
		_languageDialog = languageDialog ?? throw new ArgumentNullException(nameof(languageDialog));
		_eulaDialog = eulaDialog ?? throw new ArgumentNullException(nameof(eulaDialog));
		_gameDetectionService = gameDetectionService ?? throw new ArgumentNullException(nameof(gameDetectionService));
		_modPipeline = modPipeline ?? throw new ArgumentNullException(nameof(modPipeline));
		_modpackService = modpackService ?? throw new ArgumentNullException(nameof(modpackService));
		_startupNotifications = startupNotifications ?? throw new ArgumentNullException(nameof(startupNotifications));
		_notificationDrain = notificationDrain ?? throw new ArgumentNullException(nameof(notificationDrain));
		_installNotifications = installNotifications
			?? throw new ArgumentNullException(nameof(installNotifications));
		_mainWindowProvider = mainWindowProvider ?? throw new ArgumentNullException(nameof(mainWindowProvider));
		_configFileManager.PersistenceBecameUnavailable += OnPersistenceBecameUnavailable;
	}

	/// <summary>
	/// Runs startup and returns false when an intentional gate rejects shell creation.
	/// </summary>
	public async Task<bool> StartAsync(CancellationToken cancellationToken = default) {
		_logger.Information("CalradiaForge application startup began.");

		IReadOnlyList<LanguageOption> languages = _translationManager.LoadManifest();
		if (!_appSettings.EulaAccepted && languages.Count > 0
			&& _languageDialog.TrySelectLanguage(languages, _appSettings.Language, out string selectedLanguage)
			&& !string.IsNullOrWhiteSpace(selectedLanguage)) {
			_appSettings.Language = selectedLanguage;
		}

		_translationService.Initialize();
		_eulaService.Load();
		if (_eulaService.RequiresAcceptance(_appSettings)) {
			if (!_eulaDialog.RequestAcceptance(_eulaService.EulaText)) {
				_logger.Information("EULA was declined; the application shell will not be created.");
				return false;
			}
			_eulaService.RecordAcceptance(_appSettings);
		}

		PublishConfigurationWarningOnce();

		_gameDetectionService.InitializeForStartup(_appSettings);

		AcceptedModSnapshot cachedSnapshot = _modPipeline.LoadAcceptedCache();
		_logger.Debug(
			"Loaded accepted module cache snapshot {SnapshotVersion} containing {ModuleCount} modules.",
			cachedSnapshot.Version,
			cachedSnapshot.Modules.Count);

		ModPipelineResult pipelineResult = await _modPipeline.InitializeForStartupAsync(cancellationToken);
		if (!pipelineResult.Success) {
			_startupNotifications.Enqueue(new StartupNotification(
				"Module Scan",
				pipelineResult.UserSummary,
				pipelineResult.IsCancelled
					? StartupNotificationSeverity.Warning
					: StartupNotificationSeverity.Error));
		}

		_modpackService.LoadAll();
		ValidateStartupModpack();

		_installNotifications.Activate();
		await _notificationDrain.StartAsync(cancellationToken);
		MainWindow mainWindow = _mainWindowProvider.GetMainWindow();
		Application.Current.MainWindow = mainWindow;
		mainWindow.Show();
		_shellDisplayed = true;
		_logger.Information("CalradiaForge main window displayed.");
		return true;
	}

	private void PublishConfigurationWarningOnce() {
		if (_configurationWarningPublished
			|| _configFileManager.PersistenceStatus == ConfigPersistenceStatus.Available) {
			return;
		}

		_configurationWarningPublished = true;
		const string message =
			"CalradiaForge can continue for this session, but configuration changes may not survive restart.";
		_logger.Warning(
			"Configuration persistence is unavailable. LoadStatus={LoadStatus} SaveStatus={SaveStatus}. {Message}",
			_configFileManager.LastLoadResult.Status,
			_configFileManager.LastSaveResult.Status,
			message);
		StartupNotification notification = new(
			"Configuration",
			message,
			StartupNotificationSeverity.Warning);
		if (_shellDisplayed) {
			_ = PresentConfigurationWarningAsync(notification);
		} else {
			_startupNotifications.Enqueue(notification);
		}
	}

	private void OnPersistenceBecameUnavailable(
		object? sender,
		ConfigPersistenceUnavailableEventArgs e) => PublishConfigurationWarningOnce();

	private async Task PresentConfigurationWarningAsync(StartupNotification notification) {
		try {
			await _notificationDrain.PresentAsync(notification);
		} catch (Exception ex) {
			_logger.Error(ex, "Configuration persistence warning could not be presented.");
		}
	}

	private void ValidateStartupModpack() {
		ModpackModel? selected = _appSettings.ModpackStartupMode switch {
			ModpackStartupMode.AlwaysDefault =>
				_modpackService.FindByName(VanillaModules.DefaultModpackName),
			ModpackStartupMode.LastUsed when string.Equals(
				_appSettings.LastSelectedModpack,
				"Last Used",
				StringComparison.OrdinalIgnoreCase) =>
				_modpackService.LastUsedModpack,
			ModpackStartupMode.LastUsed =>
				_modpackService.FindByName(_appSettings.LastSelectedModpack),
			_ => null
		};

		if (selected is null) {
			return;
		}

		(List<ModpackEntryModel> validEntries, List<string> missingNames) =
			ModpackService.ValidateLoadOrder(selected, _modPipeline.AcceptedSnapshot.Modules);
		_modpackService.CurrentLoadOrderEntries = validEntries;
		if (missingNames.Count > 0) {
			_startupNotifications.Enqueue(new StartupNotification(
				"Modpack Validation",
				$"{selected.ModpackName} is missing {missingNames.Count} installed module(s).",
				StartupNotificationSeverity.Warning));
		}
	}
}
