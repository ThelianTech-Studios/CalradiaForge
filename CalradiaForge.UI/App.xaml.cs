namespace CalradiaForge.UI {
	using System.IO;
	using System.Windows;
	using System.Windows.Threading;
	using System.Reflection;

	using CalradiaForge.Core.Infra.Config;
	using CalradiaForge.Core.Infra.Logging;

	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		private static Logger _logger = Logger.Instance;
		public static AppConfigSettings Config { get; private set; } = null!;
		public App() {
			InitializeComponent();
		}

		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);
			var date = DateTime.Now;
			_logger.Info("Application Starting" + date);
			SetupExceptionHandeling();
			InitializeConfiguration();
		}
		private void InitializeConfiguration() {
			var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var configFolder = Path.Combine(directoryName,"Config");
			if (!Directory.Exists(configFolder)) {
				Directory.CreateDirectory(configFolder);
			}
			var configFilePath = Path.Combine(configFolder,"config.json");
			var appConfig = new AppConfig(configFilePath);
			appConfig.Load();
			Config = new AppConfigSettings(appConfig);
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



		private void LogUnhadledException(Exception ex0, string source) {
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
