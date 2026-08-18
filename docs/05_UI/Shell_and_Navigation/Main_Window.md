# Main Window

`MainWindow` is a retained singleton resolved by the UI composition root. It
owns the navigation shell, shared resources, and final shutdown preparation. It
does not own Core scan/install mechanics or raw global installer subscriptions.

The window coordinates ViewModel lifecycle transitions and gives the active
page a stable presentation host. See [Application Lifecycle](../../02_Systems/CalradiaForge_UI/Application_Lifecycle/README.md)
for process-level ownership.
