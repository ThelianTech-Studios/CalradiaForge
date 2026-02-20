namespace CalradiaForge.Core.Infra.Paths {
	using CalradiaForge.Core.Infra.Logging;

	using Microsoft.Win32;

	/// <summary>
	/// Provides registry-based detection for the Epic Games launcher installation.
	/// </summary>
	internal static class EpicDetector {
		private static readonly Logger _logger = Logger.Instance;
		/// <summary>
		/// Determines whether the Epic Games launcher appears to be installed.
		/// </summary>
		public static bool IsEpicGameInstalled() {
			var installLocation = Registry.GetValue(
								@"HKEY_LOCAL_MACHINE\SOFTWARE\EpicGames\EpicGamesLauncher",
								"InstallLocation",
								null) as string;
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("EpicDetector: Read InstallLocation.", new { InstallLocation = installLocation ?? "<null>" });
			}
			if (IsValidEpicPath(installLocation))
				return true;


			var appDataPath = Registry.GetValue(
								@"HKEY_CURRENT_USER\SOFTWARE\Epic Games\EpicGamesLauncher",
								"AppDataPath",
								null) as string;

			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("EpicDetector: Read AppDataPath.", new { AppDataPath = appDataPath ?? "<null>" });
			}
			if (string.IsNullOrWhiteSpace(appDataPath))
				return false;
			string inferredPath = Path.Combine(appDataPath, "..", "..");
			inferredPath = Path.GetFullPath(inferredPath);

			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("EpicDetector: Inferred Epic path.", new { InferredPath = inferredPath });
			}
			return IsValidEpicPath(inferredPath);
		}
		/// <summary>
		/// Validates an Epic Games launcher path by checking required files.
		/// </summary>
		private static bool IsValidEpicPath(string? path) {
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("EpicDetector: Validating Epic path.", new { Path = path ?? "<null>" });
			}
			if (string.IsNullOrWhiteSpace(path))
				return false;

			string launcherExe = Path.Combine(path, "EpicGamesLauncher.exe");
			bool exists = Directory.Exists(path) && File.Exists(launcherExe);
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("EpicDetector: Epic path validation result.", new { Path = path, LauncherExe = launcherExe, Exists = exists });
			}
			return exists;
		}
	}
}
