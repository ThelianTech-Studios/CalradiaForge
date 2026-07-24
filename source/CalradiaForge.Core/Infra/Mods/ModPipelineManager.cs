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
public sealed class ModPipelineManager {
	private readonly object _stateLock = new();
	private readonly AppSettings _appConfig;
	private readonly ModsData _modsData;
	private readonly ModInstaller _modInstaller;
	private readonly IModScanner _modScanner;
	private readonly Func<CancellationToken, Task>? _beforeCommit;
	private AcceptedModSnapshot _acceptedSnapshot = AcceptedModSnapshot.Empty;
	private TaskCompletionSource<bool>? _activeCompletion;
	private CancellationTokenSource? _activeCancellation;
	private bool _acceptingNewWork = true;
	private bool _cancellationRequested;
	private bool _commitStarted;
	private ModPipelineOperation _activeOperation;

	public ModPipelineManager(
		AppSettings appConfig,
		ModsData modsData,
		ModInstaller modInstaller,
		IModScanner? modScanner = null)
		: this(appConfig, modsData, modInstaller, modScanner, null) { }

	internal ModPipelineManager(
		AppSettings appConfig,
		ModsData modsData,
		ModInstaller modInstaller,
		IModScanner? modScanner,
		Func<CancellationToken, Task>? beforeCommit) {
		_appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
		_modsData = modsData ?? throw new ArgumentNullException(nameof(modsData));
		_modInstaller = modInstaller ?? throw new ArgumentNullException(nameof(modInstaller));
		_modScanner = modScanner ?? new ModScanner();
		_beforeCommit = beforeCommit;
	}

	public AcceptedModSnapshot AcceptedSnapshot => Volatile.Read(ref _acceptedSnapshot);
	public bool IsRefreshing => ActiveOperation is ModPipelineOperation.StartupScan or ModPipelineOperation.RefreshScan;
	public bool IsInstalling => _modInstaller.IsInstalling;
	public bool HasActiveWork => ActiveOperation != ModPipelineOperation.None;
	public bool IsAcceptingNewWork { get { lock (_stateLock) { return _acceptingNewWork; } } }
	public ModPipelineOperation ActiveOperation { get { lock (_stateLock) { return _activeOperation; } } }

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
	/// Runs an install through the authoritative <see cref="ModInstaller"/> while
	/// sharing the coordinator's single-operation admission and quiescence state.
	/// A null result means the request was not admitted.
	/// </summary>
	public async Task<ModInstallSummary?> InstallAsync(string[] archivePaths, CancellationToken token = default) {
		ArgumentNullException.ThrowIfNull(archivePaths);
		if (!TryBeginOperation(ModPipelineOperation.Install, token, out CancellationToken operationToken)) {
			return null;
		}

		try {
			return await _modInstaller.InstallAsync(archivePaths, operationToken);
		} finally {
			CompleteOperation(ModPipelineOperation.Install);
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
		if (!TryBeginOperation(operation, token, out CancellationToken operationToken)) {
			bool stopped = !IsAcceptingNewWork;
			return RejectedResult(
				operation,
				stopped ? ModPipelineStatus.AdmissionStopped : ModPipelineStatus.Busy,
				stopped ? "Mod operations are stopping." : "Another mod operation is already active.");
		}

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
			if (!TryBeginCommit(operation)) {
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
				"ModPipelineCoordinator: Accepted snapshot v{SnapshotVersion} with {ModuleCount} modules.",
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
			Log.Error(ex, "ModPipelineCoordinator: Scan failed; accepted state was preserved.");
			return new ModPipelineResult {
				Operation = operation,
				Status = ModPipelineStatus.Failed,
				UserSummary = "Module scan failed; the previous snapshot was preserved.",
				TechnicalDiagnostic = ex.ToString(),
				AcceptedSnapshot = AcceptedSnapshot
			};
		} finally {
			CompleteOperation(operation);
		}
	}

	private bool TryBeginOperation(
		ModPipelineOperation operation,
		CancellationToken callerToken,
		out CancellationToken operationToken) {
		lock (_stateLock) {
			if (!_acceptingNewWork || _activeOperation != ModPipelineOperation.None) {
				operationToken = default;
				return false;
			}
			_activeOperation = operation;
			_activeCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			_activeCancellation = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
			_cancellationRequested = false;
			_commitStarted = false;
			operationToken = _activeCancellation.Token;
			return true;
		}
	}

	/// <summary>
	/// Linearizes cancellation against scan commit. Cancellation requested before
	/// this point wins; after it, the accepted complete result owns the short
	/// synchronous persistence/publication section and cancellation is a no-op.
	/// </summary>
	private bool TryBeginCommit(ModPipelineOperation operation) {
		lock (_stateLock) {
			if (_activeOperation != operation
				|| _cancellationRequested
				|| _activeCancellation is null
				|| _activeCancellation.IsCancellationRequested) {
				return false;
			}
			_commitStarted = true;
			return true;
		}
	}

	private void CompleteOperation(ModPipelineOperation operation) {
		TaskCompletionSource<bool>? completion = null;
		CancellationTokenSource? cancellation = null;
		lock (_stateLock) {
			if (_activeOperation != operation) {
				return;
			}
			_activeOperation = ModPipelineOperation.None;
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
