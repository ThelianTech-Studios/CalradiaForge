namespace CalradiaForge.Core.Infra.Paths
	{
	using Microsoft.Win32;

	internal static class EpicDetector
		{
		public static bool IsEpicGameInstalled() {
			var installLocation = Registry.GetValue(
								@"HKEY_LOCAL_MACHINE\SOFTWARE\EpicGames\EpicGamesLauncher",
								"InstallLocation",
								null) as string;
			if (IsValidEpicPath(installLocation))
				return true;


			var appDataPath = Registry.GetValue(
								@"HKEY_CURRENT_USER\SOFTWARE\Epic Games\EpicGamesLauncher",
								"AppDataPath",
								null) as string;

			if (string.IsNullOrWhiteSpace(appDataPath))
				return false;
			string inferredPath = Path.Combine(appDataPath,"..","..");
			inferredPath=Path.GetFullPath(inferredPath);

			return IsValidEpicPath(inferredPath);
			}
		private static bool IsValidEpicPath(string? path) {
			if (string.IsNullOrWhiteSpace(path))
				return false;

			string launcherExe = Path.Combine(path,"EpicGamesLauncher.exe");
			return Directory.Exists(path)&&File.Exists(launcherExe);
			}
		}
	}
