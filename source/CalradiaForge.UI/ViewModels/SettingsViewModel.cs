namespace CalradiaForge.UI.ViewModels;

using System.Reflection;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.GamePlatform;
using CalradiaForge.Core.Infra.Localization;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Infra.Paths;
using CalradiaForge.Core.Models;
using CalradiaForge.UI.Interactions;
using CalradiaForge.UI.Lifecycle;
using CalradiaForge.UI.Threading;
using CalradiaForge.UI.Toasts;

using Serilog;

using ToolkitRelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand;
using NullableBoolAsyncRelayCommand =
	CommunityToolkit.Mvvm.Input.AsyncRelayCommand<bool?>;

/// <summary>
/// Owns Settings presentation state and invokes the existing Core settings,
/// detection, pipeline, and maintenance authorities.
/// </summary>
public sealed class SettingsViewModel : ViewModelBase {
	private readonly AppSettings _appSettings;
	private readonly LoggingSettings _loggingSettings;
	private readonly ModPipelineManager _modPipeline;
	private readonly GameDetectionService _gameDetectionService;
	private readonly ISettingsFilePicker _filePicker;
	private readonly ISettingsFolderPicker _folderPicker;
	private readonly ISettingsShellLauncher _shellLauncher;
	private readonly ISettingsDllUnblocker _dllUnblocker;
	private readonly IToastNotificationSink _notifications;
	private readonly IApplicationLifetime _applicationLifetime;
	private readonly IUiDispatcher _uiDispatcher;
	private string _gameFolderPath = string.Empty;
	private string _gameLauncherFilePath = string.Empty;
	private string _steamWorkshopFolderPath = string.Empty;
	private string _blseExePath = string.Empty;
	private int _selectedLanguageIndex = -1;
	private ModpackStartupMode _startupMode = ModpackStartupMode.LastUsed;
	private bool _isDebugModeEnabled;
	private bool _showGameExecutablePath = true;
	private bool _showWorkshopPath = true;
	private bool _isGameFolderValid;
	private bool _isWorkshopFolderValid;
	private bool _isBlsePathValidOrOptional = true;
	private string _gameFolderValidationText = string.Empty;
	private string _workshopFolderValidationText = string.Empty;
	private string _blseValidationText = string.Empty;
	private string _unblockLastRunText = string.Empty;
	private string _unblockResultText = string.Empty;
	private bool _suppressLanguageSelection;

	public SettingsViewModel(
		AppSettings appSettings,
		LoggingSettings loggingSettings,
		ModPipelineManager modPipeline,
		GameDetectionService gameDetectionService,
		ISettingsFilePicker filePicker,
		ISettingsFolderPicker folderPicker,
		ISettingsShellLauncher shellLauncher,
		ISettingsDllUnblocker dllUnblocker,
		IToastNotificationSink notifications,
		IApplicationLifetime applicationLifetime,
		IUiDispatcher uiDispatcher,
		TranslationService translator) {
		_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
		_loggingSettings = loggingSettings
			?? throw new ArgumentNullException(nameof(loggingSettings));
		_modPipeline = modPipeline ?? throw new ArgumentNullException(nameof(modPipeline));
		_gameDetectionService = gameDetectionService
			?? throw new ArgumentNullException(nameof(gameDetectionService));
		_filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
		_folderPicker = folderPicker ?? throw new ArgumentNullException(nameof(folderPicker));
		_shellLauncher = shellLauncher ?? throw new ArgumentNullException(nameof(shellLauncher));
		_dllUnblocker = dllUnblocker ?? throw new ArgumentNullException(nameof(dllUnblocker));
		_notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
		_applicationLifetime = applicationLifetime
			?? throw new ArgumentNullException(nameof(applicationLifetime));
		_uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
		Translator = translator ?? throw new ArgumentNullException(nameof(translator));

		SetDebugModeCommand = new NullableBoolAsyncRelayCommand(SetDebugModeAsync);
		SelectGameFolderCommand = new ToolkitRelayCommand(
			SelectGameFolder,
			CanSelectGameFolder);
		SelectGameExecutableCommand = new ToolkitRelayCommand(SelectGameExecutable);
		SelectWorkshopFolderCommand = new ToolkitRelayCommand(
			SelectWorkshopFolder,
			CanSelectWorkshopFolder);
		SelectBlseExecutableCommand = new ToolkitRelayCommand(SelectBlseExecutable);
		RedetectGameCommand = new ToolkitRelayCommand(RedetectGame);
		UnblockDllsCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(UnblockDllsAsync);
		ClearModCacheCommand = new ToolkitRelayCommand(ClearModCache);
		OpenConfigFolderCommand = new ToolkitRelayCommand(
			() => OpenFolder(AppPaths.ConfigDirectory, "configuration"));
		OpenLogsFolderCommand = new ToolkitRelayCommand(
			() => OpenFolder(AppPaths.LogsDirectory, "logs"));
		OpenModpacksFolderCommand = new ToolkitRelayCommand(
			() => OpenFolder(AppPaths.ModpacksDirectory, "modpacks"));
	}

