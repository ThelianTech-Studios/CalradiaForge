namespace CalradiaForge.Core.Infra.Mods;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Paths;
using CalradiaForge.Core.Models;

using Serilog;

/// <summary>
/// Core-owned sequencing boundary for scan admission, completeness decisions,
/// cache commit authorization, accepted snapshot publication, installation
/// admission, cancellation, and application-shutdown quiescence.
/// </summary>
public sealed class ModPipelineManager : IModInstallOperationNotificationSource {
	private static readonly TimeSpan _consistencyFinalizationTimeout = TimeSpan.FromSeconds(15);
	private readonly object _stateLock = new();
	private readonly AppSettings _appConfig;
	private readonly ModsData _modsData;
	private readonly ModInstaller _modInstaller;
	private readonly IModScanner _modScanner;
	private readonly IModuleUnblocker _moduleUnblocker;
	private readonly Func<CancellationToken, Task>? _beforeCommit;
	private AcceptedModSnapshot _acceptedSnapshot = AcceptedModSnapshot.Empty;
	private TaskCompletionSource<bool>? _activeCompletion;
	private CancellationTokenSource? _activeCancellation;
	private Guid _activeOperationId;
	private IReadOnlyList<Guid> _activeArchiveIds = [];
	private bool _installProgressObserved;
	private bool _acceptingNewWork = true;
	private bool _cancellationRequested;
	private bool _commitStarted;
	private ModPipelineOperation _activeOperation;

	public ModPipelineManager(
		AppSettings appConfig,
		ModsData modsData,
		ModInstaller modInstaller,
		IModScanner? modScanner = null,
		IModuleUnblocker? moduleUnblocker = null)
		: this(appConfig, modsData, modInstaller, modScanner, moduleUnblocker, null) { }

	internal ModPipelineManager(
		AppSettings appConfig,
		ModsData modsData,
		ModInstaller modInstaller,
		IModScanner? modScanner,
		Func<CancellationToken, Task>? beforeCommit)
		: this(appConfig, modsData, modInstaller, modScanner, null, beforeCommit) { }

	internal ModPipelineManager(
		AppSettings appConfig,
		ModsData modsData,
		ModInstaller modInstaller,
		IModScanner? modScanner,
		IModuleUnblocker? moduleUnblocker,
		Func<CancellationToken, Task>? beforeCommit) {
		_appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
		_modsData = modsData ?? throw new ArgumentNullException(nameof(modsData));
		_modInstaller = modInstaller ?? throw new ArgumentNullException(nameof(modInstaller));
		_modScanner = modScanner ?? new ModScanner();
		_moduleUnblocker = moduleUnblocker ?? new ModuleUnblocker();
		_beforeCommit = beforeCommit;
		_modInstaller.ExtractionProgressChanged += RelayExtractionProgress;
		_modInstaller.InstallProgressChanged += RelayArchiveCompletion;
	}

	public event Action<ModInstallProgress>? InstallProgressChanged;
	public event Action<ModInstallOperationResult>? InstallOperationCompleted;

	public AcceptedModSnapshot AcceptedSnapshot => Volatile.Read(ref _acceptedSnapshot);
	public bool IsRefreshing => ActiveOperation is ModPipelineOperation.StartupScan or ModPipelineOperation.RefreshScan;
	public bool IsInstalling => ActiveOperation == ModPipelineOperation.Install;
	public bool HasActiveWork => ActiveOperation != ModPipelineOperation.None;
	public bool IsAcceptingNewWork { get { lock (_stateLock) { return _acceptingNewWork; } } }
	public ModPipelineOperation ActiveOperation { get { lock (_stateLock) { return _activeOperation; } } }
	public Guid ActiveOperationId { get { lock (_stateLock) { return _activeOperationId; } } }

	/// <summary>
	/// Loads the current and backup caches into one accepted startup snapshot.
	/// This does not represent a filesystem scan commit.
	/// </summary>
	public AcceptedModSnapshot LoadAcceptedCache() {
		lock (_stateLock) {
			if (_activeOperation != ModPipelineOperation.None) {
				throw new InvalidOperationException("Cannot load the startup cache while mod-pipeline work is active.");
			}
			IReadOnlyList<ModuleModel> current = _modsData.LoadCurrent().ToArray();
			IReadOnlyList<ModuleModel> previous = _modsData.LoadBackup().ToArray();
			(IReadOnlyList<ModuleModel> added, IReadOnlyList<ModuleModel> removed) = DetectChanges(previous, current);
			AcceptedModSnapshot snapshot = new(_acceptedSnapshot.Version + 1, current, added, removed);
			Volatile.Write(ref _acceptedSnapshot, snapshot);
			return snapshot;
		}
	}

