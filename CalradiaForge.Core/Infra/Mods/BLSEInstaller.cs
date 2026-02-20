namespace CalradiaForge.Core.Infra.Mods {
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	using CalradiaForge.Core.Infra.Config;
	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Infra.Paths;
	using CalradiaForge.Core.Models;

	/// <summary>
	/// Handles detection and installation of the Bannerlord Software Extender (BLSE)
	/// from an extracted archive. BLSE is not a standard Bannerlord module — it ships
	/// as executables and DLLs that must be placed in the game's bin directory.
	///
	/// Archive structure:
	/// <code>
	/// BLSE-vX.Y.Z.7z
	/// └── bin
	///     ├── Win64_Shipping_Client          ← Steam / GOG / Epic / StandAlone
	///     │   ├── Bannerlord.BLSE.Standalone.exe
	///     │   ├── Bannerlord.BLSE.Launcher.exe
	///     │   ├── Bannerlord.BLSE.LauncherEx.exe
	///     │   └── (various .dll / .config files)
	///     └── Gaming.Desktop.x64_Shipping_Client  ← Xbox Game Pass PC
	///         ├── Bannerlord.BLSE.Standalone.exe
	///         └── (various .dll files)
	/// </code>
	///
	/// Installation flow:
	/// 1. Detect the correct platform subfolder based on <see cref="GameProvider"/>
	/// 2. Unblock all files in the temp source folder (removes Zone.Identifier ADS)
	/// 3. Copy unblocked files into the game's bin directory
	/// 4. Auto-set <see cref="AppConfigSettings.BLSEExePath"/>
	///
	/// Always overwrites existing files — no version comparison is performed.
	/// </summary>
	public static class BLSEInstaller {
		private static readonly Logger _logger = Logger.Instance;

		/// <summary>
		/// The definitive marker file used to identify a BLSE archive.
		/// If this executable exists anywhere in the extracted directory tree,
		/// the archive is treated as a BLSE installation package.
		/// </summary>
		private const string _blseStandaloneExeName = "Bannerlord.BLSE.Standalone.exe";

		/// <summary>
		/// Bin subfolder name used by Steam, GOG, Epic, and StandAlone installs.
		/// </summary>
		private const string _win64BinFolder = "Win64_Shipping_Client";

		/// <summary>
		/// Bin subfolder name used by Xbox Game Pass PC installs.
		/// </summary>
		private const string _gamePassBinFolder = "Gaming.Desktop.x64_Shipping_Client";

		/// <summary>
		/// Checks whether the extracted archive contains BLSE by searching
		/// for <see cref="_blseStandaloneExeName"/> anywhere in the directory tree.
		/// </summary>
		/// <param name="extractedDir">The temp directory where the archive was extracted.</param>
		/// <returns><c>true</c> if a BLSE standalone executable was found.</returns>
		public static bool IsBLSEArchive(string extractedDir) {
			if (string.IsNullOrWhiteSpace(extractedDir) || !Directory.Exists(extractedDir)) {
				return false;
			}
			try {
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("BLSEInstaller: Scanning for BLSE marker.", new { ExtractedDir = extractedDir });
				}
				string[] matches = Directory.GetFiles(extractedDir, _blseStandaloneExeName, SearchOption.AllDirectories);
				bool found = matches.Length > 0;
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("BLSEInstaller: BLSE marker scan result.", new { ExtractedDir = extractedDir, MatchCount = matches.Length });
				}
				return found;
			} catch (Exception ex) {
				_logger.Warning($"BLSEInstaller: Failed to scan for BLSE marker: {ex.Message}");
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("BLSEInstaller: BLSE marker scan failed.", new { ExtractedDir = extractedDir }, ex);
				}
				return false;
			}
		}

		/// <summary>
		/// Installs BLSE from an extracted archive into the game's bin directory.
		/// Selects the correct platform subfolder based on <see cref="AppConfigSettings.GameProvider"/>,
		/// unblocks all files in the temp source folder to strip Zone.Identifier ADS,
		/// then copies the clean files into the game's matching bin path. Always overwrites.
		/// On success, auto-sets <see cref="AppConfigSettings.BLSEExePath"/>.
		/// </summary>
		/// <param name="extractedDir">The temp directory where the BLSE archive was extracted.</param>
		/// <param name="config">App config — provides game folder path, game provider, and receives the BLSE exe path.</param>
		/// <param name="token">Cancellation token.</param>
		/// <returns>A <see cref="BLSEInstallResult"/> describing the outcome.</returns>
		public static async Task<BLSEInstallResult> InstallAsync(
			string extractedDir,
			AppConfigSettings config,
			CancellationToken token = default) {

			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("BLSEInstaller: Starting install.", new { ExtractedDir = extractedDir, GameProvider = config.GameProvider.ToString() });
			}
			// Validate game folder
			string gameFolderPath = config.GameFolderPath;
			if (string.IsNullOrWhiteSpace(gameFolderPath) || !Directory.Exists(gameFolderPath)) {
				string error = "Game folder not configured or not found. Please check your Settings.";
				_logger.Warning($"BLSEInstaller: {error}");
				return BLSEInstallResult.Fail(error);
			}

			// Determine the correct bin subfolder name for this platform
			string platformBinName = ResolvePlatformBinFolder(config.GameProvider);
			_logger.Info($"BLSEInstaller: Platform '{config.GameProvider}' resolved to bin folder '{platformBinName}'.");
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("BLSEInstaller: Platform bin resolved.", new { PlatformBinName = platformBinName });
			}

			// Locate the platform-specific source folder in the extracted archive
			// Expected path: extractedDir/bin/<platformBinName>
			string? sourceBinDir = FindPlatformBinInExtracted(extractedDir, platformBinName);
			if (sourceBinDir is null) {
				string error = $"BLSE archive does not contain the expected '{platformBinName}' folder for your platform ({config.GameProvider}).";
				_logger.Warning($"BLSEInstaller: {error}");
				return BLSEInstallResult.Fail(error);
			}

			// Determine the target bin directory in the game installation
			// For GamePass: GameFolder/Content/bin/<platformBinName>
			// For all others: GameFolder/bin/<platformBinName>
			string targetBinDir = ResolveGameBinPath(gameFolderPath, platformBinName, config.GameProvider);

			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("BLSEInstaller: Resolved bin paths.", new { SourceBinDir = sourceBinDir, TargetBinDir = targetBinDir });
			}
			if (!Directory.Exists(targetBinDir)) {
				string error = $"Game bin directory not found at '{targetBinDir}'. Please verify your game installation.";
				_logger.Warning($"BLSEInstaller: {error}");
				return BLSEInstallResult.Fail(error);
			}

			try {
				// Step 1: Unblock all BLSE files in the temp source folder before copying.
				// Strips Zone.Identifier ADS from .exe, .dll, and .config files so they
				// arrive in the game bin already clean — no secondary unblock pass needed.
				_logger.Info($"BLSEInstaller: Unblocking BLSE files in temp source '{sourceBinDir}'...");
				UnblockResult unblockResult = await DLLUnblocker.UnblockBLSEFilesAsync(sourceBinDir, token);
				_logger.Info($"BLSEInstaller: Unblock complete — {unblockResult.UnblockedCount} file(s) unblocked, {unblockResult.FailedCount} failed.");
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("BLSEInstaller: Unblock summary.", new { Unblocked = unblockResult.UnblockedCount, Failed = unblockResult.FailedCount });
				}

				// Step 2: Copy all unblocked files into the game bin folder
				_logger.Info($"BLSEInstaller: Copying BLSE files from '{sourceBinDir}' to '{targetBinDir}'...");
				int filesCopied = await Task.Run(() => CopyBinFiles(sourceBinDir, targetBinDir, token), token);
				_logger.Info($"BLSEInstaller: Copied {filesCopied} file(s) to '{targetBinDir}'.");
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("BLSEInstaller: Copy complete.", new { FilesCopied = filesCopied, TargetBinDir = targetBinDir });
				}

				// Step 3: Auto-set the BLSE exe path in config
				string blseExePath = Path.Combine(targetBinDir, _blseStandaloneExeName);
				if (File.Exists(blseExePath)) {
					config.BLSEExePath = blseExePath;
					_logger.Info($"BLSEInstaller: Auto-configured BLSEExePath to '{blseExePath}'.");
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("BLSEInstaller: BLSE exe configured.", new { BlseExePath = blseExePath });
					}
				}

				string successMessage = $"BLSE installed to game bin folder. ({filesCopied} file(s) copied)";
				_logger.Info($"BLSEInstaller: {successMessage}");
				return BLSEInstallResult.Ok(successMessage);

			} catch (OperationCanceledException) {
				throw;
			} catch (Exception ex) {
				string error = $"Failed to install BLSE: {ex.Message}";
				_logger.Error(ex, $"BLSEInstaller: {error}");
				return BLSEInstallResult.Fail(error);
			}
		}

		/// <summary>
		/// Returns the bin subfolder name appropriate for the given game provider.
		/// GamePass uses <c>Gaming.Desktop.x64_Shipping_Client</c>;
		/// all other platforms use <c>Win64_Shipping_Client</c>.
		/// </summary>
		private static string ResolvePlatformBinFolder(GameProvider provider) {
			return provider == GameProvider.GamePass
				? _gamePassBinFolder
				: _win64BinFolder;
		}

		/// <summary>
		/// Builds the full target bin path inside the game installation.
		/// GamePass installs have an extra <c>Content</c> directory:
		/// <c>GameFolder/Content/bin/Gaming.Desktop.x64_Shipping_Client</c>.
		/// All other platforms use: <c>GameFolder/bin/Win64_Shipping_Client</c>.
		/// </summary>
		private static string ResolveGameBinPath(string gameFolderPath, string platformBinName, GameProvider provider) {
			return provider == GameProvider.GamePass
				? Path.Combine(gameFolderPath, "Content", "bin", platformBinName)
				: Path.Combine(gameFolderPath, "bin", platformBinName);
		}

		/// <summary>
		/// Searches the extracted archive for the platform-specific bin subfolder.
		/// Walks the directory tree looking for a directory named
		/// <paramref name="platformBinName"/> that contains
		/// <see cref="_blseStandaloneExeName"/>.
		/// </summary>
		/// <returns>The full path to the matching bin folder, or <c>null</c> if not found.</returns>
		private static string? FindPlatformBinInExtracted(string extractedDir, string platformBinName) {
			try {
				string[] candidates = Directory.GetDirectories(
					extractedDir, platformBinName, SearchOption.AllDirectories);

				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("BLSEInstaller: Searching extracted dirs.", new { ExtractedDir = extractedDir, PlatformBinName = platformBinName, CandidateCount = candidates.Length });
				}
				foreach (string candidate in candidates) {
					if (File.Exists(Path.Combine(candidate, _blseStandaloneExeName))) {
						if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
							_logger.Debug("BLSEInstaller: Found platform bin.", new { Candidate = candidate });
						}
						return candidate;
					}
				}
			} catch (Exception ex) {
				_logger.Warning(
					$"BLSEInstaller: Error searching for '{platformBinName}' in extracted archive: {ex.Message}");
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("BLSEInstaller: Search failed.", new { ExtractedDir = extractedDir, PlatformBinName = platformBinName }, ex);
				}
			}
			return null;
		}

		/// <summary>
		/// Copies all files from the BLSE source bin directory into the game bin directory.
		/// Always overwrites existing files. Does not recurse into subdirectories —
		/// BLSE bin folders contain only flat files.
		/// </summary>
		/// <returns>The number of files copied.</returns>
		private static int CopyBinFiles(string sourceDir, string targetDir, CancellationToken token) {
			string[] files = Directory.GetFiles(sourceDir);
			int count = 0;

			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("BLSEInstaller: Copying bin files.", new { SourceDir = sourceDir, TargetDir = targetDir, FileCount = files.Length });
			}
			foreach (string sourceFile in files) {
				token.ThrowIfCancellationRequested();
				string fileName = Path.GetFileName(sourceFile);
				string targetFile = Path.Combine(targetDir, fileName);
				File.Copy(sourceFile, targetFile, overwrite: true);
				count++;
			}

			return count;
		}
	}

	/// <summary>
	/// Result of a BLSE installation attempt.
	/// Separate from <see cref="Models.ModInstallResult"/> because BLSE
	/// is not a standard mod and should not count toward mod install totals.
	/// </summary>
	public sealed class BLSEInstallResult {
		public bool Success { get; private init; }
		public string Message { get; private init; } = string.Empty;

		public static BLSEInstallResult Ok(string message) => new() { Success = true, Message = message };
		public static BLSEInstallResult Fail(string message) => new() { Success = false, Message = message };
	}
}
