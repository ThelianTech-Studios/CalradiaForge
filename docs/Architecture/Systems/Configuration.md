# Configuration

## Currently Implemented
- `AppConfig` owns JSON-backed persistence and saves through atomic same-directory file replacement.
- Invalid config JSON is logged and treated as an empty settings set so `AppConfigSettings` can seed typed defaults without crashing normal startup.
- `AppConfigSettings` provides typed access and change notifications.
- `AppPaths` resolves and creates the app's working directories.
- Configuration stores the resolved provider, game path, launcher path, optional Workshop path, and optional BLSE path. `GameProvider.Steam` with an empty Workshop path is valid and keeps local module scanning available.

## Architecture Guidance
- `AppConfig` is the only component that performs config JSON read/write.
- `AppConfigSettings` is a typed facade over `AppConfig`.
- Configuration changes are saved immediately through the underlying store.
- Config replacement must not expose a partially written destination file.
- Paths are resolved in Core, not in UI code-behind.
- Valid startup configuration is reused without detection and preserves its current Workshop value. Any automatic detection run replaces or clears Workshop from current Steam metadata.
- Total automatic failure clears all provider-derived paths and sets `ManualConfiguration`.
- Manual game selection validates the game and launcher, conservatively infers a provider from normalized path signatures, clears Workshop, and replaces or clears optional BLSE state. Invalid manual input leaves settings unchanged.
- Manual Workshop selection is Steam-only and updates only `SteamWorkshopFolderPath`; invalid input preserves the previous value.

## Key Files
- `source/CalradiaForge.Core/Infra/Config/AppConfig.cs`
- `source/CalradiaForge.Core/Infra/Config/AppConfigSettings.cs`
- `source/CalradiaForge.Core/Infra/Paths/AppPaths.cs`
- `source/CalradiaForge.Core/Infra/GamePlatform/GamePlatformDetectionResolver.cs`
- `source/CalradiaForge.Core/Infra/GamePlatform/GameDetectionService.cs`
- `source/CalradiaForge.Core/Infra/GamePlatform/Steam/SteamInstallationResolver.cs`

## Deferred / Future Work
- New config keys should be added only when source code confirms the need.
