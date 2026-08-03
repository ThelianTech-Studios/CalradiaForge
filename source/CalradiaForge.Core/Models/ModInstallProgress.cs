namespace CalradiaForge.Core.Models;

/// <summary>
/// Semantic stage for transient manual archive-install progress.
/// </summary>
public enum ModInstallProgressStage {
	OperationStarted,
	Extracting,
	ArchiveCompleted,
	Unblocking,
	Reconciling
}

/// <summary>
/// Read-only transient progress for one admitted install operation.
/// </summary>
public sealed class ModInstallProgress {
	public Guid OperationId { get; init; }
	public Guid ArchiveId { get; init; }
	public ModInstallProgressStage Stage { get; init; }
	public string ArchiveFileName { get; init; } = string.Empty;
	public int ArchiveIndex { get; init; }
	public int TotalArchives { get; init; }
	public int CurrentEntry { get; init; }
	public int BatchFilesProcessed { get; init; }
	public int EstimatedTotalFiles { get; init; }
	public int ProcessedArchiveCount { get; init; }
	public int InstalledCount { get; init; }
	public int UpgradedCount { get; init; }
	public int SkippedCount { get; init; }
	public int FailedCount { get; init; }
	public DateTime TimestampUtc { get; init; }
}
