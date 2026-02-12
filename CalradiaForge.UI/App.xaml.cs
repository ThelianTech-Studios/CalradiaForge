namespace CalradiaForge.UI {
	using System.Windows;
	using System.Windows.Threading;

	using CalradiaForge.Core.Infra.Config;
	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Infra.Paths;

	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		private static Logger _logger = Logger.Instance;
		public static AppConfigSettings AppConfig { get; private set; } = null!;
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
			var appConfig = new AppConfig(AppPaths.ConfigFilePath);
			appConfig.Load();
			AppConfig = new AppConfigSettings(appConfig);
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
