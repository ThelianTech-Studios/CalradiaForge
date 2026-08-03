namespace CalradiaForge.UI.Lifecycle;

using Serilog;

/// <summary>
/// Coordinates application work quiescence and authorized state persistence.
/// Provider disposal and final WPF/process shutdown remain owned by <c>App</c>.
/// </summary>
public sealed class ApplicationShutdownCoordinator {
	private readonly object _gate = new();
	private readonly IApplicationWorkController _workController;
	private readonly StartupNotificationDrainCoordinator _notificationDrain;
	private readonly IApplicationStatePersistence _statePersistence;
	private readonly ILogger _logger;
	private readonly List<Exception> _beginFailures = [];
	private bool _shutdownStarted;
	private bool _completionPerformed;

	public ApplicationShutdownCoordinator(
		IApplicationWorkController workController,
		StartupNotificationDrainCoordinator notificationDrain,
		IApplicationStatePersistence statePersistence,
		ILogger logger) {
		_workController = workController ?? throw new ArgumentNullException(nameof(workController));
		_notificationDrain = notificationDrain ?? throw new ArgumentNullException(nameof(notificationDrain));
		_statePersistence = statePersistence ?? throw new ArgumentNullException(nameof(statePersistence));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <summary>
	/// Permanently stops new work admission and requests cooperative cancellation.
	/// Repeated calls are harmless.
	/// </summary>
	public void BeginShutdown() {
		bool shouldBegin;
		lock (_gate) {
			shouldBegin = !_shutdownStarted;
			_shutdownStarted = true;
		}
		if (!shouldBegin) {
			return;
		}

		try {
			_workController.StopAcceptingNewWork();
		} catch (Exception ex) {
			RecordBeginFailure(ex, "Failed to stop new work admission during shutdown.");
		}

		try {
			_workController.RequestCancellation();
		} catch (Exception ex) {
			RecordBeginFailure(ex, "Failed to request active-work cancellation during shutdown.");
		}
	}

	/// <summary>
	/// Waits for safe quiescence for one bounded interval.
	/// </summary>
	/// <returns><c>true</c> when quiescence completed within the interval.</returns>
	public async Task<bool> WaitForQuiescenceAsync(TimeSpan interval, CancellationToken cancellationToken = default) {
		if (interval <= TimeSpan.Zero) {
			throw new ArgumentOutOfRangeException(nameof(interval));
		}

		BeginShutdown();
		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeout.CancelAfter(interval);
		try {
			await _workController.WaitForQuiescenceAsync(timeout.Token);
			return true;
		} catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
			return false;
		}
	}

	/// <summary>
	/// Stops startup notification delivery and persists authorized state.
	/// Each stage is attempted even when another stage fails.
	/// </summary>
	public async Task<IReadOnlyList<Exception>> CompleteShutdownAsync() {
		lock (_gate) {
			if (_completionPerformed) {
				return [];
			}
			_completionPerformed = true;
		}

		List<Exception> failures;
		lock (_gate) {
			failures = [.. _beginFailures];
		}
		try {
			await _notificationDrain.StopAsync();
		} catch (Exception ex) {
			RecordStageFailure(
				failures,
				ex,
				"Failed to stop the startup notification drain during shutdown.");
		}

		try {
			_statePersistence.Persist();
		} catch (Exception ex) {
			RecordStageFailure(
				failures,
				ex,
				"Failed to persist application state during shutdown.");
		}

		return failures;
	}

	/// <summary>
	/// Performs the synchronous subset that is safe during WPF's operating-system
	/// session-ending callback. This path must return without cancelling Windows.
	/// </summary>
	public IReadOnlyList<Exception> CompleteOperatingSystemShutdown() {
		BeginShutdown();
		lock (_gate) {
			if (_completionPerformed) {
				return [];
			}
			_completionPerformed = true;
		}

		List<Exception> failures;
		lock (_gate) {
			failures = [.. _beginFailures];
		}
		try {
			_notificationDrain.RequestStop();
		} catch (Exception ex) {
			RecordStageFailure(
				failures,
				ex,
				"Failed to request notification-drain cancellation during OS shutdown.");
		}

		try {
			_statePersistence.Persist();
		} catch (Exception ex) {
			RecordStageFailure(
				failures,
				ex,
				"Failed to persist application state during OS shutdown.");
		}

		return failures;
	}

	private void RecordBeginFailure(Exception exception, string message) {
		lock (_gate) {
			_beginFailures.Add(exception);
		}
		TryLogFailure(exception, message);
	}

	private void RecordStageFailure(List<Exception> failures, Exception exception, string message) {
		failures.Add(exception);
		TryLogFailure(exception, message);
	}

	private void TryLogFailure(Exception exception, string message) {
		try {
			_logger.Error(exception, message);
		} catch {
			// Failure collection must not let diagnostics skip unrelated shutdown stages.
		}
	}
}
