# CalradiaForge.Tests

## Project Overview

`CalradiaForge.Tests` is the xUnit correctness-test project. Its project-level folders mirror the application projects so coverage remains owned by the layer it verifies.

## Currently Implemented

- `Core.Tests` contains deterministic Core unit, regression, result, and integration-style filesystem tests.
- `UI.Tests` contains focused registration, provider/logger identity, startup-notification drain, and shutdown-coordination tests.
- Phase 5 coverage exercises Steam metadata resolution, automatic and manual detection workflows, the startup notification queue, invalid-configuration cache preservation, split-root scanning, and Novus preset validation.
- Phase 6.A coverage exercises structured local/Workshop scan outcomes, parse and duplicate diagnostics, accepted-snapshot/cache commit gating, startup/refresh reuse, deterministic busy rejection, cooperative cancellation, stop-admission, quiescence, and awaitable installer completion.
- Phase 7 coverage exercises install terminal-status precedence, progress
  correlation, unblocking and private reconciliation sequencing, bounded
  cancellation finalization, exactly-once quiescence, application-lifetime
  notification presentation, DI activation, and Launcher identity/localization
  boundaries.
- Fixtures use isolated operating-system temporary directories and generated XML/zip/JSON data.
- Archive and installer tests continue through `ModExtractor`, `ModInstaller`, `ModsData`, and `ModpackData` rather than replacing authoritative owners.
- The test project targets `net10.0-windows7.0`, enables WPF compilation, and references both Core and UI so the Phase 6.B composition graph can be verified.
- `Nexus.Tests` remains reserved. Current UI tests do not claim full WPF automation or interactive dialog coverage.

## Dependencies

- xUnit and the .NET test SDK package set selected in the project file.
- `CalradiaForge.Core`.
- `CalradiaForge.UI`.

## Design Constraints

- Do not reference WPF from Core tests.
- Do not use real Steam libraries, Bannerlord installations, app configuration, Nexus credentials, or network access.
- Do not add timing assertions to correctness tests.
- Treat real Windows/Steam split-library behavior and WPF toast interaction as owner smoke-test gates rather than automated-test claims.
- Keep interactive startup, dialogs, navigation, shutdown/restart, and fatal-error presentation in the manual smoke matrix even when their underlying coordinators have automated coverage.
- Keep each test inside the folder for its owning application project.
