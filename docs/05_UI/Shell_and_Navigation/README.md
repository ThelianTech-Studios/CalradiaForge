# Shell and Navigation

The WPF shell is `MainWindow`. It hosts the retained Launcher, Modpacks,
Settings, and FAQ pages and coordinates their lifecycle through the
`IViewModelLifecycle` boundary. Navigation serializes initialize, deactivate,
display, and activate work.

| Document | Scope |
| --- | --- |
| [Main Window](Main_Window.md) | Shell composition and lifecycle |
| [Navigation Model](Navigation_Model.md) | Retained page navigation and ordering |
