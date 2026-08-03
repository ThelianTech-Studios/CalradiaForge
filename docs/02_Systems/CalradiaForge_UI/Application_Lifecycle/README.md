# UI Application Lifecycle

`App` owns the validated service provider, WPF startup, global exception
handlers, shutdown, restart, and final disposal. `ApplicationStartupCoordinator`
and `ApplicationShutdownCoordinator` own ordered lifecycle work. The
application work controller adapts Core pipeline admission/cancellation and the
state-persistence adapter saves authorized application state.

`MainWindow` and the retained pages/ViewModels are composed as singletons. The
shell serializes lifecycle transitions so navigation does not race initialization
or deactivation. Structural ownership is described in
[Composition and Lifecycle](../../../01_Architecture/Composition_and_Lifecycle.md).
