namespace CalradiaForge.Core.Infra.Mods {
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;

	using CalradiaForge.Core.Infra.Config;
	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Models;

	/// <summary>
	/// Orchestrates batch mod installation from archive files.
	/// Queues archives and processes them one-by-one on a background thread.
	/// Handles extraction, mod root detection, version comparison, and placement
	/// into the game's Modules folder.
	/// </summary>
	public sealed class ModInstaller {
		private readonly AppConfigSettings _appConfig;
		private readonly Logger _logger = Logger.Instance;

		public ModInstaller(AppConfigSettings appConfig) {
			_appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
		}

		/// <summary>
		/// Supported archive file extensions for the file dialog filter.
		/// </summary>
		public static string FileDialogFilter =>
			"Mod Archives (*.zip;*.rar;*.7z;*.tar;*.gz)|*.zip;*.rar;*.7z;*.tar;*.gz|All Files (*.*)|*.*";

		/// <summary>
		/// Validates that the game directory is configured and exists.
		/// Call this before opening the file dialog.
		/// </summary>
		/// <param name="errorMessage">A user-friendly message if validation fails.</param>
		/// <returns><c>true</c> if the game directory is valid and ready for installation.</returns>
		public bool ValidateGameDirectory(out string errorMessage) {
			string gameFolderPath = _appConfig.GameFolderPath;
			if (string.IsNullOrWhiteSpace(gameFolderPath)) {
				errorMessage = "Game directory has not been set. Please go to Settings and set the game directory path.";
				return false;
			}
			if (!Directory.Exists(gameFolderPath)) {
				errorMessage = $"Game directory not found at '{gameFolderPath}'. Please check your Settings.";
				return false;
			}
			string modulesPath = _appConfig.ModulesDirectoryPath;
			if (string.IsNullOrWhiteSpace(modulesPath) || !Directory.Exists(modulesPath)) {
				errorMessage = $"Modules folder not found at '{modulesPath}'. Please verify the game installation.";
				return false;
			}
			errorMessage = string.Empty;
			return true;
		}

		/// <summary>
		/// Installs mods from the given archive file paths.
		/// Archives are queued and processed sequentially on a background thread.
		/// Each archive is extracted, its mod root found, version-checked against
		/// any existing installation, and placed into the Modules folder.
		/// </summary>
		/// <param name="archivePaths">Array of full paths to archive files selected by the user.</param>
		/// <param name="token">Cancellation token.</param>
		/// <returns>A summary of all install results.</returns>
		public async Task<ModInstallSummary> InstallModsAsync(
			string[] archivePaths,
			CancellationToken token = default) {

			ModInstallSummary summary = new();
			string modulesPath = _appConfig.ModulesDirectoryPath;

			// Build the processing queue
			Queue<string> installQueue = new(archivePaths);
			_logger.Info($"ModInstaller: Queued {installQueue.Count} archive(s) for installation.");

			while (installQueue.Count > 0) {
				token.ThrowIfCancellationRequested();
				string archivePath = installQueue.Dequeue();
				string archiveFileName = Path.GetFileName(archivePath);
				ModInstallResult result = await ProcessSingleArchiveAsync(archivePath, archiveFileName, modulesPath, token);
				summary.Results.Add(result);
			}

			_logger.Info($"ModInstaller: Batch complete. {summary.ToSummaryString()}");
			return summary;
		}

		#region Single Archive Processing

		private async Task<ModInstallResult> ProcessSingleArchiveAsync(
			string archivePath,
			string archiveFileName,
			string modulesPath,
			CancellationToken token) {

			ModInstallResult result = new() { ArchiveFileName = archiveFileName };
			string? tempDir = null;

			try {
				// Extract archive to temp directory
				tempDir = await ModExtractor.ExtractToTempAsync(archivePath, token);
				if (tempDir is null) {
					result.Status = ModInstallStatus.Failed;
					result.Message = "Failed to extract archive.";
					return result;
				}

				// Find the true mod root (handles lazy nested folders)
				string? modRoot = ModExtractor.FindModRoot(tempDir);
				if (modRoot is null) {
					result.Status = ModInstallStatus.Failed;
					result.Message = "No SubModule.xml found in archive. Not a valid Bannerlord mod.";
					return result;
				}

				// Parse mod metadata from the extracted SubModule.xml
				string xmlPath = Path.Combine(modRoot, "SubModule.xml");
				ModuleModel? newMod = ModParser.Parse(xmlPath, modRoot);
				if (newMod is null || string.IsNullOrEmpty(newMod.ModuleId)) {
					result.Status = ModInstallStatus.Failed;
					result.Message = "Failed to parse SubModule.xml from archive.";
					return result;
				}

				result.ModuleId = newMod.ModuleId;
				result.ModuleName = newMod.ModuleName;
				result.InstalledVersion = newMod.ModuleVersion;

				// Determine the target directory in the game's Modules folder
				string modFolderName = new DirectoryInfo(modRoot).Name;
				string targetPath = Path.Combine(modulesPath, modFolderName);

				// Check if mod is already installed — version comparison
				VersionCheckOutcome versionOutcome = CheckExistingVersion(targetPath, newMod);
				result.PreviousVersion = versionOutcome.ExistingVersion;

				switch (versionOutcome.Action) {
					case VersionAction.Skip:
						result.Status = ModInstallStatus.Skipped;
						result.Message = $"Already installed with same or newer version ({versionOutcome.ExistingVersion}).";
						_logger.Info($"ModInstaller: Skipped '{newMod.ModuleName}' — {result.Message}");
						return result;

					case VersionAction.Upgrade:
						// Safe delete the old version before copying the new one
						SafeDeleteDirectory(targetPath);
						result.Status = ModInstallStatus.Upgraded;
						result.Message = $"Upgraded from {versionOutcome.ExistingVersion} to {newMod.ModuleVersion}.";
						break;

					case VersionAction.Install:
						result.Status = ModInstallStatus.Installed;
						result.Message = "Installed successfully.";
						break;
				}

				// Copy the mod root into the Modules folder
				await Task.Run(() => CopyDirectory(modRoot, targetPath, token), token);
				_logger.Info($"ModInstaller: {result.Status} '{newMod.ModuleName}' ({newMod.ModuleVersion}) to '{targetPath}'");
				return result;

			} catch (OperationCanceledException) {
				result.Status = ModInstallStatus.Failed;
				result.Message = "Installation was cancelled.";
				throw;
			} catch (Exception ex) {
				result.Status = ModInstallStatus.Failed;
				result.Message = $"Error: {ex.Message}";
				_logger.Error(ex, $"ModInstaller: Failed to install '{archiveFileName}'");
				return result;
			} finally {
				// Always clean up the temp extraction directory
				if (tempDir is not null) {
					ModExtractor.CleanupTempDirectory(tempDir);
				}
			}
		}

