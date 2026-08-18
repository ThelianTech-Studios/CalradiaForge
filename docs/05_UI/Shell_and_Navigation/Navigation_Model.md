# Navigation Model

The current shell retains the Launcher, Modpacks, FAQ, and Settings page
instances. Navigation is explicit and serialized; each retained ViewModel has
an initialization/deactivation/activation lifecycle rather than being recreated
on every view change.

Page-specific behavior belongs in [Pages and ViewModels](../Pages_and_ViewModels/README.md).
The navigation label shown to the user may differ from the source page class
name; current source uses `LauncherPage` while the navigation copy can present
the Home label.
