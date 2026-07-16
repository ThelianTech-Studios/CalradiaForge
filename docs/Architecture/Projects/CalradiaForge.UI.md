# CalradiaForge.UI

## Project Overview
CalradiaForge.UI is the WPF presentation layer.
It owns application startup, windows, pages, styling, and binding to Core services.

## Purpose
Render the user interface and translate user intent into calls to Core services.

## Responsibilities
### Currently Implemented
- Host the main window and page navigation.
- Present mods, modpacks, settings, FAQ, EULA, and language selection views.
- Surface toast notifications.
- Coordinate startup sequence and service initialization.
- Signal MainWindow readiness and map queued Core startup notifications into the existing toast surface.
- Invoke the Core game-detection workflow for Settings re-detection and bounded manual path configuration.
- Bind translated strings into the UI.
- Persist user-driven settings through Core services.

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

## Major Systems
- `App.xaml` startup wiring
- `MainWindow`
- `ModsPage`
- `ModpacksPage`
- `SettingsPage`
- `FaqPage`
- `EulaWindow`
- `LanguageSelectWindow`
- Toast overlay and toast view models
- Resource dictionaries for theming and page styling

## Design Constraints
- Keep WPF code in the presentation layer.
- Keep business rules in Core services.
- Avoid duplicating persistence or parsing logic in code-behind.
- Keep navigation-driven refreshes synchronized with the Core services rather than reimplementing them in UI.

## Known Extension Points
- New pages should consume existing services instead of duplicating logic.
- Future Nexus UI surfaces should call into the Nexus assembly rather than implementing network behavior in page code.

## Deferred Work
- Any new Nexus UI is deferred until the Nexus assembly has implementation behind it.
