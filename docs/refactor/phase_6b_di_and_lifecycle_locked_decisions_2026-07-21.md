# Phase 6.B Locked Decisions — Dependency Injection and Application Lifecycle Foundation

## Status

Owner-approved planning direction for:

```text
Phase 6.B — Dependency Injection and Application Lifecycle Foundation
```

This ledger supersedes older unresolved DI, logger-lifecycle, log-archive, settings, and shutdown alternatives.

A proposed later “Decision 31” that introduced separate DI default-initialization factories is explicitly rejected as redundant and is not part of the locked architecture.

## Current Source State

This is a future implementation contract; it does not claim Phase 6.B is implemented. Current WPF startup still uses `StartupUri`, manual construction, static `App.*` access, retained pages constructed by `MainWindow`, a non-retaining `SerilogLoggerFactory.Create()`, and non-quiescent `OnExit` cleanup. Current `AppConfig` and `AppConfigSettings` remain live, and `Logger.Instance` remains the normal logger until Phase 6.C.

---

# 1. Core Architectural Rule

```text
DI owns object construction and dependency delivery.
ApplicationStartupCoordinator owns ordered startup behavior.
ApplicationShutdownCoordinator owns ordered quiescence/persistence behavior.
App owns the root provider and final WPF/process lifecycle.
```

---

# 2. Sole WPF Startup Entry

`App.OnStartup` is the sole WPF startup entry.

Remove `StartupUri` when Phase 6.B activates DI startup.

`App.xaml.cs` does not manually construct/load normal application services, pages, or `MainWindow`.

`App`:

1. creates one `IServiceCollection`,
2. registers its existing host instance as the application-lifetime boundary,
3. calls Core registration extensions,
4. calls UI registration extensions,
5. builds exactly one root provider,
6. resolves one `ApplicationStartupCoordinator`,
7. invokes ordered startup,
8. privately retains provider ownership for shutdown/restart.

Before provider construction succeeds, `App` retains only the minimal top-level emergency catch boundary.

---

# 3. One Collection and One Provider

Use one application composition root:

```text
App.xaml.cs
└── IServiceCollection
    ├── AddCalradiaForgeCore(...)
    └── AddCalradiaForgeUi(...)
        ↓
    BuildServiceProvider() exactly once
```

Rules:

- only `App` calls `BuildServiceProvider()`,
- Core never builds or returns a provider,
- UI never builds a second provider,
- registration modules do not resolve services during registration,
- no temporary provider is built,
- no global `App.Services` or service locator is exposed,
- Core registration contains only Core services,
- UI registration contains WPF/UI services and coordinators,
- Core remains WPF-free.

Core may reference DI abstractions; the executable/UI project owns the full provider build.

---

# 4. Provider Validation

Build the provider with validation enabled in all builds:

```csharp
new ServiceProviderOptions
{
    ValidateOnBuild = true,
    ValidateScopes = true
}
```

Composition failures must surface during startup rather than remain hidden until a page is first used.

---

# 5. Lifetime Policy

Use singleton by default for application-wide state and shared infrastructure.

Do not create custom scopes for the WPF application.

Use transients only for genuinely short-lived, operation-local objects with no shared mutable state, global event ownership, workflow ownership, or shutdown responsibility.

Strong singleton defaults include:

- `ConfigFileManager`,
- `AppSettings`,
- `LoggingSettings`,
- `ModPipelineCoordinator`,
- `ModService` or its retained state owner,
- game-detection workflow/services,
- startup notification queue,
- translation services,
- toast/navigation/shell services,
- stateless path/platform adapters,
- `ApplicationStartupCoordinator`,
- `ApplicationShutdownCoordinator`,
- `MainWindow` and retained application pages,
- one shared Serilog logger.

Phase 6.A defines the mod-pipeline lifecycle contract consumed here.

---

# 6. Preserve Current Page Lifetime

Preserve the current one-instance-per-application page behavior.

- Resolve one singleton `MainWindow`.
- Construct each primary page once through DI.
- Retain and reuse those page instances from `MainWindow`.
- Constructor-inject services instead of using static `App.*` properties.
- Preserve current code-behind, page-owned state, `DataContext` usage, bindings, navigation refresh, and `Loaded`/`Unloaded` behavior.

Do not add:

- transient navigation pages,
- navigation scopes,
- a page catalog,
- broad ViewModel extraction,
- a full MVVM rewrite.

Page/ViewModel lifetime redesign belongs to the later MVVM phase.

---

# 7. Remove Static `App.*` Service Access

Remove static `App.*` access for services and shared mutable state.

Use constructor injection for `MainWindow`, pages, dialog services, and normal UI dependencies.