	public Task<ModPipelineResult> InitializeForStartupAsync(CancellationToken token = default) =>
		RunScanAsync(ModPipelineOperation.StartupScan, token);

	public Task<ModPipelineResult> RefreshAsync(CancellationToken token = default) =>
		RunScanAsync(ModPipelineOperation.RefreshScan, token);

	/// <summary>
	/// Runs the authoritative Core install pipeline: validation/admission,
	/// installer mechanics, required DLL maintenance, accepted-state
	/// reconciliation, terminal publication, and quiescence release.
	/// </summary>
	public async Task<ModInstallOperationResult> InstallAsync(
		string[]? archivePaths,
		CancellationToken token = default) {
		Guid operationId = Guid.NewGuid();
		OperationAdmissionResult availability = GetAdmissionAvailability();
		if (availability != OperationAdmissionResult.Admitted) {
			return PublishRejectedInstall(operationId, availability);
		}

		if (!TryValidateInstallRequest(archivePaths, out ModInstallDiagnosticCode validationCode, out string validationDiagnostic)) {
			return PublishTerminal(new ModInstallOperationResult {
				OperationId = operationId,
				Status = ModInstallOperationStatus.ValidationFailed,
				AcceptedSnapshot = AcceptedSnapshot,
				DiagnosticCodes = [validationCode],
				TechnicalDiagnostics = [validationDiagnostic]
			});
		}

		OperationAdmissionResult admission = TryBeginOperation(
			ModPipelineOperation.Install,
			operationId,
			archivePaths!,
			token,
			out CancellationToken operationToken);
		if (admission != OperationAdmissionResult.Admitted) {
			return PublishRejectedInstall(operationId, admission);
		}

		ModInstallOperationResult? terminal = null;
		ModInstallSummary summary = new();
		Exception? installerException = null;
		bool cancellationObserved = false;
		UnblockResult? unblockResult = null;
		ModPipelineResult? reconciliationResult = null;
		List<ModInstallDiagnosticCode> diagnosticCodes = [];
		List<string> technicalDiagnostics = [];

		try {
			PublishProgress(new ModInstallProgress {
				OperationId = operationId,
				Stage = ModInstallProgressStage.OperationStarted,
				TotalArchives = archivePaths!.Length,
				TimestampUtc = DateTime.UtcNow
			});

			try {
				ModInstallSummary? installerSummary =
					await _modInstaller.InstallAsync(archivePaths, operationToken).ConfigureAwait(false);
				summary = SnapshotSummary(installerSummary);
				if (installerSummary is null && !operationToken.IsCancellationRequested) {
					diagnosticCodes.Add(ModInstallDiagnosticCode.InstallerAdmissionInvariantViolation);
					technicalDiagnostics.Add(
						"ModInstaller rejected work after ModPipelineManager admitted the operation.");
				}
			} catch (OperationCanceledException) when (operationToken.IsCancellationRequested) {
				cancellationObserved = true;
				summary = SnapshotSummary(
					InstallProgressWasObserved(operationId) ? _modInstaller.LastSummary : null);
			} catch (Exception ex) {
				installerException = ex;
				summary = SnapshotSummary(
					InstallProgressWasObserved(operationId) ? _modInstaller.LastSummary : null);
				diagnosticCodes.Add(ModInstallDiagnosticCode.InstallerFailed);
				technicalDiagnostics.Add(ex.ToString());
				Log.Error(ex, "ModPipelineManager: Installer execution failed for operation {OperationId}.", operationId);
			}

			cancellationObserved |= operationToken.IsCancellationRequested;
			bool hasStandardModuleChanges = HasStandardModuleChanges(summary);
			bool needsConsistencyFinalization = hasStandardModuleChanges
				|| (cancellationObserved && InstallProgressWasObserved(operationId))
				|| (installerException is not null && InstallProgressWasObserved(operationId));

			if (needsConsistencyFinalization) {
				using CancellationTokenSource finalizationCancellation =
					new(_consistencyFinalizationTimeout);
				CancellationToken finalizationToken = finalizationCancellation.Token;

				PublishProgress(new ModInstallProgress {
					OperationId = operationId,
					Stage = ModInstallProgressStage.Unblocking,
					TotalArchives = archivePaths.Length,
					ProcessedArchiveCount = summary.TotalCount,
					TimestampUtc = DateTime.UtcNow
				});
				try {
					unblockResult = await _moduleUnblocker.UnblockModulesAsync(
						_appConfig.ModulesDirectoryPath,
						finalizationToken).ConfigureAwait(false);
					if (!unblockResult.Succeeded) {
						diagnosticCodes.Add(ModInstallDiagnosticCode.UnblockFailed);
						if (!string.IsNullOrWhiteSpace(unblockResult.TechnicalDiagnostic)) {
							technicalDiagnostics.Add(unblockResult.TechnicalDiagnostic);
						}
					} else if (unblockResult.FailedCount > 0) {
						diagnosticCodes.Add(ModInstallDiagnosticCode.UnblockCompletedWithFailures);
						technicalDiagnostics.Add(
							$"{unblockResult.FailedCount} file(s) could not be unblocked.");
					}
				} catch (OperationCanceledException) when (finalizationToken.IsCancellationRequested) {
					diagnosticCodes.Add(ModInstallDiagnosticCode.FinalizationTimedOut);
					technicalDiagnostics.Add(
						$"Install consistency finalization exceeded {_consistencyFinalizationTimeout.TotalSeconds:0} seconds.");
				} catch (Exception ex) {
					unblockResult = new UnblockResult {
						Succeeded = false,
						TechnicalDiagnostic = ex.ToString()
					};
					diagnosticCodes.Add(ModInstallDiagnosticCode.UnblockFailed);
					technicalDiagnostics.Add(ex.ToString());
					Log.Error(ex, "ModPipelineManager: Modules DLL unblocking failed for operation {OperationId}.", operationId);
				}

				if (!finalizationToken.IsCancellationRequested) {
					PublishProgress(new ModInstallProgress {
						OperationId = operationId,
						Stage = ModInstallProgressStage.Reconciling,
						TotalArchives = archivePaths.Length,
						ProcessedArchiveCount = summary.TotalCount,
						TimestampUtc = DateTime.UtcNow
					});
					try {
						reconciliationResult = await ExecuteScanAndCommitAsync(
							ModPipelineOperation.Install,
							finalizationToken,
							allowCommitAfterCancellation: true).ConfigureAwait(false);
						AddReconciliationDiagnostics(reconciliationResult, diagnosticCodes, technicalDiagnostics);
					} catch (OperationCanceledException) when (finalizationToken.IsCancellationRequested) {
						diagnosticCodes.Add(ModInstallDiagnosticCode.FinalizationTimedOut);
						technicalDiagnostics.Add(
							$"Install consistency finalization exceeded {_consistencyFinalizationTimeout.TotalSeconds:0} seconds.");
					} catch (Exception ex) {
						diagnosticCodes.Add(ModInstallDiagnosticCode.ReconciliationFailed);
						technicalDiagnostics.Add(ex.ToString());
						Log.Error(ex, "ModPipelineManager: Install reconciliation failed for operation {OperationId}.", operationId);
					}
				}
			}

			cancellationObserved |= operationToken.IsCancellationRequested;
			if (cancellationObserved) {
				diagnosticCodes.Add(ModInstallDiagnosticCode.Cancelled);
			}
			if (summary.TotalCount == 0
				&& installerException is null
				&& !cancellationObserved
				&& !diagnosticCodes.Contains(ModInstallDiagnosticCode.InstallerAdmissionInvariantViolation)) {
				diagnosticCodes.Add(ModInstallDiagnosticCode.InstallerReturnedNoResults);
				technicalDiagnostics.Add("The installer completed without producing an archive result.");
			}

			terminal = new ModInstallOperationResult {
				OperationId = operationId,
				Status = ClassifyInstall(
					summary,
					unblockResult,
					reconciliationResult,
					cancellationObserved,
					installerException,
					diagnosticCodes),
				Summary = summary,
				UnblockResult = SnapshotUnblockResult(unblockResult),
				ReconciliationResult = reconciliationResult,
				AcceptedSnapshot = AcceptedSnapshot,
				DiagnosticCodes = diagnosticCodes.Distinct().ToArray(),
				TechnicalDiagnostics = technicalDiagnostics.ToArray()
			};
			return PublishTerminal(terminal);
		} catch (Exception ex) {
			Log.Error(ex, "ModPipelineManager: Unexpected install-pipeline failure for operation {OperationId}.", operationId);
			terminal = new ModInstallOperationResult {
				OperationId = operationId,
				Status = ModInstallOperationStatus.Failed,
				Summary = SnapshotSummary(_modInstaller.LastSummary),
				AcceptedSnapshot = AcceptedSnapshot,
				DiagnosticCodes = [ModInstallDiagnosticCode.InstallerFailed],
				TechnicalDiagnostics = [ex.ToString()]
			};
			return PublishTerminal(terminal);
		} finally {
			CompleteOperation(ModPipelineOperation.Install, operationId);
		}
	}

