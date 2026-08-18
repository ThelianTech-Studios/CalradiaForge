# UI Threading

`IUiDispatcher` and `WpfUiDispatcher` isolate dispatcher access. ViewModels
apply accepted snapshots and observable state on the UI dispatcher; Core result
and progress types remain WPF-neutral. High-frequency progress may be throttled
or coalesced by the UI presenter without changing the Core terminal result.