	private TranslationStrings T => Translator.Strings;

	public TranslationService Translator { get; }
	public IReadOnlyList<LanguageOption> AvailableLanguages => Translator.AvailableLanguages;
	public string VersionText { get; } = BuildVersionText();

	public NullableBoolAsyncRelayCommand SetDebugModeCommand { get; }
	public ToolkitRelayCommand SelectGameFolderCommand { get; }
	public ToolkitRelayCommand SelectGameExecutableCommand { get; }
	public ToolkitRelayCommand SelectWorkshopFolderCommand { get; }
	public ToolkitRelayCommand SelectBlseExecutableCommand { get; }
	public ToolkitRelayCommand RedetectGameCommand { get; }
	public CommunityToolkit.Mvvm.Input.AsyncRelayCommand UnblockDllsCommand { get; }
	public ToolkitRelayCommand ClearModCacheCommand { get; }
	public ToolkitRelayCommand OpenConfigFolderCommand { get; }
	public ToolkitRelayCommand OpenLogsFolderCommand { get; }
	public ToolkitRelayCommand OpenModpacksFolderCommand { get; }

	public string GameFolderPath {
		get => _gameFolderPath;
		private set => SetProperty(ref _gameFolderPath, value);
	}

	public string GameLauncherFilePath {
		get => _gameLauncherFilePath;
		private set => SetProperty(ref _gameLauncherFilePath, value);
	}

	public string SteamWorkshopFolderPath {
		get => _steamWorkshopFolderPath;
		private set => SetProperty(ref _steamWorkshopFolderPath, value);
	}

	public string BLSEExePath {
		get => _blseExePath;
		private set => SetProperty(ref _blseExePath, value);
	}

	public int SelectedLanguageIndex {
		get => _selectedLanguageIndex;
		set {
			if (!SetProperty(ref _selectedLanguageIndex, value)
				|| _suppressLanguageSelection
				|| value < 0
				|| value >= AvailableLanguages.Count) {
				return;
			}
			LanguageOption selected = AvailableLanguages[value];
			Translator.SetLanguage(selected.Code);
			UpdateValidation();
			Log.Debug(
				"SettingsViewModel: Language selection changed to {LanguageCode} ({LanguageDisplayName}).",
				selected.Code,
				selected.DisplayName);
		}
	}

	public bool IsLanguageSelectionEnabled => AvailableLanguages.Count > 1;

	public bool IsLastUsedStartupMode {
		get => _startupMode == ModpackStartupMode.LastUsed;
		set { if (value) SetStartupMode(ModpackStartupMode.LastUsed); }
	}

	public bool IsAlwaysDefaultStartupMode {
		get => _startupMode == ModpackStartupMode.AlwaysDefault;
		set { if (value) SetStartupMode(ModpackStartupMode.AlwaysDefault); }
	}

	public bool IsAlwaysAskStartupMode {
		get => _startupMode == ModpackStartupMode.AlwaysAsk;
		set { if (value) SetStartupMode(ModpackStartupMode.AlwaysAsk); }
	}

	public bool IsDebugModeEnabled {
		get => _isDebugModeEnabled;
		private set => SetProperty(ref _isDebugModeEnabled, value);
	}

	public bool ShowGameExecutablePath {
		get => _showGameExecutablePath;
		private set => SetProperty(ref _showGameExecutablePath, value);
	}

	public bool ShowWorkshopPath {
		get => _showWorkshopPath;
		private set => SetProperty(ref _showWorkshopPath, value);
	}

	public bool IsGameFolderValid {
		get => _isGameFolderValid;
		private set => SetProperty(ref _isGameFolderValid, value);
	}

	public bool IsWorkshopFolderValid {
		get => _isWorkshopFolderValid;
		private set => SetProperty(ref _isWorkshopFolderValid, value);
	}

	public bool IsBlsePathValidOrOptional {
		get => _isBlsePathValidOrOptional;
		private set => SetProperty(ref _isBlsePathValidOrOptional, value);
	}

