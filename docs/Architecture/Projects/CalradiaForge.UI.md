# CalradiaForge.UI

## Project Overview
CalradiaForge.UI is the WPF presentation layer.
It owns application startup, windows, pages, styling, and binding to Core services.

## Purpose
Render the user interface and translate user intent into calls to Core services.

## Responsibilities
### Currently Implemented
- Host the main window and page navigation.
- Present mods, modpacks, settings, FAQ, EULA, language selection, and themed lifecycle-confirmation views.
- Surface toast notifications.
- Own one validated application provider through `App`; Core and UI registration modules extend the same collection.
- Coordinate ordered startup through `ApplicationStartupCoordinator` and defer shell/page resolution until language, EULA, detection, pipeline, modpack, and notification gates pass.
- Coordinate explicit shutdown/restart through `IApplicationLifetime` and `ApplicationShutdownCoordinator`.
- Route startup, explicit refresh, install admission, cache clearing, and modpack module reads through the accepted mod-pipeline boundary.
- Signal MainWindow readiness and map queued Core startup notifications into the existing toast surface.
- Invoke the Core game-detection workflow for Settings re-detection and bounded manual path configuration.
- Bind translated strings into the UI.
- Persist user-driven settings through Core services.
- Constructor-inject retained singleton pages and application services instead of using static `App.*` service access.

## Public Boundaries
- UI should not contain filesystem or game-specific business logic.
- UI should not own long-running install or scan work.
- UI can decide when to call a service, but not how the service performs the work.

## Dependencies
### Currently Implemented
- `CalradiaForge.Core`
- WPF runtime
- MahApps.Metro
- gong-wpf-dragdrop
- Microsoft.Extensions.DependencyInjection

## Major Systems
- `App.xaml` startup wiring
- `MainWindow`
- `ModsPage`
- `ModpacksPage`
- `SettingsPage`
- `FaqPage`
- `EulaWindow`
- `LanguageSelectWindow`
- `ConfirmDialogWindow`
- Toast overlay and toast view models
- UI composition, application-lifecycle, and dialog services under `Composition`, `Lifecycle`, and `Dialogs`
- Resource dictionaries for theming and page styling

## Design Constraints
- Keep WPF code in the presentation layer.
- Keep business rules in Core services.
- Avoid duplicating persistence or parsing logic in code-behind.
- Keep navigation-driven refreshes synchronized with the Core services rather than reimplementing them in UI.
- Capture one accepted module snapshot for each UI/modpack operation instead of reading mutable scan lists piecemeal.
- Preserve one `MainWindow` and one instance of each primary page for the application lifetime.
- Dialog services create a fresh modal window for every invocation.

## Known Extension Points
- New pages should consume existing services instead of duplicating logic.
- Future Nexus UI surfaces should call into the Nexus assembly rather than implementing network behavior in page code.

## Deferred Work
- **Planned Phase 7 only:** behavior-preserving `ModsPage` -> `LauncherPage`
  rename and **Launcher** label; an explicitly activated application-lifetime
  presenter owns correlated progress/final toast lifecycle through generic
  `ToastService`. The temporary page reconciles the accepted snapshot before
  semantic completion; Phase 8 transfers this to `LauncherViewModel`. `ModsPage`
  remains reserved for future mod management.
- Any new Nexus UI is deferred until the Nexus assembly has implementation behind it.
- Full MVVM extraction, transient page navigation, and navigation scopes remain deferred.
