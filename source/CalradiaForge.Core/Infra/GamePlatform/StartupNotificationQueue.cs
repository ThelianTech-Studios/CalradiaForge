namespace CalradiaForge.Core.Infra.GamePlatform;

using System.Collections.Concurrent;

using CalradiaForge.Core.Models;

/// <summary>
/// Holds startup notifications until the UI signals that it can present them.
/// </summary>
public sealed class StartupNotificationQueue {
	private readonly ConcurrentQueue<StartupNotification> _notifications = new();
	private readonly SemaphoreSlim _drainLock = new(1, 1);
	private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

	/// <summary>Gets the number of notifications currently awaiting delivery.</summary>
	public int Count => _notifications.Count;

	/// <summary>Gets whether UI readiness has been signaled.</summary>
	public bool IsReady => _ready.Task.IsCompletedSuccessfully;

	/// <summary>Adds one notification to the FIFO queue.</summary>
	public void Enqueue(StartupNotification notification) {
		ArgumentNullException.ThrowIfNull(notification);
		_notifications.Enqueue(notification);
	}

	/// <summary>
	/// Signals that the UI delivery surface is ready. Repeated calls are harmless.
	/// </summary>
	public void SignalReady() {
		_ready.TrySetResult();
	}

	/// <summary>
	/// Waits for readiness and delivers queued notifications in FIFO order.
	/// An item is removed only after its callback completes successfully.
	/// </summary>
	public async Task DrainWhenReadyAsync(
		Func<StartupNotification, CancellationToken, Task> deliverAsync,
		CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(deliverAsync);
		await _ready.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
		await _drainLock.WaitAsync(cancellationToken).ConfigureAwait(false);
		try {
			while (_notifications.TryPeek(out StartupNotification? notification)) {
				cancellationToken.ThrowIfCancellationRequested();
				await deliverAsync(notification, cancellationToken).ConfigureAwait(false);
				_notifications.TryDequeue(out _);
			}
		} finally {
			_drainLock.Release();
		}
	}
}