	public string GameFolderValidationText {
		get => _gameFolderValidationText;
		private set => SetProperty(ref _gameFolderValidationText, value);
	}

	public string WorkshopFolderValidationText {
		get => _workshopFolderValidationText;
		private set => SetProperty(ref _workshopFolderValidationText, value);
	}

	public string BlseValidationText {
		get => _blseValidationText;
		private set => SetProperty(ref _blseValidationText, value);
	}

	public string UnblockLastRunText {
		get => _unblockLastRunText;
		private set => SetProperty(ref _unblockLastRunText, value);
	}

	public string UnblockResultText {
		get => _unblockResultText;
		private set => SetProperty(ref _unblockResultText, value);
	}

	protected override async Task OnInitializeAsync(CancellationToken cancellationToken) {
		await SynchronizeStateAsync(cancellationToken);
		Log.Debug(
			"SettingsViewModel: Initialized for game provider {GameProvider}.",
			_appSettings.GameProvider);
	}

	protected override Task OnActivateAsync(CancellationToken cancellationToken) =>
		SynchronizeStateAsync(cancellationToken);

	private Task SynchronizeStateAsync(CancellationToken cancellationToken) =>
		_uiDispatcher.InvokeAsync(() => {
			GameFolderPath = _appSettings.GameFolderPath;
			GameLauncherFilePath = _appSettings.GameLauncherFilePath;
			SteamWorkshopFolderPath = _appSettings.SteamWorkshopFolderPath;
			BLSEExePath = _appSettings.BLSEExePath;
			UpdateStartupMode(_appSettings.ModpackStartupMode);
			IsDebugModeEnabled = _loggingSettings.DebugMode;
			UpdatePathVisibility();
			UpdateValidation();
			LoadUnblockStatus();
			_suppressLanguageSelection = true;
			try {
				int index = AvailableLanguages
					.Select((language, itemIndex) => new { language, itemIndex })
					.FirstOrDefault(item => string.Equals(
						item.language.Code,
						Translator.ActiveLanguageCode,
						StringComparison.OrdinalIgnoreCase))?.itemIndex ?? 0;
				SetProperty(
					ref _selectedLanguageIndex,
					AvailableLanguages.Count == 0 ? -1 : index,
					nameof(SelectedLanguageIndex));
				OnPropertyChanged(nameof(AvailableLanguages));
				OnPropertyChanged(nameof(IsLanguageSelectionEnabled));
			} finally {
				_suppressLanguageSelection = false;
			}
			Log.Debug(
				"SettingsViewModel: Synchronized state: game folder {GameFolderPath}, launcher {GameLauncherFilePath}, Workshop folder {SteamWorkshopFolderPath}, BLSE executable {BLSEExePath}, debug mode {DebugMode}.",
				GameFolderPath,
				GameLauncherFilePath,
				SteamWorkshopFolderPath,
				BLSEExePath,
				IsDebugModeEnabled);
		}, cancellationToken);

	private void SetStartupMode(ModpackStartupMode mode) {
		if (_startupMode == mode) {
			return;
		}
		_appSettings.ModpackStartupMode = mode;
		UpdateStartupMode(mode);
		Log.Debug(
			"SettingsViewModel: Modpack startup mode changed to {ModpackStartupMode}.",
			mode);
	}

	private void UpdateStartupMode(ModpackStartupMode mode) {
		if (_startupMode == mode) {
			return;
		}
		_startupMode = mode;
		OnPropertyChanged(nameof(IsLastUsedStartupMode));
		OnPropertyChanged(nameof(IsAlwaysDefaultStartupMode));
		OnPropertyChanged(nameof(IsAlwaysAskStartupMode));
	}

	private async Task SetDebugModeAsync(bool? enabled) {
		if (enabled is null || _loggingSettings.DebugMode == enabled.Value) {
			return;
		}
		_loggingSettings.DebugMode = enabled.Value;
		IsDebugModeEnabled = enabled.Value;
		Log.Information(
			"SettingsViewModel: DebugMode changed to {DebugMode}; restart required.",
			enabled.Value);
		await _applicationLifetime.RequestRestartAsync(RestartReason.DebugModeChanged);
	}

