namespace CalradiaForge.Core.Infra.Launch {
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading.Tasks;

	using CalradiaForge.Core.Infra.Config;
	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Infra.Paths;
	using CalradiaForge.Core.Models;

	

	// TODO: Implement launcher config write for Epic/GamePass platforms.
	// The approach (similar to Novus Launcher) would be:
	// 1. Write the load order into Bannerlord's vanilla launcher config file
	//    so the vanilla launcher picks it up with the correct mod arrangement.
	// 2. For GamePass: Launch the vanilla launcher executable, user presses Play there.
	// 3. For Epic: Instruct the user to launch through Epic Games Store.
	// This requires researching the exact config file format and location
	// (likely LauncherData.xml or similar in the game's Documents folder).

	/// <summary>
	/// Handles launching Bannerlord with a given load order.
	/// Receives all dependencies explicitly — never accesses global state.
	/// Supports Steam and StandAlone/GOG via direct EXE launch.
	/// Epic Games and GamePass are not supported for direct launch due to
	/// TaleWorlds' authentication requirements — users must launch through
	/// their respective platform clients.
	/// </summary>
	public sealed class GameLauncher {
		private const string _steamAppId = "261550";
		private const string _steamProcessName = "steam";
		private const string _steamProtocolUri = "steam://open/main";
		private const int _steamStartupDelayMs = 8000;
		private const int _steamPollIntervalMs = 1000;
		private const int _steamMaxWaitMs = 60000;
		private readonly AppConfigSettings _config;
		private readonly Logger _logger = Logger.Instance;

		public GameLauncher(AppConfigSettings config) {
			_config = config ?? throw new ArgumentNullException(nameof(config));
		}

		/// <summary>
		/// Validates that the game can be launched with the current configuration.
		/// All platforms require a valid game folder and a locatable executable.
		/// Epic Games and GamePass are blocked from direct launch with a descriptive message.
		/// Steam validation is handled separately during launch via
		/// <see cref="EnsureSteamRunningAsync"/> which auto-starts Steam if needed.
		/// </summary>
		/// <param name="error">Describes the validation failure, if any.</param>
		/// <returns><c>true</c> when the configuration is valid for launch.</returns>
		public bool CanLaunch(out string error) {
			if (_config.GameProvider == GameProvider.NotInitialized) {
				error = "Game platform not configured. Please check your Settings.";
				return false;
			}

			if (_config.GameProvider == GameProvider.EpicGames) {
				error = "Epic Games requires authentication through the Epic launcher. " +
					"Your load order is ready — launch Bannerlord through the Epic Games Store.";
				return false;
			}

			if (_config.GameProvider == GameProvider.GamePass) {
				error = "GamePass requires Xbox account login through the Xbox app. " +
					"Your load order is ready — launch Bannerlord through the Xbox app.";
				return false;
			}

			if (string.IsNullOrWhiteSpace(_config.GameFolderPath) || !Directory.Exists(_config.GameFolderPath)) {
				error = "Game folder not found. Please check your Settings.";
				return false;
			}

			if (string.IsNullOrWhiteSpace(_config.GameLauncherFilePath) || !File.Exists(_config.GameLauncherFilePath)) {
				error = "Game executable not found. Please check your Settings.";
				return false;
			}

			error = string.Empty;
			return true;
		}

		/// <summary>
		/// Launches Bannerlord with the specified load order.
		/// For Steam installs, ensures Steam is running first — auto-launching it
		/// if needed and waiting for the client to initialize before starting the game.
		/// For Epic Games and GamePass, launch is blocked by <see cref="CanLaunch"/>
		/// with a descriptive message guiding the user to launch through their platform client.
		/// The load order is passed exactly as provided by the modpack — no reordering
		/// is applied. Framework mods (Harmony, ButterLib, etc.) must load before
		/// core game modules so their IL patches can hook engine methods before
		/// initialization. The modpack templates define the correct order.
		/// </summary>
		/// <param name="loadOrder">The active load order to pass to the game.</param>
		/// <returns>A result indicating success or the reason for failure.</returns>
		public async Task<GameLaunchResult> LaunchAsync(List<ModuleModel> loadOrder) {
			if (!CanLaunch(out string validationError)) {
				return GameLaunchResult.Fail(validationError);
			}

			// Ensure Steam is running for Steam installs before launching the game
			if (_config.GameProvider == GameProvider.Steam) {
				GameLaunchResult steamResult = await EnsureSteamRunningAsync();
				if (!steamResult.Success) {
					return steamResult;
				}
			}

			string arguments = BuildLaunchArguments(loadOrder);
			_logger.Info($"GameLauncher: Launching with arguments: {arguments}");

			try {
				return LaunchExe(arguments);
			} catch (Exception ex) {
				_logger.Error($"GameLauncher: Failed to launch game.", ex);
				return GameLaunchResult.Fail($"Launch failed: {ex.Message}");
			}
		}

