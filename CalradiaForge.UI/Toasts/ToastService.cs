namespace CalradiaForge.UI.Toasts {
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
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
	public sealed class ToastService {
		private readonly Dispatcher _dispatcher;
		private readonly Dictionary<Guid, DispatcherTimer> _timers = [];
		private readonly TimeSpan _closeAnimationDuration = TimeSpan.FromMilliseconds(160);

		public ObservableCollection<ToastViewModel> VisibleToasts { get; } = [];
		public int MaxVisible { get; } = 3;

		public ToastService(Dispatcher dispatcher) {
			_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
		}

		/// <summary>
		/// Shows a toast notification and returns its unique Id.
		/// </summary>
		public Guid Show(ToastRequest request) {
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

		/// <summary>Closes a toast by Id with a fade-out animation.</summary>
		public void Close(Guid toastId) {
			RunOnUiThread(() => CloseInternal(toastId, animate: true));
		}

		/// <summary>Pauses the auto-dismiss timer for a toast (e.g. on mouse hover).</summary>
		public void Pause(Guid toastId) {
			RunOnUiThread(() => PauseInternal(toastId));
		}

		/// <summary>Resumes a paused auto-dismiss timer.</summary>
		public void Resume(Guid toastId) {
			RunOnUiThread(() => ResumeInternal(toastId));
		}

		/// <summary>Updates progress and optionally the message on a progress toast.</summary>
		public void UpdateProgress(Guid toastId, double value, double max, string? message = null) {
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

		private void InsertToast(ToastViewModel viewModel) {
			if (VisibleToasts.Count >= MaxVisible) {
				ToastViewModel oldest = VisibleToasts.Last();
				CloseInternal(oldest.Id, animate: false);
			}

			VisibleToasts.Insert(0, viewModel);
		}

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

		private async Task RemoveAfterDelayAsync(ToastViewModel viewModel) {
			await Task.Delay(_closeAnimationDuration);
			_dispatcher.Invoke(() => {
				VisibleToasts.Remove(viewModel);
			});
		}

		private void StopTimer(Guid toastId) {
			if (_timers.TryGetValue(toastId, out DispatcherTimer? timer)) {
				timer.Stop();
				_timers.Remove(toastId);
			}
		}

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

		private static TimeSpan GetDefaultDuration(ToastSeverity severity) {
			return severity switch {
				ToastSeverity.Success => TimeSpan.FromSeconds(5),
				ToastSeverity.Warning => TimeSpan.FromSeconds(3),
				ToastSeverity.Error => TimeSpan.FromSeconds(8),
				_ => TimeSpan.FromSeconds(5)
			};
		}

		private static Brush ResolveAccentBrush(ToastSeverity severity) {
			string key = severity switch {
				ToastSeverity.Success => "TB.Acc.Gold",
				ToastSeverity.Warning => "TB.Acc.Orange",
				ToastSeverity.Error => "TB.Acc.Red",
				_ => "TB.Acc.Amber"
			};

			return (Brush)Application.Current.FindResource(key);
		}

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

		private void RunOnUiThread(Action action) {
			if (_dispatcher.CheckAccess()) {
				action();
				return;
			}

			_dispatcher.Invoke(action);
		}

		private T RunOnUiThread<T>(Func<T> func) {
			if (_dispatcher.CheckAccess()) {
				return func();
			}

			return _dispatcher.Invoke(func);
		}

		#endregion
	}
}