	public bool ValidateInstallGameDirectory(out string errorMessage) =>
		_modInstaller.ValidateGameDirectory(out errorMessage);

	/// <summary>
	/// Clears persisted and accepted cache state only when no operation is active.
	/// </summary>
	public bool ClearCache() {
		lock (_stateLock) {
			if (!_acceptingNewWork || _activeOperation != ModPipelineOperation.None) {
				return false;
			}
			if (!_modsData.ClearCache()) {
				return false;
			}
			AcceptedModSnapshot current = _acceptedSnapshot;
			Volatile.Write(ref _acceptedSnapshot, new AcceptedModSnapshot(
				current.Version + 1,
				[],
				[],
				current.Modules.ToArray()));
			return true;
		}
	}

	/// <summary>
	/// Permanently stops new pipeline admission for the current application lifetime.
	/// </summary>
	public void StopAcceptingNewWork() {
		lock (_stateLock) {
			_acceptingNewWork = false;
		}
	}

	/// <summary>
	/// Cooperatively cancels the active scan or installation, if any.
	/// </summary>
	public void RequestCancellation() {
		CancellationTokenSource? cancellation;
		ModPipelineOperation operation;
		lock (_stateLock) {
			if (_activeOperation == ModPipelineOperation.None || _commitStarted) {
				return;
			}
			_cancellationRequested = true;
			cancellation = _activeCancellation;
			operation = _activeOperation;
		}
		if (cancellation is { IsCancellationRequested: false }) {
			try {
				cancellation.Cancel();
			} catch (ObjectDisposedException) {
				// Operation completion won the race; quiescence is already reached.
			}
		}
		if (operation == ModPipelineOperation.Install) {
			_modInstaller.CancelInstall();
		}
	}

