namespace CalradiaForge.Core.Infra.Paths {
	using CalradiaForge.Core.Infra.Config;
	using CalradiaForge.Core.Infra.Logging;

	using Microsoft.Win32;

	/// <summary>
	/// Provides helpers for auto-detecting game install paths and resolving folders.
	/// </summary>
	public static class GamePathsHelper {
		private static readonly Logger _logger = Logger.Instance;
		private const string _steamGameID = "261550";
		private const string _bannerlordFolderName = "Mount & Blade II Bannerlord";

		/// <summary>
		/// Known BLSE Standalone executable name.
		/// This is the CLI-oriented launcher that accepts the same
		/// <c>/singleplayer _MODULES_*...*_MODULES_</c> arguments as Bannerlord.exe
		/// and launches the game directly without opening a GUI launcher.
		/// </summary>
		private const string _blseStandaloneExeName = "Bannerlord.BLSE.Standalone.exe";

		/// <summary>
		/// Master toggle for Epic Games and GamePass platform detection.
		/// Set to <c>false</c> to disable detection — the detection methods
		/// (<see cref="TryDetectEpic"/>) remain fully intact and unchanged
		/// for re-enablement once platform testing becomes possible.
		/// When disabled, auto-detection skips Epic/GamePass and falls through
		/// to <see cref="GameProvider.StandAlone"/>.
		/// </summary>
		private static readonly bool _enableUnsupportedPlatforms = false;