		#endregion

		#region Version Checking

		private enum VersionAction { Install, Upgrade, Skip }

		private sealed class VersionCheckOutcome {
			public VersionAction Action { get; init; }
			public string? ExistingVersion { get; init; }
		}

		/// <summary>
		/// Checks whether the mod is already installed and compares versions.
		/// Returns whether to install fresh, upgrade, or skip.
		/// </summary>
		private static VersionCheckOutcome CheckExistingVersion(string targetPath, ModuleModel newMod) {
			if (!Directory.Exists(targetPath)) {
				return new VersionCheckOutcome { Action = VersionAction.Install };
			}

			// Try to parse the existing installed mod's SubModule.xml
			string existingXml = Path.Combine(targetPath, "SubModule.xml");
			if (!File.Exists(existingXml)) {
				// Folder exists but no SubModule.xml — treat as fresh install (overwrite junk)
				return new VersionCheckOutcome { Action = VersionAction.Install };
			}

			ModuleModel? existingMod = ModParser.Parse(existingXml, targetPath);
			if (existingMod is null || string.IsNullOrEmpty(existingMod.ModuleVersion)) {
				return new VersionCheckOutcome { Action = VersionAction.Install };
			}

			int comparison = CompareModVersions(newMod.ModuleVersion, existingMod.ModuleVersion);

			if (comparison > 0) {
				return new VersionCheckOutcome {
					Action = VersionAction.Upgrade,
					ExistingVersion = existingMod.ModuleVersion
				};
			}

			return new VersionCheckOutcome {
				Action = VersionAction.Skip,
				ExistingVersion = existingMod.ModuleVersion
			};
		}

		/// <summary>
		/// Compares two Bannerlord mod version strings.
		/// Handles the <c>v</c> prefix and <c>*</c> wildcard.
		/// Returns: positive if <paramref name="newVersion"/> is newer,
		/// 0 if equal, negative if older.
		/// </summary>
		private static int CompareModVersions(string newVersion, string existingVersion) {
			// Strip leading 'v' characters (Bannerlord uses "v1.3.3" or "vv1.3.4")
			string cleanNew = newVersion.TrimStart('v', 'V');
			string cleanExisting = existingVersion.TrimStart('v', 'V');

			// Remove wildcard suffixes (e.g., "1.3.4.*" → "1.3.4")
			cleanNew = cleanNew.TrimEnd('.', '*');
			cleanExisting = cleanExisting.TrimEnd('.', '*');

			if (Version.TryParse(cleanNew, out Version? parsedNew) &&
				Version.TryParse(cleanExisting, out Version? parsedExisting)) {
				return parsedNew.CompareTo(parsedExisting);
			}

			// Fallback to string comparison if parsing fails
			return string.Compare(cleanNew, cleanExisting, StringComparison.OrdinalIgnoreCase);
		}

		#endregion

		#region File System Helpers

		/// <summary>
		/// Recursively copies a directory and all its contents to a target path.
		/// </summary>
		private static void CopyDirectory(string sourceDir, string targetDir, CancellationToken token) {
			Directory.CreateDirectory(targetDir);

			foreach (string file in Directory.GetFiles(sourceDir)) {
				token.ThrowIfCancellationRequested();
				string targetFile = Path.Combine(targetDir, Path.GetFileName(file));
				File.Copy(file, targetFile, overwrite: true);
			}

			foreach (string subDir in Directory.GetDirectories(sourceDir)) {
				token.ThrowIfCancellationRequested();
				string targetSubDir = Path.Combine(targetDir, Path.GetFileName(subDir));
				CopyDirectory(subDir, targetSubDir, token);
			}
		}

		/// <summary>
		/// Safely deletes a directory and all its contents.
		/// Logs a warning if deletion fails instead of throwing.
		/// </summary>
		private static void SafeDeleteDirectory(string path) {
			try {
				if (Directory.Exists(path)) {
					Directory.Delete(path, recursive: true);
				}
			} catch (Exception ex) {
				Logger.Instance.Warning($"ModInstaller: Failed to delete directory '{path}': {ex.Message}");
			}
		}

		#endregion
	}
}
