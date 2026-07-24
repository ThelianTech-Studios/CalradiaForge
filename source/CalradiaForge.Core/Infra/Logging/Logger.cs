namespace CalradiaForge.Core.Infra.Logging {
	using CalradiaForge.Core.Infra.Paths;

	using Newtonsoft.Json;

	/// <summary>
	/// Provides thread-safe logging for application diagnostics and user-facing events.
	/// This legacy compatibility path remains in place for existing Core and UI call sites for the time being.
	/// Migration and retirement are deferred to Phase 6.C after provider-owned Serilog behavior is verified.
	/// </summary>
	[Obsolete("This class is now marked as Legacy and will be removed in a future version after the refactor to Serilog is in place.")]
	public sealed class Logger {
		/// <summary>
		/// Represents the severity level of a log entry.
		/// </summary>
		public enum LogLevel {
			Debug,
			Info,
			Warning,
			Error
		}

		private static readonly Lazy<Logger> _instance = new(() => new Logger());
		/// <summary>
		/// Gets the singleton logger instance.
		/// </summary>
		public static Logger Instance => _instance.Value;

		private readonly object _lock = new();
		private readonly string _sessionLogFilePath;
		private readonly string _sessionId;
		/// <summary>
		/// Gets the folder where log files are stored.
		/// </summary>
		public string LogFolder { get; private set; }
		/// <summary>
		/// Gets or sets whether log entries are mirrored to the debugger output.
		/// </summary>
		public bool MirrorToDebug { get; set; } = true;

		/// <summary>
		/// The minimum log level that will be written to the log file.
		/// Messages below this level are silently discarded.
		/// Set to <see cref="LogLevel.Debug"/> to capture everything,
		/// or <see cref="LogLevel.Info"/> to suppress debug noise in release.
		/// Controlled by the DebugMode toggle in Settings.
		/// </summary>
		public LogLevel MinimumLevel { get; set; } = LogLevel.Info;

		/// <summary>
		/// Initializes a new logger instance and prepares the session log file.
		/// </summary>
		private Logger() {
			LogFolder = AppPaths.LogsDirectory;
			_sessionId = DateTime.Now.ToString("yyyy-MM-dd HH-mm");
			_sessionLogFilePath = Path.Combine(LogFolder, $"CalradiaForge_log_{_sessionId}.log");

			CleanupOldLegacyLogs(maxAgeDays: 14);
		}

		#region Public API

		/// <summary>
		/// Logs a verbose diagnostic message.
		/// </summary>
		public void Debug(string message) => Log(message, LogLevel.Debug);
		/// <summary>
		/// Logs a verbose diagnostic message with structured data.
		/// </summary>
		public void Debug(string message, object? data) {
			string? serialized = SerializeDebugData(data);
			string finalMessage = serialized is null ? message : $"{message} | data: {serialized}";
			Log(finalMessage, LogLevel.Debug);
		}
		/// <summary>
		/// Logs a verbose diagnostic message with exception details.
		/// </summary>
		public void Debug(string message, Exception ex) {
			Log(message + Environment.NewLine + ex, LogLevel.Debug);
		}
		/// <summary>
		/// Logs a verbose diagnostic message with structured data and exception details.
		/// </summary>
		public void Debug(string message, object? data, Exception ex) {
			string? serialized = SerializeDebugData(data);
			string finalMessage = serialized is null ? message : $"{message} | data: {serialized}";
			Log(finalMessage + Environment.NewLine + ex, LogLevel.Debug);
		}
		/// <summary>
		/// Logs an error message.
		/// </summary>
		public void Error(string message) => Log(message, LogLevel.Error);
		/// <summary>
		/// Logs an error message with exception details.
		/// </summary>
		public void Error(string message, Exception ex) => Log(message + Environment.NewLine + ex, LogLevel.Error);
		/// <summary>
		/// Logs an exception and optional additional diagnostic details.
		/// </summary>
		public void Error(Exception ex, params string[] additionalInfo) {
			string errorMessage = ex.ToString();
			if (additionalInfo.Length > 0) {
				errorMessage += Environment.NewLine + string.Join(Environment.NewLine, additionalInfo);
			}
			Log(errorMessage, LogLevel.Error);
		}
		/// <summary>
		/// Logs an informational message.
		/// </summary>
		public void Info(string message) => Log(message, LogLevel.Info);
		/// <summary>
		/// Writes a log entry at the specified level.
		/// </summary>
		public void Log(string message, LogLevel level) {
			if (level < MinimumLevel) {
				return;
			}
			string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
			string logEntry = $"[{timestamp}] [{level}] {message}";
			try {
				lock (_lock) {
					File.AppendAllText(_sessionLogFilePath, logEntry + Environment.NewLine);
				}
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine($"[Logger] Failed to write log: {ex.Message}");
			}
			if (MirrorToDebug) {
				System.Diagnostics.Debug.WriteLine(logEntry);
			}
		}
		/// <summary>
		/// Logs a warning message.
		/// </summary>
		public void Warning(string message) => Log(message, LogLevel.Warning);

		#endregion

		#region Private Helpers

		/// <summary>
		/// Deletes log files older than the specified retention period.
		/// </summary>
		private void CleanupOldLegacyLogs(int maxAgeDays) {
			try {
				DateTime cutoff = DateTime.Now.AddDays(-maxAgeDays);
				foreach (string file in Directory.GetFiles(LogFolder, "CalradiaForge_log_*.log")) {
					if (File.GetCreationTime(file) < cutoff) {
						File.Delete(file);
					}
				}
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine($"[Logger] Failed to cleanup logs: {ex.Message}");
			}
		}
		/// <summary>
		/// Serializes debug payloads to a compact JSON string.
		/// </summary>
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
