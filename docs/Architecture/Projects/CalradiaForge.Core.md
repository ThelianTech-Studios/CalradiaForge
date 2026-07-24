# CalradiaForge.Core

## Project Overview
CalradiaForge.Core contains the app's runtime logic and shared data models.
It is the foundation for mod management, modpack persistence, configuration, localization, logging, launch behavior, and EULA handling.

## Purpose
Provide the deterministic, UI-agnostic behavior that the WPF app consumes.

## Responsibilities
### Currently Implemented
- Resolve application paths and create required folders.
- Resolve Steam client, library, Bannerlord, and Workshop paths through testable Core boundaries.
- Coordinate startup reuse, automatic re-detection, manual game selection, manual Steam Workshop selection, and pre-UI startup notifications without WPF dependencies.
- Persist and read configuration data.
- Provide strongly typed configuration access.
- Parse and scan Bannerlord modules.
- Coordinate startup/refresh scans, per-root completeness, cache commit authorization, atomic accepted module snapshots, deterministic operation admission, cancellation, and quiescence.
- Install mods and handle archive extraction.
- Manage BLSE installation as a special-case archive flow.
- Store mod cache and modpack data.
- Atomically persist configuration, mod-cache, named-modpack, and last-used JSON data; recover a corrupt current mod cache from its validated backup.
- Launch Bannerlord and BLSE.
- Load localization files and apply language settings.
- Load and record EULA acceptance state.
- Emit diagnostic logs.
- Contribute Core services to a caller-owned `IServiceCollection` without building or exposing a provider.

## Public Boundaries
- Public services are consumed by `CalradiaForge.UI` and `CalradiaForge.ConsoleUtils`.
- Core does not reference WPF.
- Data helpers own file I/O for their domain.

## Dependencies
### Currently Implemented
- `Newtonsoft.Json`
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Serilog` and the approved file/async/enrichment packages
- `SevenZipWrapper`

### Upstream Consumers
- `CalradiaForge.UI`
- `CalradiaForge.ConsoleUtils`
- `CalradiaForge.Tests` exercises Core behavior through deterministic fake-root fixtures.
- `CalradiaForge.Benchmarks` measures selected Core workflows without changing production behavior.
- `CalradiaForge.Nexus` depends on Core when it gains implementation

## Major Systems
- Configuration: `ConfigFileManager`, `AppSettings`, `LoggingSettings`, `AppPaths`
- Dependency registration: `CalradiaForgeCoreServiceCollectionExtensions`, `CalradiaForgeCoreOptions`
- Logging: provider-owned `Serilog.ILogger`, global structured `Log.*` calls, `SerilogLoggerFactory`, `LogFileLifecycle`, and the error-only `EmergencyStartupLogWriter`
- Localization: `TranslationService`, `TranslationManager`, `TranslationStrings`
- Paths and platform detection: `GamePlatformDetectionResolver`, `GameDetectionService`, `StartupNotificationQueue`, `GamePathValidator`, `GameProvider`, `ISteamClientRootProvider`, `ISteamInstallationResolver`, `SteamInstallationResolver`, `SteamResolutionResult`, `EpicDetector`, `EpicManifestReader`
- Launch: `GameLauncher`, `LaunchTarget`
- EULA: `EulaService`
- Mods: `ModPipelineManager`, `IModScanner`, `ModScanner`, `ModParser`, `ModInstaller`, `ModExtractor`, `BLSEInstaller`, `ModsData`, `AcceptedModSnapshot`, `ModPipelineResult`, `ModScanResult`
- Modpacks: `ModpackService`, `ModpackData`, `ModpackFileHelper`, `VanillaModules`, `NovusPresetConverter`

## Design Constraints
- Keep Core free of WPF references.
- Keep UI behavior out of Core services.
- Preserve `ConfigFileManager` as the only JSON-backed config-file manager.
- Preserve `ModInstaller` and `ModExtractor` as the authority for archive install flow.
- Preserve `ModpackData` and `ModsData` as the file I/O boundary for their domains.
- Keep shared atomic-write mechanics internal to Core data/config owners rather than moving domain persistence into services or UI.
- Keep cache rotation/save authorization and accepted module-state publication in `ModPipelineManager`; rejected or incomplete scans preserve the prior snapshot.
- Core registration must not register WPF types or build an application provider.

## Known Extension Points
- Nexus integration should not be added directly to Core.
- New application configuration values belong in `AppSettings`; early logging preferences belong in `LoggingSettings`.
- New systems should follow the existing service/data-helper split.

## Deferred Work
- Epic and Game Pass launcher support remains intentionally constrained by the current launcher behavior.
- Nexus networking is intentionally not part of Core.
