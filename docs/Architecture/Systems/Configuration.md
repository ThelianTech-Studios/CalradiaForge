# Configuration

## Currently Implemented
- `AppConfig` owns JSON-backed persistence and saves through atomic same-directory file replacement.
- Invalid config JSON is logged and treated as an empty settings set so `AppConfigSettings` can seed typed defaults without crashing normal startup.
- `AppConfigSettings` provides typed access and change notifications.
- `AppPaths` resolves and creates the app's working directories.
- Steam configuration stores the resolved provider, game path, launcher path, and optional Workshop path. `GameProvider.Steam` with an empty Workshop path is valid and keeps local module scanning available.

## Architecture Guidance
- `AppConfig` is the only component that performs config JSON read/write.
- `AppConfigSettings` is a typed facade over `AppConfig`.
- Configuration changes are saved immediately through the underlying store.
- Config replacement must not expose a partially written destination file.
- Paths are resolved in Core, not in UI code-behind.
- Normal detection preserves a valid configured Workshop path; explicit re-detection recomputes it from current Steam metadata. An unmatched valid manual game folder is classified as `StandAlone` and stale Workshop configuration is cleared.

## Key Files
- `source/CalradiaForge.Core/Infra/Config/AppConfig.cs`
- `source/CalradiaForge.Core/Infra/Config/AppConfigSettings.cs`
- `source/CalradiaForge.Core/Infra/Paths/AppPaths.cs`
- `source/CalradiaForge.Core/Infra/Paths/GamePathsHelper.cs`
- `source/CalradiaForge.Core/Infra/Paths/SteamInstallationResolver.cs`

## Deferred / Future Work
- New config keys should be added only when source code confirms the need.
