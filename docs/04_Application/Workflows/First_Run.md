# First Run

1. The WPF host builds and validates the Core/UI provider.
2. Configuration is loaded with defaults-first recovery.
3. The application performs its startup platform/cache/modpack preparation.
4. `EulaService` determines whether acceptance is required. The EULA dialog
   must be accepted before the main shell is shown; decline/close ends startup.
5. The startup notification queue is drained through UI presentation.
6. The retained shell and pages/ViewModels are initialized.

This sequence is a cross-capability guide. The authoritative owners are [EULA](../../02_Systems/CalradiaForge_Core/EULA/README.md),
[Configuration](../../02_Systems/CalradiaForge_Core/Configuration/README.md),
and [Application Lifecycle](../../02_Systems/CalradiaForge_UI/Application_Lifecycle/README.md).
