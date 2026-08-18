# Notifications

The UI notification layer renders generic toasts and the install-specific
correlated lifecycle. `ToastService` owns visible toast state, severity,
duration, pause/resume, and dismissal. `InstallNotificationPresenter` owns
operation handles, stale-progress rejection, and terminal transition ordering.

See [Core/UI notification ownership](../../02_Systems/CalradiaForge_UI/Notifications/README.md)
and [Diagnostics](../../04_Application/Capabilities/Diagnostics.md).
