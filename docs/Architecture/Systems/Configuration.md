# Configuration

## Currently Implemented
- `AppConfig` owns JSON-backed persistence and saves through atomic same-directory file replacement.
- Invalid config JSON is logged and treated as an empty settings set so `AppConfigSettings` can seed typed defaults without crashing normal startup.
- `AppConfigSettings` provides typed access and change notifications.
- `AppPaths` resolves and creates the app's working directories.

## Architecture Guidance
- `AppConfig` is the only component that performs config JSON read/write.
- `AppConfigSettings` is a typed facade over `AppConfig`.
- Configuration changes are saved immediately through the underlying store.
- Config replacement must not expose a partially written destination file.
- Paths are resolved in Core, not in UI code-behind.

## Key Files
- `source/CalradiaForge.Core/Infra/Config/AppConfig.cs`
- `source/CalradiaForge.Core/Infra/Config/AppConfigSettings.cs`
- `source/CalradiaForge.Core/Infra/Paths/AppPaths.cs`

## Deferred / Future Work
- New config keys should be added only when source code confirms the need.