	/// <summary>
	/// Awaits completion of work active at each observation point. Call
	/// <see cref="StopAcceptingNewWork"/> first to make the boundary final.
	/// </summary>
	public async Task WaitForQuiescenceAsync(CancellationToken token = default) {
		while (true) {
			Task completion;
			lock (_stateLock) {
				if (_activeOperation == ModPipelineOperation.None || _activeCompletion is null) {
					return;
				}
				completion = _activeCompletion.Task;
			}
			await completion.WaitAsync(token);
		}
	}

	private async Task<ModPipelineResult> RunScanAsync(ModPipelineOperation operation, CancellationToken token) {
		Guid operationId = Guid.NewGuid();
		OperationAdmissionResult admission = TryBeginOperation(
			operation,
			operationId,
			[],
			token,
			out CancellationToken operationToken);
		if (admission != OperationAdmissionResult.Admitted) {
			return RejectedResult(
				operation,
				admission == OperationAdmissionResult.AdmissionStopped
					? ModPipelineStatus.AdmissionStopped
					: ModPipelineStatus.Busy,
				admission == OperationAdmissionResult.AdmissionStopped
					? "Mod operations are stopping."
					: "Another mod operation is already active.");
		}

		try {
			return await ExecuteScanAndCommitAsync(
				operation,
				operationToken,
				allowCommitAfterCancellation: false).ConfigureAwait(false);
		} finally {
			CompleteOperation(operation, operationId);
		}
	}

