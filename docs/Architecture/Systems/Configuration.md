# Configuration

## Currently Implemented
- `ConfigFileManager` owns JSON-backed key/value persistence in `config.json` and saves through atomic same-directory file replacement.
- First-run or newly introduced missing defaults are applied as one configuration update, avoiding repeated replacement of the same file during startup.
- Existing valid empty optional settings do not trigger a startup rewrite; ordinary setting changes continue to persist immediately.
- The shared atomic writer uses bounded retries for transient Windows sharing, lock, and replacement failures.
- Invalid config JSON is treated as an empty settings set so the typed settings facades can seed defaults without crashing normal startup.
- `AppSettings` provides typed application settings and change notifications.
- `LoggingSettings` owns the persisted `DebugMode` preference required before Serilog construction. It shares the same `ConfigFileManager` and `config.json`; it is not a second configuration store.
- `ConfigFileManager` and `LoggingSettings` perform no ordinary logging during pre-Serilog bootstrap; unrecoverable bootstrap failures are diagnosed by the app-owned emergency startup writer.
- Log archive retention is a fixed logging policy and is no longer configurable. Startup removes the retired `LogFileDaysToKeep` key when present.
- `AppPaths` resolves and creates the app's working directories.
- Configuration stores the resolved provider, game path, launcher path, optional Workshop path, and optional BLSE path. `GameProvider.Steam` with an empty Workshop path is valid and keeps local module scanning available.

## Architecture Guidance
- `ConfigFileManager` is the only component that performs config JSON read/write.
- `AppSettings` and `LoggingSettings` are typed facades over the same `ConfigFileManager`.
- Configuration changes are saved immediately through the underlying store.
- Config replacement must not expose a partially written destination file.
- Paths are resolved in Core, not in UI code-behind.
- Valid startup configuration is reused without detection and preserves its current Workshop value. Any automatic detection run replaces or clears Workshop from current Steam metadata.
- Total automatic failure clears all provider-derived paths and sets `ManualConfiguration`.
- Manual game selection validates the game and launcher, conservatively infers a provider from normalized path signatures, clears Workshop, and replaces or clears optional BLSE state. Invalid manual input leaves settings unchanged.
- Manual Workshop selection is Steam-only and updates only `SteamWorkshopFolderPath`; invalid input preserves the previous value.

## Key Files
- `source/CalradiaForge.Core/Infra/Config/ConfigFileManager.cs`
- `source/CalradiaForge.Core/Infra/Config/AppSettings.cs`
- `source/CalradiaForge.Core/Infra/Config/LoggingSettings.cs`
- `source/CalradiaForge.Core/Infra/Paths/AppPaths.cs`
- `source/CalradiaForge.Core/Infra/GamePlatform/GamePlatformDetectionResolver.cs`
- `source/CalradiaForge.Core/Infra/GamePlatform/GameDetectionService.cs`
- `source/CalradiaForge.Core/Infra/GamePlatform/Steam/SteamInstallationResolver.cs`

## Deferred / Future Work
- New config keys should be added only when source code confirms the need.
