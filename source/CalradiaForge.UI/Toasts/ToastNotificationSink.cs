namespace CalradiaForge.UI.Toasts;

/// <summary>
/// Generic UI boundary for opening a notification whose concrete toast identity
/// remains private to the returned handle.
/// </summary>
public interface IToastNotificationSink {
	IToastNotificationHandle Open(ToastRequest request);
}

/// <summary>
/// Opaque handle for updating, transitioning, or closing one notification.
/// </summary>
public interface IToastNotificationHandle : IDisposable {
	void UpdateProgress(double value, double maximum, string? message = null);
	void Transition(ToastRequest request);
	void Close();
}

/// <summary>
/// Adapts the application toast renderer to the generic notification-handle
/// contract without adding workflow-specific behavior to <see cref="ToastService"/>.
/// </summary>
public sealed class ToastNotificationSink : IToastNotificationSink {
	private readonly ToastService _toasts;

	public ToastNotificationSink(ToastService toasts) {
		_toasts = toasts ?? throw new ArgumentNullException(nameof(toasts));
	}

	public IToastNotificationHandle Open(ToastRequest request) {
		ArgumentNullException.ThrowIfNull(request);
		return new ToastNotificationHandle(_toasts, _toasts.Show(request));
	}

	private sealed class ToastNotificationHandle : IToastNotificationHandle {
		private readonly object _gate = new();
		private readonly ToastService _toasts;
		private Guid _toastId;
		private bool _closed;
		private bool _transitioned;

		public ToastNotificationHandle(ToastService toasts, Guid toastId) {
			_toasts = toasts;
			_toastId = toastId;
		}

		public void UpdateProgress(double value, double maximum, string? message = null) {
			Guid toastId;
			lock (_gate) {
				if (_closed || _transitioned) {
					return;
				}
				toastId = _toastId;
			}
			_toasts.UpdateProgress(toastId, value, maximum, message);
		}

		public void Transition(ToastRequest request) {
			ArgumentNullException.ThrowIfNull(request);
			Guid currentId;
			lock (_gate) {
				if (_closed || _transitioned) {
					return;
				}
				_transitioned = true;
				currentId = _toastId;
			}

			Guid replacementId = _toasts.Replace(currentId, request);
			bool closeReplacement;
			lock (_gate) {
				closeReplacement = _closed;
				if (!closeReplacement) {
					_toastId = replacementId;
				}
			}
			if (closeReplacement) {
				try {
					_toasts.Close(replacementId);
				} catch (ObjectDisposedException) {
					// Provider shutdown won the transition race.
				}
			}
		}

		public void Close() {
			Guid toastId;
			lock (_gate) {
				if (_closed) {
					return;
				}
				_closed = true;
				toastId = _toastId;
				_toastId = Guid.Empty;
			}

			try {
				_toasts.Close(toastId);
			} catch (ObjectDisposedException) {
				// Provider shutdown may dispose the renderer before a best-effort
				// cleanup call reaches this handle.
			}
		}

		public void Dispose() => Close();
	}
}
