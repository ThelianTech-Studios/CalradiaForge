namespace CalradiaForge.UI {
	using System.Windows;
	using System.Windows.Threading;

	using CalradiaForge.Core.Infra.Config;
	using CalradiaForge.Core.Infra.Eula;
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
		public static AppConfigSettings AppConfig { get; private set; } = null!;
		public static ModService ModService { get; private set; } = null!;
		public static ModInstaller ModInstaller { get; private set; } = null!;
		public static ModpackService ModpackService { get; private set; } = null!;
		public static GameLauncher GameLauncher { get; private set; } = null!;
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
			InitializeModServices();
			InitializeModpackServices();
			InitializeLauncherService();
			InitializeToastService();


			// Apply saved debug mode to logger verbosity
			if (AppConfig.DebugMode) {
				_logger.MinimumLevel = Logger.LogLevel.Debug;
				_logger.Info("App: Debug mode enabled — logging verbose diagnostic messages.");
			}
		}

		/// <summary>
		/// Handles application shutdown and performs cleanup.
		/// </summary>
		protected override void OnExit(ExitEventArgs e) {
			if (AppConfig.DebugMode) {
				_logger.Debug("App: OnExit begin.");
			}
			try {
				if (ModInstaller?.IsInstalling == true) {
					_logger.Info("App: Cancelling in-progress mod installation on exit.");
					ModInstaller.CancelInstall();
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
			AppConfig = new AppConfigSettings(appConfig);
			if (AppConfig.DebugMode) {
				_logger.MinimumLevel = Logger.LogLevel.Debug;
				_logger.Info("App: Debug mode enabled — logging verbose diagnostic messages.");
			}
			if (AppConfig.DebugMode) {
				_logger.Debug("App: Initializing configuration.", new { AppPaths.ConfigFilePath });
			}
			if ((AppConfig.GameProvider == GameProvider.NotInitialized) || string.IsNullOrWhiteSpace(AppConfig.GameFolderPath)) {
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("App: Game paths not initialized. Running auto-detection.");
				}
				GamePathsHelper.TryAutoDetectGameFolder(AppConfig);
			}

			if (AppConfig.DebugMode) {
				_logger.Debug("App: Configuration initialized.", new {
					AppConfig.DebugMode,
					AppConfig.Language,
					AppConfig.GameProvider
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
				LanguageSelectWindow languageWindow = new(availableLanguages, AppConfig.Language);
				bool? result = languageWindow.ShowDialog();

				if (result == true && !string.IsNullOrWhiteSpace(languageWindow.SelectedLanguageCode)) {
					AppConfig.Language = languageWindow.SelectedLanguageCode;
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

			if (!eulaService.RequiresAcceptance(AppConfig)) {
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

			eulaService.RecordAcceptance(AppConfig);
			_logger.Info("App: EULA accepted.");
			return true;
		}

		/// <summary>
		/// Initializes mod services and loads cached data.
		/// </summary>
		private void InitializeModServices() {
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("App: Initializing mod services.", new { AppPaths.ModsCurrentFilePath, AppPaths.ModsBackupFilePath });
			}
			var modsData = new ModsData(AppPaths.ModsCurrentFilePath, AppPaths.ModsBackupFilePath);
			ModService = new ModService(AppConfig, modsData);
			ModService.LoadFromCache();
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("App: Mod cache loaded.", new { Count = ModService.CurrentMods.Count });
			}
			ModInstaller = new ModInstaller(AppConfig);
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
			GameLauncher = new GameLauncher(AppConfig);
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
			Translator = new TranslationService(loader, AppConfig);
			if (!AppConfig.EulaAccepted) {
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
