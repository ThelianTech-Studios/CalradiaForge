# Settings Capability

The Settings surface exposes game/platform paths, Workshop recovery, startup
modpack selection, language, debug mode, DLL-unblock maintenance, data-folder
shortcuts, EULA/about state, and restart behavior where applicable.

Settings changes are typed through `AppSettings`; persistence is attempted by
the Core configuration store and failure is surfaced through the existing
notification lifecycle. Presentation state and interaction adapters are owned
by [Settings UI](../../05_UI/Pages_and_ViewModels/Settings.md).
