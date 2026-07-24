namespace CalradiaForge.Core.Infra.GamePlatform.Epic {
	using Microsoft.Win32;

	using Serilog;

	/// <summary>
	/// Provides registry-based detection for the Epic Games launcher installation.
	/// </summary>
	internal static class EpicDetector {
		/// <summary>
		/// Determines whether the Epic Games launcher appears to be installed.
		/// </summary>
		public static bool IsEpicGameInstalled() {
			var installLocation = Registry.GetValue(
								@"HKEY_LOCAL_MACHINE\SOFTWARE\EpicGames\EpicGamesLauncher",
								"InstallLocation",
								null) as string;
			Log.Debug(
				"EpicDetector: Read InstallLocation. InstallLocation={InstallLocation}",
				installLocation ?? "<null>");
			if (IsValidEpicPath(installLocation))
				return true;


			var appDataPath = Registry.GetValue(
								@"HKEY_CURRENT_USER\SOFTWARE\Epic Games\EpicGamesLauncher",
								"AppDataPath",
								null) as string;

			Log.Debug(
				"EpicDetector: Read AppDataPath. AppDataPath={AppDataPath}",
				appDataPath ?? "<null>");
			if (string.IsNullOrWhiteSpace(appDataPath))
				return false;
			string inferredPath = Path.Combine(appDataPath, "..", "..");
			inferredPath = Path.GetFullPath(inferredPath);

			Log.Debug("EpicDetector: Inferred Epic path. InferredPath={InferredPath}", inferredPath);
			return IsValidEpicPath(inferredPath);
		}
		/// <summary>
		/// Validates an Epic Games launcher path by checking required files.
		/// </summary>
		private static bool IsValidEpicPath(string? path) {
			Log.Debug("EpicDetector: Validating Epic path. Path={Path}", path ?? "<null>");
			if (string.IsNullOrWhiteSpace(path))
				return false;

			string launcherExe = Path.Combine(path, "EpicGamesLauncher.exe");
			bool exists = Directory.Exists(path) && File.Exists(launcherExe);
			Log.Debug(
				"EpicDetector: Epic path validation result. Path={Path} LauncherExe={LauncherExe} Exists={Exists}",
				path,
				launcherExe,
				exists);
			return exists;
		}
	}
}
