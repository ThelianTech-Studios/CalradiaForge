namespace CalradiaForge.Core.Models;

/// <summary>
/// Terminal status for one application-level manual archive install request.
/// </summary>
public enum ModInstallOperationStatus {
	Succeeded,
	SucceededWithWarnings,
	PartiallyFailed,
	Cancelled,
	RejectedBusy,
	RejectedAdmissionStopped,
	ValidationFailed,
	Failed
}

/// <summary>
/// Stable, UI-neutral diagnostic codes for install admission and finalization.
/// </summary>
public enum ModInstallDiagnosticCode {
	None,
	ArchiveSelectionEmpty,
	GameDirectoryNotConfigured,
	GameDirectoryMissing,
	ModulesDirectoryMissing,
	Busy,
	AdmissionStopped,
	InstallerAdmissionInvariantViolation,
	InstallerReturnedNoResults,
	InstallerFailed,
	UnblockCompletedWithFailures,
	UnblockFailed,
	ReconciliationCompletedWithWarnings,
	ReconciliationIncomplete,
	ReconciliationFailed,
	FinalizationTimedOut,
	Cancelled
}

/// <summary>
/// Immutable terminal description of the authoritative Core install pipeline.
/// </summary>
public sealed class ModInstallOperationResult {
	private ModInstallSummary _summary = new();
	private UnblockResult? _unblockResult;
	private IReadOnlyList<ModInstallDiagnosticCode> _diagnosticCodes =
		Array.AsReadOnly(Array.Empty<ModInstallDiagnosticCode>());
	private IReadOnlyList<string> _technicalDiagnostics =
		Array.AsReadOnly(Array.Empty<string>());

	public Guid OperationId { get; init; }
	public ModInstallOperationStatus Status { get; init; }
	public ModInstallSummary Summary {
		get => SnapshotSummary(_summary);
		init => _summary = SnapshotSummary(value);
	}
	public UnblockResult? UnblockResult {
		get => SnapshotUnblockResult(_unblockResult);
		init => _unblockResult = SnapshotUnblockResult(value);
	}
	public ModPipelineResult? ReconciliationResult { get; init; }
	public AcceptedModSnapshot AcceptedSnapshot { get; init; } = AcceptedModSnapshot.Empty;
	public IReadOnlyList<ModInstallDiagnosticCode> DiagnosticCodes {
		get => _diagnosticCodes;
		init => _diagnosticCodes = Array.AsReadOnly((value ?? []).ToArray());
	}
	public IReadOnlyList<string> TechnicalDiagnostics {
		get => _technicalDiagnostics;
		init => _technicalDiagnostics = Array.AsReadOnly((value ?? []).ToArray());
	}

	public bool WasAdmitted => Status is not ModInstallOperationStatus.RejectedBusy
		and not ModInstallOperationStatus.RejectedAdmissionStopped
		and not ModInstallOperationStatus.ValidationFailed;

	public bool Success => Status is ModInstallOperationStatus.Succeeded
		or ModInstallOperationStatus.SucceededWithWarnings;
	public bool IsCancelled => Status == ModInstallOperationStatus.Cancelled;

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
}
