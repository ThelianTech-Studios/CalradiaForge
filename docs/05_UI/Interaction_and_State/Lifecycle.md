# ViewModel Lifecycle

Retained ViewModels implement an explicit awaitable lifecycle. The shell
serializes initialization, deactivation, display, and activation, and retains
one-time DataContext ownership for each retained page. This allows language
refresh, navigation away/return, and operation completion to be ordered without
recreating the service graph.

Process shutdown and provider disposal remain owned by [App lifecycle](../../02_Systems/CalradiaForge_UI/Application_Lifecycle/README.md).
