# Application Lifecycle

## Currently Implemented

- `App.OnStartup` is the sole WPF startup entry. `App.xaml` uses `OnExplicitShutdown` and no longer declares `StartupUri`.
- `App` creates one `IServiceCollection`, registers the existing host as `IApplicationLifetime`, calls the Core and UI registration modules, and builds one root `ServiceProvider`.
- Provider validation uses `ValidateOnBuild` and `ValidateScopes` in every configuration.
- `ApplicationStartupCoordinator` owns ordered startup behavior and explicitly depends on the provider-owned `Serilog.ILogger`.
- Startup initializes localization and first-launch language selection, applies the EULA gate, runs game detection, initializes the accepted mod pipeline, loads and validates startup modpack state, starts the notification drain, and only then resolves and shows `MainWindow`.
- `MainWindow` and primary pages are retained singletons. Modal dialog services are singletons whose invocations create fresh windows, including the XAML-backed themed `ConfirmDialogWindow`.
- `StartupNotificationDrainCoordinator` starts one readiness wait, drains Core notifications FIFO after `MainWindow.Loaded`, and supports explicit cancellation during shutdown.
- `ApplicationShutdownCoordinator` stops new mod-pipeline admission, requests cancellation, awaits quiescence, stops notification delivery, and persists authorized last-used modpack state.
- `App` guards shutdown/restart commitment and snapshots the authoritative mod-pipeline operation before an interruption warning. Idle close proceeds without confirmation; restart always receives its ordinary prompt and receives one additional warning only when tracked work is active.
- `App` owns the renewable 15-second graceful-shutdown choice, disposes the provider once, starts a replacement process only after disposal, and then terminates WPF.
- WPF dispatcher failures are fatal, use native WPF `MessageBox` presentation, and route to controlled shutdown. Unobserved task exceptions are logged and observed without automatically becoming fatal. AppDomain termination cleanup remains best effort.
- Before the shared Serilog instance becomes operational, fatal provider/bootstrap/logger-construction and app-owned exception boundaries use the synchronous best-effort `EmergencyStartupLogWriter`. After activation they use the shared Serilog pipeline, and no Serilog call is made after provider disposal begins.

## Ownership

```text
App
├── owns the sole IServiceCollection and root ServiceProvider
├── owns final WPF/process shutdown and restart
├── resolves ApplicationStartupCoordinator as the initial application root
└── disposes the provider exactly once

ApplicationStartupCoordinator
├── owns ordered startup gates
└── defers MainWindow resolution until the gates pass

ApplicationShutdownCoordinator
├── owns ordered quiescence and persistence
└── never disposes the provider or terminates WPF
```

Core registration contributes only Core services. UI registration contributes WPF windows, pages, dialogs, notification presentation, and application coordinators. No registration module builds a provider, and no global provider/service-locator property is exposed.

## Shutdown and Restart Rules

- Normal close commits immediately when the authoritative mod pipeline is idle. When tracked work is active, the themed shutdown dialog warns before commitment; cancellation leaves the application running.
- Restart first presents the ordinary themed restart confirmation. After acceptance, one active-operation snapshot determines whether a second restart warning is required; cancellation at either prompt aborts restart without reverting settings already persisted for the next process.
- Once shutdown commits, normal operation does not resume.
- One 15-second interval is granted at a time. Interactive timeout presents `Exit Anyway` as the primary action and `Keep Waiting` as the secondary/custom-close result.
- Fatal startup, dispatcher failure, and operating-system shutdown use bounded best effort without interactive timeout prompts.
- WPF operating-system session ending is synchronous: it never cancels Windows logoff/shutdown, immediately stops admission and requests cancellation, synchronously cancels notification draining and attempts authorized persistence, and then lets `OnExit` perform fallback provider/logger disposal. This forced path cannot prove quiescence.
- `Environment.Exit`, process kill, and fire-and-forget lifecycle work are not used.
- Provider disposal is the sole normal Serilog close path.
- Clearing the app's operational-logger state precedes provider disposal, preventing lifecycle/global-exception callbacks from logging through the disposed logger.
- Restart uses the current executable path and starts the replacement only after provider disposal and file release.

## Key Files

- `source/CalradiaForge.UI/App.xaml`
- `source/CalradiaForge.UI/App.xaml.cs`
- `source/CalradiaForge.UI/Composition/ServiceCollectionExtensions.cs`
- `source/CalradiaForge.UI/Lifecycle/ApplicationStartupCoordinator.cs`
- `source/CalradiaForge.UI/Lifecycle/ApplicationShutdownCoordinator.cs`
- `source/CalradiaForge.UI/Lifecycle/ApplicationLifecycleConfirmationPolicy.cs`
- `source/CalradiaForge.UI/Dialogs/ApplicationDialogService.cs`
- `source/CalradiaForge.UI/Views/ConfirmDialogWindow.xaml`
- `source/CalradiaForge.UI/Resources/ConfirmDialogWindowStyles.xaml`
- `source/CalradiaForge.UI/Lifecycle/StartupNotificationDrainCoordinator.cs`
- `source/CalradiaForge.UI/Lifecycle/IApplicationLifetime.cs`
- `source/CalradiaForge.Core/Infra/DependencyInjection/CalradiaForgeCoreServiceCollectionExtensions.cs`

## Verification Boundary

Automated tests verify registration lifetimes, provider validation, one global DI logger identity, FIFO notification draining, cancellation before UI readiness, bounded quiescence, idempotent shutdown coordination, lifecycle reason contracts, confirmation-model mappings, active-operation preflight policy, and delayed-choice result mapping.

Interactive WPF startup, dialog appearance, close/restart behavior, active-work timeout choices, fatal-dispatcher presentation, and log availability still require the owner-maintained manual smoke matrix. Automated verification does not mark that matrix complete.
