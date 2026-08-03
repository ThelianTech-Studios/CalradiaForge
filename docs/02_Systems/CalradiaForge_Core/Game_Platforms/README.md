# Core Game Platforms

`GamePlatformDetectionResolver` and `GameDetectionService` coordinate supported
platform detection and configuration. The Steam path uses
`SteamInstallationResolver`, a Windows Steam-client-root provider, Valve key
value parsing, library/manifest discovery, and explicit resolution diagnostics.
The launcher also has manual game and Workshop configuration paths.

`GameProvider` distinguishes platform states used by the application. Epic
detector code exists in Core, but public compatibility claims remain governed by
[Application Compatibility](../../../04_Application/Compatibility/README.md)
and require appropriate evidence.

`StartupNotificationQueue` is a transient Core-to-UI handoff. UI presentation
details belong in [Notifications](../../CalradiaForge_UI/Notifications/README.md).