	private async Task<ModPipelineResult> ExecuteScanAndCommitAsync(
		ModPipelineOperation operation,
		CancellationToken operationToken,
		bool allowCommitAfterCancellation) {
		try {
			if (_appConfig.GameProvider is GameProvider.NotInitialized or GameProvider.ManualConfiguration
				|| !GamePathValidator.ValidateGameFolder(_appConfig.GameFolderPath)) {
				return RejectedResult(operation, ModPipelineStatus.InvalidConfiguration,
					"Game configuration is invalid; the previous module snapshot was preserved.");
			}

			ModScanResult scan = await _modScanner.ScanAsync(_appConfig, operationToken);
			operationToken.ThrowIfCancellationRequested();
			if (scan.IsCancelled || operationToken.IsCancellationRequested) {
				return FromScan(operation, ModPipelineStatus.Cancelled, scan, false,
					"Module scan was cancelled; the previous snapshot was preserved.");
			}
			if (!scan.IsComplete) {
				return FromScan(operation, ModPipelineStatus.Incomplete, scan, false,
					"Module scan was incomplete; the previous snapshot was preserved.");
			}
			operationToken.ThrowIfCancellationRequested();
			if (_beforeCommit is not null) {
				await _beforeCommit(operationToken);
			}
			if (!TryBeginCommit(operation, allowCommitAfterCancellation)) {
				return FromScan(operation, ModPipelineStatus.Cancelled, scan, false,
					"Module scan was cancelled; the previous snapshot was preserved.");
			}

			AcceptedModSnapshot previous = AcceptedSnapshot;
			IReadOnlyList<ModuleModel> modules = scan.Modules.ToArray();
			(IReadOnlyList<ModuleModel> added, IReadOnlyList<ModuleModel> removed) =
				DetectChanges(previous.Modules, modules);

			// The coordinator authorizes these writes only after completeness is known.
			_modsData.RotateDataFiles();
			_modsData.SaveCurrent(modules.ToList());
			AcceptedModSnapshot accepted = new(previous.Version + 1, modules, added, removed);
			Volatile.Write(ref _acceptedSnapshot, accepted);

			Log.Information(
				"ModPipelineManager: Accepted snapshot v{SnapshotVersion} with {ModuleCount} modules.",
				accepted.Version,
				modules.Count);
			return new ModPipelineResult {
				Operation = operation,
				Status = ModPipelineStatus.Succeeded,
				IsComplete = true,
				WasCommitted = true,
				HasChanges = added.Count > 0 || removed.Count > 0,
				LocalRoot = scan.LocalRoot,
				WorkshopRoot = scan.WorkshopRoot,
				DiscoveredModuleCount = modules.Count,
				LocalModuleCount = scan.LocalModuleCount,
				WorkshopModuleCount = scan.WorkshopModuleCount,
				Warnings = scan.Warnings,
				DuplicateModuleIds = scan.DuplicateModuleIds,
				UserSummary = $"Found {modules.Count} installed modules.",
				TechnicalDiagnostic = string.Join("; ", scan.TechnicalDiagnostics),
				AcceptedSnapshot = accepted
			};
		} catch (OperationCanceledException) when (operationToken.IsCancellationRequested) {
			return RejectedResult(operation, ModPipelineStatus.Cancelled,
				"Module operation was cancelled; the previous snapshot was preserved.");
		} catch (Exception ex) {
			Log.Error(ex, "ModPipelineManager: Scan failed; accepted state was preserved.");
			return new ModPipelineResult {
				Operation = operation,
				Status = ModPipelineStatus.Failed,
				UserSummary = "Module scan failed; the previous snapshot was preserved.",
				TechnicalDiagnostic = ex.ToString(),
				AcceptedSnapshot = AcceptedSnapshot
			};
		}
	}

	private OperationAdmissionResult GetAdmissionAvailability() {
		lock (_stateLock) {
			if (!_acceptingNewWork) {
				return OperationAdmissionResult.AdmissionStopped;
			}
			return _activeOperation == ModPipelineOperation.None
				? OperationAdmissionResult.Admitted
				: OperationAdmissionResult.Busy;
		}
	}

	private OperationAdmissionResult TryBeginOperation(
		ModPipelineOperation operation,
		Guid operationId,
		IReadOnlyList<string> archivePaths,
		CancellationToken callerToken,
		out CancellationToken operationToken) {
		lock (_stateLock) {
			if (!_acceptingNewWork) {
				operationToken = default;
				return OperationAdmissionResult.AdmissionStopped;
			}
			if (_activeOperation != ModPipelineOperation.None) {
				operationToken = default;
				return OperationAdmissionResult.Busy;
			}
			_activeOperation = operation;
			_activeOperationId = operationId;
			_activeArchiveIds = archivePaths.Select(_ => Guid.NewGuid()).ToArray();
			_installProgressObserved = false;
			_activeCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			_activeCancellation = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
			_cancellationRequested = false;
			_commitStarted = false;
			operationToken = _activeCancellation.Token;
			return OperationAdmissionResult.Admitted;
		}
	}

	/// <summary>
	/// Linearizes cancellation against scan commit. Cancellation requested before
	/// this point wins; after it, the accepted complete result owns the short
	/// synchronous persistence/publication section and cancellation is a no-op.
	/// </summary>
	private bool TryBeginCommit(ModPipelineOperation operation, bool allowCommitAfterCancellation) {
		lock (_stateLock) {
			if (_activeOperation != operation
				|| _activeCancellation is null
				|| (!allowCommitAfterCancellation
					&& (_cancellationRequested || _activeCancellation.IsCancellationRequested))) {
				return false;
			}
			_commitStarted = true;
			return true;
		}
	}

	private void CompleteOperation(ModPipelineOperation operation, Guid operationId) {
		TaskCompletionSource<bool>? completion = null;
		CancellationTokenSource? cancellation = null;
		lock (_stateLock) {
			if (_activeOperation != operation || _activeOperationId != operationId) {
				return;
			}
			_activeOperation = ModPipelineOperation.None;
			_activeOperationId = Guid.Empty;
			_activeArchiveIds = [];
			_installProgressObserved = false;
			completion = _activeCompletion;
			cancellation = _activeCancellation;
			_activeCompletion = null;
			_activeCancellation = null;
			_cancellationRequested = false;
			_commitStarted = false;
		}
		cancellation?.Dispose();
		completion?.TrySetResult(true);
	}

