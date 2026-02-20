namespace CalradiaForge.UI {
	using System.Windows;
	using System.Windows.Threading;

	using CalradiaForge.Core.Infra.Config;
	using CalradiaForge.Core.Infra.Launch;
	using CalradiaForge.Core.Infra.Localization;
	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Infra.Modpacks;
	using CalradiaForge.Core.Infra.Mods;
	using CalradiaForge.Core.Infra.Paths;

	using CalradiaForge.UI.Toasts;

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
		public App() {
			InitializeComponent();
		}

		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);
			_logger.Info("Application Starting");
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("App: Debugging enabled.");
			}
			SetupExceptionHandeling();
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("App: Exception handlers wired.");
			}
			InitializeConfiguration();
			InitializeModServices();
			InitializeModpackServices();
			InitializeLauncherService();
			InitializeToastService();
			InitializeTranslatorService();

			// Apply saved debug mode to logger verbosity
			if (AppConfig.DebugMode) {
				_logger.MinimumLevel = Logger.LogLevel.Debug;
				_logger.Info("App: Debug mode enabled — logging verbose diagnostic messages.");
			}
		}

		protected override void OnExit(ExitEventArgs e) {
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("App: OnExit begin.");
			}
			try {
				if (ModInstaller?.IsInstalling==true) {
					_logger.Info("App: Cancelling in-progress mod installation on exit.");
					ModInstaller.CancelInstall();
					}
				if (ModpackService?.CurrentLoadOrderEntries is { Count:>0 } entries) {
					ModpackService.SaveLastUsed(entries);
					_logger.Info("App: Saved last-used load order on exit.");
					}
				} catch (Exception ex) {
				_logger.Error(ex,"App: Failed to clean up on exit.");
				}
			base.OnExit(e);
			}
		private void InitializeConfiguration() {
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("App: Initializing configuration.", new { AppPaths.ConfigFilePath });
			}
			var appConfig = new AppConfig(AppPaths.ConfigFilePath);
			appConfig.Load();
			AppConfig = new AppConfigSettings(appConfig);
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("App: Configuration initialized.", new {
					AppConfig.DebugMode,
					AppConfig.Language,
					AppConfig.GameProvider
				});
			}
		}

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
		private void InitializeLauncherService() {
					GameLauncher = new GameLauncher(AppConfig);
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("App: GameLauncher initialized.");
					}
		}

		private void InitializeToastService() {
			Toasts = new ToastService(Dispatcher);
		}
		private void InitializeTranslatorService() {
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("App: Initializing translator service.", new { AppPaths.LanguagesDirectory });
			}
			var loader = new TranslationManager(AppPaths.LanguagesDirectory);
			Translator = new TranslationService(loader, AppConfig);
			Translator.Initialize();
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("App: Translator initialized.", new {
					Count = Translator.AvailableLanguages.Count,
					Translator.ActiveLanguageCode
				});
			}
		}

		private void SetupExceptionHandeling() {
			AppDomain.CurrentDomain.UnhandledException += delegate (object s,UnhandledExceptionEventArgs e) {
				LogUnhadledException((Exception)e.ExceptionObject,"AppDomain.CurrentDomain.UnhandledException");
			};
			base.DispatcherUnhandledException += delegate (object s,DispatcherUnhandledExceptionEventArgs e) {
				LogUnhadledException(e.Exception,"Application.Current.DispatcherUnhandledException");
				e.Handled = true;
			};
			TaskScheduler.UnobservedTaskException += delegate (object? s, UnobservedTaskExceptionEventArgs e) {
				LogUnhadledException(e.Exception,"TaskScheduler.UnobservedTaskException");
				e.SetObserved();
			};
		}
		public void LogUnhadledException(Exception ex0, string source) {
			string text = $"Unhandled exception from {source}";
			try {
				var name = System.Reflection.Assembly.GetExecutingAssembly().GetName();
				text += $" in {name.Name} v{name.Version}";

			} catch (Exception ex) {
				_logger.Error(ex, "Exception in LogUnhandeledExceeption");
			} finally {
				_logger.Error(ex0, text);
			}
		}




	}
}
