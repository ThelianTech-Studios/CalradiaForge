# Settings Page and ViewModel

`SettingsPage` and `SettingsViewModel` present typed settings, game and Workshop
path validation, re-detection/manual selection, language refresh, DLL
unblocking, folder shortcuts, debug behavior, and restart requests. WPF file or
folder selection is isolated behind interaction adapters.

The ViewModel consumes Core services and does not own persistence mechanics or
platform detection. See [Settings capability](../../04_Application/Capabilities/Settings.md)
and [Core Configuration](../../02_Systems/CalradiaForge_Core/Configuration/README.md).