		/// <summary>
		/// Builds the complete Bannerlord CLI argument string.
		/// Format: <c>/singleplayer _MODULES_*Mod1*Mod2*...*_MODULES_</c>
		/// The game mode flag is required — without it Bannerlord crashes on startup.
		/// </summary>
		private static string BuildLaunchArguments(List<ModuleModel> loadOrder) {
			string modulesArg = BuildModulesArgument(loadOrder);
			if (string.IsNullOrEmpty(modulesArg)) {
				return "/singleplayer";
			}
			return $"/singleplayer {modulesArg}";
		}

		/// <summary>
		/// Builds the Bannerlord <c>_MODULES_</c> command-line segment from a load order.
		/// Preserves the exact order from the modpack/user — no reordering is applied.
		/// The modpack templates (<see cref="Modpacks.VanillaModules"/>) and user-defined
		/// modpacks are responsible for correct dependency ordering.
		/// Format: <c>_MODULES_*Mod1*Mod2*...*_MODULES_</c>
		/// </summary>
		private static string BuildModulesArgument(List<ModuleModel> loadOrder) {
			if (loadOrder.Count == 0) {
				return string.Empty;
			}

			List<string> ids = loadOrder
				.Where(m => !string.IsNullOrWhiteSpace(m.ModuleId))
				.Select(m => m.ModuleId)
				.ToList();

			if (ids.Count == 0) {
				return string.Empty;
			}

			return $"_MODULES_*{string.Join("*", ids)}*_MODULES_";
		}

		/// <summary>
		/// Launches the game by starting the executable from
		/// <see cref="AppConfigSettings.GameLauncherFilePath"/>.
		/// For Steam installs, sets the <c>SteamAppId</c> environment variable
		/// so Steam recognizes the process and enables overlay, achievements,
		/// and workshop integration.
		/// </summary>
		private GameLaunchResult LaunchExe(string arguments) {
			string exePath = _config.GameLauncherFilePath;

			ProcessStartInfo startInfo = new() {
				FileName = exePath,
				Arguments = arguments,
				UseShellExecute = false,
				WorkingDirectory = Path.GetDirectoryName(exePath) ?? string.Empty
			};

			if (_config.GameProvider == GameProvider.Steam) {
				startInfo.Environment["SteamAppId"] = _steamAppId;
			}

			Process.Start(startInfo);
			_logger.Info($"GameLauncher: Launched '{exePath}' (Platform: {_config.GameProvider}).");
			return GameLaunchResult.Ok($"Game launched via {_config.GameProvider}.");
		}

		/// <summary>
		/// Ensures the Steam client is running before launching a Steam game.
		/// If Steam is already running, returns immediately.
		/// If not, launches Steam via the <c>steam://</c> protocol URI and polls
		/// for the process to appear, waiting up to <see cref="_steamMaxWaitMs"/>.
		/// After the process appears, waits an additional <see cref="_steamStartupDelayMs"/>
		/// for Steam to fully initialize its API (login, overlay, etc.).
		/// </summary>
		/// <returns>
		/// <see cref="GameLaunchResult.Ok"/> when Steam is confirmed running;
		/// <see cref="GameLaunchResult.Fail"/> if Steam could not be started within the timeout.
		/// </returns>
		private async Task<GameLaunchResult> EnsureSteamRunningAsync() {
			if (IsSteamRunning()) {
				_logger.Info("GameLauncher: Steam is already running.");
				return GameLaunchResult.Ok("Steam is running.");
			}

			_logger.Info("GameLauncher: Steam is not running. Attempting to start Steam...");

			try {
				Process.Start(new ProcessStartInfo {
					FileName = _steamProtocolUri,
					UseShellExecute = true
				});
			} catch (Exception ex) {
				_logger.Error("GameLauncher: Failed to start Steam.", ex);
				return GameLaunchResult.Fail("Failed to start Steam. Please launch Steam manually and try again.");
			}

			// Poll until the Steam process appears or timeout
			int elapsed = 0;
			while (!IsSteamRunning() && elapsed < _steamMaxWaitMs) {
				await Task.Delay(_steamPollIntervalMs);
				elapsed += _steamPollIntervalMs;
			}

			if (!IsSteamRunning()) {
				_logger.Warning("GameLauncher: Steam did not start within the timeout period.");
				return GameLaunchResult.Fail("Steam did not start in time. Please launch Steam manually and try again.");
			}

			// Wait for Steam to fully initialize (login, overlay, API)
			_logger.Info($"GameLauncher: Steam process detected. Waiting {_steamStartupDelayMs}ms for full initialization...");
			await Task.Delay(_steamStartupDelayMs);

			_logger.Info("GameLauncher: Steam is ready.");
			return GameLaunchResult.Ok("Steam started successfully.");
		}

		/// <summary>
		/// Checks whether the Steam client process is currently running.
		/// Looks for the <c>steam</c> process by name. This is required for
		/// Steam game installs because the Steam API DLL expects a running
		/// client to initialize — without it the game crashes on startup.
		/// </summary>
		/// <returns><c>true</c> if a Steam client process is detected.</returns>
		private static bool IsSteamRunning() {
			try {
				Process[] steamProcesses = Process.GetProcessesByName(_steamProcessName);
				bool running = steamProcesses.Length > 0;

				foreach (Process process in steamProcesses) {
					process.Dispose();
				}

				return running;
			} catch {
				// If we can't check, assume it's running to avoid blocking launch
				return true;
			}
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
