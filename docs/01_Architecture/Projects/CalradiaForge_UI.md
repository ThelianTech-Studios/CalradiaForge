# CalradiaForge.UI

`CalradiaForge.UI` is the WPF application and composition layer. It owns the
application host, provider composition, startup/shutdown/restart coordination,
pages, ViewModels, dialogs, interaction adapters, UI dispatching, resources,
and toast presentation.

UI invokes Core services but does not duplicate their mechanics. The retained
pages and ViewModels are registered as singletons, with lifecycle transitions
coordinated by the shell.

See [UI systems](../../02_Systems/CalradiaForge_UI/README.md) and [presentation
documentation](../../05_UI/README.md).
