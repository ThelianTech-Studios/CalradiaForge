namespace CalradiaForge.UI {
	using System.Windows;
	using System.Windows.Threading;

	using CalradiaForge.Core.Infra.Config;
	using CalradiaForge.Core.Infra.Eula;
	using CalradiaForge.Core.Infra.GamePlatform;
	using CalradiaForge.Core.Infra.GamePlatform.Steam;
	using CalradiaForge.Core.Infra.Launch;
	using CalradiaForge.Core.Infra.Localization;
	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Infra.Modpacks;
	using CalradiaForge.Core.Infra.Mods;
	using CalradiaForge.Core.Infra.Paths;

	using CalradiaForge.UI.Toasts;
	using CalradiaForge.UI.Views;

	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		private static Logger _logger = Logger.Instance;
		public static AppConfigSettings AppSettingsInstance { get; private set; } = null!;
		public static ModPipelineManager ModPipelineManager { get; private set; } = null!;
		public static ModInstaller ModInstaller { get; private set; } = null!;
		public static ModpackService ModpackService { get; private set; } = null!;
		public static GameLauncher GameLauncher { get; private set; } = null!;
		public static StartupNotificationQueue StartupNotifications { get; private set; } = null!;
		public static GameDetectionService GameDetectionService { get; private set; } = null!;
		public static ToastService Toasts { get; private set; } = null!;
		public static TranslationService Translator { get; private set; } = null!;
		/// <summary>
		/// Initializes the WPF application instance.
		/// </summary>
		public App() {
			InitializeComponent();
		}
		/// <summary>
		/// Handles application startup and initializes services.
		/// </summary>
		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);
			_logger.Info("Application Starting");
			SetupExceptionHandling();
			InitializeConfiguration();
			InitializeTranslatorService();
			if (!EulaAcceptance()) {
				_logger.Info("App: EULA declined. Shutting down.");
				Shutdown();
				return;
			}
			InitializeGameDetectionService();
			InitializeModServices();
			InitializeModpackServices();
			InitializeLauncherService();
			InitializeToastService();


			// Apply saved debug mode to logger verbosity
			if (AppSettingsInstance.DebugMode) {
				_logger.MinimumLevel = Logger.LogLevel.Debug;
				_logger.Info("App: Debug mode enabled — logging verbose diagnostic messages.");
			}
		}

		/// <summary>
		/// Handles application shutdown and performs cleanup.
		/// </summary>
		protected override void OnExit(ExitEventArgs e) {
			if (AppSettingsInstance.DebugMode) {
				_logger.Debug("App: OnExit begin.");
			}
			try {
				if (ModPipelineManager is not null) {
					ModPipelineManager.StopAcceptingNewWork();
					ModPipelineManager.RequestCancellation();
				}
				if (ModpackService?.CurrentLoadOrderEntries is { Count: > 0 } entries) {
					ModpackService.SaveLastUsed(entries);
					_logger.Info("App: Saved last-used load order on exit.");
				}
			} catch (Exception ex) {
				_logger.Error(ex, "App: Failed to clean up on exit.");
			}
			base.OnExit(e);
		}
		/// <summary>
		/// Loads configuration from disk and initializes settings.
		/// </summary>
		private void InitializeConfiguration() {
			var appConfig = new AppConfig(AppPaths.ConfigFilePath);
			appConfig.Load();
			AppSettingsInstance = new AppConfigSettings(appConfig);
			if (AppSettingsInstance.DebugMode) {
				_logger.MinimumLevel = Logger.LogLevel.Debug;
				_logger.Info("App: Debug mode enabled — logging verbose diagnostic messages.");
			}
			if (AppSettingsInstance.DebugMode) {
				_logger.Debug("App: Initializing configuration.", new { AppPaths.ConfigFilePath });
			}
			if (AppSettingsInstance.DebugMode) {
				_logger.Debug("App: Configuration initialized.", new {
					AppSettingsInstance.DebugMode,
					AppSettingsInstance.Language,
					AppSettingsInstance.GameProvider
				});
			}
		}
		/// <summary>
		/// Shows the first-run language selection window before translator initialization.
		/// The selected language is persisted to config and then used by Translator.Initialize().
		/// </summary>
		private void InitializeLangSelection(TranslationManager translationManager) {
			var availableLanguages = translationManager.LoadManifest();
			if (availableLanguages.Count == 0) {
				_logger.Warning("App: No languages found in manifest; skipping language selection window.");
				return;
			}

			// Prevent app shutdown when the modal language window closes.
			ShutdownMode previousMode = ShutdownMode;
			ShutdownMode = ShutdownMode.OnExplicitShutdown;

			try {
				LanguageSelectWindow languageWindow = new(availableLanguages, AppSettingsInstance.Language);
				bool? result = languageWindow.ShowDialog();

				if (result == true && !string.IsNullOrWhiteSpace(languageWindow.SelectedLanguageCode)) {
					AppSettingsInstance.Language = languageWindow.SelectedLanguageCode;
					_logger.Info($"App: First-run language selected '{languageWindow.SelectedLanguageCode}'.");
				} else {
					_logger.Info("App: Language selection window closed without confirmation. Keeping configured default language.");
				}
			} finally {
				ShutdownMode = previousMode;
			}
		}
		/// <summary>
		/// Checks EULA acceptance state and shows the EULA window if the user has not yet accepted.
		/// Returns <c>true</c> when the user has accepted (or was already accepted).
		/// Returns <c>false</c> when the user declined — the caller is responsible for shutting down.
		/// </summary>
		private bool EulaAcceptance() {
			EulaService eulaService = new();
			eulaService.Load();

			if (!eulaService.RequiresAcceptance(AppSettingsInstance)) {
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("App: EULA already accepted. Skipping prompt.");
				}
				return true;
			}

			// Temporarily prevent shutdown when the EULA dialog closes.
			// WPF auto-assigns the first window as MainWindow, so closing
			// the EulaWindow would trigger OnMainWindowClose before the
			// real MainWindow (from StartupUri) is ever created.
			ShutdownMode previousMode = ShutdownMode;
			ShutdownMode = ShutdownMode.OnExplicitShutdown;

			EulaWindow eulaWindow = new(eulaService.EulaText);
			bool? result = eulaWindow.ShowDialog();

			// Restore the normal shutdown mode so MainWindow controls lifetime.
			ShutdownMode = previousMode;

			if (result != true || !eulaWindow.Accepted) {
				return false;
			}

			eulaService.RecordAcceptance(AppSettingsInstance);
			_logger.Info("App: EULA accepted.");
			return true;
		}
		/// <summary>
		/// Initializes the game-detection service and its startup notification queue.
		/// </summary>
		private void InitializeGameDetectionService() {
			StartupNotifications = new StartupNotificationQueue();// This should be abstracted out into its own Service class if we want to reuse it in other startup scenarios like ModServices we queue up notifications for results of the Mods loaded from cache, etc.
			GamePlatformDetectionResolver resolver = new(
				new WindowsSteamClientRootProvider(),
				new SteamInstallationResolver());
			GameDetectionService = new GameDetectionService(resolver, StartupNotifications);
			GameDetectionService.InitializeForStartup(AppSettingsInstance);
		}
		/// <summary>
		/// Initializes the bounded mod pipeline and loads its accepted cache snapshot.
		/// </summary>
		private void InitializeModServices() {
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("App: Initializing mod services.", new { AppPaths.ModsCurrentFilePath, AppPaths.ModsBackupFilePath });
			}
			ModsData modsData = new(AppPaths.ModsCurrentFilePath, AppPaths.ModsBackupFilePath);
			ModInstaller = new ModInstaller(AppSettingsInstance);
			ModPipelineManager = new ModPipelineManager(AppSettingsInstance, modsData, ModInstaller);
			var snapshot = ModPipelineManager.LoadAcceptedCache();
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("App: Accepted mod cache loaded.", new { snapshot.Version, Count = snapshot.Modules.Count });
			}
		}
		/// <summary>
		/// Initializes the modpack service and loads modpack data.
		/// </summary>
		private void InitializeModpackServices() {
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("App: Initializing modpack services.", new { AppPaths.ModpacksDirectory });
			}
			var modpackData = new ModpackData(AppPaths.ModpacksDirectory, AppPaths.LastUsedModsFilePath);
			ModpackService = new ModpackService(modpackData);
			ModpackService.LoadAll();
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("App: Modpacks loaded.", new { Count = ModpackService.AllModpacks.Count });
			}
		}
		/// <summary>
		/// Initializes the game launcher service.
		/// </summary>
		private void InitializeLauncherService() {
			GameLauncher = new GameLauncher(AppSettingsInstance);
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("App: GameLauncher initialized.");
			}
		}

		/// <summary>
		/// Initializes the toast notification service.
		/// </summary>
		private void InitializeToastService() {
			Toasts = new ToastService(Dispatcher);
		}
		/// <summary>
		/// Initializes the translation service and loads language data.
		/// </summary>
		private void InitializeTranslatorService() {
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("App: Initializing translator service.", new { AppPaths.LanguagesDirectory });
			}
			var loader = new TranslationManager(AppPaths.LanguagesDirectory, AppPaths.LanguagesManifestFilePath, AppPaths.DefaultLanguageFilePath);
			Translator = new TranslationService(loader, AppSettingsInstance);
			if (!AppSettingsInstance.EulaAccepted) {
				InitializeLangSelection(loader);
			}
			Translator.Initialize();
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("App: Translator initialized.", new {
					Count = Translator.AvailableLanguages.Count,
					Translator.ActiveLanguageCode
				});
			}
		}
		/// <summary>
		/// Wires global exception handlers for application-level errors.
		/// </summary>
		private void SetupExceptionHandling() {
			AppDomain.CurrentDomain.UnhandledException += delegate (object s, UnhandledExceptionEventArgs e) {
				LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");
			};
			base.DispatcherUnhandledException += delegate (object s, DispatcherUnhandledExceptionEventArgs e) {
				LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
				e.Handled = true;
			};
			TaskScheduler.UnobservedTaskException += delegate (object? s, UnobservedTaskExceptionEventArgs e) {
				LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
				e.SetObserved();
			};
		}
		/// <summary>
		/// Logs unhandled exceptions with context information.
		/// </summary>
		public void LogUnhandledException(Exception ex0, string source) {
			string text = $"Unhandled exception from {source}";
			try {
				var name = System.Reflection.Assembly.GetExecutingAssembly().GetName();
				text += $" in {name.Name} v{name.Version}";

			} catch (Exception ex) {
				_logger.Error(ex, "Exception in LogUnhandledException");
			} finally {
				_logger.Error(ex0, text);
			}
		}
	}
}