Do not add static compatibility properties solely to avoid constructor changes.

Legitimate WPF framework statics remain allowed, including:

```text
Application.Current.Dispatcher
Application.Current.Shutdown()
Application.Current.Resources
Application.Current.MainWindow
```

The legacy `Logger.Instance` global remains temporarily through 6.B and is removed in 6.C.

---

# 8. Configuration Naming and Responsibility

Rename:

```text
AppConfig         → ConfigFileManager
AppConfigSettings → AppSettings
```

`ConfigFileManager` is the low-level custom JSON settings file reader/writer/manager.

`AppSettings` is the application-facing settings object.

Introduce separate:

```text
LoggingSettings
```

`LoggingSettings` owns only the persisted `DebugMode` required before Serilog construction.

Remove `LogFileDaysToKeep` from configuration. Retention is a fixed seven-day logging policy.

Normal services depend on `AppSettings` or `LoggingSettings`, not raw string keys.

---

# 9. Preserve the Object-Based Settings Load/Default/Save Flow

Do not invent a second initialization architecture.

The locked settings lifecycle is:

1. the applicable settings file path and settings instance/type participate in construction/load through `ConfigFileManager`,
2. when an existing file is present and valid, load the persisted settings instance,
3. when the file is missing, empty, or malformed, produce a new empty settings instance,
4. the settings object populates its default property values because the returned instance is empty,
5. `ConfigFileManager` saves the resulting settings instance,
6. later property changes persist through the same manager.

Do not add:

- a separate central defaults-initializer service,
- redundant `InitializeDefaults()` DI factories merely to duplicate the object’s own first-load behavior,
- a second “current settings” copy,
- immutable logging-session state,
- a generic host/configuration stack.

Malformed JSON is discarded and regenerated from defaults. No backup/recovery copy is required.

Unsupported filesystem/device/access failures may propagate into fatal startup handling. Do not add alternate storage locations or in-memory nonpersistent mode.

---

# 10. Logging Bootstrap Dependency

The final logger must initialize after configuration load because `LoggingSettings.DebugMode` controls the minimum level.

Required dependency chain:

```text
ConfigFileManager loads LoggingSettings
→ LoggingSettings.DebugMode is available
→ startup log-file lifecycle runs
→ one Serilog logger is constructed
→ the same logger is assigned to Serilog.Log.Logger
→ ApplicationStartupCoordinator may execute Log.* calls
```

`ApplicationStartupCoordinator` explicitly depends on `Serilog.ILogger` so resolving the coordinator forces logger activation before `StartAsync()`.

`App` does not separately resolve and sequence the logger.

Bootstrap configuration/settings code required to create the logger must not depend on the final logger during construction.

Phase 6.B may retain current legacy/debug diagnostics at that boundary. Phase 6.C replaces that with the narrow emergency writer.

---

# 11. Final Serilog Model

Construct exactly one shared Serilog logger through a DI singleton factory.

Assign that same instance to:

```csharp
Serilog.Log.Logger
```

Global `Log.*` is the normal final application logging API.

This is the explicit exception to the general no-global-services rule.

Ownership:

```text
App owns root provider lifetime
→ DI owns the factory-created Serilog singleton
→ provider disposal flushes/disposes that logger
```

Prohibit:

- a second logger pipeline,
- consumer `Dispose()` calls,
- a separate `Log.CloseAndFlush()` path,
- logging after provider disposal begins.

---

# 12. Fixed Debug-Mode Behavior

At startup:

```text
Read LoggingSettings.DebugMode
→ construct Serilog at Debug or Information
→ active logger keeps that level for the full process lifetime
```

The logger is not dynamically reconfigured.

When the user changes debug mode:

1. persist the new value immediately,
2. immediately display a restart-required dialog,
3. prompt in both directions (`false→true` and `true→false`),
4. if restart is accepted, use the app-owned restart pipeline,
5. if restart is declined, keep the persisted value and leave the current logger unchanged,
6. the persisted value applies on the next process start.

Do not add:

- `LoggingSessionState`,
- `DebugModeAtStartup`,
- a pending-restart flag,
- runtime level switching,
- rollback when restart is declined.

The active logger instance already embodies the current process configuration.

---

# 13. Startup-Only Log Archival

The active log file is:

```text
CalradiaForge_Latest.log
```

On shutdown:

- quiesce the app,
- dispose the provider/logger,
- leave `CalradiaForge_Latest.log` in place,
- do not rename, move, archive, truncate, or delete it.

Reason: users must be able to upload the latest log after the application closes.

On the next startup, before opening the new Serilog file:

1. if `CalradiaForge_Latest.log` is missing, continue normally,
2. if present, read its last-write timestamp,
3. fall back to creation time only if last-write time cannot be obtained,
4. rename it using:

   ```text
   CalradiaForge_yyyy-MM-dd_HH-mm.log
   ```

5. precision stops at minutes,
6. do not add numeric collision suffixes,
7. do not overwrite an existing archive,
8. destination collision is an ordinary rename failure,
9. rename failure is nonfatal and leaves the existing archive untouched,
10. record only best-effort pre-Serilog diagnostics,
11. attempt to truncate/overwrite `Latest` for the new session,
12. if truncation fails, allow the file sink to attempt append-compatible opening,
13. if the final logger cannot initialize, route to the normal bootstrap-failure path.

Do not invent recovery filenames or complex archive preservation.

---

# 14. Seven-Day Retention

Retention is fixed:

```text
7 days
```

At startup, after previous-`Latest` archival handling and before the new active logger opens:

- delete archived CalradiaForge logs older than seven days,
- never include `CalradiaForge_Latest.log`,
- ignore missing files,
- ignore individual archive deletion failures,
- do not expose a configurable retention setting.

---

# 15. Startup Coordinator and Deferred MainWindow Resolution

Use one DI-resolved `ApplicationStartupCoordinator`.

Do not constructor-inject `MainWindow` directly into it, because that would construct the shell/page graph before language/EULA gates.

Use a narrow typed deferred `MainWindow` factory/provider.

Locked startup sequence:

1. `App` creates the collection and registrations.
2. `App` builds the validated root provider.
3. `App` resolves `ApplicationStartupCoordinator`; DI constructs config/logging/startup dependencies.
4. Log application startup.
5. Initialize translation infrastructure.
6. Run first-launch language selection.
7. Run EULA gate.
8. Run Phase 5 game detection/config validation.
9. Run approved Phase 6.A mod-pipeline startup initialization.
10. Load/validate modpack state against the accepted module snapshot.
11. Start the notification drain waiting for UI readiness.
12. Resolve `MainWindow` and retained pages for the first time.
13. Assign `Application.Current.MainWindow`.
14. Show `MainWindow`.
15. `MainWindow.Loaded` signals toast-host readiness.
16. Drain queued startup notifications FIFO.

If EULA is declined or fatal startup fails before shell creation, do not resolve/show `MainWindow`.

Constructors should capture dependencies; explicit methods perform behavior, except the locked settings load/default/save construction flow required to produce usable settings instances.

---

# 16. Dedicated Startup Notification Drain

Retain one Core-safe `StartupNotificationQueue` singleton.

Add one UI `StartupNotificationDrainCoordinator` singleton.

The drain coordinator owns:

- one-time drain task,
- readiness waiting,
- cancellation,
- mapping Core notifications to UI toasts,
- drain errors,
- completion.

`MainWindow` only signals that the toast host is loaded/ready.

Rules:

- FIFO,
- no polling,
- no arbitrary delay,
- no dedicated background thread,
- queue/coordinator remain provider-owned,
- active waiters/subscriptions are released when draining completes,
- pacing/deduplication/severity expansion is deferred unless already required.

---

# 17. Dialog Services

Represent application-level modal interactions through dedicated UI dialog services.

Register dialog services as singletons. Each invocation creates a fresh WPF dialog window.

Use this pattern for:

- language selection,
- EULA acceptance,
- normal shutdown confirmation,
- restart confirmation,
- delayed-shutdown `Continue Waiting / Exit Anyway` choice,
- similar application-level modal workflows.

Coordinators depend on narrow dialog-service contracts, not concrete windows or a global provider.

Dialog windows are not application singletons.

---

# 18. App-Owned Lifecycle Interface

Define a narrow interface implemented by the existing WPF `App` host, conceptually:

```csharp
public interface IApplicationLifetime
{
    Task RequestShutdownAsync(ShutdownReason reason);
    Task RequestRestartAsync(RestartReason reason);
}
```

Register the existing `App` instance:

```csharp
services.AddSingleton<IApplicationLifetime>(this);
```

DI does not construct or dispose `App`.

UI consumers receive the interface through constructor injection.

Do not expose:

- `IServiceProvider`,
- windows,
- settings,
- logger internals,
- general service access.

---

# 19. Explicit Async Shutdown

Use:

```text
ShutdownMode = OnExplicitShutdown
```

Provide one guarded app-owned shutdown path.

`MainWindow` close is intercepted. It never disposes the provider.

Normal shutdown sequence:

