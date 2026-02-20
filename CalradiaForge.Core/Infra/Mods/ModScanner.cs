namespace CalradiaForge.Core.Infra.Mods
	{
	using System.Collections.Generic;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;

	using CalradiaForge.Core.Infra.Config;
	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Models;

	public sealed class ModScanner
		{
		private static readonly Logger _logger = Logger.Instance;

		public static async Task<List<ModuleModel>> ScanForModsAsync(AppConfigSettings config,CancellationToken token = default,List<ModuleModel>? allMods = null) {
			allMods??=new List<ModuleModel>();
			string modulesDirectory = config.ModulesDirectoryPath;
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModScanner: Starting scan.", new { ModulesDirectory = modulesDirectory, IsSteam = config.IsGameFromSteam });
			}
			if (!string.IsNullOrWhiteSpace(modulesDirectory)&&Directory.Exists(modulesDirectory)) {
				List<ModuleModel> gameModules = await Task.Run(() => ScanDirectory(modulesDirectory,token),token);
				allMods.AddRange(gameModules);
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModScanner: Scanned game modules.", new { Directory = modulesDirectory, Count = gameModules.Count });
				}
				} else {
				_logger.Warning($"ModScanner: Modules directory '{modulesDirectory}' not found or not configured.");
				}
			if (config.IsGameFromSteam) {
				string workshopDirectory = config.SteamWorkshopFolderPath;
				if (!string.IsNullOrWhiteSpace(workshopDirectory)&&Directory.Exists(workshopDirectory)) {
					List<ModuleModel> workshopMods = await Task.Run(() => ScanDirectory(workshopDirectory,token),token);
					allMods.AddRange(workshopMods);
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("ModScanner: Scanned workshop mods.", new { Directory = workshopDirectory, Count = workshopMods.Count });
					}
					} else {
					_logger.Warning($"ModScanner: Steam Workshop directory '{workshopDirectory}' not found or not configured.");
					}
				} else {
				_logger.Info("ModScanner: Game is not from Steam, skipping Steam Workshop scan.");
				}
			_logger.Info($"ModScanner: Found {allMods.Count} total mods across all directories.");
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModScanner: Scan complete.", new { TotalCount = allMods.Count });
			}
			return allMods;
			}
		private static List<ModuleModel> ScanDirectory(string directoryPath,CancellationToken token) {
			List<ModuleModel> mods = new List<ModuleModel>();
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModScanner: Scanning directory.", new { Directory = directoryPath });
			}
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
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModScanner: Inspecting subdirectory.", new { Directory = subDir });
				}
				ModuleModel? mod = TryParseModuleFromDirectory(subDir);
				if (mod is not null) {
					if (!mod.IsSinglePlayerMod) {
						_logger.Info($"ModScanner: Skipping multiplayer-only mod '{mod.ModuleName}' ({mod.ModuleId}).");
						continue;
						}
					mods.Add(mod);
					}
				}
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModScanner: Directory scan complete.", new { Directory = directoryPath, Count = mods.Count });
			}
			return mods;
			}
		private static ModuleModel? TryParseModuleFromDirectory(string directoryPath) {
			string xmlFilePath = Path.Combine(directoryPath,"SubModule.xml");
			if (File.Exists(xmlFilePath)) {
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModScanner: Found SubModule.xml.", new { Directory = directoryPath, XmlPath = xmlFilePath });
				}
				return ModParser.Parse(xmlFilePath,directoryPath);
				}
			try {
				string[] innerDirs = Directory.GetDirectories(directoryPath);
				foreach (string innerDir in innerDirs) {
					string nestedPath = Path.Combine(innerDir,"SubModule.xml");
					if (File.Exists(nestedPath)) {
						if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
							_logger.Debug("ModScanner: Found nested SubModule.xml.", new { Directory = directoryPath, XmlPath = nestedPath });
						}
						return ModParser.Parse(nestedPath,innerDir);
						}
					}
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModScanner: No SubModule.xml found.", new { Directory = directoryPath });
					}
				} catch (Exception ex) {
				_logger.Error($"ModScanner: Failed searching nested directories in'{directoryPath}'",ex);
				}
			return null;
			}
		}
	}
