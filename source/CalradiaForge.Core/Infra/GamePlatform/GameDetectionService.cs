namespace CalradiaForge.Core.Infra.GamePlatform;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Logging;
using CalradiaForge.Core.Infra.Paths;
using CalradiaForge.Core.Models;

/// <summary>
/// Owns startup, re-detection, and bounded manual game-path workflows.
/// </summary>
public sealed class GameDetectionService {
	private readonly GamePlatformDetectionResolver _resolver;
	private readonly StartupNotificationQueue _startupNotifications;
	private readonly Logger _logger = Logger.Instance;

	/// <summary>Initializes the workflow with its Core dependencies.</summary>
	public GameDetectionService(
		GamePlatformDetectionResolver resolver,
		StartupNotificationQueue startupNotifications) {
		_resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
		_startupNotifications = startupNotifications
			?? throw new ArgumentNullException(nameof(startupNotifications));
	}

	/// <summary>
	/// Reuses a valid configured installation or runs detection and queues one result.
	/// </summary>
	public void InitializeForStartup(AppConfigSettings config) {
		ArgumentNullException.ThrowIfNull(config);
		if (HasValidConfiguredGame(config)) {
			return;
		}

		GameProvider provider = _resolver.DetectGame(config);
		QueueStartupResult(config, provider);
		return;
	}

	/// <summary>Runs an explicit automatic re-detection.</summary>
	public GameProvider RedetectGame(AppConfigSettings config) {
		ArgumentNullException.ThrowIfNull(config);
		return _resolver.DetectGame(config);
	}

	/// <summary>
	/// Applies one manually selected game folder without invoking platform metadata discovery.
	/// </summary>
	public bool ApplyManualGameFolder(AppConfigSettings config, string selectedGameFolder) {
		ArgumentNullException.ThrowIfNull(config);
		if (string.IsNullOrWhiteSpace(selectedGameFolder)) {
			return false;
		}
		try {
			string normalizedPath = GamePlatformDetectionResolver.NormalizeDirectory(selectedGameFolder);
			if (!GamePathValidator.ValidateGameFolder(normalizedPath)) {
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("GameDetectionWorkflow: Manual game folder rejected.");
				}
				return false;
			}

			string launcherPath = GamePlatformDetectionResolver.ResolveStandardLauncherPath(normalizedPath);
			if (!GamePathValidator.ValidateGameExecutable(launcherPath)) {
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("GameDetectionWorkflow: Manual launcher rejected.");
				}
				return false;
			}

			GameProvider provider = InferManualProvider(normalizedPath);
			string blsePath = GamePlatformDetectionResolver.ResolveOptionalBlsePath(normalizedPath);
			config.GameFolderPath = normalizedPath;
			config.GameLauncherFilePath = launcherPath;
			config.SteamWorkshopFolderPath = string.Empty;
			config.BLSEExePath = blsePath;
			config.GameProvider = provider;
			return true;
		} catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) {
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("GameDetectionWorkflow: Manual game folder could not be normalized.", ex);
			}
			return false;
		}
	}

	/// <summary>
	/// Applies a manually selected Workshop root only for an existing Steam configuration.
	/// </summary>
	public bool ApplyManualSteamWorkshopFolder(
		AppConfigSettings config,
		string selectedWorkshopFolder) {
		ArgumentNullException.ThrowIfNull(config);
		if (config.GameProvider != GameProvider.Steam) {
			return false;
		}
		if (string.IsNullOrWhiteSpace(selectedWorkshopFolder)) {
			return false;
		}

		try {
			string normalizedPath = GamePlatformDetectionResolver.NormalizeDirectory(selectedWorkshopFolder);
			if (!GamePathValidator.ValidateWorkshopFolder(normalizedPath)) {
				return false;
			}

			config.SteamWorkshopFolderPath = normalizedPath;
			return true;
		} catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) {
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("GameDetectionWorkflow: Manual Workshop folder could not be normalized.", ex);
			}
			return false;
		}
	}

	private static bool HasValidConfiguredGame(AppConfigSettings config) {
		if (config.GameProvider is GameProvider.NotInitialized or GameProvider.ManualConfiguration) {
			return false;
		}

		return GamePathValidator.ValidateGameFolder(config.GameFolderPath)
			&& GamePathValidator.ValidateGameExecutable(config.GameLauncherFilePath);
	}

	private void QueueStartupResult(AppConfigSettings config, GameProvider provider) {
		StartupNotification notification = provider switch {
			GameProvider.Steam when string.IsNullOrWhiteSpace(config.SteamWorkshopFolderPath) => new(
				"Steam Workshop Not Found",
				"Steam Bannerlord was detected without a Workshop folder. Local modules remain available, and the Workshop folder can be selected in Settings.",
				StartupNotificationSeverity.Warning),
			GameProvider.ManualConfiguration => new(
				"Game Configuration Required",
				"Bannerlord could not be detected automatically. Select the game folder in Settings.",
				StartupNotificationSeverity.Warning),
			_ => new(
				"Bannerlord Detected",
				$"Bannerlord was detected through {provider}.",
				StartupNotificationSeverity.Success)
		};
		_startupNotifications.Enqueue(notification);
	}

	private static GameProvider InferManualProvider(string normalizedPath) {
		string[] segments = normalizedPath.Split(
			[Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
			StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		if (ContainsSequence(segments, "steamapps", "common")) {
			return GameProvider.Steam;
		}
		if (ContainsSegment(segments, "Epic Games")) {
			return GameProvider.EpicGames;
		}
		if (ContainsSegment(segments, "XboxGames")) {
			return GameProvider.GamePass;
		}
		if (ContainsSegment(segments, "GOG Games")
			|| ContainsSequence(segments, "GOG Galaxy", "Games")) {
			return GameProvider.GOG;
		}
		return GameProvider.StandAlone;
	}

	private static bool ContainsSegment(IEnumerable<string> segments, string expected) {
		return segments.Any(segment => string.Equals(segment, expected, StringComparison.OrdinalIgnoreCase));
	}

	private static bool ContainsSequence(IReadOnlyList<string> segments, string first, string second) {
		for (int index = 0; index < segments.Count - 1; index++) {
			if (string.Equals(segments[index], first, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(segments[index + 1], second, StringComparison.OrdinalIgnoreCase)) {
				return true;
			}
		}
		return false;
	}
}