	private bool TryValidateInstallRequest(
		string[]? archivePaths,
		out ModInstallDiagnosticCode diagnosticCode,
		out string technicalDiagnostic) {
		if (archivePaths is null || archivePaths.Length == 0) {
			diagnosticCode = ModInstallDiagnosticCode.ArchiveSelectionEmpty;
			technicalDiagnostic = "At least one archive path is required.";
			return false;
		}
		if (string.IsNullOrWhiteSpace(_appConfig.GameFolderPath)) {
			diagnosticCode = ModInstallDiagnosticCode.GameDirectoryNotConfigured;
			technicalDiagnostic = "The game directory is not configured.";
			return false;
		}
		if (!Directory.Exists(_appConfig.GameFolderPath)) {
			diagnosticCode = ModInstallDiagnosticCode.GameDirectoryMissing;
			technicalDiagnostic = $"The configured game directory does not exist: '{_appConfig.GameFolderPath}'.";
			return false;
		}
		if (string.IsNullOrWhiteSpace(_appConfig.ModulesDirectoryPath)
			|| !Directory.Exists(_appConfig.ModulesDirectoryPath)) {
			diagnosticCode = ModInstallDiagnosticCode.ModulesDirectoryMissing;
			technicalDiagnostic = $"The configured Modules directory does not exist: '{_appConfig.ModulesDirectoryPath}'.";
			return false;
		}
		diagnosticCode = ModInstallDiagnosticCode.None;
		technicalDiagnostic = string.Empty;
		return true;
	}

	private ModInstallOperationResult PublishRejectedInstall(
		Guid operationId,
		OperationAdmissionResult admission) {
		bool stopped = admission == OperationAdmissionResult.AdmissionStopped;
		return PublishTerminal(new ModInstallOperationResult {
			OperationId = operationId,
			Status = stopped
				? ModInstallOperationStatus.RejectedAdmissionStopped
				: ModInstallOperationStatus.RejectedBusy,
			AcceptedSnapshot = AcceptedSnapshot,
			DiagnosticCodes = [
				stopped
					? ModInstallDiagnosticCode.AdmissionStopped
					: ModInstallDiagnosticCode.Busy
			],
			TechnicalDiagnostics = [
				stopped
					? "Mod-pipeline admission has stopped."
					: "Another mod-pipeline operation is active."
			]
		});
	}

	private ModInstallOperationResult PublishTerminal(ModInstallOperationResult result) {
		foreach (Action<ModInstallOperationResult> handler in
			InstallOperationCompleted?.GetInvocationList().Cast<Action<ModInstallOperationResult>>() ?? []) {
			try {
				handler(result);
			} catch (Exception ex) {
				Log.Error(ex, "ModPipelineManager: Install terminal-result subscriber failed.");
			}
		}
		return result;
	}

	private void PublishProgress(ModInstallProgress progress) {
		foreach (Action<ModInstallProgress> handler in
			InstallProgressChanged?.GetInvocationList().Cast<Action<ModInstallProgress>>() ?? []) {
			try {
				handler(progress);
			} catch (Exception ex) {
				Log.Error(ex, "ModPipelineManager: Install progress subscriber failed.");
			}
		}
	}

	private void RelayExtractionProgress(ExtractionProgress progress) {
		Guid operationId;
		Guid archiveId;
		lock (_stateLock) {
			if (_activeOperation != ModPipelineOperation.Install
				|| _activeOperationId == Guid.Empty
				|| progress.ArchiveIndex < 1
				|| progress.ArchiveIndex > _activeArchiveIds.Count) {
				return;
			}
			operationId = _activeOperationId;
			archiveId = _activeArchiveIds[progress.ArchiveIndex - 1];
			_installProgressObserved = true;
		}
		PublishProgress(new ModInstallProgress {
			OperationId = operationId,
			ArchiveId = archiveId,
			Stage = ModInstallProgressStage.Extracting,
			ArchiveFileName = progress.ArchiveFileName,
			ArchiveIndex = progress.ArchiveIndex,
			TotalArchives = progress.TotalArchives,
			CurrentEntry = progress.CurrentEntry,
			BatchFilesProcessed = progress.BatchFilesExtracted,
			EstimatedTotalFiles = progress.EstimatedTotalFiles,
			TimestampUtc = progress.TimestampUtc
		});
	}

