# Launcher

## Currently Implemented
- `LauncherPage` is the retained primary UI surface for installed-module
  visibility, load-order preparation, modpack application, and launch controls.
- The visible primary-navigation label and localization identity are
  **Launcher**.
- `GameLauncher` starts Bannerlord or BLSE with the active load order.
- `GameDetectionService` coordinates startup, re-detection, and manual configuration through `GamePlatformDetectionResolver`; Steam library and Workshop resolution are described in [Platform and Path Detection](PlatformAndPathDetection.md).
- `GamePathValidator` validates user-selected game folders and executables.
- Steam launches are auto-started when needed.
- Epic Games and Game Pass are currently blocked from direct launch in the implemented launcher flow.

## Architecture Guidance
- Launch logic belongs in Core, not UI.
- The UI should gather intent and pass the validated load order to the launcher.
- Steam support can launch directly with the expected environment setup.
- Epic and Game Pass are intentionally constrained until the dedicated platform path is implemented.

## Key Files
- `source/CalradiaForge.Core/Infra/Launch/GameLauncher.cs`
- `source/CalradiaForge.Core/Infra/Launch/LaunchTarget.cs`
- `source/CalradiaForge.Core/Infra/GamePlatform/GameDetectionService.cs`
- `source/CalradiaForge.Core/Infra/GamePlatform/GamePlatformDetectionResolver.cs`
- `source/CalradiaForge.Core/Infra/Paths/GamePathValidator.cs`
- `source/CalradiaForge.Core/Infra/GamePlatform/GameProvider.cs`

## Deferred / Future Work

- `LauncherPage` remains distinct from the implemented Core `GameLauncher`.
  Phase 8 moves its temporary presentation reconciliation to
  `LauncherViewModel`; a future dedicated `ModsPage` is not implemented.
- Epic/Game Pass launch backend and config-write support remain deferred.
