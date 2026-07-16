# Platform and Path Detection

## Currently Implemented

- `WindowsSteamClientRootProvider` reads the Steam client discovery root from the current-user Windows registry boundary.
- `SteamInstallationResolver` treats the client root as one candidate, parses `steamapps/libraryfolders.vdf`, normalizes and case-insensitively deduplicates registered libraries, and validates `appmanifest_261550.acf` metadata before accepting a Bannerlord installation.
- Steam game resolution and Workshop resolution are independent. A valid Steam game remains `GameProvider.Steam` when no Workshop directory exists, and local modules remain scannable.
- Workshop selection prefers the Bannerlord library, then one deterministic alternate library with valid `appworkshop_261550.acf` evidence preferred. Multiple Workshop roots are never merged.
- `GamePlatformDetectionResolver` runs the supported automatic workflow, validates the game and launcher paths before committing settings, and clears stale provider-derived paths on total failure before setting `ManualConfiguration`.
- Automatic detection replaces or clears the configured Workshop path. Valid startup configuration is reused without rerunning detection, so its current Workshop value is preserved.
- `GameDetectionService` owns startup reuse/detection, explicit re-detection, manual game-folder selection, and Steam-only manual Workshop selection. Startup initialization is `void` and commits through `AppConfigSettings`; explicit re-detection returns `GameProvider`; manual operations return `bool`.
- Manual game selection uses conservative normalized path signatures, clears the prior Workshop value, resolves the standard launcher, and replaces or clears optional BLSE state. It does not reverse-match through Steam metadata.
- Startup detection feedback is held in a Core-safe FIFO `StartupNotificationQueue`. `MainWindow` begins the waiter during construction, signals readiness from `Loaded`, maps notifications into the existing toast surface, and cancels the wait when the window closes.
- `AppConfigSettings.ModulesDirectoryPath` is the authoritative derived Modules path. The legacy `GamePathsHelper` has been removed.

## Boundaries

- UI decides when detection or manual configuration runs; Core decides validation, provider ordering, path selection, and committed settings.
- `ISteamClientRootProvider` isolates registry lookup, and `ISteamInstallationResolver` owns Steam metadata parsing and deterministic candidate selection.
- `AppConfigSettings` remains the sole committed provider/game/launcher/Workshop/BLSE state.
- Core notifications contain only toast-relevant text and severity. They contain no WPF types, windows, controls, settings, or filesystem paths.
- `ModScanner` consumes configured paths. It does not discover Steam libraries, infer providers, or parse Steam metadata.
- Core remains free of WPF references.

## Key Files

- `source/CalradiaForge.Core/Infra/GamePlatform/GamePlatformDetectionResolver.cs`
- `source/CalradiaForge.Core/Infra/GamePlatform/GameDetectionService.cs`
- `source/CalradiaForge.Core/Infra/GamePlatform/StartupNotificationQueue.cs`
- `source/CalradiaForge.Core/Infra/GamePlatform/Steam/ISteamClientRootProvider.cs`
- `source/CalradiaForge.Core/Infra/GamePlatform/Steam/SteamInstallationResolver.cs`
- `source/CalradiaForge.Core/Infra/GamePlatform/Steam/SteamResolutionResult.cs`
- `source/CalradiaForge.Core/Infra/Paths/GamePathValidator.cs`
- `source/CalradiaForge.Core/Models/StartupNotification.cs`
- `source/CalradiaForge.UI/App.xaml.cs`
- `source/CalradiaForge.UI/Views/MainWindow.xaml.cs`
- `source/CalradiaForge.UI/Pages/SettingsPage.xaml.cs`

## Verification Boundary

Automated tests use temporary fake roots and injected client-root providers. Coverage includes same-root and split-root installs, malformed or unsafe metadata, deterministic Workshop precedence, Steam without Workshop, direct configuration commits and clearing, startup reuse and queued feedback, manual configuration, invalid-scan cache preservation, an exact 11-module scan, and a Novus preset regression.

These tests do not read a real registry or Steam installation and do not prove real Epic, Game Pass, or GOG support. Owner smoke testing on a real Windows Steam split-library installation and manual WPF inspection remain required before release approval.

## Deferred / Future Work

- Phase 6.A registers the finalized resolver, workflow, notification queue, and platform dependencies in the application composition root; it must not redesign Phase 5 behavior.
- A general structured scanner-result/cache contract remains Phase 7 work.
- Epic, Game Pass, and GOG real-world automatic detection/support claims remain deferred. The existing Epic branch stays disabled by default.