		/// <summary>
		/// Attempts to auto-detect the game installation and update configuration values.
		/// </summary>
		public static void TryAutoDetectGameFolder(AppConfigSettings config) {
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("GamePathsHelper: Starting auto-detection.");
			}
			if (TryDetectSteam(config))
				return;
			if (_enableUnsupportedPlatforms) {
				if (TryDetectEpic(config)) {
					return;
				}
			}
			config.GameProvider = GameProvider.StandAlone;
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("GamePathsHelper: Auto-detection fallback to standalone.");
			}
		}
		#region Detection Methods

		/// <summary>
		/// Attempts to detect a Steam installation and populate configuration paths.
		/// </summary>
		private static bool TryDetectSteam(AppConfigSettings config) {
			try {
				string? steamPath = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam", "SteamPath", null) as string;
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("GamePathsHelper: Steam registry read.", new { SteamPath = steamPath ?? "<null>" });
				}
				if (string.IsNullOrWhiteSpace(steamPath)) {
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("GamePathsHelper: Steam path missing.");
					}
					return false;
				} else {
					string gamePath = Path.Combine(steamPath, "steamapps", "common", _bannerlordFolderName);
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("GamePathsHelper: Checking Steam game path.", new { GamePath = gamePath, Exists = Directory.Exists(gamePath) });
					}
					if (!Directory.Exists(gamePath)) {
						return false;
					} else {
						config.GameProvider = GameProvider.Steam;
						config.GameFolderPath = gamePath;
						config.GameLauncherFilePath = Path.Combine(gamePath, "bin", "Win64_Shipping_Client", "Bannerlord.exe");
						string workshopPath = Path.Combine(steamPath, "steamapps", "workshop", "content", _steamGameID);
						if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
							_logger.Debug("GamePathsHelper: Checking Steam workshop path.", new { WorkshopPath = workshopPath, Exists = Directory.Exists(workshopPath) });
						}
						if (!Directory.Exists(workshopPath)) {
							throw new DirectoryNotFoundException($"The expected Steam Workshop folder was not found at '{workshopPath}'. Please check your Settings");
						} else {
							config.SteamWorkshopFolderPath = workshopPath;
						}
						TryDetectBLSE(config, gamePath);
						if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
							_logger.Debug("GamePathsHelper: Steam detection succeeded.", new { GamePath = gamePath, WorkshopPath = workshopPath });
						}
						return true;
					}
				}
			} catch (Exception ex) {
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("GamePathsHelper: Steam detection failed.", ex);
				}
				return false;
			}
		}
		/// <summary>
		/// Attempts to detect an Epic Games installation and populate configuration paths.
		/// </summary>
		private static bool TryDetectEpic(AppConfigSettings config) {
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("GamePathsHelper: Starting Epic detection.");
			}
			if (!EpicDetector.IsEpicGameInstalled()) {
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("GamePathsHelper: Epic launcher not detected.");
				}
				return false;
			}
			string? epicPath = EpicManifestReader.TryGetInstallLocation();
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("GamePathsHelper: Epic manifest result.", new { EpicPath = epicPath ?? "<null>" });
			}
			if (string.IsNullOrWhiteSpace(epicPath)) {
				return false;
			}
			config.GameProvider = GameProvider.EpicGames;
			config.GameFolderPath = epicPath;
			config.GameLauncherFilePath = Path.Combine(epicPath, "bin", "Win64_Shipping_Client", "Bannerlord.exe");
			TryDetectBLSE(config, epicPath);
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("GamePathsHelper: Epic detection succeeded.", new { GamePath = epicPath });
			}
			return true;
		}


		#endregion

		#region BLSE Detection

		/// <summary>
		/// Attempts to locate the BLSE Standalone executable in the game's
		/// <c>bin\Win64_Shipping_Client</c> directory — the same folder that
		/// contains <c>Bannerlord.exe</c>. If found, populates
		/// <see cref="AppConfigSettings.BLSEExePath"/> so the Play button
		/// can offer BLSE as a launch target without manual configuration.
		/// </summary>
		/// <param name="config">Config to update with the detected BLSE path.</param>
		/// <param name="gameFolderPath">Root Bannerlord installation folder.</param>
		private static void TryDetectBLSE(AppConfigSettings config, string gameFolderPath) {
			string blsePath = Path.Combine(gameFolderPath, "bin", "Win64_Shipping_Client", _blseStandaloneExeName);
			bool exists = File.Exists(blsePath);
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("GamePathsHelper: Checking BLSE path.", new { BlsePath = blsePath, Exists = exists });
			}
			if (exists) {
				config.BLSEExePath = blsePath;
			}
		}

		#endregion

		/// <summary>
		/// Resolves the Modules folder for the configured game install.
		/// </summary>
		/// <param name="appConfig">The application config containing game folder path.</param>
		/// <returns>Full path to the Modules folder.</returns>
		/// <exception cref="DirectoryNotFoundException">Thrown when the Modules folder does not exist.</exception>
		public static string GetModulesFolder(AppConfigSettings appConfig) {
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("GamePathsHelper: Resolving modules folder.", new { GameFolderPath = appConfig.GameFolderPath });
			}
			if (string.IsNullOrWhiteSpace(appConfig.GameFolderPath) || !Directory.Exists(appConfig.GameFolderPath)) {
				throw new DirectoryNotFoundException($"The provided path '{appConfig.GameFolderPath}' is not valid or does not exist.");
			}
			string modulesPath = Path.GetFullPath(appConfig.GameFolderPath, "Modules");
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("GamePathsHelper: Checking modules folder.", new { ModulesPath = modulesPath, Exists = Directory.Exists(modulesPath) });
			}
			if (!Directory.Exists(modulesPath)) {
				throw new DirectoryNotFoundException($"Modules folder not found at folder path: '{modulesPath}'.");
			}
			return modulesPath;
		}

		/// <summary>
		/// Resolves the Steam Workshop folder when the game is configured for Steam.
		/// </summary>
		public static string? GetSteamWorkshopFolder(AppConfigSettings appConfig) {
			if (appConfig.IsGameFromSteam) {
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("GamePathsHelper: Resolving Steam workshop folder.", new { WorkshopPath = appConfig.SteamWorkshopFolderPath });
				}
				if (!string.IsNullOrWhiteSpace(appConfig.SteamWorkshopFolderPath) && Directory.Exists(appConfig.SteamWorkshopFolderPath)) {
					string workshopPath = Path.GetFullPath(appConfig.SteamWorkshopFolderPath);
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("GamePathsHelper: Checking Steam workshop folder.", new { WorkshopPath = workshopPath, Exists = Directory.Exists(workshopPath) });
					}
					if (Directory.Exists(workshopPath)) {
						return workshopPath;
					}
					throw new DirectoryNotFoundException($"The Steam Workshop folder does not exist at: '{workshopPath}'. Please check your Settings");
				} else {
					throw new DirectoryNotFoundException($"The Steam Workshop folder path '{appConfig.SteamWorkshopFolderPath}' is not valid or does not exist. Please check your settings.");
				}
			} else {
				return null;
			}
		}
	}
}
