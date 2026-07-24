namespace CalradiaForge.UI.Lifecycle;

using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Models;

/// <summary>
/// Explicit application-shutdown boundary for the Phase 6.A mod pipeline.
/// </summary>
public interface IApplicationWorkController {
	ModPipelineOperation CaptureActiveOperation();
	void StopAcceptingNewWork();
	void RequestCancellation();
	Task WaitForQuiescenceAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Adapts the live mod-pipeline manager to the application lifecycle contract.
/// </summary>
public sealed class ModPipelineApplicationWorkController : IApplicationWorkController {
	private readonly ModPipelineManager _manager;

	public ModPipelineApplicationWorkController(ModPipelineManager manager) {
		_manager = manager ?? throw new ArgumentNullException(nameof(manager));
	}

	public ModPipelineOperation CaptureActiveOperation() => _manager.ActiveOperation;

	public void StopAcceptingNewWork() => _manager.StopAcceptingNewWork();

	public void RequestCancellation() => _manager.RequestCancellation();

	public Task WaitForQuiescenceAsync(CancellationToken cancellationToken) =>
		_manager.WaitForQuiescenceAsync(cancellationToken);
}
