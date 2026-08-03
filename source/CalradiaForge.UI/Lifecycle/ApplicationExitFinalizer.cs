namespace CalradiaForge.UI.Lifecycle;

/// <summary>
/// Guarantees final WPF shutdown after the irreversible lifecycle commitment.
/// </summary>
internal static class ApplicationExitFinalizer {
	internal static async Task CompleteAsync(
		Func<Task> disposeProviderAsync,
		Action? startReplacement,
		Action shutdown,
		Action<Exception> reportFailure) {
		ArgumentNullException.ThrowIfNull(disposeProviderAsync);
		ArgumentNullException.ThrowIfNull(shutdown);
		ArgumentNullException.ThrowIfNull(reportFailure);

		bool disposalSucceeded = false;
		try {
			await disposeProviderAsync();
			disposalSucceeded = true;
		} catch (Exception ex) {
			TryReportFailure(reportFailure, ex);
		}

		try {
			if (disposalSucceeded) {
				startReplacement?.Invoke();
			}
		} catch (Exception ex) {
			TryReportFailure(reportFailure, ex);
		} finally {
			shutdown();
		}
	}

	private static void TryReportFailure(Action<Exception> reportFailure, Exception exception) {
		try {
			reportFailure(exception);
		} catch {
			// Diagnostics must never prevent the final WPF shutdown action.
		}
	}
}