1. confirm before commitment,
2. guard duplicate requests,
3. resolve/use `ApplicationShutdownCoordinator`,
4. stop new workflow admission,
5. request cancellation,
6. await safe quiescence,
7. stop notification drain,
8. persist required state,
9. return control to `App`,
10. `App` disposes the root provider asynchronously,
11. provider disposal flushes/closes Serilog,
12. leave `CalradiaForge_Latest.log` in place,
13. call WPF shutdown.

`OnExit` is final safety/fallback only. It must not duplicate normal disposal.

---

# 20. Explicit Shutdown Dependencies

`ApplicationShutdownCoordinator` has explicit constructor dependencies on known lifecycle/state owners.

Do not use:

- a generic shutdown-participant registry,
- WPF exit-event subscriptions as the primary cleanup mechanism,
- service disposal as a substitute for quiescence.

Services expose idempotent async stop/quiescence methods.

They do not:

- dispose the root provider,
- close Serilog,
- terminate WPF,
- dispose dependencies they do not own.

Phase 6.A supplies the mod-pipeline quiescence dependency.

---

# 21. Shutdown and Restart Confirmation Before Commitment

Normal MainWindow close first inspects the authoritative active mod-pipeline operation:

- when no meaningful interruptible work is active, close commits without a confirmation dialog;
- when tracked work is active, one themed shutdown warning presents the localized operation summary;
- cancelling or closing that warning aborts shutdown without beginning commitment;
- accepting the warning commits shutdown through the guarded async path.

Restart always presents its ordinary themed confirmation first. If that confirmation is accepted, one authoritative active-operation snapshot determines whether a second interruption warning is required. Cancelling or closing either restart prompt aborts restart without reverting settings already persisted for the replacement process.

Once shutdown begins, it is irreversible. The application will not resume normal operation after provider disposal begins.

Fatal/noninteractive shutdown paths bypass normal confirmation.

---

# 22. Bounded Graceful Shutdown

Initial cooperative shutdown budget:

```text
15 seconds
```

If interactive shutdown has not quiesced within the budget, show:

```text
Continue Waiting
Exit Anyway
```

`Continue Waiting` grants another bounded 15-second interval.

Do not wait indefinitely without renewed user choice.

`Exit Anyway` means controlled best effort, not process kill:

- continue remaining safe persistence,
- dispose provider where possible,
- complete shutdown.

Do not use:

- `Environment.Exit`,
- `Process.Kill`,
- fire-and-forget shutdown.

For fatal startup, EULA rejection, provider-build failure, OS shutdown, or noninteractive paths: do not show the timeout choice; perform bounded best effort and continue.

Collect shutdown failures so one stage does not automatically skip unrelated safe stages.

---

# 23. Classified Global Exception Policy

`App.xaml.cs` owns global host exception subscriptions and the top-level startup boundary.

## Startup/provider failures

Catch provider construction, root resolution, and startup-coordinator failures at the app boundary.

- before provider/logger availability: use transitional diagnostics in 6.B,
- when provider exists: request controlled fatal shutdown,
- do not show `MainWindow` after failed startup.

## WPF dispatcher exceptions

Treat as fatal:

1. log fatal exception,
2. mark handled only long enough to avoid uncontrolled WPF termination,
3. show fatal-error information when possible,
4. invoke controlled fatal shutdown,
5. do not resume normal app operation.

## Unobserved task exceptions

- log the exception,
- call `SetObserved()`,
- continue by default.

Application-owned workflows must still await/handle their tasks explicitly.

## AppDomain unhandled exceptions

- best-effort fatal logging,
- attempt controlled cleanup only when runtime termination is not already underway and it is safe,
- do not assume interactive 15-second prompting is available.

---

# 24. App-Owned Restart

Restart is an app-owned exit mode using the same controlled lifecycle.

UI requests restart through `IApplicationLifetime`; UI does not start processes.

Sequence:

1. confirm before commitment,
2. quiesce workflows,
3. persist state,
4. dispose provider/logger,
5. release active files,
6. start a replacement process from the current executable path,
7. finish old WPF shutdown.

Start the replacement only after provider disposal.

Use the current executable path rather than a hard-coded filename.

If replacement launch fails after disposal, the old disposed app still exits; do not attempt to reactivate it.

---

# 25. Phase 6.B Transitional Legacy Logger Boundary

Do not rewrite all legacy logger call sites in 6.B.

The existing legacy `Logger` remains temporarily for call sites and bootstrap diagnostics that cannot yet use final Serilog.

Do not rewrite it into the final emergency writer during 6.B.

Phase 6.C:

- migrates all normal callers,
- removes the general-purpose legacy logger,
- adds the narrow `EmergencyStartupLogWriter`.

