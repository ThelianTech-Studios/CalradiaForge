namespace CalradiaForge.Core.Infra.Logging;

using System.Diagnostics;
using System.Text;

using CalradiaForge.Core.Infra.Paths;

/// <summary>
/// Writes fatal startup diagnostics when the provider-owned Serilog pipeline is unavailable.
/// </summary>
public static class EmergencyStartupLogWriter {
	private const string FileName = "CalradiaForge_StartupFailure.log";

	/// <summary>
	/// Appends one fatal startup diagnostic using the primary logs directory and narrow safe fallbacks.
	/// Writer failures are reported only to debugger output and never escape this method.
	/// </summary>
	public static void TryWrite(Exception exception, string context) {
		try {
			ArgumentNullException.ThrowIfNull(exception);
			ArgumentException.ThrowIfNullOrWhiteSpace(context);
			TryWrite(
				exception,
				context,
				BuildCandidateDirectories(),
				message => Debug.WriteLine(message));
		} catch (Exception writerException) {
			Debug.WriteLine($"[EmergencyStartupLogWriter] Startup diagnostic failed: {writerException}");
		}
	}

	internal static bool TryWrite(
		Exception exception,
		string context,
		IEnumerable<string> candidateDirectories,
		Action<string>? debuggerWrite = null) {
		ArgumentNullException.ThrowIfNull(exception);
		ArgumentException.ThrowIfNullOrWhiteSpace(context);
		ArgumentNullException.ThrowIfNull(candidateDirectories);

		string entry = BuildEntry(exception, context);
		foreach (string directory in candidateDirectories
			.Where(path => !string.IsNullOrWhiteSpace(path))
			.Distinct(StringComparer.OrdinalIgnoreCase)) {
			try {
				Directory.CreateDirectory(directory);
				File.AppendAllText(Path.Combine(directory, FileName), entry, Encoding.UTF8);
				return true;
			} catch (Exception writeException) {
				TryReport(
					debuggerWrite,
					$"[EmergencyStartupLogWriter] Could not write startup diagnostic to '{directory}': {writeException.Message}");
			}
		}

		TryReport(
			debuggerWrite,
			$"[EmergencyStartupLogWriter] No startup diagnostic file could be written. {context}{Environment.NewLine}{exception}");
		return false;
	}

	private static IEnumerable<string> BuildCandidateDirectories() {
		yield return Path.Combine(AppPaths.RootDirectory, "Logs");

		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		if (!string.IsNullOrWhiteSpace(localAppData)) {
			yield return Path.Combine(localAppData, "CalradiaForge", "Logs");
		}

		string tempDirectory = Path.GetTempPath();
		if (!string.IsNullOrWhiteSpace(tempDirectory)) {
			yield return Path.Combine(tempDirectory, "CalradiaForge");
		}
	}

	private static string BuildEntry(Exception exception, string context) =>
		$"[{DateTimeOffset.UtcNow:O}] {context}{Environment.NewLine}{exception}{Environment.NewLine}";

	private static void TryReport(Action<string>? debuggerWrite, string message) {
		try {
			debuggerWrite?.Invoke(message);
		} catch {
			// Emergency diagnostics must not interfere with fatal startup shutdown.
		}
	}
}
