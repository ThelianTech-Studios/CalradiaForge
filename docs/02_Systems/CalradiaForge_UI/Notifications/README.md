# UI Notifications

`InstallNotificationPresenter` is the application-lifetime bridge from Core
install progress/terminal results to the generic `ToastService`. It owns
operation correlation, stale-progress rejection, progress-to-terminal
transition, and launcher presentation-completion ordering. `ToastService` and
its sink/rendering types own severity, timing, dismissal, and visible toast
state.

Startup notifications use the queued Core handoff and a dedicated UI drain
coordinator. Presentation details belong in [UI Notifications](../../../05_UI/Notifications/README.md).