	private void SelectGameFolder() {
		string? selectedPath = _folderPicker.PickGameFolder();
		if (string.IsNullOrWhiteSpace(selectedPath)) {
			return;
		}
		if (!_gameDetectionService.ApplyManualGameFolder(_appSettings, selectedPath)) {
			Log.Warning(
				"SettingsViewModel: Game folder selection was rejected by the detection workflow.");
			ShowToast(new ToastRequest {
				Title = "Invalid Game Folder",
				Message = "Select a valid Bannerlord installation containing the standard launcher executable.",
				Severity = ToastSeverity.Error
			});
			UpdateValidation();
			return;
		}

		SynchronizeCoreState();
		ShowSteamWorkshopWarningIfNeeded();
		Log.Debug(
			"SettingsViewModel: Game folder {GameFolderPath} selected.",
			selectedPath);
	}

	private bool CanSelectGameFolder() =>
		_appSettings.GameProvider is GameProvider.NotInitialized
			or GameProvider.ManualConfiguration;

	private void SelectGameExecutable() {
		string? selectedPath = _filePicker.PickGameExecutable();
		if (string.IsNullOrWhiteSpace(selectedPath)) {
			return;
		}
		if (!GamePathValidator.ValidateGameExecutable(selectedPath)) {
			Log.Warning("SettingsViewModel: Executable validation failed.");
			ShowToast(new ToastRequest {
				Title = "Invalid Executable",
				Message = "Please select a valid Bannerlord executable.",
				Severity = ToastSeverity.Error
			});
			return;
		}
		_appSettings.GameLauncherFilePath = selectedPath;
		GameLauncherFilePath = selectedPath;
		Log.Debug(
			"SettingsViewModel: Game executable {GameLauncherFilePath} selected.",
			selectedPath);
	}

	private void SelectWorkshopFolder() {
		string? selectedPath = _folderPicker.PickWorkshopFolder();
		if (string.IsNullOrWhiteSpace(selectedPath)) {
			return;
		}
		if (!_gameDetectionService.ApplyManualSteamWorkshopFolder(
			_appSettings,
			selectedPath)) {
			Log.Warning(
				"SettingsViewModel: Workshop folder selection was rejected by the detection workflow.");
			ShowToast(new ToastRequest {
				Title = "Invalid Workshop Folder",
				Message = "Select a valid Bannerlord Workshop folder for the current Steam installation.",
				Severity = ToastSeverity.Error
			});
			return;
		}
		SteamWorkshopFolderPath = _appSettings.SteamWorkshopFolderPath;
		UpdateValidation();
		Log.Debug(
			"SettingsViewModel: Workshop folder {SteamWorkshopFolderPath} selected.",
			selectedPath);
	}

	private bool CanSelectWorkshopFolder() =>
		_appSettings.GameProvider == GameProvider.Steam
		&& !IsWorkshopFolderValid;

	private void SelectBlseExecutable() {
		string? selectedPath = _filePicker.PickBlseExecutable();
		if (string.IsNullOrWhiteSpace(selectedPath)) {
			return;
		}
		if (!GamePathValidator.ValidateGameExecutable(selectedPath)) {
			Log.Warning("SettingsViewModel: BLSE executable validation failed.");
			ShowToast(new ToastRequest {
				Title = "Invalid BLSE Executable",
				Message = "Please select a valid BLSE standalone executable.",
				Severity = ToastSeverity.Error
			});
			UpdateBlseValidation();
			return;
		}
		_appSettings.BLSEExePath = selectedPath;
		BLSEExePath = selectedPath;
		UpdateBlseValidation();
		Log.Information(
			"SettingsViewModel: BLSE executable set to {BLSEExePath}.",
			selectedPath);
		Log.Debug(
			"SettingsViewModel: BLSE executable {BLSEExePath} selected.",
			selectedPath);
	}

