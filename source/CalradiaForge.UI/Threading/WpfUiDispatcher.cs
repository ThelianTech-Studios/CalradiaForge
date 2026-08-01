namespace CalradiaForge.UI.Threading;

using System.Windows.Threading;

/// <summary>
/// Awaitable WPF implementation of the narrow presentation dispatcher boundary.
/// </summary>
public sealed class WpfUiDispatcher(Dispatcher dispatcher) : IUiDispatcher {
	private readonly Dispatcher _dispatcher = dispatcher
		?? throw new ArgumentNullException(nameof(dispatcher));

	public bool CheckAccess() => _dispatcher.CheckAccess();

	public async Task InvokeAsync(
		Action action,
		CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(action);
		cancellationToken.ThrowIfCancellationRequested();

		if (CheckAccess()) {
			action();
			return;
		}

		await _dispatcher.InvokeAsync(
			action,
			DispatcherPriority.Normal,
			cancellationToken);
	}
}
