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
	/// When an archive does not contain a SubModule.xml, falls back to
	/// BLSE detection and installs it to the game's bin directory if matched.
	///
	/// The install task is owned by this service, not the calling page.
	/// The UI layer starts the install via <see cref="StartInstallAsync"/> and
	/// observes progress via <see cref="InstallProgressChanged"/>.
	/// Navigation away from the page does not cancel or orphan the task.
	/// </summary>
	public sealed class ModInstaller {
		private readonly AppConfigSettings _appConfig;
		private readonly Logger _logger = Logger.Instance;
		private readonly SemaphoreSlim _installLock = new(1, 1);
		private CancellationTokenSource? _cts;

		/// <summary>
		/// Accepted archive file extensions for mod installation (case-insensitive).
		/// <c>.7z</c> is temporarily excluded due to SharpCompress block-compression
		/// performance issues causing ~25 minute extraction times for large mods.
		/// Will be re-enabled once native 7-Zip extraction is implemented.
		/// </summary>
		private static readonly HashSet<string> _acceptedExtensions = new(StringComparer.OrdinalIgnoreCase) {
			".zip", ".rar"
		};

		public ModInstaller(AppConfigSettings appConfig) {
			_appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
		}

		/// <summary>
		/// Supported archive file extensions for the file dialog filter.
		/// </summary>
		public static string FileDialogFilter =>
			"Mod Archives (*.zip;*.rar)|*.zip;*.rar|All Files (*.*)|*.*";

		/// <summary>
		/// Indicates whether an installation batch is currently running.
		/// The UI layer can bind to this to disable buttons or show spinners.
		/// </summary>
		public bool IsInstalling { get; private set; }

		/// <summary>
		/// The summary of the most recently completed install batch.
		/// Available after <see cref="InstallCompleted"/> fires.
		/// <c>null</c> if no install has completed yet.
		/// </summary>
		public ModInstallSummary? LastSummary { get; private set; }

		/// <summary>
		/// Raised on the thread pool after each individual archive is processed.
		/// The UI layer must dispatch to the UI thread before updating controls.
		/// Payload: the cumulative <see cref="ModInstallSummary"/> so far.
		/// </summary>
		public event Action<ModInstallSummary>? InstallProgressChanged;

		/// <summary>
		/// Raised on the thread pool when the entire batch completes (success or failure).
		/// The UI layer must dispatch to the UI thread before updating controls.
		/// Payload: the final <see cref="ModInstallSummary"/>.
		/// </summary>
		public event Action<ModInstallSummary>? InstallCompleted;

		/// <summary>
		/// Raised on the extraction thread during archive extraction.
		/// Reports batch-level cumulative file counts and per-archive context
		/// so the UI can render a single progress bar that never resets.
		/// The UI layer must dispatch to the UI thread before updating controls.
		/// </summary>
		public event Action<ExtractionProgress>? ExtractionProgressChanged;

		/// <summary>
		/// Checks whether the given file has an accepted archive extension.
		/// </summary>
		/// <param name="filePath">Full path or file name to check.</param>
		/// <returns><c>true</c> if the extension is <c>.zip</c> or <c>.rar</c>.</returns>
		public static bool IsAcceptedArchive(string filePath) {
			string extension = Path.GetExtension(filePath);
			return _acceptedExtensions.Contains(extension);
		}

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
		/// Starts a batch mod installation on a background thread.
		/// Returns immediately — progress is reported via <see cref="InstallProgressChanged"/>
		/// and completion via <see cref="InstallCompleted"/>.
		/// Guarded by a semaphore — only one batch can run at a time.
		/// The task is owned by this service and survives page navigation.
		/// </summary>
		/// <param name="archivePaths">Array of full paths to archive files selected by the user.</param>
		/// <returns>
		/// <c>true</c> if the install was started.
		/// <c>false</c> if an install is already in progress.
		/// </returns>
		public bool StartInstallAsync(string[] archivePaths) {
			if (!_installLock.Wait(0)) {
				_logger.Warning("ModInstaller: Install already in progress. Ignoring duplicate request.");
				return false;
			}

			IsInstalling = true;
			_cts = new CancellationTokenSource();
			CancellationToken token = _cts.Token;

			// Fire-and-forget on the thread pool — the service owns this task's lifetime
			_ = Task.Run(async () => {
				ModInstallSummary summary = new();
				try {
					summary = await InstallModsAsync(archivePaths, token);
				} catch (OperationCanceledException) {
					_logger.Info("ModInstaller: Install batch was cancelled.");
				} catch (Exception ex) {
					_logger.Error(ex, "ModInstaller: Unhandled exception in install batch.");
				} finally {
					LastSummary = summary;
					IsInstalling = false;
					_cts?.Dispose();
					_cts = null;
					_installLock.Release();
					InstallCompleted?.Invoke(summary);
				}
			});

			return true;
		}

		/// <summary>
		/// Cancels the currently running install batch, if any.
		/// </summary>
		public void CancelInstall() {
			if (_cts is { IsCancellationRequested: false } cts) {
				_logger.Info("ModInstaller: Cancellation requested by user.");
				cts.Cancel();
			}
		}

		/// <summary>
		/// Installs mods from the given archive file paths.
		/// Archives are queued and processed sequentially.
		/// Each archive is extracted, its mod root found, version-checked against
		/// any existing installation, and placed into the Modules folder.
		/// If no SubModule.xml is found, the archive is checked for BLSE and
		/// installed to the game bin if detected.
		/// Raises <see cref="InstallProgressChanged"/> after each archive.
		/// Raises <see cref="ExtractionProgressChanged"/> during extraction with
		/// batch-level cumulative file counts for smooth progress reporting.
		/// </summary>
		/// <param name="archivePaths">Array of full paths to archive files selected by the user.</param>
		/// <param name="token">Cancellation token.</param>
		/// <returns>A summary of all install results.</returns>
		private async Task<ModInstallSummary> InstallModsAsync(
			string[] archivePaths,
			CancellationToken token) {

			ModInstallSummary summary = new();
			string modulesPath = _appConfig.ModulesDirectoryPath;

			// Build the processing queue
			Queue<string> installQueue = new(archivePaths);
			int totalArchives = archivePaths.Length;
			_logger.Info($"ModInstaller: Queued {installQueue.Count} archive(s) for installation.");

			// Batch-level tracking for cumulative extraction progress
			DateTime batchStartUtc = DateTime.UtcNow;
			int batchFilesExtracted = 0;

			// Pre-estimate total files across all archives using file-size heuristic
			int estimatedTotalFiles = 0;
			int[] perArchiveEstimates = new int[totalArchives];
			for (int i = 0; i < totalArchives; i++) {
				perArchiveEstimates[i] = ModExtractor.EstimateFileCount(archivePaths[i]);
				estimatedTotalFiles += perArchiveEstimates[i];
			}

			int archiveIndex = 0;
			while (installQueue.Count > 0) {
				token.ThrowIfCancellationRequested();
				string archivePath = installQueue.Dequeue();
				string archiveFileName = Path.GetFileName(archivePath);
				int currentArchiveIndex = archiveIndex;
				int archiveFilesExtracted = 0;

				// Reject unsupported archive formats before attempting extraction
				if (!IsAcceptedArchive(archivePath)) {
					string ext = Path.GetExtension(archivePath);
					ModInstallResult skipped = new() {
						ArchiveFileName = archiveFileName,
						Status = ModInstallStatus.Failed,
						Message = $"Unsupported archive format '{ext}'. Only .zip and .rar are currently accepted."
					};
					_logger.Warning($"ModInstaller: Rejected '{archiveFileName}' — unsupported format '{ext}'.");
					summary.Results.Add(skipped);
					InstallProgressChanged?.Invoke(summary);
					archiveIndex++;
					continue;
				}

				// Per-file extraction callback — builds batch-level progress
				void OnFileExtracted(int localCount) {
					archiveFilesExtracted = localCount;
					int currentBatchTotal = batchFilesExtracted + localCount;
					ExtractionProgressChanged?.Invoke(new ExtractionProgress {
						CurrentEntry = localCount,
						ArchiveFileName = archiveFileName,
						ArchiveIndex = currentArchiveIndex + 1,
						TotalArchives = totalArchives,
						BatchFilesExtracted = currentBatchTotal,
						EstimatedTotalFiles = estimatedTotalFiles,
						TimestampUtc = DateTime.UtcNow,
						BatchStartUtc = batchStartUtc
					});
				}	

				ModInstallResult result = await ProcessSingleArchiveAsync(
					archivePath, archiveFileName, modulesPath, token, OnFileExtracted);

				// After archive completes, refine the estimate:
				// replace this archive's heuristic estimate with the actual count
				int previousEstimate = perArchiveEstimates[currentArchiveIndex];
				int actualCount = archiveFilesExtracted;
				estimatedTotalFiles = estimatedTotalFiles - previousEstimate + actualCount;
				batchFilesExtracted += actualCount;

				summary.Results.Add(result);
				InstallProgressChanged?.Invoke(summary);
				archiveIndex++;
			}

			_logger.Info($"ModInstaller: Batch complete. {summary.ToSummaryString()}");
			return summary;
		}

		#region Single Archive Processing

		private async Task<ModInstallResult> ProcessSingleArchiveAsync(
			string archivePath,
			string archiveFileName,
			string modulesPath,
			CancellationToken token,
			Action<int>? onFileExtracted = null) {

			ModInstallResult result = new() { ArchiveFileName = archiveFileName };
			string? tempDir = null;

			try {
				// Extract archive to temp directory with per-file progress
				tempDir = await ModExtractor.ExtractToTempAsync(archivePath, token, onFileExtracted);
				if (tempDir is null) {
					result.Status = ModInstallStatus.Failed;
					result.Message = "Failed to extract archive.";
					return result;
				}

				// Find the true mod root (handles lazy nested folders)
				string? modRoot = ModExtractor.FindModRoot(tempDir);

				// ── BLSE fallback ──────────────────────────────────────────────
				// If no SubModule.xml was found, check if this is a BLSE archive.
				// BLSE is not a standard Bannerlord module — it ships as exes/DLLs
				// that go into the game's bin directory, not the Modules folder.
				if (modRoot is null) {
					if (BLSEInstaller.IsBLSEArchive(tempDir)) {
						return await ProcessBLSEInstallAsync(tempDir, archiveFileName, token);
					}

					// Not a mod and not BLSE — genuinely invalid archive
					result.Status = ModInstallStatus.Failed;
					result.Message = "No SubModule.xml found in archive. Not a valid Bannerlord mod.";
					return result;
				}
				// ── End BLSE fallback ──────────────────────────────────────────

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

		/// <summary>
		/// Handles BLSE installation as a special case within the archive processing pipeline.
		/// Delegates to <see cref="BLSEInstaller.InstallAsync"/> for platform-aware file copying.
		/// Returns a <see cref="ModInstallResult"/> with <see cref="ModInstallStatus.Installed"/>
		/// on success so it flows through the normal summary pipeline, but does NOT count
		/// toward <see cref="ModInstallSummary.InstalledCount"/> because BLSE is not a mod.
		/// The result is identified by <see cref="ModInstallResult.ModuleId"/> being set to "BLSE".
		/// </summary>
		private async Task<ModInstallResult> ProcessBLSEInstallAsync(
			string tempDir,
			string archiveFileName,
			CancellationToken token) {

			_logger.Info($"ModInstaller: Detected BLSE archive in '{archiveFileName}'. Delegating to BLSEInstaller.");

			BLSEInstallResult blseResult = await BLSEInstaller.InstallAsync(tempDir, _appConfig, token);

			ModInstallResult result = new() {
				ArchiveFileName = archiveFileName,
				ModuleId = "BLSE",
				ModuleName = "Bannerlord Software Extender (BLSE)"
			};

			if (blseResult.Success) {
				result.Status = ModInstallStatus.Installed;
				result.Message = blseResult.Message;
				_logger.Info($"BLSEInstaller: BLSE installed successfully from '{archiveFileName}'.");
			} else {
				result.Status = ModInstallStatus.Failed;
				result.Message = blseResult.Message;
				_logger.Warning($"BLSEInstaller: BLSE installation failed from '{archiveFileName}': {blseResult.Message}");
			}

			return result;
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
