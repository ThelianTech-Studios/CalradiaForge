namespace CalradiaForge.Core.Infra.GamePlatform;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.GamePlatform.Epic;
using CalradiaForge.Core.Infra.GamePlatform.Steam;
using CalradiaForge.Core.Infra.Logging;
using CalradiaForge.Core.Infra.Paths;

/// <summary>
/// Detects Bannerlord through supported platform metadata and commits one
/// coherent provider/path selection to <see cref="AppSettings"/>.
/// </summary>
public sealed class GamePlatformDetectionResolver {
	private const string _bannerlordExecutableName = "Bannerlord.exe";
	private const string _blseStandaloneExecutableName = "Bannerlord.BLSE.Standalone.exe";
	private static readonly bool _enableUnsupportedPlatforms = false;
	private readonly ISteamClientRootProvider _steamClientRootProvider;
	private readonly ISteamInstallationResolver _steamInstallationResolver;
	private readonly Logger _logger = Logger.Instance;

	/// <summary>
	/// Initializes the resolver with replaceable Steam metadata dependencies.
	/// </summary>
	public GamePlatformDetectionResolver(ISteamClientRootProvider steamClientRootProvider, ISteamInstallationResolver steamInstallationResolver) {
		_steamClientRootProvider = steamClientRootProvider
			?? throw new ArgumentNullException(nameof(steamClientRootProvider));
		_steamInstallationResolver = steamInstallationResolver
			?? throw new ArgumentNullException(nameof(steamInstallationResolver));
	}

	/// <summary>
	/// Runs automatic detection in the approved provider order.
	/// </summary>
	public GameProvider DetectGame(AppSettings config) {
		ArgumentNullException.ThrowIfNull(config);

		try {
			if (TryDetectSteam(config)) {
				return config.GameProvider;
			}

			if (_enableUnsupportedPlatforms && TryDetectEpic(config)) {
				return config.GameProvider;
			}
		} catch (Exception ex) {
			_logger.Error(ex, "GamePlatformDetectionResolver: Automatic detection failed unexpectedly.");
		}
		ApplyManualConfigurationFallback(config);
		return config.GameProvider;
	}

	private bool TryDetectSteam(AppSettings config) {
		try {
			string? steamClientRoot = _steamClientRootProvider.GetSteamClientRoot();
			if (string.IsNullOrWhiteSpace(steamClientRoot)) {
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("GamePlatformDetectionResolver: Steam client root was not found.");
				}
				return false;
			}

			SteamResolutionResult result = _steamInstallationResolver.ResolveBannerlord(
				steamClientRoot,
				new SteamResolutionOptions(PreserveExistingWorkshopPath: false));
			LogSteamDiagnostics(result.Diagnostics);
			if (!result.IsGameResolved || string.IsNullOrWhiteSpace(result.GameFolderPath)) {
				return false;
			}

			string gameFolderPath = NormalizeDirectory(result.GameFolderPath);
			if (!GamePathValidator.ValidateGameFolder(gameFolderPath)) {
				_logger.Warning($"GamePlatformDetectionResolver: Resolved Steam game path was rejected.");
				return false;
			}

			string launcherPath = ResolveStandardLauncherPath(gameFolderPath);
			if (!GamePathValidator.ValidateGameExecutable(launcherPath)) {
				_logger.Warning($"GamePlatformDetectionResolver: Resolved Steam launcher was rejected.");
				return false;
			}

			string workshopPath = ResolveOptionalWorkshopPath(result.WorkshopFolderPath);
			string blsePath = ResolveOptionalBlsePath(gameFolderPath);

			config.GameFolderPath = gameFolderPath;
			config.GameLauncherFilePath = launcherPath;
			config.SteamWorkshopFolderPath = workshopPath;
			config.BLSEExePath = blsePath;
			config.GameProvider = GameProvider.Steam;
			return true;
		} catch (Exception ex) {
			_logger.Error(ex, "GamePlatformDetectionResolver: Steam detection failed.");
			return false;
		}
	}

	private bool TryDetectEpic(AppSettings config) {
		try {
			if (!EpicDetector.IsEpicGameInstalled()) {
				return false;
			}

			string? detectedPath = EpicManifestReader.TryGetInstallLocation();
			if (string.IsNullOrWhiteSpace(detectedPath)) {
				return false;
			}

			string gameFolderPath = NormalizeDirectory(detectedPath);
			if (!GamePathValidator.ValidateGameFolder(gameFolderPath)) {
				return false;
			}

			string launcherPath = ResolveStandardLauncherPath(gameFolderPath);
			if (!GamePathValidator.ValidateGameExecutable(launcherPath)) {
				return false;
			}

			string blsePath = ResolveOptionalBlsePath(gameFolderPath);
			config.GameFolderPath = gameFolderPath;
			config.GameLauncherFilePath = launcherPath;
			config.SteamWorkshopFolderPath = string.Empty;
			config.BLSEExePath = blsePath;
			config.GameProvider = GameProvider.EpicGames;
			return true;
		} catch (Exception ex) {
			_logger.Error(ex, "GamePlatformDetectionResolver: Epic detection failed.");
			return false;
		}
	}

	private void LogSteamDiagnostics(IEnumerable<SteamPathDiagnostic> diagnostics) {
		if (_logger.MinimumLevel != Logger.LogLevel.Debug) {
			return;
		}

		foreach (SteamPathDiagnostic diagnostic in diagnostics) {
			_logger.Debug("GamePlatformDetectionResolver: Steam path diagnostic.", new {
				diagnostic.Code,
				diagnostic.Message,
				diagnostic.Path,
				diagnostic.IsWarning
			});
		}
	}

	private static void ApplyManualConfigurationFallback(AppSettings config) {
		config.GameFolderPath = string.Empty;
		config.GameLauncherFilePath = string.Empty;
		config.SteamWorkshopFolderPath = string.Empty;
		config.BLSEExePath = string.Empty;
		config.GameProvider = GameProvider.ManualConfiguration;
	}

	internal static string NormalizeDirectory(string path) {
		return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path.Trim()));
	}

	internal static string ResolveStandardLauncherPath(string gameFolderPath) {
		return Path.Combine(
			gameFolderPath,
			"bin",
			"Win64_Shipping_Client",
			_bannerlordExecutableName);
	}

	internal static string ResolveOptionalBlsePath(string gameFolderPath) {
		try {
			string candidate = Path.Combine(
				gameFolderPath,
				"bin",
				"Win64_Shipping_Client",
				_blseStandaloneExecutableName);
			return File.Exists(candidate) ? candidate : string.Empty;
		} catch (Exception ex) {
			Logger.Instance.Error(ex, "GamePlatformDetectionResolver: Optional BLSE check failed.");
			return string.Empty;
		}
	}

	private string ResolveOptionalWorkshopPath(string? workshopFolderPath) {
		if (string.IsNullOrWhiteSpace(workshopFolderPath)) {
			return string.Empty;
		}

		try {
			string normalizedPath = NormalizeDirectory(workshopFolderPath);
			if (GamePathValidator.ValidateWorkshopFolder(normalizedPath)) {
				return normalizedPath;
			}
		} catch (Exception ex) {
			_logger.Error(ex, "GamePlatformDetectionResolver: Optional Workshop path could not be normalized.");
		}

		return string.Empty;
	}
}
