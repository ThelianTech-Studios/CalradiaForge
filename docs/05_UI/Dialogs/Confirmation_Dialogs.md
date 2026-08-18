# Confirmation Dialogs

`ConfirmDialogWindow` is a themed, owner-aware modal window. The dialog model
and purpose enum are WPF-neutral Core data; `ConfirmDialogModelResolver` and
`ApplicationDialogService` map them to presentation. Lifecycle confirmation
occurs before shutdown/restart commitment and active-work warnings are explicit.

The process-level order is owned by [Application Lifecycle](../../02_Systems/CalradiaForge_UI/Application_Lifecycle/README.md).