	private void RedetectGame() {
		Log.Information("SettingsViewModel: Re-detecting game installation.");
		Log.Debug(
			"SettingsViewModel: Starting game detection from game folder {GameFolderPath} and provider {GameProvider}.",
			_appSettings.GameFolderPath,
			_appSettings.GameProvider);
		GameProvider provider = _gameDetectionService.RedetectGame(_appSettings);
		SynchronizeCoreState();
		Log.Information(
			"SettingsViewModel: Manual detection completed for platform {GameProvider}.",
			_appSettings.GameProvider);
		Log.Debug(
			"SettingsViewModel: Detection results: game folder {GameFolderPath}, launcher {GameLauncherFilePath}, Workshop folder {SteamWorkshopFolderPath}, BLSE executable {BLSEExePath}, provider {GameProvider}.",
			_appSettings.GameFolderPath,
			_appSettings.GameLauncherFilePath,
			_appSettings.SteamWorkshopFolderPath,
			_appSettings.BLSEExePath,
			_appSettings.GameProvider);
		bool detected = provider != GameProvider.ManualConfiguration
			&& GamePathValidator.ValidateGameFolder(_appSettings.GameFolderPath);
		bool steamWithoutWorkshop = provider == GameProvider.Steam
			&& string.IsNullOrWhiteSpace(_appSettings.SteamWorkshopFolderPath);
		ShowToast(new ToastRequest {
			Title = steamWithoutWorkshop
				? T.Toast_SteamWorkshopNotFoundTitle
				: detected ? "Game Detected" : "Detection Failed",
			Message = steamWithoutWorkshop
				? T.Toast_SteamWorkshopNotFoundMessage
				: detected
					? $"Bannerlord found via {_appSettings.GameProvider}."
					: "Could not auto-detect Bannerlord. Please select the game folder manually.",
			Severity = steamWithoutWorkshop || !detected
				? ToastSeverity.Warning
				: ToastSeverity.Success
		});
	}

	private async Task UnblockDllsAsync() {
		if (!GamePathValidator.ValidateGameFolder(_appSettings.GameFolderPath)) {
			Log.Warning(
				"SettingsViewModel: Cannot unblock DLLs because the game folder is invalid.");
			ShowToast(new ToastRequest {
				Title = "Cannot Unblock DLLs",
				Message = "Invalid game folder.",
				Severity = ToastSeverity.Warning
			});
			return;
		}

		UnblockLastRunText = "Last run:  Running...";
		UnblockResultText = "Result:  —";
		try {
			UnblockResult result = await _dllUnblocker.UnblockAllAsync(
				_appSettings.ModulesDirectoryPath);
			string timestamp = DateTime.Now.ToString("yyyy-MM-dd  h:mm tt");
			string summary = result.ToSummaryString();
			_appSettings.LastUnblockRunDate = timestamp;
			_appSettings.LastUnblockRunResult = summary;
			await _uiDispatcher.InvokeAsync(() => {
				UnblockLastRunText = $"Last run:  {timestamp}";
				UnblockResultText = $"Result:  {summary}";
				ShowToast(new ToastRequest {
					Title = "DLL Unblock Complete",
					Message = summary,
					Severity = ToastSeverity.Success
				});
			});
			Log.Debug(
				"SettingsViewModel: DLL unblock summary: {UnblockSummary}",
				summary);
		} catch (Exception ex) {
			Log.Error(ex, "SettingsViewModel: Unblock operation failed.");
			Log.Debug(ex, "SettingsViewModel: Unblock failed.");
			await _uiDispatcher.InvokeAsync(() => {
				UnblockResultText = $"Result:  Error - {ex.Message}";
				ShowToast(new ToastRequest {
					Title = "Unblock Failed",
					Message = ex.Message,
					Severity = ToastSeverity.Error
				});
			});
		}
	}

	private void ClearModCache() {
		bool success = _modPipeline.ClearCache();
		ShowToast(new ToastRequest {
			Title = success ? "Cache Cleared" : "Cache Clear Failed",
			Message = success
				? "Mod cache has been cleared. A fresh scan will run on next launch."
				: "Could not clear the mod cache. Check logs for details.",
			Severity = success ? ToastSeverity.Success : ToastSeverity.Error
		});
		Log.Debug(
			"SettingsViewModel: Clear mod cache completed with success {Success}.",
			success);
	}

	private void SynchronizeCoreState() {
		GameFolderPath = _appSettings.GameFolderPath;
		GameLauncherFilePath = _appSettings.GameLauncherFilePath;
		SteamWorkshopFolderPath = _appSettings.SteamWorkshopFolderPath;
		BLSEExePath = _appSettings.BLSEExePath;
		UpdatePathVisibility();
		UpdateValidation();
	}

	private void UpdatePathVisibility() {
		switch (_appSettings.GameProvider) {
			case GameProvider.Steam:
				ShowGameExecutablePath = false;
				ShowWorkshopPath = true;
				break;
			case GameProvider.EpicGames:
			case GameProvider.StandAlone:
				ShowGameExecutablePath = true;
				ShowWorkshopPath = false;
				break;
			default:
				ShowGameExecutablePath = true;
				ShowWorkshopPath = true;
				break;
		}
		Log.Debug(
			"SettingsViewModel: Updated path section visibility for game provider {GameProvider}.",
			_appSettings.GameProvider);
	}

