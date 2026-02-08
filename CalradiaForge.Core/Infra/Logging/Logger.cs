namespace CalradiaForge.Core.Infra.Logging {
	using System;
	using System.Diagnostics;
	public sealed class Logger {
		public enum LogLevel {
			Debug,
			Info,
			Warning,
			Error
		}

		private static readonly Lazy<Logger> _instance = new(() => new Logger());
		public static Logger Instance => _instance.Value;

		private readonly object _lock = new();
		public string LogFolder { get; private set; }

		public bool MirrorToDebug { get; set; } = true;

		private Logger() {
			LogFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Logs");
			Directory.CreateDirectory(LogFolder);
		}
		public void Log(string message,LogLevel level) {
			string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
			string logEntry = $"[{timestamp}] [{level}] {message}";
			string fileName = Path.Combine(LogFolder,$"CalradiaForge_log_{DateTime.Now:yyyy-MM-dd}.txt");

			lock (_lock) {
				File.AppendAllText(fileName,logEntry + Environment.NewLine);
			}
			if (MirrorToDebug) {
				Debug.WriteLine(logEntry);
			}
		}

		public void Info(string message) => Log(message,LogLevel.Info);
		public void Warning(string message) => Log(message,LogLevel.Warning);
		public void Error(string message) => Log(message,LogLevel.Error);
		public void Error(Exception ex,params string[] additionalInfo) {
			string errorMessage = ex.ToString();
			if (additionalInfo.Length > 0) {
				errorMessage += Environment.NewLine + string.Join(Environment.NewLine,additionalInfo);
			}
			Log(errorMessage,LogLevel.Error);
		}
	}
}
