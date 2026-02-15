namespace CalradiaForge.Core.Infra.Paths
	{
	using CalradiaForge.Core.Infra.Config;

	using Microsoft.Win32;

	public static class GamePathsHelper
		{

		private const string _steamGameID = "261550";
		private const string _bannerlordFolderName = "Mount & Blade II Bannerlord";
		public static void TryAutoDetectGameFolder(AppConfigSettings config) {
			if (TryDetectSteam(config))
				return;
			if (TryDetectEpic(config))
				return;
			config.GameProvider=GameProvider.StandAlone;
			}
		#region Detection Methods

		private static bool TryDetectSteam(AppConfigSettings config) {
			try {
				string? steamPath = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam","SteamPath",null) as string;
				if (string.IsNullOrWhiteSpace(steamPath)) {
					//Implement Logger to log this information
					return false;
					} else {
					string gamePath = Path.Combine(steamPath,"steamapps","common",_bannerlordFolderName);
					if (!Directory.Exists(gamePath)) {
						//Implement Logger to log this information
						return false;
						} else {
						config.GameProvider=GameProvider.Steam;
						config.GameFolderPath=gamePath;
						config.GameLauncherFilePath=Path.Combine(gamePath,"bin","Win64_Shipping_Client","Bannerlord.exe");
						string workshopPath = Path.Combine(steamPath,"steamapps","workshop","content",_steamGameID);
						if (!Directory.Exists(workshopPath)) {
							throw new DirectoryNotFoundException($"The expected Steam Workshop folder was not found at '{workshopPath}'. Please Check your Settings");
							} else { config.SteamWorkshopFolderPath=workshopPath; }
						return true;
						}
					}
				} catch {
				//Implement Logger to log this exception
				return false;
				}
			}
		private static bool TryDetectEpic(AppConfigSettings config) {
			if (!EpicDetector.IsEpicGameInstalled()) {
				return false;
				}
			string? epicPath = EpicManifestReader.TryGetInstallLocation();
			if (string.IsNullOrWhiteSpace(epicPath)) {
				return false;
				}
			config.GameProvider=GameProvider.EpicGames;
			config.GameFolderPath=epicPath;
			config.GameLauncherFilePath=Path.Combine(epicPath,"bin","Win64_Shipping_Client","Bannerlord.exe");
			return true;
			}


		#endregion


		public static string GetModulesFolder(AppConfigSettings appConfig) {
			if (string.IsNullOrWhiteSpace(appConfig.GameFolderPath)||!Directory.Exists(appConfig.GameFolderPath)) {
				throw new DirectoryNotFoundException($"The provided path '{appConfig.GameFolderPath}' is not valid or does not exist.");
				}
			string modulesPath = Path.GetFullPath(appConfig.GameFolderPath,"Modules");
			if (!Directory.Exists(modulesPath)) {
				throw new DirectoryNotFoundException($"Modules Folder not found at folderpath: '{modulesPath}'.");
				}
			return modulesPath;
			}

		public static string? GetSteamWorkshopFolder(AppConfigSettings appConfig) {
			if (appConfig.IsGameFromSteam) {
				if (!string.IsNullOrWhiteSpace(appConfig.SteamWorkshopFolderPath)&&Directory.Exists(appConfig.SteamWorkshopFolderPath)) {
					string workshopPath = Path.GetFullPath(appConfig.SteamWorkshopFolderPath);
					if (Directory.Exists(workshopPath)) {
						return workshopPath;
						}
					throw new DirectoryNotFoundException($"The Steam Workshop folder does not exist at: '{workshopPath}'. Please Check your Settings");
					} else {
					throw new DirectoryNotFoundException($"The Steam Workshop folder path '{appConfig.SteamWorkshopFolderPath}' is not valid or does not exist. Please check your settings.");
					}
				} else {
				return null;
				}
			}

		#region Old Code - References only, not used anymore.
		//Old Way to detect steam marked as Depreacated and Obselete. Leaving here as ref to Old code for Learning purposes
		//public static void TryAutoDetectSteamWorkshopFolder(AppConfigSettings config) {
		//	try {
		//		string? steamPath = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam","SteamPath",null) as string;
		//		if (!string.IsNullOrWhiteSpace(steamPath) && Directory.Exists(steamPath)) {
		//			string workshopPath = Path.Combine(steamPath,"steamapps","workshop","content",_steamGameID);
		//			if (Directory.Exists(workshopPath)) {
		//				config.SteamWorkshopFolderPath = workshopPath;
		//				config.Save();
		//				return;
		//			} else {
		//				throw new DirectoryNotFoundException($"The expected Steam Workshop folder was not found at '{workshopPath}'.");
		//			}
		//		}
		//	} catch {
		//		throw new DirectoryNotFoundException("Unable to auto-detect the Steam Workshop folder. Please select it manually in the settings.");
		//	}
		//}
		#endregion
		}
	}
