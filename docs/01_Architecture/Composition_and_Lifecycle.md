# Composition and Lifecycle

`CalradiaForge.UI.App` is the WPF application host and sole root-provider and
process-lifecycle owner. `OnStartup` registers the Core and UI services on one
shared `ServiceCollection`, builds one validated provider, marks the Serilog
pipeline operational after provider construction, and delegates startup to
`ApplicationStartupCoordinator`.

`ServiceCollectionExtensions` registers UI services, retained singleton pages,
retained singleton ViewModels, dialogs, interaction adapters, notification
services, and lifecycle coordinators. Core registration creates the settings,
logging, platform, mod, modpack, launch, EULA, and localization services.

## Lifecycle order

1. WPF starts `App` and subscribes global exception handlers.
2. Core and UI services are registered and the provider is validated.
3. Startup coordination loads settings, initializes the accepted cache/pipeline,
   loads modpacks, resolves language/EULA/UI prerequisites, and then resolves
   the shell.
4. `MainWindow` serializes retained ViewModel/page lifecycle transitions.
5. Shutdown stops new work, requests cooperative cancellation, waits for
   quiescence in bounded intervals, completes persistence/disposal, and then
   calls WPF shutdown.
6. Restart disposes the current provider before starting a replacement process.
7. `OnExit` is a synchronous last-resort disposal fallback.

Fatal startup and global exception handling use the provider-owned Serilog
pipeline when operational and `EmergencyStartupLogWriter` before that point.
The lifecycle implementation is in `source/CalradiaForge.UI/App.xaml.cs` and
`source/CalradiaForge.UI/Lifecycle/`.

Detailed notification and UI lifecycle behavior belongs in
[UI systems](../02_Systems/CalradiaForge_UI/README.md) and [UI](../05_UI/README.md).