	private void UpdateValidation() {
		IsGameFolderValid =
			GamePathValidator.ValidateGameFolder(_appSettings.GameFolderPath);
		IsWorkshopFolderValid =
			_appSettings.GameProvider == GameProvider.Steam
			&& GamePathValidator.ValidateWorkshopFolder(
				_appSettings.SteamWorkshopFolderPath);
		GameFolderValidationText = IsGameFolderValid
			? "✓ Valid Bannerlord installation detected."
			: "✗ Invalid Bannerlord installation.";
		WorkshopFolderValidationText = IsWorkshopFolderValid
			? T.Settings_WorkshopFolderValid
			: T.Settings_WorkshopFolderInvalid;
		SelectGameFolderCommand.NotifyCanExecuteChanged();
		SelectWorkshopFolderCommand.NotifyCanExecuteChanged();
		Log.Debug(
			IsGameFolderValid
				? "SettingsViewModel: Game folder {GameFolderPath} is valid."
				: "SettingsViewModel: Game folder {GameFolderPath} is invalid.",
			_appSettings.GameFolderPath);
		UpdateBlseValidation();
	}

	private void UpdateBlseValidation() {
		if (!string.IsNullOrWhiteSpace(_appSettings.BLSEExePath)
			&& GamePathValidator.ValidateGameExecutable(_appSettings.BLSEExePath)) {
			IsBlsePathValidOrOptional = true;
			BlseValidationText = "✓ BLSE executable found.";
			Log.Debug(
				"SettingsViewModel: BLSE executable path {BLSEExePath} is valid.",
				_appSettings.BLSEExePath);
		} else if (string.IsNullOrWhiteSpace(_appSettings.BLSEExePath)) {
			IsBlsePathValidOrOptional = true;
			BlseValidationText =
				"Not configured — optional. Select if you use BLSE mods.";
			Log.Debug("SettingsViewModel: BLSE path is not configured.");
		} else {
			IsBlsePathValidOrOptional = false;
			BlseValidationText = "✗ The selected BLSE executable was not found.";
			Log.Debug(
				"SettingsViewModel: BLSE executable path {BLSEExePath} is invalid.",
				_appSettings.BLSEExePath);
		}
	}

	private void LoadUnblockStatus() {
		UnblockLastRunText = string.IsNullOrWhiteSpace(_appSettings.LastUnblockRunDate)
			? "Last run:  Never"
			: $"Last run:  {_appSettings.LastUnblockRunDate}";
		UnblockResultText = string.IsNullOrWhiteSpace(_appSettings.LastUnblockRunResult)
			? "Result:  —"
			: $"Result:  {_appSettings.LastUnblockRunResult}";
	}

	private void ShowSteamWorkshopWarningIfNeeded() {
		if (_appSettings.GameProvider != GameProvider.Steam
			|| GamePathValidator.ValidateWorkshopFolder(
				_appSettings.SteamWorkshopFolderPath)) {
			return;
		}
		ShowToast(new ToastRequest {
			Title = T.Toast_SteamWorkshopNotFoundTitle,
			Message = T.Toast_SteamWorkshopNotFoundMessage,
			Severity = ToastSeverity.Warning
		});
	}

	private void OpenFolder(string folderPath, string displayName) {
		try {
			if (_shellLauncher.OpenFolder(folderPath)) {
				Log.Debug(
					"SettingsViewModel: Opened {FolderKind} folder {FolderPath}.",
					displayName,
					folderPath);
				return;
			}

			Log.Warning(
				"SettingsViewModel: Failed to open {FolderKind} folder {FolderPath}.",
				displayName,
				folderPath);
			ShowToast(new ToastRequest {
				Title = "Open Folder Failed",
				Message = $"Could not open the {displayName} folder.",
				Severity = ToastSeverity.Error
			});
		} catch (Exception ex) {
			Log.Error(
				ex,
				"SettingsViewModel: Folder launch failed for {FolderKind} folder {FolderPath}.",
				displayName,
				folderPath);
			ShowToast(new ToastRequest {
				Title = "Open Folder Failed",
				Message = $"Could not open the {displayName} folder: {ex.Message}",
				Severity = ToastSeverity.Error
			});
		}
	}

	private void ShowToast(ToastRequest request) => _notifications.Open(request);

	private static string BuildVersionText() {
		Version? version = Assembly.GetExecutingAssembly().GetName().Version;
		return version is not null
			? $"Version {version.Major}.{version.Minor}.{version.Build}"
			: "Version 1.0.0";
	}
}
