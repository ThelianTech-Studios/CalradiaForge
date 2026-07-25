namespace CalradiaForge.UI.Toasts {
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Media;
	using System.Windows.Threading;

	using MahApps.Metro.IconPacks;

	/// <summary>
	/// Application-wide toast notification service. Manages a capped list of
	/// visible toasts with auto-dismiss timers, pause/resume, and progress updates.
	/// Must be created on the UI thread or provided the UI dispatcher.
	/// </summary>
	public sealed class ToastService : IDisposable {
		private readonly Dispatcher _dispatcher;
		private readonly Dictionary<Guid, DispatcherTimer> _timers = [];
		private readonly TimeSpan _closeAnimationDuration = TimeSpan.FromMilliseconds(160);
		private readonly CancellationTokenSource _shutdownCancellation = new();
		private bool _disposed;

		/// <summary>
		/// Gets the collection of currently visible toasts.
		/// </summary>
		public ObservableCollection<ToastViewModel> VisibleToasts { get; } = [];
		/// <summary>
		/// Gets the maximum number of visible toasts allowed at once.
		/// </summary>
		public int MaxVisible { get; } = 3;

		/// <summary>
		/// Initializes the toast service with the dispatcher used for UI updates.
		/// </summary>
		public ToastService(Dispatcher dispatcher) {
			_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
		}

		/// <summary>
		/// Shows a toast notification and returns its unique Id.
		/// </summary>
		public Guid Show(ToastRequest request) {
			ArgumentNullException.ThrowIfNull(request);
			ThrowIfDisposed();
			return RunOnUiThread(() => {
				ToastViewModel viewModel = CreateViewModel(request);
				InsertToast(viewModel);

				if (!viewModel.IsPersistent) {
					TimeSpan duration = request.Duration ?? GetDefaultDuration(request.Severity);
					StartTimer(viewModel, duration);
				}

				return viewModel.Id;
			});
		}

		/// <summary>
		/// Atomically replaces an existing toast with a new request and returns the
		/// replacement Id. If the original toast is no longer visible, the request
		/// is shown as a new toast.
		/// </summary>
		public Guid Replace(Guid toastId, ToastRequest request) {
			ArgumentNullException.ThrowIfNull(request);
			ThrowIfDisposed();
			return RunOnUiThread(() => {
				ToastViewModel replacement = CreateViewModel(request);
				ToastViewModel? current = VisibleToasts.FirstOrDefault(t => t.Id == toastId);
				if (current is null) {
					InsertToast(replacement);
				} else {
					int index = VisibleToasts.IndexOf(current);
					StopTimer(toastId);
					VisibleToasts[index] = replacement;
				}

				if (!replacement.IsPersistent) {
					TimeSpan duration = request.Duration ?? GetDefaultDuration(request.Severity);
					StartTimer(replacement, duration);
				}

				return replacement.Id;
			});
		}

		/// <summary>Closes a toast by Id with a fade-out animation.</summary>
		public void Close(Guid toastId) {
			ThrowIfDisposed();
			RunOnUiThread(() => CloseInternal(toastId, animate: true));
		}

		/// <summary>Pauses the auto-dismiss timer for a toast (e.g. on mouse hover).</summary>
		public void Pause(Guid toastId) {
			ThrowIfDisposed();
			RunOnUiThread(() => PauseInternal(toastId));
		}

		/// <summary>Resumes a paused auto-dismiss timer.</summary>
		public void Resume(Guid toastId) {
			ThrowIfDisposed();
			RunOnUiThread(() => ResumeInternal(toastId));
		}

		/// <summary>Updates progress and optionally the message on a progress toast.</summary>
		public void UpdateProgress(Guid toastId, double value, double max, string? message = null) {
			ThrowIfDisposed();
			RunOnUiThread(() => {
				ToastViewModel? viewModel = VisibleToasts.FirstOrDefault(t => t.Id == toastId);
				if (viewModel is null) {
					return;
				}

				viewModel.ProgressMax = max;
				viewModel.ProgressValue = value;

				if (!string.IsNullOrWhiteSpace(message)) {
					viewModel.Message = message;
				}
			});
		}

		#region Internal

		/// <summary>
		/// Creates a view model instance from the toast request.
		/// </summary>
		private ToastViewModel CreateViewModel(ToastRequest request) {
			ToastViewModel viewModel = new(Close) {
				Title = request.Title,
				Message = request.Message,
				TemplateKey = string.IsNullOrWhiteSpace(request.TemplateKey)
					? ToastTemplateKeys.Default
					: request.TemplateKey,
				IsPersistent = request.IsPersistent,
				AllowClickDismiss = request.AllowClickDismiss,
				ShowCloseButton = request.ShowCloseButton,
				AccentBrush = ResolveAccentBrush(request.Severity),
				IconKind = ResolveIconKind(request.Severity),
				ProgressMax = request.ProgressMax,
				ProgressValue = request.ProgressValue
			};

			return viewModel;
		}

		/// <summary>
		/// Inserts a toast into the visible list, evicting the oldest when full.
		/// </summary>
		private void InsertToast(ToastViewModel viewModel) {
			if (VisibleToasts.Count >= MaxVisible) {
				ToastViewModel oldest = VisibleToasts.Last();
				CloseInternal(oldest.Id, animate: false);
			}

			VisibleToasts.Insert(0, viewModel);
		}

		/// <summary>
		/// Starts the auto-dismiss timer for the specified toast.
		/// </summary>
		private void StartTimer(ToastViewModel viewModel, TimeSpan duration) {
			DispatcherTimer timer = new() {
				Interval = duration
			};

			viewModel.TimerStartUtc = DateTime.UtcNow;
			viewModel.RemainingTime = duration;
			viewModel.IsTimerRunning = true;

			timer.Tick += (_, _) => Close(viewModel.Id);
			_timers[viewModel.Id] = timer;
			timer.Start();
		}

		/// <summary>
		/// Closes the toast and optionally animates its removal.
		/// </summary>
		private void CloseInternal(Guid toastId, bool animate) {
			ToastViewModel? viewModel = VisibleToasts.FirstOrDefault(t => t.Id == toastId);
			if (viewModel is null) {
				return;
			}

			StopTimer(toastId);

			if (!animate) {
				VisibleToasts.Remove(viewModel);
				return;
			}

			viewModel.IsClosing = true;
			_ = RemoveAfterDelayAsync(viewModel);
		}

		/// <summary>
		/// Removes a toast after the close animation delay.
		/// </summary>
		private async Task RemoveAfterDelayAsync(ToastViewModel viewModel) {
			try {
				await Task.Delay(_closeAnimationDuration, _shutdownCancellation.Token);
				if (_dispatcher.HasShutdownStarted || _dispatcher.HasShutdownFinished) {
					return;
				}
				await _dispatcher.InvokeAsync(
					() => VisibleToasts.Remove(viewModel),
					DispatcherPriority.Normal,
					_shutdownCancellation.Token);
			} catch (OperationCanceledException) when (_shutdownCancellation.IsCancellationRequested) {
				// Provider-owned shutdown cancels pending toast animations.
			}
		}

		/// <summary>
		/// Stops and removes the timer associated with a toast.
		/// </summary>
		private void StopTimer(Guid toastId) {
			if (_timers.TryGetValue(toastId, out DispatcherTimer? timer)) {
				timer.Stop();
				_timers.Remove(toastId);
			}
		}

		/// <summary>
		/// Pauses a toast timer and records remaining time.
		/// </summary>
		private void PauseInternal(Guid toastId) {
			ToastViewModel? viewModel = VisibleToasts.FirstOrDefault(t => t.Id == toastId);
			if (viewModel is null || viewModel.IsPersistent || !viewModel.IsTimerRunning) {
				return;
			}

			if (_timers.TryGetValue(toastId, out DispatcherTimer? timer)) {
				TimeSpan elapsed = DateTime.UtcNow - viewModel.TimerStartUtc;
				viewModel.RemainingTime = viewModel.RemainingTime - elapsed;
				timer.Stop();
				viewModel.IsTimerRunning = false;
			}
		}

		/// <summary>
		/// Resumes a paused toast timer.
		/// </summary>
		private void ResumeInternal(Guid toastId) {
			ToastViewModel? viewModel = VisibleToasts.FirstOrDefault(t => t.Id == toastId);
			if (viewModel is null || viewModel.IsPersistent || viewModel.IsTimerRunning) {
				return;
			}

			if (viewModel.RemainingTime <= TimeSpan.Zero) {
				CloseInternal(toastId, animate: true);
				return;
			}

			if (_timers.TryGetValue(toastId, out DispatcherTimer? timer)) {
				timer.Interval = viewModel.RemainingTime;
				viewModel.TimerStartUtc = DateTime.UtcNow;
				viewModel.IsTimerRunning = true;
				timer.Start();
			}
		}

		/// <summary>
		/// Returns a default display duration for the specified severity.
		/// </summary>
		private static TimeSpan GetDefaultDuration(ToastSeverity severity) {
			return severity switch {
				ToastSeverity.Success => TimeSpan.FromSeconds(5),
				ToastSeverity.Warning => TimeSpan.FromSeconds(3),
				ToastSeverity.Error => TimeSpan.FromSeconds(8),
				_ => TimeSpan.FromSeconds(5)
			};
		}

		/// <summary>
		/// Resolves the accent brush associated with a toast severity.
		/// </summary>
		private static Brush ResolveAccentBrush(ToastSeverity severity) {
			string key = severity switch {
				ToastSeverity.Success => "TB.Acc.Gold",
				ToastSeverity.Warning => "TB.Acc.Orange",
				ToastSeverity.Error => "TB.Acc.Red",
				_ => "TB.Acc.Amber"
			};

			return (Brush)Application.Current.FindResource(key);
		}

		/// <summary>
		/// Resolves the icon kind associated with a toast severity.
		/// </summary>
		private static PackIconMaterialKind ResolveIconKind(ToastSeverity severity) {
			return severity switch {
				ToastSeverity.Success => PackIconMaterialKind.CheckCircleOutline,
				ToastSeverity.Warning => PackIconMaterialKind.AlertCircleOutline,
				ToastSeverity.Error => PackIconMaterialKind.CloseCircleOutline,
				_ => PackIconMaterialKind.InformationOutline
			};
		}

		#endregion

		#region Dispatcher Helpers

		/// <summary>
		/// Executes an action on the UI thread.
		/// </summary>
		private void RunOnUiThread(Action action) {
			if (_dispatcher.CheckAccess()) {
				action();
				return;
			}

			_dispatcher.Invoke(action);
		}

		/// <summary>
		/// Executes a function on the UI thread and returns its result.
		/// </summary>
		private T RunOnUiThread<T>(Func<T> func) {
			if (_dispatcher.CheckAccess()) {
				return func();
			}

			return _dispatcher.Invoke(func);
		}

		#endregion

		/// <summary>Stops provider-owned timers and pending animation work.</summary>
		public void Dispose() {
			if (_disposed) {
				return;
			}
			_disposed = true;
			_shutdownCancellation.Cancel();

			void StopAllTimers() {
				foreach (DispatcherTimer timer in _timers.Values) {
					timer.Stop();
				}
				_timers.Clear();
			}

			if (!_dispatcher.HasShutdownStarted && !_dispatcher.HasShutdownFinished) {
				if (_dispatcher.CheckAccess()) {
					StopAllTimers();
				} else {
					_dispatcher.Invoke(StopAllTimers);
				}
			} else {
				StopAllTimers();
			}

			_shutdownCancellation.Dispose();
		}

		private void ThrowIfDisposed() {
			ObjectDisposedException.ThrowIf(_disposed, this);
		}
	}
}
