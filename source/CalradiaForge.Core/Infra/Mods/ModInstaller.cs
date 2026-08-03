namespace CalradiaForge.Core.Infra.Mods {
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;

	using CalradiaForge.Core.Infra.Config;
	using CalradiaForge.Core.Models;

	using Serilog;

	/// <summary>
	/// Orchestrates batch mod installation from archive files.
	/// Queues archives and processes them one-by-one on a background thread.
	/// Handles extraction, mod root detection, version comparison, and placement
	/// into the game's Modules folder.
	/// When an archive does not contain a SubModule.xml, falls back to
	/// BLSE detection and installs it to the game's bin directory if matched.
	///
	/// The install task is owned by this service, not the calling page.
	/// The pipeline awaits <see cref="InstallAsync"/> and the UI observes progress
	/// via <see cref="InstallProgressChanged"/>.
	/// Navigation away from the page does not cancel or orphan the task.
	/// </summary>
	public sealed class ModInstaller {
		private readonly AppSettings _appConfig;
		private readonly SemaphoreSlim _installLock = new(1, 1);
		private readonly object _stateLock = new();
		private CancellationTokenSource? _cts;
		private bool _isInstalling;

		/// <summary>
		/// Accepted archive file extensions for mod installation (case-insensitive).
		/// </summary>
		private static readonly HashSet<string> _acceptedExtensions = new(StringComparer.OrdinalIgnoreCase) {
			".zip", ".rar", ".7z"
		};

		/// <summary>
		/// Initializes a new mod installer with the provided configuration.
		/// </summary>
		public ModInstaller(AppSettings appConfig) {
			_appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
		}

		/// <summary>
		/// Supported archive file extensions for the file dialog filter.
		/// </summary>
		public static string FileDialogFilter =>
			"Mod Archives (*.zip;*.rar;*.7z)|*.zip;*.rar;*.7z|All Files (*.*)|*.*";

		/// <summary>
		/// Indicates whether an installation batch is currently running.
		/// The UI layer can bind to this to disable buttons or show spinners.
		/// </summary>
		public bool IsInstalling { get { lock (_stateLock) { return _isInstalling; } } }

		/// <summary>
		/// The summary of the most recently completed install batch.
		/// Available after <see cref="InstallAsync"/> completes.
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
		/// <returns><c>true</c> if the extension is <c>.zip</c>, <c>.rar</c>, or <c>.7z</c>.</returns>
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
			Log.Debug(
				"ModInstaller: Validating game directory {GameFolderPath} with modules path {ModulesPath}.",
				gameFolderPath,
				_appConfig.ModulesDirectoryPath);
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
			Log.Debug(
				"ModInstaller: Game directory {GameFolderPath} validated with modules path {ModulesPath}.",
				gameFolderPath,
				modulesPath);
			return true;
		}

		/// <summary>
		/// Starts and awaits a batch mod installation.
		/// Progress is reported via <see cref="InstallProgressChanged"/>.
		/// Guarded by a semaphore — only one batch can run at a time.
		/// The returned task is the deterministic operation-lifetime boundary.
		/// </summary>
		/// <param name="archivePaths">Array of full paths to archive files selected by the user.</param>
		/// <returns>The completed summary, or <c>null</c> when another install is active.</returns>
		public async Task<ModInstallSummary?> InstallAsync(
			string[] archivePaths,
			CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(archivePaths);
			if (!await _installLock.WaitAsync(0, cancellationToken)) {
				Log.Warning("ModInstaller: Install already in progress. Ignoring duplicate request.");
				return null;
			}

			Log.Debug("ModInstaller: Starting install batch with {ArchiveCount} archives.", archivePaths.Length);
			CancellationTokenSource operationCancellation =
				CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			lock (_stateLock) {
				_isInstalling = true;
				_cts = operationCancellation;
			}
			CancellationToken token = operationCancellation.Token;

			ModInstallSummary summary = new();
			try {
				// Keep archive enumeration, extraction orchestration, and filesystem
				// continuations off the WPF caller context while retaining an awaitable
				// service-owned operation lifetime.
				await Task.Run(() => InstallModsAsync(archivePaths, summary, token), token)
					.ConfigureAwait(false);
			} catch (OperationCanceledException) when (token.IsCancellationRequested) {
				Log.Information("ModInstaller: Install batch was cancelled.");
			} catch (Exception ex) {
				Log.Error(ex, "ModInstaller: Unhandled exception in install batch.");
				throw;
			} finally {
				LastSummary = summary;
				lock (_stateLock) {
					_isInstalling = false;
					if (ReferenceEquals(_cts, operationCancellation)) {
						_cts = null;
					}
				}
				operationCancellation.Dispose();
				_installLock.Release();
			}

			return summary;
		}

		/// <summary>
		/// Cancels the currently running install batch, if any.
		/// </summary>
		public void CancelInstall() {
			CancellationTokenSource? cts;
			lock (_stateLock) {
				cts = _cts;
			}
			if (cts is { IsCancellationRequested: false }) {
				Log.Information("ModInstaller: Cancellation requested.");
				try {
					cts.Cancel();
				} catch (ObjectDisposedException) {
					// Completion won the race; the operation is already quiescent.
				}
			}
		}
		#region InstallModsAsync
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
		private async Task InstallModsAsync(
			string[] archivePaths,
			ModInstallSummary summary,
			CancellationToken token) {

			string modulesPath = _appConfig.ModulesDirectoryPath;

			// Build the processing queue
			Queue<string> installQueue = new(archivePaths);
			int totalArchives = archivePaths.Length;
			Log.Information("ModInstaller: Queued {ArchiveCount} archive(s) for installation.", installQueue.Count);
			Log.Debug(
				"ModInstaller: Install queue prepared with {ArchiveCount} archives for {ModulesPath}.",
				totalArchives,
				modulesPath);

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

				Log.Debug(
					"ModInstaller: Processing archive {ArchivePath} ({ArchiveIndex} of {TotalArchives}).",
					archivePath,
					currentArchiveIndex + 1,
					totalArchives);

				// Reject unsupported archive formats before attempting extraction
				if (!IsAcceptedArchive(archivePath)) {
					string ext = Path.GetExtension(archivePath);
					ModInstallResult skipped = new() {
						ArchiveFileName = archiveFileName,
						Status = ModInstallStatus.Failed,
						Message = $"Unsupported archive format '{ext}'. Only .zip, .rar, and .7z are currently accepted."
					};
					Log.Warning(
						"ModInstaller: Rejected {ArchiveFileName} — unsupported format {ArchiveExtension}.",
						archiveFileName,
						ext);
					summary.Results.Add(skipped);
					RaiseInstallProgressChanged(summary);
					archiveIndex++;
					continue;
				}

				// Per-file extraction callback — builds batch-level progress
				void OnFileExtracted(int localCount) {
					archiveFilesExtracted = localCount;
					int currentBatchTotal = batchFilesExtracted + localCount;
					RaiseExtractionProgressChanged(new ExtractionProgress {
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
				RaiseInstallProgressChanged(summary);
				archiveIndex++;
			}

			Log.Information(
				"ModInstaller: Batch complete; installed: {InstalledCount}; upgraded: {UpgradedCount}; skipped: {SkippedCount}; failed: {FailedCount}; BLSE status: {BlseStatus}.",
				summary.InstalledCount,
				summary.UpgradedCount,
				summary.SkippedCount,
				summary.FailedCount,
				summary.BLSEResult?.Status.ToString() ?? "NotProcessed");
			Log.Debug("ModInstaller: Batch complete with {ResultCount} results.", summary.Results.Count);
		}

		private void RaiseInstallProgressChanged(ModInstallSummary summary) {
			foreach (Action<ModInstallSummary> handler in
				InstallProgressChanged?.GetInvocationList().Cast<Action<ModInstallSummary>>() ?? []) {
				try {
					handler(summary);
				} catch (Exception ex) {
					Log.Error(ex, "ModInstaller: Install progress subscriber failed.");
				}
			}
		}

		private void RaiseExtractionProgressChanged(ExtractionProgress progress) {
			foreach (Action<ExtractionProgress> handler in
				ExtractionProgressChanged?.GetInvocationList().Cast<Action<ExtractionProgress>>() ?? []) {
				try {
					handler(progress);
				} catch (Exception ex) {
					Log.Error(ex, "ModInstaller: Extraction progress subscriber failed.");
				}
			}
		}
		#endregion

		#region Single Archive Processing

		/// <summary>
		/// Processes a single archive from extraction through copy into the Modules folder.
		/// </summary>
		private async Task<ModInstallResult> ProcessSingleArchiveAsync(
			string archivePath,
			string archiveFileName,
			string modulesPath,
			CancellationToken token,
			Action<int>? onFileExtracted = null) {

			ModInstallResult result = new() { ArchiveFileName = archiveFileName };
			string? tempDir = null;

			try {
				// Open, validate entry containment, then extract with per-file progress.
				ArchiveExtractionResult extractionResult = await ModExtractor.ExtractToTempResultAsync(
					archivePath, token, onFileExtracted);
				tempDir = extractionResult.TempDirectory;
				Log.Debug(
					"ModInstaller: Archive {ArchivePath} extracted to {TempDirectory}.",
					archivePath,
					tempDir ?? "<null>");
				if (!extractionResult.Success || tempDir is null) {
					result.Status = ModInstallStatus.Failed;
					result.Message = string.IsNullOrWhiteSpace(extractionResult.Message)
						? "Failed to extract archive."
						: extractionResult.Message;
					return result;
				}

				string[] subModuleXmlFiles = Directory.GetFiles(
					tempDir, "SubModule.xml", SearchOption.AllDirectories);

				// ── BLSE fallback ──────────────────────────────────────────────
				// If no SubModule.xml was found, check if this is a BLSE archive.
				// BLSE is not a standard Bannerlord module — it ships as exes/DLLs
				// that go into the game's bin directory, not the Modules folder.
				if (subModuleXmlFiles.Length == 0) {
					if (BLSEInstaller.IsBLSEArchive(tempDir)) {
						Log.Debug("ModInstaller: BLSE fallback triggered for {TempDirectory}.", tempDir);
						return await ProcessBLSEInstallAsync(tempDir, archiveFileName, token);
					}

					// Not a mod and not BLSE — genuinely invalid archive
					result.Status = ModInstallStatus.Failed;
					result.Message = "No SubModule.xml found in archive. Not a valid Bannerlord mod.";
					return result;
				}
				// ── End BLSE fallback ──────────────────────────────────────────

				if (subModuleXmlFiles.Length != 1) {
					result.Status = ModInstallStatus.Failed;
					result.Message = $"Module preflight blocked: archive contains {subModuleXmlFiles.Length} SubModule.xml files; exactly one is required.";
					return result;
				}

				ModuleInstallPreflightResult preflight = PreflightModuleInstall(
					tempDir, modulesPath, subModuleXmlFiles[0]);
				if (!preflight.Success || preflight.Module is null) {
					result.Status = ModInstallStatus.Failed;
					result.Message = preflight.Message;
					Log.Warning("ModInstaller: {PreflightMessage}", preflight.Message);
					return result;
				}

				ModuleModel newMod = preflight.Module;
				string modRoot = preflight.ModuleRootPath;

				result.ModuleId = newMod.ModuleId!;
				result.ModuleName = newMod.ModuleName!;
				result.InstalledVersion = newMod.ModuleVersion!;

				string targetPath = preflight.TargetPath;
				Log.Debug(
					"ModInstaller: Target module folder {ModuleFolderName} at {TargetPath}.",
					preflight.ModuleFolderName,
					targetPath);

				// Check if mod is already installed — version comparison
				VersionCheckOutcome versionOutcome = CheckExistingVersion(newMod, preflight.ExistingModule);
				result.PreviousVersion = versionOutcome.ExistingVersion;

				Log.Debug(
					"ModInstaller: Version check outcome {VersionAction}; existing: {ExistingVersion}; new: {NewVersion}.",
					versionOutcome.Action,
					versionOutcome.ExistingVersion ?? "<none>",
					newMod.ModuleVersion);

				switch (versionOutcome.Action) {
					case VersionAction.Skip:
						result.Status = ModInstallStatus.Skipped;
						result.Message = $"Already installed with same or newer version ({versionOutcome.ExistingVersion}).";
						Log.Information(
							"ModInstaller: Skipped {ModuleName} — {ResultMessage}",
							newMod.ModuleName,
							result.Message);
						return result;

					case VersionAction.Upgrade:
						// Do not copy over a partially deleted previous installation.
						if (!SafeDeleteDirectory(targetPath)) {
							result.Status = ModInstallStatus.Failed;
							result.Message = "Upgrade blocked because the existing module folder could not be removed completely.";
							return result;
						}
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
				Log.Information(
					"ModInstaller: {InstallStatus} {ModuleName} ({ModuleVersion}) to {TargetPath}.",
					result.Status,
					newMod.ModuleName,
					newMod.ModuleVersion,
					targetPath);
				Log.Debug(
					"ModInstaller: Copy complete to {TargetPath} with status {InstallStatus}.",
					targetPath,
					result.Status);
				return result;

			} catch (OperationCanceledException) {
				result.Status = ModInstallStatus.Failed;
				result.Message = "Installation was cancelled.";
				throw;
			} catch (Exception ex) {
				result.Status = ModInstallStatus.Failed;
				result.Message = $"Error: {ex.Message}";
				Log.Error(ex, "ModInstaller: Failed to install {ArchiveFileName}.", archiveFileName);
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

			Log.Information(
				"ModInstaller: Detected BLSE archive in {ArchiveFileName}. Delegating to BLSEInstaller.",
				archiveFileName);
			Log.Debug(
				"ModInstaller: Processing BLSE install from {ArchiveFileName} in {TempDirectory}.",
				archiveFileName,
				tempDir);

			BLSEInstallResult blseResult = await BLSEInstaller.InstallAsync(tempDir, _appConfig, token);

			ModInstallResult result = new() {
				ArchiveFileName = archiveFileName,
				ModuleId = "BLSE",
				ModuleName = "Bannerlord Software Extender (BLSE)"
			};

			if (blseResult.Success) {
				result.Status = ModInstallStatus.Installed;
				result.Message = blseResult.Message;
				Log.Information("BLSEInstaller: BLSE installed successfully from {ArchiveFileName}.", archiveFileName);
				Log.Debug(
					"ModInstaller: BLSE install succeeded for {ArchiveFileName}: {ResultMessage}",
					archiveFileName,
					blseResult.Message);
			} else {
				result.Status = ModInstallStatus.Failed;
				result.Message = blseResult.Message;
				Log.Warning(
					"BLSEInstaller: BLSE installation failed from {ArchiveFileName}: {ResultMessage}",
					archiveFileName,
					blseResult.Message);
				Log.Debug(
					"ModInstaller: BLSE install failed for {ArchiveFileName}: {ResultMessage}",
					archiveFileName,
					blseResult.Message);
			}

			return result;
		}

		#endregion

		#region Module Preflight

		private static ModuleInstallPreflightResult PreflightModuleInstall(
			string extractedDir,
			string modulesPath,
			string subModuleXmlPath) {
			try {
				string extractionRoot = Path.GetFullPath(extractedDir)
					.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				string moduleRoot = Path.GetFullPath(
					Path.GetDirectoryName(subModuleXmlPath) ?? string.Empty)
					.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

				if (!IsPathAtOrBelowRoot(moduleRoot, extractionRoot)) {
					return PreflightFailure("Module preflight blocked: SubModule.xml resolves outside the extraction directory.");
				}

				string moduleFolderName = new DirectoryInfo(moduleRoot).Name;
				if (string.IsNullOrWhiteSpace(moduleFolderName)) {
					return PreflightFailure("Module preflight blocked: module folder name could not be resolved.");
				}

				ModuleModel? module = ModParser.Parse(subModuleXmlPath, moduleRoot);
				if (module is null || string.IsNullOrWhiteSpace(module.ModuleId)) {
					return PreflightFailure("Module preflight blocked: SubModule.xml could not provide a valid module identity.");
				}

				string modulesRoot = Path.GetFullPath(modulesPath)
					.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				string targetPath = Path.GetFullPath(Path.Combine(modulesRoot, moduleFolderName))
					.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				if (!IsPathBelowRoot(targetPath, modulesRoot)) {
					return PreflightFailure("Module preflight blocked: target folder resolves outside the configured Modules directory.");
				}

				if (File.Exists(targetPath)) {
					return PreflightFailure("Module preflight blocked: target path is an existing file, not a module folder.");
				}

				ModuleModel? existingModule = null;
				if (Directory.Exists(targetPath)) {
					string existingXmlPath = Path.Combine(targetPath, "SubModule.xml");
					if (!File.Exists(existingXmlPath)) {
						return PreflightFailure("Module preflight blocked: the existing target folder has no SubModule.xml and will not be overwritten automatically.");
					}

					existingModule = ModParser.Parse(existingXmlPath, targetPath);
					if (existingModule is null || string.IsNullOrWhiteSpace(existingModule.ModuleId)) {
						return PreflightFailure("Module preflight blocked: the existing target folder has an unknown module identity.");
					}

					if (!string.Equals(module.ModuleId, existingModule.ModuleId, StringComparison.OrdinalIgnoreCase)) {
						return PreflightFailure(
							$"Module preflight blocked: archive module id '{module.ModuleId}' does not match existing module id '{existingModule.ModuleId}'.");
					}
				}

				return new ModuleInstallPreflightResult {
					Success = true,
					ModuleRootPath = moduleRoot,
					ModuleFolderName = moduleFolderName,
					SubModuleXmlPath = subModuleXmlPath,
					TargetPath = targetPath,
					Module = module,
					ExistingModule = existingModule
				};
			} catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException or UnauthorizedAccessException) {
				return PreflightFailure($"Module preflight blocked: {ex.Message}");
			}
		}

		private static ModuleInstallPreflightResult PreflightFailure(string message) => new() {
			Success = false,
			Message = message
		};

		private static bool IsPathAtOrBelowRoot(string candidatePath, string rootPath) {
			return string.Equals(candidatePath, rootPath, StringComparison.OrdinalIgnoreCase) ||
				IsPathBelowRoot(candidatePath, rootPath);
		}

		private static bool IsPathBelowRoot(string candidatePath, string rootPath) {
			string rootPrefix = rootPath + Path.DirectorySeparatorChar;
			return candidatePath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase);
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
		private static VersionCheckOutcome CheckExistingVersion(
			ModuleModel newMod,
			ModuleModel? existingMod) {
			if (existingMod is null) {
				return new VersionCheckOutcome { Action = VersionAction.Install };
			}

			if (string.IsNullOrWhiteSpace(existingMod.ModuleVersion)) {
				return new VersionCheckOutcome { Action = VersionAction.Install };
			}

			int comparison = CompareModVersions(newMod.ModuleVersion!, existingMod.ModuleVersion);

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
		/// Compares two module version strings and returns ordering semantics.
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

			Log.Debug(
				"ModInstaller: Version parse fallback for new version {NewVersion} ({CleanNewVersion}) and existing version {ExistingVersion} ({CleanExistingVersion}).",
				newVersion,
				cleanNew,
				existingVersion,
				cleanExisting);
			// Fallback to string comparison if parsing fails
			return string.Compare(cleanNew, cleanExisting, StringComparison.OrdinalIgnoreCase);
		}

		#endregion

		#region File System Helpers

		/// <summary>
		/// Copies a directory tree to a target path, honoring cancellation.
		/// </summary>
		private static void CopyDirectory(string sourceDir, string targetDir, CancellationToken token) {
			Log.Debug(
				"ModInstaller: Copying directory from {SourceDirectory} to {TargetDirectory}.",
				sourceDir,
				targetDir);
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

			Log.Debug(
				"ModInstaller: Directory copy complete from {SourceDirectory} to {TargetDirectory}.",
				sourceDir,
				targetDir);
		}

		/// <summary>
		/// Deletes a directory and all its contents, returning whether the target
		/// is confirmed absent. Logs and returns false instead of throwing.
		/// </summary>
		private static bool SafeDeleteDirectory(string path) {
			try {
				if (Directory.Exists(path)) {
					Directory.Delete(path, recursive: true);
				}
				return !Directory.Exists(path);
			} catch (Exception ex) {
				Log.Warning(ex, "ModInstaller: Failed to delete directory {DirectoryPath}.", path);
				return false;
			}
		}

		#endregion
	}
}
