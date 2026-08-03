# Terminology

| Term | Meaning |
| --- | --- |
| Accepted snapshot | The last complete module scan state that `ModPipelineManager` authorized and published. |
| Core | `CalradiaForge.Core`; the WPF-free runtime library. |
| Launcher | The retained primary page and its ViewModel; current source uses `LauncherPage` and `LauncherViewModel`. |
| Module | A Bannerlord module represented by the Core module model and discovered from installed roots. |
| Modpack | A persisted named load-order definition, distinct from the currently accepted installed-module snapshot. |
| Mod pipeline | The Core-owned scan/install admission, cancellation, reconciliation, and accepted-state boundary. |
| Nexus | The optional/reserved `CalradiaForge.Nexus` project; future architecture must remain clearly separate from shipped behavior. |
| Public release | A version explicitly marked `Public Release` or `public Release` in the historical changelog. |
| Review evidence | A bounded report whose claims are tied to a stated source, test, runtime, or benchmark baseline. |
| Source endpoint | The accepted committed source state used as the basis for implementation or release bookkeeping. |