	private void RelayArchiveCompletion(ModInstallSummary liveSummary) {
		Guid operationId;
		Guid archiveId;
		int totalArchives;
		lock (_stateLock) {
			if (_activeOperation != ModPipelineOperation.Install
				|| _activeOperationId == Guid.Empty
				|| liveSummary.TotalCount < 1
				|| liveSummary.TotalCount > _activeArchiveIds.Count) {
				return;
			}
			operationId = _activeOperationId;
			archiveId = _activeArchiveIds[liveSummary.TotalCount - 1];
			totalArchives = _activeArchiveIds.Count;
			_installProgressObserved = true;
		}
		ModInstallSummary summary = SnapshotSummary(liveSummary);
		ModInstallResult? archiveResult = summary.Results.LastOrDefault();
		PublishProgress(new ModInstallProgress {
			OperationId = operationId,
			ArchiveId = archiveId,
			Stage = ModInstallProgressStage.ArchiveCompleted,
			ArchiveFileName = archiveResult?.ArchiveFileName ?? string.Empty,
			ArchiveIndex = summary.TotalCount,
			TotalArchives = totalArchives,
			ProcessedArchiveCount = summary.TotalCount,
			InstalledCount = summary.InstalledCount,
			UpgradedCount = summary.UpgradedCount,
			SkippedCount = summary.SkippedCount,
			FailedCount = summary.FailedCount,
			TimestampUtc = DateTime.UtcNow
		});
	}

	private bool InstallProgressWasObserved(Guid operationId) {
		lock (_stateLock) {
			return _activeOperation == ModPipelineOperation.Install
				&& _activeOperationId == operationId
				&& _installProgressObserved;
		}
	}

	private static ModInstallSummary SnapshotSummary(ModInstallSummary? source) => new() {
		Results = source?.Results.Select(result => new ModInstallResult {
			ArchiveFileName = result.ArchiveFileName,
			ModuleId = result.ModuleId,
			ModuleName = result.ModuleName,
			Status = result.Status,
			Message = result.Message,
			InstalledVersion = result.InstalledVersion,
			PreviousVersion = result.PreviousVersion,
			ExtractedFileCount = result.ExtractedFileCount
		}).ToList() ?? []
	};

	private static UnblockResult? SnapshotUnblockResult(UnblockResult? source) => source is null
		? null
		: new UnblockResult {
			Succeeded = source.Succeeded,
			UnblockedCount = source.UnblockedCount,
			FailedCount = source.FailedCount,
			FailedFiles = source.FailedFiles.ToList(),
			TechnicalDiagnostic = source.TechnicalDiagnostic
		};

	private static bool HasStandardModuleChanges(ModInstallSummary summary) =>
		summary.Results.Any(result =>
			!string.Equals(result.ModuleId, "BLSE", StringComparison.OrdinalIgnoreCase)
			&& result.Status is ModInstallStatus.Installed or ModInstallStatus.Upgraded);

	private static ModInstallOperationStatus ClassifyInstall(
		ModInstallSummary summary,
		UnblockResult? unblockResult,
		ModPipelineResult? reconciliationResult,
		bool cancellationObserved,
		Exception? installerException,
		IReadOnlyCollection<ModInstallDiagnosticCode> diagnosticCodes) {
		if (installerException is not null
			|| diagnosticCodes.Contains(ModInstallDiagnosticCode.InstallerAdmissionInvariantViolation)
			|| diagnosticCodes.Contains(ModInstallDiagnosticCode.ReconciliationFailed)) {
			return ModInstallOperationStatus.Failed;
		}
		if (cancellationObserved) {
			return ModInstallOperationStatus.Cancelled;
		}

		bool hasSuccess = summary.Results.Any(result =>
			result.Status is ModInstallStatus.Installed or ModInstallStatus.Upgraded);
		bool hasFailure = summary.Results.Any(result => result.Status == ModInstallStatus.Failed);
		bool hasWarning = summary.Results.Any(result => result.Status == ModInstallStatus.Skipped);
		if (diagnosticCodes.Contains(ModInstallDiagnosticCode.FinalizationTimedOut)
			|| diagnosticCodes.Contains(ModInstallDiagnosticCode.UnblockFailed)) {
			return hasSuccess
				? ModInstallOperationStatus.PartiallyFailed
				: ModInstallOperationStatus.Failed;
		}
		if (summary.TotalCount == 0) {
			return ModInstallOperationStatus.Failed;
		}

		ModInstallOperationStatus status = hasFailure
			? hasSuccess
				? ModInstallOperationStatus.PartiallyFailed
				: ModInstallOperationStatus.Failed
			: hasWarning
				? ModInstallOperationStatus.SucceededWithWarnings
				: ModInstallOperationStatus.Succeeded;

		if (reconciliationResult is { Success: false }) {
			return hasSuccess
				? ModInstallOperationStatus.PartiallyFailed
				: ModInstallOperationStatus.Failed;
		}
		if (unblockResult is { Succeeded: false }) {
			return hasSuccess
				? ModInstallOperationStatus.PartiallyFailed
				: ModInstallOperationStatus.Failed;
		}
		if (unblockResult is { FailedCount: > 0 }
			&& status == ModInstallOperationStatus.Succeeded) {
			return ModInstallOperationStatus.SucceededWithWarnings;
		}
		if (diagnosticCodes.Contains(ModInstallDiagnosticCode.UnblockCompletedWithFailures)
			&& status == ModInstallOperationStatus.Succeeded) {
			return ModInstallOperationStatus.SucceededWithWarnings;
		}
		if (diagnosticCodes.Contains(ModInstallDiagnosticCode.ReconciliationCompletedWithWarnings)
			&& status == ModInstallOperationStatus.Succeeded) {
			return ModInstallOperationStatus.SucceededWithWarnings;
		}
		return status;
	}

