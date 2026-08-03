namespace CalradiaForge.Core.Models;

/// <summary>
/// Describes the outcome of scanning one configured module root.
/// </summary>
public enum ModScanRootStatus {
	NotApplicable,
	NotConfigured,
	Succeeded,
	Missing,
	Inaccessible,
	Partial,
	Failed,
	Cancelled
}

/// <summary>
/// Structured outcome for one local or Workshop scan root.
/// </summary>
public sealed class ModScanRootResult {
	public ModScanRootStatus Status { get; init; }
	public int ModuleCount { get; init; }
	public IReadOnlyList<string> Diagnostics { get; init; } = [];

	public bool IsComplete => Status is ModScanRootStatus.NotApplicable
		or ModScanRootStatus.NotConfigured
		or ModScanRootStatus.Succeeded;
}

/// <summary>
/// Low-level discovery result returned by <see cref="Infra.Mods.ModScanner"/>.
/// It contains no cache or accepted-state decision.
/// </summary>
public sealed class ModScanResult {
	public IReadOnlyList<ModuleModel> Modules { get; init; } = [];
	public ModScanRootResult LocalRoot { get; init; } = new();
	public ModScanRootResult WorkshopRoot { get; init; } = new();
	public IReadOnlyList<string> Warnings { get; init; } = [];
	public IReadOnlyList<string> DuplicateModuleIds { get; init; } = [];
	public IReadOnlyList<string> TechnicalDiagnostics { get; init; } = [];
	public bool IsCancelled { get; init; }

	public bool IsComplete => !IsCancelled && LocalRoot.IsComplete && WorkshopRoot.IsComplete;
	public int LocalModuleCount => LocalRoot.ModuleCount;
	public int WorkshopModuleCount => WorkshopRoot.ModuleCount;
}
