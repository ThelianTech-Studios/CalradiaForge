namespace CalradiaForge.Core.Infra.Logging;

/// <summary>Provides the bounded local log retention policy used by the Serilog foundation.</summary>
public static class LogRetentionPolicy {
	private const int FallbackRetentionDays = 7;

	/// <summary>Removes old CalradiaForge log files while leaving recent logs intact.</summary>
	public static void Cleanup(string logDirectory, int maxAgeSetting) {
		int maxAgeDays = maxAgeSetting > 0 ? maxAgeSetting : FallbackRetentionDays;

		if (!Directory.Exists(logDirectory)) {
			return;
		}
		DateTime cutoff = DateTime.UtcNow.AddDays(-maxAgeDays);
		foreach (string path in Directory.EnumerateFiles(logDirectory, "CalradiaForge*.log")) {
			try {
				if (File.GetLastWriteTimeUtc(path) < cutoff) {
					File.Delete(path);
				}
			} catch (IOException) { } catch (UnauthorizedAccessException) { }
		}
	}
}
