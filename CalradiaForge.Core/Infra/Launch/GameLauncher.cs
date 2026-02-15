namespace CalradiaForge.Core.Infra.Launch {
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;

	using CalradiaForge.Core.Infra.Config;
	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Infra.Paths;
	using CalradiaForge.Core.Models;

	/// <summary>
	/// Handles launching Bannerlord with a given load order.
	/// Receives all dependencies explicitly — never accesses global state.
	/// Supports Steam, Epic, and StandAlone launch modes.
	/// </summary>
	public sealed class GameLauncher {
		private const string _steamAppId = "261550";
		private readonly AppConfigSettings _config;
		private readonly Logger _logger = Logger.Instance;

		public GameLauncher(AppConfigSettings config) {
			_config = config ?? throw new ArgumentNullException(nameof(config));
		}

		/// <summary>
		/// Validates that the game can be launched with the current configuration.
		/// </summary>
		/// <param name="error">Describes the validation failure, if any.</param>
		/// <returns><c>true</c> when the configuration is valid for launch.</returns>
		public bool CanLaunch(out string error) {
			if (_config.GameProvider == GameProvider.NotInitialized) {
				error = "Game platform not configured. Please check your Settings.";
				return false;
			}

			if (string.IsNullOrWhiteSpace(_config.GameFolderPath) || !Directory.Exists(_config.GameFolderPath)) {
				error = "Game folder not found. Please check your Settings.";
				return false;
			}

			// Steam launches via protocol — no exe validation needed
			if (_config.GameProvider == GameProvider.Steam) {
				error = string.Empty;
				return true;
			}

			// Epic and StandAlone require a valid launcher exe
			if (string.IsNullOrWhiteSpace(_config.GameLauncherFilePath) || !File.Exists(_config.GameLauncherFilePath)) {
				error = "Game executable not found. Please check your Settings.";
				return false;
			}

			error = string.Empty;
			return true;
		}

		/// <summary>
		/// Launches Bannerlord with the specified load order.
		/// Builds the <c>_MODULES_</c> argument string from the provided module list.
		/// </summary>
		/// <param name="loadOrder">The active load order to pass to the game.</param>
		/// <returns>A result indicating success or the reason for failure.</returns>
		public GameLaunchResult Launch(List<ModuleModel> loadOrder) {
			if (!CanLaunch(out string validationError)) {
				return GameLaunchResult.Fail(validationError);
			}

			string modulesArg = BuildModulesArgument(loadOrder);
			_logger.Info($"GameLauncher: Launching with modules: {modulesArg}");

			try {
				switch (_config.GameProvider) {
					case GameProvider.Steam:
						return LaunchViaSteam(modulesArg);
					case GameProvider.EpicGames:
					case GameProvider.StandAlone:
						return LaunchViaExe(modulesArg);
					default:
						return GameLaunchResult.Fail("Unknown game platform. Please check your Settings.");
				}
			} catch (Exception ex) {
				_logger.Error($"GameLauncher: Failed to launch game.", ex);
				return GameLaunchResult.Fail($"Launch failed: {ex.Message}");
			}
		}

		/// <summary>
		/// Builds the Bannerlord <c>_MODULES_</c> command-line argument from a load order.
		/// Format: <c>_MODULES_*Native*SandBoxCore*SandBox*...</c>
		/// Each module ID is prefixed with <c>*</c>.
		/// </summary>
		private static string BuildModulesArgument(List<ModuleModel> loadOrder) {
			if (loadOrder.Count == 0) {
				return string.Empty;
			}
			IEnumerable<string> ids = loadOrder
				.Where(m => !string.IsNullOrWhiteSpace(m.ModuleId))
				.Select(m => m.ModuleId);
			return $"_MODULES_*{string.Join("*", ids)}*_MODULES_";
		}

		/// <summary>
		/// Launches the game via the Steam browser protocol.
		/// </summary>
		private GameLaunchResult LaunchViaSteam(string modulesArg) {
			// Steam launch URI: steam://run/{appId}//{args}
			// The double slash separates the app ID from the launch arguments
			string launchUri = $"steam://run/{_steamAppId}//{modulesArg}";
			Process.Start(new ProcessStartInfo {
				FileName = launchUri,
				UseShellExecute = true
			});
			_logger.Info($"GameLauncher: Launched via Steam protocol.");
			return GameLaunchResult.Ok("Game launched via Steam.");
		}

		/// <summary>
		/// Launches the game by starting the executable directly (Epic / StandAlone).
		/// </summary>
		private GameLaunchResult LaunchViaExe(string modulesArg) {
			string exePath = _config.GameLauncherFilePath;
			Process.Start(new ProcessStartInfo {
				FileName = exePath,
				Arguments = modulesArg,
				UseShellExecute = false,
				WorkingDirectory = Path.GetDirectoryName(exePath) ?? string.Empty
			});
			_logger.Info($"GameLauncher: Launched via exe '{exePath}'.");
			return GameLaunchResult.Ok("Game launched.");
		}
	}

	/// <summary>
	/// Represents the outcome of a game launch attempt.
	/// </summary>
	public sealed class GameLaunchResult {
		public bool Success { get; private init; }
		public string Message { get; private init; } = string.Empty;

		public static GameLaunchResult Ok(string message) => new() { Success = true, Message = message };
		public static GameLaunchResult Fail(string message) => new() { Success = false, Message = message };
	}
}
