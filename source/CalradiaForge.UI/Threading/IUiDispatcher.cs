namespace CalradiaForge.UI.Threading;

/// <summary>
/// Marshals only presentation-state application to the WPF UI thread.
/// </summary>
public interface IUiDispatcher {
	bool CheckAccess();

	Task InvokeAsync(
		Action action,
		CancellationToken cancellationToken = default);
}
