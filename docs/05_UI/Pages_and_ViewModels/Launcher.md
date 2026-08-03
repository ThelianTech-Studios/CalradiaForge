# Launcher Page and ViewModel

`LauncherPage` is the retained primary page. `LauncherViewModel` owns launcher
presentation state for module collections, filtering/search, selected modpack,
install/refresh/launch commands, busy/cancel/status/error state, and the
accepted-snapshot correlation required by the install notification presenter.

The ViewModel does not recreate `ModPipelineManager` admission, cancellation,
reconciliation, terminal classification, or installer mechanics. WPF binding
and dispatcher constraints are summarized in [Interaction and State](../Interaction_and_State/README.md);
capability rules are owned by [Launcher](../../04_Application/Capabilities/Launcher.md).