	private static void AddReconciliationDiagnostics(
		ModPipelineResult reconciliation,
		ICollection<ModInstallDiagnosticCode> diagnosticCodes,
		ICollection<string> technicalDiagnostics) {
		if (reconciliation.Success) {
			if (reconciliation.Warnings.Count > 0 || reconciliation.DuplicateModuleIds.Count > 0) {
				diagnosticCodes.Add(ModInstallDiagnosticCode.ReconciliationCompletedWithWarnings);
				foreach (string warning in reconciliation.Warnings) {
					if (!string.IsNullOrWhiteSpace(warning)) {
						technicalDiagnostics.Add(warning);
					}
				}
				if (reconciliation.DuplicateModuleIds.Count > 0) {
					technicalDiagnostics.Add(
						$"Reconciliation resolved duplicate module identifiers: {string.Join(", ", reconciliation.DuplicateModuleIds)}.");
				}
			}
			return;
		}
		diagnosticCodes.Add(reconciliation.Status == ModPipelineStatus.Incomplete
			? ModInstallDiagnosticCode.ReconciliationIncomplete
			: ModInstallDiagnosticCode.ReconciliationFailed);
		if (!string.IsNullOrWhiteSpace(reconciliation.TechnicalDiagnostic)) {
			technicalDiagnostics.Add(reconciliation.TechnicalDiagnostic);
		}
	}

	private enum OperationAdmissionResult {
		Admitted,
		Busy,
		AdmissionStopped
	}

	private ModPipelineResult RejectedResult(
		ModPipelineOperation operation,
		ModPipelineStatus status,
		string summary) => new() {
			Operation = operation,
			Status = status,
			UserSummary = summary,
			TechnicalDiagnostic = summary,
			AcceptedSnapshot = AcceptedSnapshot
		};

	private ModPipelineResult FromScan(
		ModPipelineOperation operation,
		ModPipelineStatus status,
		ModScanResult scan,
		bool committed,
		string summary) => new() {
			Operation = operation,
			Status = status,
			IsComplete = scan.IsComplete,
			WasCommitted = committed,
			LocalRoot = scan.LocalRoot,
			WorkshopRoot = scan.WorkshopRoot,
			DiscoveredModuleCount = scan.Modules.Count,
			LocalModuleCount = scan.LocalModuleCount,
			WorkshopModuleCount = scan.WorkshopModuleCount,
			Warnings = scan.Warnings,
			DuplicateModuleIds = scan.DuplicateModuleIds,
			UserSummary = summary,
			TechnicalDiagnostic = string.Join("; ", scan.TechnicalDiagnostics),
			AcceptedSnapshot = AcceptedSnapshot
		};

	private static (IReadOnlyList<ModuleModel> Added, IReadOnlyList<ModuleModel> Removed) DetectChanges(
		IReadOnlyList<ModuleModel> previous,
		IReadOnlyList<ModuleModel> current) {
		HashSet<string> previousIds = previous
			.Select(module => module.ModuleId)
			.Where(id => !string.IsNullOrWhiteSpace(id))
			.Cast<string>()
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
		HashSet<string> currentIds = current
			.Select(module => module.ModuleId)
			.Where(id => !string.IsNullOrWhiteSpace(id))
			.Cast<string>()
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
		return (
			current.Where(module => module.ModuleId is not null && !previousIds.Contains(module.ModuleId)).ToArray(),
			previous.Where(module => module.ModuleId is not null && !currentIds.Contains(module.ModuleId)).ToArray());
	}
}
