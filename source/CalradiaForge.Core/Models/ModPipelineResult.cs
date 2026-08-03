namespace CalradiaForge.Core.Models;

/// <summary>
/// Identifies the bounded operation requested from the mod pipeline.
/// </summary>
public enum ModPipelineOperation {
	None,
	StartupScan,
	RefreshScan,
	Install
}

/// <summary>
/// Workflow-specific status for a mod-pipeline scan operation.
/// </summary>
public enum ModPipelineStatus {
	Succeeded,
	InvalidConfiguration,
	Incomplete,
	Cancelled,
	Busy,
	AdmissionStopped,
	Failed
}

/// <summary>
/// One atomically published, monotonically versioned module snapshot.
/// </summary>
public sealed class AcceptedModSnapshot {
	public static AcceptedModSnapshot Empty { get; } = new(0, [], [], []);

	public AcceptedModSnapshot(
		long version,
		IReadOnlyList<ModuleModel> modules,
		IReadOnlyList<ModuleModel>? addedModules = null,
		IReadOnlyList<ModuleModel>? removedModules = null) {
		Version = version;
		Modules = modules ?? throw new ArgumentNullException(nameof(modules));
		AddedModules = addedModules ?? [];
		RemovedModules = removedModules ?? [];
		AcceptedAtUtc = DateTime.UtcNow;
	}

	public long Version { get; }
	public IReadOnlyList<ModuleModel> Modules { get; }
	public IReadOnlyList<ModuleModel> AddedModules { get; }
	public IReadOnlyList<ModuleModel> RemovedModules { get; }
	public DateTime AcceptedAtUtc { get; }
}

/// <summary>
/// Structured result returned by startup and explicit refresh scans.
/// </summary>
public sealed class ModPipelineResult {
	public ModPipelineOperation Operation { get; init; }
	public ModPipelineStatus Status { get; init; }
	public bool IsComplete { get; init; }
	public bool WasCommitted { get; init; }
	public bool HasChanges { get; init; }
	public ModScanRootResult LocalRoot { get; init; } = new();
	public ModScanRootResult WorkshopRoot { get; init; } = new();
	public int DiscoveredModuleCount { get; init; }
	public int LocalModuleCount { get; init; }
	public int WorkshopModuleCount { get; init; }
	public IReadOnlyList<string> Warnings { get; init; } = [];
	public IReadOnlyList<string> DuplicateModuleIds { get; init; } = [];
	public string UserSummary { get; init; } = string.Empty;
	public string TechnicalDiagnostic { get; init; } = string.Empty;
	public AcceptedModSnapshot AcceptedSnapshot { get; init; } = AcceptedModSnapshot.Empty;

	public bool Success => Status == ModPipelineStatus.Succeeded;
	public bool IsCancelled => Status == ModPipelineStatus.Cancelled;
}
