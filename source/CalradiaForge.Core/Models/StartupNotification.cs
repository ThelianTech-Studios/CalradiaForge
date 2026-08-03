namespace CalradiaForge.Core.Models;

/// <summary>
/// Severity understood by the UI when presenting a startup notification.
/// </summary>
public enum StartupNotificationSeverity {
	Info,
	Success,
	Warning,
	Error
}

/// <summary>
/// Compact, UI-framework-neutral notification queued before UI readiness.
/// </summary>
public sealed record StartupNotification(
	string Title,
	string Message,
	StartupNotificationSeverity Severity = StartupNotificationSeverity.Info);