No public release occurs between incomplete Phase 6 subphases if transitional overlap would expose inconsistent behavior.

---

# 26. Phase 6.B Explicit Exclusions

Do not include:

- broad `Logger.Instance` migration,
- final emergency-writer replacement,
- generic Host Builder,
- `Microsoft.Extensions.Logging.ILogger<T>`,
- `Serilog.Extensions.Hosting`,
- `Serilog.Extensions.Logging`,
- `Serilog.Settings.Configuration`,
- `Microsoft.Extensions.Configuration.Json`,
- custom DI scopes,
- service locator,
- full MVVM rewrite,
- transient page navigation redesign,
- Steam detection redesign,
- mod-pipeline redesign already owned by 6.A,
- Nexus implementation,
- unrelated installer behavior changes.

---

# 27. Required Tests and Verification

Require focused evidence for:

## Composition

- exactly one provider,
- provider validation enabled,
- Core/UI modules extend the same collection,
- no temporary provider,
- intended singleton identity,
- constructor resolution for all registered roots.

## UI startup

- `StartupUri` cannot create a second window,
- `MainWindow` resolves once,
- retained pages resolve once,
- EULA rejection does not resolve/show shell,
- language/EULA dialogs use fresh windows,
- startup notifications wait and drain FIFO after readiness.

## Settings/logging bootstrap

- missing/empty/malformed settings regenerate defaults using the locked object flow,
- `LoggingSettings.DebugMode` is available before logger construction,
- one logger is constructed and assigned globally,
- provider disposal is the only close path,
- debug mode controls new-process minimum level,
- changing debug mode persists immediately and prompts in both directions.

## Log files

- previous `Latest` archives at startup using last-write time and minute precision,
- missing `Latest` is a no-op,
- creation-time fallback works,
- collision/rename failure does not overwrite archive,
- rename failure proceeds to overwrite/append attempt,
- seven-day cleanup excludes active `Latest`,
- shutdown leaves `Latest` available.

## Shutdown/restart

- duplicate requests are guarded,
- idle normal close commits without a dialog,
- active normal close shows one warning before commitment,
- restart shows its ordinary confirmation and only adds a second warning for active work,
- cancel/custom-close prevents shutdown or restart at every applicable prompt,
- 15-second timeout choice behaves correctly,
- continue-waiting adds a bounded interval,
- exit-anyway performs best effort without process kill,
- active mod work is cancelled/quiesced through Phase 6.A contract,
- provider disposes exactly once,
- restart starts replacement only after disposal,
- restart launch failure still exits safely.

## Exceptions

- dispatcher exception routes to fatal shutdown rather than normal continuation,
- unobserved task exception is logged/observed without automatic fatal shutdown,
- provider/startup failure does not show shell,
- AppDomain termination path remains best effort.

## Regression

- current Phase 5 behavior remains intact,
- solution builds,
- all tests pass,
- Core remains WPF-free.

---

# 28. Exit Criteria

Phase 6.B is complete only when:

- one validated provider owns the application graph,
- startup is coordinator-driven without `StartupUri`,
- static service access is removed from the approved surface,
- page lifetime is preserved through DI,
- settings names/responsibilities match the locked model,
- logging initializes after `LoggingSettings.DebugMode`,
- one global/shared logger has one provider-disposal close path,
- startup-only archival and seven-day retention work,
- controlled shutdown/restart work through `IApplicationLifetime`,
- Phase 6.A quiescence is integrated,
- global exceptions follow the classified policy,
- targeted tests and manual WPF smoke checks pass,
- legacy logger call-site migration remains deferred to 6.C.

---

# 29. Manual WPF Smoke Matrix

Document manual checks for:

- normal startup with valid existing config,
- first-run language selection,
- EULA accept/decline,
- Steam detected with Workshop,
- Steam detected without Workshop,
- manual configuration fallback,
- startup mod initialization,
- queued toast delivery after MainWindow load,
- page navigation/state preservation,
- debug toggle enable/disable and restart prompt,
- decline restart then exit normally,
- normal close confirm/cancel,
- close during active mod work,
- timeout continue-waiting,
- timeout exit-anyway,
- controlled restart,
- fatal dispatcher handling,
- latest log remaining after shutdown,
- previous latest archiving on next startup.

---

# 30. Documentation Handoff

After future implementation, migrate only proven behavior into canonical architecture and lifecycle documents.

Do not mark Phase 6.B complete in this documentation-only task.

## Implementation Stop Conditions

Stop and request owner direction rather than creating a second provider, custom WPF scope model, transient page redesign, alternate settings/default-initialization architecture, session logging-state object, second logging pipeline, or additional normal logger close path.
