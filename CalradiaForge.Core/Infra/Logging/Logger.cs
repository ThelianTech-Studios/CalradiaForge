namespace CalradiaForge.Core.Infra.Logging
	{
	using System;
	using System.Diagnostics;
	using System.IO;

	using CalradiaForge.Core.Infra.Paths;

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

		private Logger() {
			LogFolder=AppPaths.LogsDirectory;
			_sessionId=DateTime.Now.ToString("yyyy-MM-dd HH-mm");
			_sessionLogFilePath=Path.Combine(LogFolder,$"CalradiaForge_log_{_sessionId}.log");

			CleanupOldLogs(maxAgeDays: 14);
			}
		public void Log(string message,LogLevel level) {
			string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
			string logEntry = $"[{timestamp}] [{level}] {message}";
			try {
				lock (_lock) {
					File.AppendAllText(_sessionLogFilePath,logEntry+Environment.NewLine);
					}
				} catch (Exception ex) {
				Debug.WriteLine($"[Logger] Failed to write log: {ex.Message}");
				}
			if (MirrorToDebug) {
				Debug.WriteLine(logEntry);
				}
			}
		public void Info(string message) => Log(message,LogLevel.Info);
		public void Warning(string message) => Log(message,LogLevel.Warning);
		public void Error(string message) => Log(message,LogLevel.Error);
		public void Error(string message,Exception ex) => Log(message+Environment.NewLine+ex,LogLevel.Error);
		public void Error(Exception ex,params string[] additionalInfo) {
			string errorMessage = ex.ToString();
			if (additionalInfo.Length>0) {
				errorMessage+=Environment.NewLine+string.Join(Environment.NewLine,additionalInfo);
				}
			Log(errorMessage,LogLevel.Error);
			}
		private void CleanupOldLogs(int maxAgeDays) {
			try {
				DateTime cutoff = DateTime.Now.AddDays(-maxAgeDays);
				foreach (string file in Directory.GetFiles(LogFolder,"CalradiaForge_*.log")) {
					if (File.GetCreationTime(file)<cutoff) {
						File.Delete(file);
						}
					}
				} catch (Exception ex) {
				Debug.WriteLine($"[Logger] Failed to write log: {ex.Message}");
				}
			}
		}
	}
