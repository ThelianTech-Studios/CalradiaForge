namespace CalradiaForge.Core.Infra.Logging;

using System.Diagnostics;

/// <summary>Prepares the active application log and applies fixed archive retention at startup.</summary>
internal class LogFileLifecycle {
	private const int ArchiveRetentionDays = 7;
	private const string ActiveLogFileName = "CalradiaForge_Latest.log";
	private readonly string _logDirectory;
	private readonly string _activeLogFilePath;
	private readonly Func<DateTime> _utcNow;

	internal LogFileLifecycle(
		string logDirectory,
		string activeLogFilePath,
		Func<DateTime>? utcNow = null) {
		ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);
		ArgumentException.ThrowIfNullOrWhiteSpace(activeLogFilePath);
		_logDirectory = logDirectory;
		_activeLogFilePath = activeLogFilePath;
		_utcNow = utcNow ?? (() => DateTime.UtcNow);
	}

	/// <summary>Archives the previous active log, cleans expired archives, and prepares a new active file.</summary>
	internal void PrepareForStartup() {
		Directory.CreateDirectory(_logDirectory);
		ArchivePreviousActiveLog();
		DeleteExpiredArchives();
		PrepareActiveLogForNewSession();
	}

	protected virtual DateTime GetLastWriteTime(string path) => File.GetLastWriteTime(path);
	protected virtual DateTime GetCreationTime(string path) => File.GetCreationTime(path);
	protected virtual void MoveFile(string sourcePath, string destinationPath) =>
		File.Move(sourcePath, destinationPath, overwrite: false);
	protected virtual void TruncateFile(string path) {
		using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.Read);
	}

	private void ArchivePreviousActiveLog() {
		if (!File.Exists(_activeLogFilePath)) {
			return;
		}

		DateTime timestamp;
		try {
			timestamp = GetLastWriteTime(_activeLogFilePath);
		} catch (Exception ex) when (IsExpectedFileFailure(ex)) {
			try {
				timestamp = GetCreationTime(_activeLogFilePath);
			} catch (Exception creationException) when (IsExpectedFileFailure(creationException)) {
				Debug.WriteLine($"[Logging] Failed to read the previous active log timestamp: {creationException.Message}");
				return;
			}
		}

		string archiveName = $"CalradiaForge_{timestamp:yyyy-MM-dd_HH-mm}.log";
		string archivePath = Path.Combine(_logDirectory, archiveName);
		try {
			MoveFile(_activeLogFilePath, archivePath);
		} catch (Exception ex) when (IsExpectedFileFailure(ex)) {
			Debug.WriteLine($"[Logging] Failed to archive the previous active log: {ex.Message}");
		}
	}

	private void DeleteExpiredArchives() {
		DateTime cutoff = _utcNow().AddDays(-ArchiveRetentionDays);
		string[] archives;
		try {
			archives = Directory.EnumerateFiles(_logDirectory, "CalradiaForge_*.log").ToArray();
		} catch (Exception ex) when (IsExpectedFileFailure(ex)) {
			Debug.WriteLine($"[Logging] Failed to enumerate archived logs: {ex.Message}");
			return;
		}

		foreach (string path in archives) {
			if (string.Equals(Path.GetFileName(path), ActiveLogFileName, StringComparison.OrdinalIgnoreCase)) {
				continue;
			}
			try {
				if (File.GetLastWriteTimeUtc(path) < cutoff) {
					File.Delete(path);
				}
			} catch (Exception ex) when (IsExpectedFileFailure(ex)) {
				Debug.WriteLine($"[Logging] Failed to delete expired archive '{path}': {ex.Message}");
			}
		}
	}

	private void PrepareActiveLogForNewSession() {
		if (!File.Exists(_activeLogFilePath)) {
			return;
		}
		try {
			TruncateFile(_activeLogFilePath);
		} catch (Exception ex) when (IsExpectedFileFailure(ex)) {
			// The Serilog file sink gets the final append-compatible open attempt.
			Debug.WriteLine($"[Logging] Failed to truncate the active log before startup: {ex.Message}");
		}
	}

	private static bool IsExpectedFileFailure(Exception exception) =>
		exception is IOException
			or UnauthorizedAccessException
			or NotSupportedException
			or System.Security.SecurityException;
}
