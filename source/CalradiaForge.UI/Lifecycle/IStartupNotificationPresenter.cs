namespace CalradiaForge.UI.Lifecycle;

using CalradiaForge.Core.Models;
using CalradiaForge.UI.Toasts;

/// <summary>
/// UI presentation boundary used by the startup notification drain.
/// </summary>
public interface IStartupNotificationPresenter {
	Task PresentAsync(StartupNotification notification, CancellationToken cancellationToken);
}

/// <summary>
/// Maps Core-safe startup notifications to the existing toast surface.
/// </summary>
public sealed class ToastStartupNotificationPresenter : IStartupNotificationPresenter {
	private readonly ToastService _toasts;

	public ToastStartupNotificationPresenter(ToastService toasts) {
		_toasts = toasts ?? throw new ArgumentNullException(nameof(toasts));
	}

	public Task PresentAsync(StartupNotification notification, CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		_toasts.Show(new ToastRequest {
			Title = notification.Title,
			Message = notification.Message,
			Severity = notification.Severity switch {
				StartupNotificationSeverity.Success => ToastSeverity.Success,
				StartupNotificationSeverity.Warning => ToastSeverity.Warning,
				StartupNotificationSeverity.Error => ToastSeverity.Error,
				_ => ToastSeverity.Info
			}
		});
		return Task.CompletedTask;
	}
}
