namespace CalradiaForge.UI.Lifecycle;

using CalradiaForge.Core.Infra.GamePlatform;
using CalradiaForge.Core.Models;

using Serilog;

/// <summary>
/// Owns the one-time wait for UI readiness and FIFO startup-notification drain.
/// </summary>
public sealed class StartupNotificationDrainCoordinator {
	private readonly object _gate = new();
	private readonly StartupNotificationQueue _queue;
	private readonly IStartupNotificationPresenter _presenter;
	private readonly ILogger _logger;
	private CancellationTokenSource? _cancellation;
	private Task? _drainTask;

	public StartupNotificationDrainCoordinator(
		StartupNotificationQueue queue,
		IStartupNotificationPresenter presenter,
		ILogger logger) {
		_queue = queue ?? throw new ArgumentNullException(nameof(queue));
		_presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <summary>
	/// Starts the one-time drain task without waiting for UI readiness.
	/// </summary>
	public Task StartAsync(CancellationToken cancellationToken = default) {
		lock (_gate) {
			if (_drainTask is not null) {
				return Task.CompletedTask;
			}

			_cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			_drainTask = DrainAsync(_cancellation.Token);
			return Task.CompletedTask;
		}
	}

	/// <summary>
	/// Signals that the MainWindow toast host has loaded.
	/// </summary>
	public void SignalReady() => _queue.SignalReady();

	/// <summary>Presents a lifecycle notification after the startup queue has drained.</summary>
	public async Task PresentAsync(
		StartupNotification notification,
		CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(notification);
		try {
			await _presenter.PresentAsync(notification, cancellationToken);
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			throw;
		} catch (Exception ex) {
			_logger.Error(ex, "Lifecycle notification presentation failed.");
			throw;
		}
	}

	/// <summary>
	/// Cancels and awaits the drain task. Repeated calls are harmless.
	/// </summary>
	public async Task StopAsync() {
		Task? drainTask;
		lock (_gate) {
			drainTask = _drainTask;
		}

		if (drainTask is null) {
			return;
		}

		RequestStop();

		try {
			await drainTask;
		} catch (OperationCanceledException) {
			// Expected when shutdown wins the readiness/drain race.
		} finally {
			lock (_gate) {
				_cancellation?.Dispose();
				_cancellation = null;
			}
		}
	}

	/// <summary>
	/// Synchronously requests cancellation without awaiting UI-bound drain work.
	/// Used by forced operating-system session teardown.
	/// </summary>
	public void RequestStop() {
		CancellationTokenSource? cancellation;
		lock (_gate) {
			cancellation = _cancellation;
		}
		if (cancellation is { IsCancellationRequested: false }) {
			cancellation.Cancel();
		}
	}

	private async Task DrainAsync(CancellationToken cancellationToken) {
		try {
			await _queue.DrainWhenReadyAsync(_presenter.PresentAsync, cancellationToken);
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			throw;
		} catch (Exception ex) {
			_logger.Error(ex, "Startup notification drain failed.");
			throw;
		}
	}
}
