namespace CalradiaForge.Core.Infra.Logging
	{
	using CalradiaForge.Core.Infra.Paths;
	using Newtonsoft.Json;

	public sealed class Logger
		{
		public enum LogLevel
			{
			Debug,
			Info,
			Warning,
			Error
			}

		private static readonly Lazy<Logger> _instance = new(() => new Logger());
		public static Logger Instance => _instance.Value;

		private readonly object _lock = new();
		private readonly string _sessionLogFilePath;
		private readonly string _sessionId;
		public string LogFolder { get; private set; }
		public bool MirrorToDebug { get; set; } = true;

		/// <summary>
		/// The minimum log level that will be written to the log file.
		/// Messages below this level are silently discarded.
		/// Set to <see cref="LogLevel.Debug"/> to capture everything,
		/// or <see cref="LogLevel.Info"/> to suppress debug noise in release.
		/// Controlled by the DebugMode toggle in Settings.
		/// </summary>
		public LogLevel MinimumLevel { get; set; } = LogLevel.Info;

		private Logger() {
			LogFolder=AppPaths.LogsDirectory;
			_sessionId=DateTime.Now.ToString("yyyy-MM-dd HH-mm");
			_sessionLogFilePath=Path.Combine(LogFolder,$"CalradiaForge_log_{_sessionId}.log");

			CleanupOldLogs(maxAgeDays: 14);
			}

		#region Public API

		/// <summary>
		/// Logs a verbose diagnostic message. Only written when
		/// <see cref="MinimumLevel"/> is set to <see cref="LogLevel.Debug"/>
		/// (i.e. when the user enables Debug Mode in Settings).
		/// Use for granular operational detail that would be too noisy for normal use.
		/// </summary>
		public void Debug(string message) => Log(message,LogLevel.Debug);
		public void Debug(string message, object? data) {
			string? serialized = SerializeDebugData(data);
			string finalMessage = serialized is null ? message : $"{message} | data: {serialized}";
			Log(finalMessage, LogLevel.Debug);
		}
		public void Debug(string message, Exception ex) {
			Log(message + Environment.NewLine + ex, LogLevel.Debug);
		}
		public void Debug(string message, object? data, Exception ex) {
			string? serialized = SerializeDebugData(data);
			string finalMessage = serialized is null ? message : $"{message} | data: {serialized}";
			Log(finalMessage + Environment.NewLine + ex, LogLevel.Debug);
		}
		public void Error(string message) => Log(message,LogLevel.Error);
		public void Error(string message,Exception ex) => Log(message+Environment.NewLine+ex,LogLevel.Error);
		public void Error(Exception ex,params string[] additionalInfo) {
			string errorMessage = ex.ToString();
			if (additionalInfo.Length>0) {
				errorMessage+=Environment.NewLine+string.Join(Environment.NewLine,additionalInfo);
				}
			Log(errorMessage,LogLevel.Error);
			}
		public void Info(string message) => Log(message,LogLevel.Info);
		public void Log(string message,LogLevel level) {
			if (level < MinimumLevel) {
				return;
			}
			string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
			string logEntry = $"[{timestamp}] [{level}] {message}";
			try {
				lock (_lock) {
					File.AppendAllText(_sessionLogFilePath,logEntry+Environment.NewLine);
					}
				} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine($"[Logger] Failed to write log: {ex.Message}");
				}
			if (MirrorToDebug) {
				System.Diagnostics.Debug.WriteLine(logEntry);
				}
			}
		public void Warning(string message) => Log(message,LogLevel.Warning);

		#endregion

		#region Private Helpers

		private void CleanupOldLogs(int maxAgeDays) {
			try {
				DateTime cutoff = DateTime.Now.AddDays(-maxAgeDays);
				foreach (string file in Directory.GetFiles(LogFolder,"CalradiaForge_*.log")) {
					if (File.GetCreationTime(file)<cutoff) {
						File.Delete(file);
						}
					}
				} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine($"[Logger] Failed to cleanup logs: {ex.Message}");
				}
			}
		private static string? SerializeDebugData(object? data) {
			if (data is null) {
				return null;
			}
			try {
				return JsonConvert.SerializeObject(data, Formatting.None);
			} catch {
				return null;
			}
		}

		#endregion
	}
}
