namespace CalradiaForge.Core.Infra.Mods
	{
	using CalradiaForge.Core.Infra.Config;
	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Models;

	public sealed class ModScanner
		{
		private static readonly Logger _logger = Logger.Instance;

		public static async Task<List<ModuleModel>> ScanForModsAsync(AppConfigSettings config,CancellationToken token = default,List<ModuleModel>? allMods = null) {
			allMods??=new List<ModuleModel>();
			string modulesDirectory = config.ModulesDirectoryPath;
			if (!string.IsNullOrWhiteSpace(modulesDirectory)&&Directory.Exists(modulesDirectory)) {
				List<ModuleModel> gameModules = await Task.Run(() => ScanDirectory(modulesDirectory,token),token);
				allMods.AddRange(gameModules);
				} else {
				_logger.Warning($"ModScanner: Modules directory '{modulesDirectory}' not found or not configured.");
				}
			if (config.IsGameFromSteam) {
				string workshopDirectory = config.SteamWorkshopFolderPath;
				if (!string.IsNullOrWhiteSpace(workshopDirectory)&&Directory.Exists(workshopDirectory)) {
					List<ModuleModel> workshopMods = await Task.Run(() => ScanDirectory(workshopDirectory,token),token);
					allMods.AddRange(workshopMods);
					} else {
					_logger.Warning($"ModScanner: Steam Workshop directory '{workshopDirectory}' not found or not configured.");
					}
				} else {
				_logger.Info("ModScanner: Game is not from Steam, skipping Steam Workshop scan.");
				}
			_logger.Info($"ModScanner: Found {allMods.Count} total mods across all directories.");
			return allMods;
			}
		private static List<ModuleModel> ScanDirectory(string directoryPath,CancellationToken token) {
			List<ModuleModel> mods = new List<ModuleModel>();
			if (!Directory.Exists(directoryPath)) {
				_logger.Warning($"ModScanner: Directory: '{directoryPath}' does not exist. Skipping scan.");
				return mods;
				}
			string[] subDirectories;
			try {
				subDirectories=Directory.GetDirectories(directoryPath);
				} catch (Exception ex) {
				_logger.Error($"ModScanner: Failed to enumerate directories in '{directoryPath}', {ex.Message}");
				return mods;
				}
			foreach (string subDir in subDirectories) {
				token.ThrowIfCancellationRequested();
				ModuleModel? mod = TryParseModuleFromDirectory(subDir);
				if (mod is not null) {
					mods.Add(mod);
					}
				}
			return mods;
			}
		private static ModuleModel? TryParseModuleFromDirectory(string directoryPath) {
			string xmlFilePath = Path.Combine(directoryPath,"SubModule.xml");
			if (File.Exists(xmlFilePath)) {
				return ModParser.Parse(xmlFilePath,directoryPath);
				}
			try {
				string[] innerDirs = Directory.GetDirectories(directoryPath);
				foreach (string innerDir in innerDirs) {
					string nestedPath = Path.Combine(innerDir,"SubModule.xml");
					if (File.Exists(nestedPath)) {
						return ModParser.Parse(nestedPath,innerDir);
						}
					}
				} catch (Exception ex) {
				_logger.Error($"ModScanner: Failed searching nested directories in'{directoryPath}'",ex);
				}
			return null;
			}
		}
	}
