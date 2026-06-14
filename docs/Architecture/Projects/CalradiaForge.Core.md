# CalradiaForge.Core

## Project Overview
CalradiaForge.Core contains the app's runtime logic and shared data models.
It is the foundation for mod management, modpack persistence, configuration, localization, logging, launch behavior, and EULA handling.

## Purpose
Provide the deterministic, UI-agnostic behavior that the WPF app consumes.

## Responsibilities
### Currently Implemented
- Resolve application paths and create required folders.
- Persist and read configuration data.
- Provide strongly typed configuration access.
- Parse and scan Bannerlord modules.
- Install mods and handle archive extraction.
- Manage BLSE installation as a special-case archive flow.
- Store mod cache and modpack data.
- Launch Bannerlord and BLSE.
- Load localization files and apply language settings.
- Load and record EULA acceptance state.
- Emit diagnostic logs.

## Public Boundaries
- Public services are consumed by `CalradiaForge.UI` and `CalradiaForge.ConsoleUtils`.
- Core does not reference WPF.
- Data helpers own file I/O for their domain.

## Dependencies
### Currently Implemented
- `Newtonsoft.Json`
- `SevenZipWrapper`

### Upstream Consumers
- `CalradiaForge.UI`
- `CalradiaForge.ConsoleUtils`
- `CalradiaForge.Nexus` depends on Core when it gains implementation

## Major Systems
- Configuration: `AppConfig`, `AppConfigSettings`, `AppPaths`
- Logging: `Logger`
- Localization: `TranslationService`, `TranslationManager`, `TranslationStrings`
- Paths and platform detection: `GamePathsHelper`, `GamePathValidator`, `GameProvider`, `EpicDetector`, `EpicManifestReader`
- Launch: `GameLauncher`, `LaunchTarget`
- EULA: `EulaService`
- Mods: `ModService`, `ModScanner`, `ModParser`, `ModInstaller`, `ModExtractor`, `BLSEInstaller`, `ModsData`
- Modpacks: `ModpackService`, `ModpackData`, `ModpackFileHelper`, `VanillaModules`, `NovusPresetConverter`

## Design Constraints
- Keep Core free of WPF references.
- Keep UI behavior out of Core services.
- Preserve `AppConfig` as the only JSON-backed config store.
- Preserve `ModInstaller` and `ModExtractor` as the authority for archive install flow.
- Preserve `ModpackData` and `ModsData` as the file I/O boundary for their domains.

## Known Extension Points
- Nexus integration should not be added directly to Core.
- New configuration values belong in `AppConfigSettings` with matching documentation updates.
- New systems should follow the existing service/data-helper split.

## Deferred Work
- Epic and Game Pass launcher support remains intentionally constrained by the current launcher behavior.
- Nexus networking is intentionally not part of Core.
