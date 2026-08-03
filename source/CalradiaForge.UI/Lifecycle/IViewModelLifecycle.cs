namespace CalradiaForge.UI.Lifecycle;

/// <summary>
/// Defines the explicit lifecycle used by retained, stateful ViewModels.
/// </summary>
public interface IViewModelLifecycle {
	bool IsInitialized { get; }
	bool IsActive { get; }

	Task InitializeAsync(CancellationToken cancellationToken = default);
	Task ActivateAsync(CancellationToken cancellationToken = default);
	Task DeactivateAsync(CancellationToken cancellationToken = default);
}
