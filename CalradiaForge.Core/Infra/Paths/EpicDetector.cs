namespace CalradiaForge.Core.Infra.Paths
	{
	using CalradiaForge.Core.Infra.Logging;

	using Microsoft.Win32;

	internal static class EpicDetector
		{
		private static readonly Logger _logger = Logger.Instance;
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
			string inferredPath = Path.Combine(appDataPath,"..","..");
			inferredPath=Path.GetFullPath(inferredPath);

			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("EpicDetector: Inferred Epic path.", new { InferredPath = inferredPath });
			}
			return IsValidEpicPath(inferredPath);
			}
		private static bool IsValidEpicPath(string? path) {
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("EpicDetector: Validating Epic path.", new { Path = path ?? "<null>" });
			}
			if (string.IsNullOrWhiteSpace(path))
				return false;

			string launcherExe = Path.Combine(path,"EpicGamesLauncher.exe");
			bool exists = Directory.Exists(path)&&File.Exists(launcherExe);
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("EpicDetector: Epic path validation result.", new { Path = path, LauncherExe = launcherExe, Exists = exists });
			}
			return exists;
			}
		}
	}
