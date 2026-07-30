# Phase 7 Expected Implementation Migration Map

Status: planning inventory only. This map does not represent implemented source
changes and is intentionally excluded from the committed-source
[`MIGRATION_MAP.md`](../MIGRATION_MAP.md).

## Baseline and ownership

Baseline: `dev-V0-14-CodeRefactor` at `203464d`; source/test state is unchanged
from `2fec7c9` except for the Phase 7 readiness audit. `ModPipelineManager`
remains the sole Core application-operation owner; `ModInstaller` and
`ModExtractor` retain mechanics; Core remains WPF-free.

| Expected change | Current owner / behavior | Planned owner / behavior | Dependencies and validation | Follow-up |
|---|---|---|---|---|
| `ModsPage.xaml` and `.xaml.cs` -> `LauncherPage.xaml` and `.xaml.cs` | Retained DI `ModsPage`; `MainWindow` navigation and `Nav_ModsTab` use Mods identity. | Historical Phase 7 plan: behavior-preserving `LauncherPage` and Launcher label; do not rename mod-domain types. | Clean XAML/DI/navigation build; localization and rename-boundary tests; stale-reference inventory. | Current Phase 8 clarification: retain `LauncherPage`, add `LauncherViewModel`, use visible **Home**, and do not create `LauncherView`; reserve `ModsPage` for future mod management. |
| `ModInstallOperationResult` and status/diagnostics | `InstallAsync` returns nullable `ModInstallSummary`; rejection is ambiguous. | Non-null install-specific result with `Succeeded`, `SucceededWithWarnings`, `PartiallyFailed`, `Cancelled`, `RejectedBusy`, `RejectedAdmissionStopped`, `ValidationFailed`, and `Failed`; retain summary, unblock, scan, accepted snapshot, stable diagnostics. | Status-precedence and no-null tests; Core/UI dependency checks. | No generic result framework. |
| Progress model and operation identity | Installer emits page-observed summary/extraction events. | Manager relays read-only, correlated per-archive progress by operation/archive identity; high-frequency messages are not unbounded history or mutable summaries. | Operation-ID, relay, subscriber, and UI throttling/coalescing tests. | Future Nexus mapping remains undecided. |
| Manager terminal publication | Manager admits, awaits installer, and releases. | One immutable terminal result after authoritative Core pipeline completion. | Exactly-one terminal result and cleanup tests. | Presenter consumes semantic information only. |
| DLL unblocking | `ModsPage` calls `DLLUnblocker` after install. | Manager invokes Modules-directory unblocking as a Core child outcome. | Injectable unblock seam; warning/failure/cancellation tests. | Installer/extractor mechanics unchanged. |
| Scan/commit reconciliation | `ModsPage` calls public `RefreshAsync` after install. | Manager reuses a private scan/commit path for refresh and install reconciliation; public refresh is never recursively admitted. | Scan/accepted-snapshot and nested-admission tests. | Persistence helpers retain write mechanics. |
| Cancellation finalization | Manager cancellation and installer cancellation are separate current mechanics. | Manager classifies busy versus admission-stopped, retains partial summary, performs bounded consistency finalization, and releases/quiesces exactly once; no implicit rollback. | Cancellation, partial filesystem change, BLSE classification, invariant and quiescence tests. | Phase 8 consumes the settled result. |
| UI notification presenter | Page owns raw toast ID and direct installer subscriptions. | Explicitly activated app-lifetime UI presenter owns an opaque operation-notification handle, stale-progress rejection, progress-to-terminal transition, and standalone rejection notices. | Presenter lifecycle, activation, no-duplicate-toast, and navigation-away tests. | A broader typed coordinator is Nexus-deferred. |
| Temporary launcher completion | Page syncs collections, reapplies modpack, recalculates launch state. | `LauncherPage` temporarily reconciles accepted snapshot and reports semantic completion before final user notification. | Presentation reconciliation ordering tests. | Phase 8 moves this to `LauncherViewModel` or an approved presentation owner. |
| DI and tests | UI registers retained pages/ToastService; no install presenter. | Register and explicitly activate presenter; preserve Core/UI separation and retained page baseline. | Composition, XAML, rename, Core sequencing, and presenter tests. | No ViewModel manager or second scheduler. |

The detailed owner decisions are indexed in the
[Phase 7 locked decisions reference](phase_7_locked_decisions_2026-07-24.md).
