# Result And Workflow Policy

## Purpose

Standardize how risky workflows report success, failure, warnings, progress, cancellation, and user-facing messages.

## Current Source Observations

- `ModInstaller` reports progress through events and stores `LastSummary`.
- `ModInstallSummary`, `ModInstallResult`, and `BLSEInstallResult` already provide workflow-specific result shapes.
- `LauncherPage.xaml.cs` starts manual installs and temporarily reconciles
  accepted-snapshot presentation, selected-modpack state, and launch readiness.
  It no longer observes `ModInstaller`, retains raw toast IDs, invokes DLL
  unblocking, or recursively requests post-install refresh.
- `ModpacksPage.xaml.cs` uses tuple-style service results for import and booleans for save/create.
- `SettingsPage.xaml.cs` uses direct messages/toasts around validation and tool operations.

## Policy

Use workflow-specific result types first. Phase 7 explicitly does not introduce a
broader shared `Result` / `Result<T>` framework.

Result objects should support:

- Success/failure.
- Result or error code where useful.
- User-facing message.
- Technical/log message.
- Warnings.
- Affected file, path, module, or operation where useful.
- Cancellation state where useful.

## Implemented Phase 6.A Mod-Pipeline Manager Foundation

Phase 6.A implemented one Core-owned `ModPipelineManager`. It sequences configured Phase 5 paths, `ModScanner`, existing cache helpers, retained module state, and the applicable installer completion/cancellation boundary. `ModScanner` remains discovery-only and never owns detection, workflow sequencing, cache authorization, or accepted-state publication.

Startup initialization and explicit refresh use the same coordinator contract. The coordinator validates preconditions, controls deterministic operation admission and cancellation, evaluates per-root completeness, decides commit eligibility, publishes one coherent accepted snapshot/version, and returns a workflow-specific result. The result identifies success/failure/cancellation, completeness, commit status, local and Workshop outcomes/counts, warnings, duplicate/parse diagnostics, a compact localizable UI summary, and technical logging context. It is not an application-wide `Result<T>` hierarchy.

Only an approved complete result may rotate cache, save current state, and publish the accepted snapshot. Invalid configuration, partial or indeterminate results, cancellation, and unexpected failure never commit; the previous snapshot remains available and cannot be interpreted as module removal. Persistence helpers decide how files are written; the coordinator decides when writing is authorized.

The active-work contract identifies current work, stops new admission, requests cooperative cancellation, exposes idempotently observable completion, and supports awaitable quiescence. It does not replace `ModInstaller`/`ModExtractor`, dispose the provider, close Serilog, terminate WPF, or create an unbounded job queue.

## Implemented Phase 7 Install Outcome And Progress Boundary

Phase 7 implements a non-null, install-specific
`ModInstallOperationResult`; it does not create another coordinator.
`ModPipelineManager` remains the sole
application owner of admission, cancellation, operation identity, classification,
reconciliation, and quiescence. `ModInstaller` and `ModExtractor` retain
mechanics; Core remains UI-independent.

The result statuses are `Succeeded`, `SucceededWithWarnings`,
`PartiallyFailed`, `Cancelled`, `RejectedBusy`, `RejectedAdmissionStopped`,
`ValidationFailed`, and `Failed`. It preserves the `ModInstallSummary`,
applicable `UnblockResult`, authoritative scan/refresh result, accepted snapshot,
and stable diagnostics without localized text, WPF types, toast data, or mutable
UI state. `ModInstallSummary.ToSummaryString()` stops being the authoritative
toast-formatting path after its callers migrate.

Progress is separate: manager-relayed, read-only per-archive messages correlate
operation and archive identity, may be high-frequency, are not an unbounded
history, and never expose a live mutable summary. The UI may throttle/coalesce
them. One immutable terminal Core result concludes each admitted operation.

## Implemented Phase 7 Notification And Temporary Presentation Boundary

Initial shared UI state should support:

- `IsBusy`
- `StatusMessage`
- `ErrorMessage`
- `CanCancel`
- `CurrentOperation`
- `LastOperationResult` where useful

An explicitly activated application-lifetime UI presenter observes manager
semantics and owns one opaque operation-notification handle, stale-progress
rejection, progress-to-terminal transition, and standalone pre-admission
notifications. `ToastService` remains the generic renderer and `MainWindow` the
host; pages/ViewModels do not retain raw toast IDs or subscribe to installer
events for application-wide notification. UI owns severity, localization, timing,
persistence, and dismissal. Final notification waits for `LauncherPage`'s
temporary presentation reconciliation; Phase 8 moves that reconciliation to
`LauncherViewModel`. The presenter remains the sole raw-toast and correlated
progress-to-terminal notification owner.

## Approved, Unimplemented Phase 8 Consumption Rules

Phase 8 ViewModels consume existing workflow-specific manager results; they do
not recreate admission, operation identity, authoritative cancellation,
reconciliation, terminal classification, quiescence, installer/extractor
mechanics, or a generic `Result<T>` hierarchy. For every accepted Launcher
correlation, `LauncherViewModel` applies the accepted snapshot through the UI
dispatcher, reapplies the selected modpack, recalculates derived launch state,
updates status/warning/error/current/last-result state, and reports semantic
completion exactly once before the presenter finalizes its terminal toast.

The consumption matrix distinguishes success, warnings, partial/validation
failure, busy/admission-stopped rejection, cancellation before/after changes,
unblock warning/failure, refresh/reconciliation failure, presentation
reconciliation failure, unexpected failure, and navigation away/return. See the
[Phase 8 locked decisions](phase_8_locked_decisions_2026-07-29.md).

## Scanner Result And Warning Planning

`SteamResolutionResult` describes resolver outcomes, and the Phase 5 production coordinator applies accepted results directly to `AppConfigSettings`. `GameDetectionService.InitializeForStartup(...)` is intentionally `void`, explicit re-detection returns `GameProvider`, and manual operations return `bool` rather than introducing a general game-detection result. A compact queued startup notification is a transient UI handoff, not a domain-result hierarchy.

`ModPipelineManager` currently returns the implemented workflow-specific
`ModPipelineResult` for scan/refresh; it wraps scanner discovery without
duplicating the Phase 5 detection workflow. Phase 7 adds only the locked install
contract and does not reopen scan-result fields.

Scanner results should distinguish:

- Local modules scanned successfully.
- Local root missing or invalid.
- Local root inaccessible.
- Workshop modules scanned successfully.
- Workshop root intentionally not configured.
- Workshop root absent but optional.
- Configured Workshop root inaccessible.
- Valid Workshop root with zero modules.
- Module parse warning or failure.
- Duplicate module identifier.
- Cancellation.
- Unexpected scan failure.

User-facing messages should be concise and actionable. Technical details, candidate paths, exceptions, and skip reasons should be written to local logs as appropriate. The logger does not automatically redact or sanitize them; callers must not intentionally supply credentials or authentication material.

## Performance Verification

Later performance work may measure result allocation, progress-update frequency, cancellation checks, event dispatch, cleanup, and coordinator overhead where stable boundaries exist. These measurements must preserve progress, cancellation, failure mapping, cleanup, and user-facing diagnostics.

Phase 6.A supplies stop/cancel/awaitable quiescence so no workflow callback targets disposed UI/provider/logger state. Phase 6.B owns provider/logger disposal, leaves `Latest` after shutdown, and performs no shutdown archival. This lifecycle rule does not authorize a Phase 7 result-model redesign.

## Local Implementation Steps

| Step | Work | Verification |
|---|---|---|
| 1 | Completed Phase 6.A: audit scanner/cache/installer/startup/modpack consumers and define per-root completeness/commit policy. | Current ownership and all no-commit cases are recorded. |
| 2 | Completed Phase 6.A: `ModPipelineManager` gates rotation/save, publishes the accepted snapshot, and routes startup plus refresh. | Complete results commit once; rejected results preserve the prior snapshot. |
| 3 | Completed Phase 6.A: deterministic admission, cancellation, idempotent completion, and awaitable quiescence. | Shutdown integration cannot outlive UI/provider/logger state. |
| 4 | Preserve the implemented Phase 5 detection workflow without adding a general detection-result hierarchy. | Provider/manual outcomes, queued feedback, and technical logs remain covered. |
| 5 | Phase 7: implement the locked non-null install result, manager-relayed progress, and manager reconciliation. | Status, cancellation/finalization, unblock, scan, and operation-ID tests pass. |
| 6 | Phase 7: activate the UI presenter and remove page-owned global installer notification subscriptions. | Navigation-away observation, stale progress, and notification lifecycle tests pass. |
| 7 | Phase 7: rename `ModsPage` to `LauncherPage` behavior-preservingly. | DI/XAML/navigation/localization/test references are reconciled. |
| 8 | Phase 8: move temporary launcher presentation state into `LauncherViewModel`. | Busy/status/error/cancel and start/progress/terminal states update predictably. |

## Guardrails

- Do not put WPF types in Core results.
- Do not replace all exceptions with result objects blindly.
- Do not hide technical failures; log them with useful diagnostic context.
- User-facing messages must be clear and non-secret.
- Do not duplicate installer mechanics inside the coordinator.
- Keep `ModScanner` discovery-only; do not duplicate cache serialization or Phase 5 detection inside the coordinator.
- Do not introduce a broad transaction framework, universal result hierarchy, or unbounded generic job queue for the Phase 6.A foundation.
- Do not drop progress, cancellation, cleanup, or result reporting to improve throughput.

## Verification Expectations

- Existing workflows still produce user-visible status and toasts.
- Install success, failure, partial failure, and cancellation are observable.
- Result logs preserve useful diagnostic values; credential-owning callers must not intentionally pass credentials to them.
- ViewModel tests cover operation state as UI extraction begins.
- Future Nexus download-to-install handoff uses the coordinator shape without bypassing `ModInstaller`.
- Scanner warnings distinguish no Workshop mods installed from Workshop path not resolved and Workshop path scan failure.
- Phase 6.A tests prove startup and refresh reuse one coordinator; complete/partial root distinctions; commit flags; accepted-snapshot publication; cache-rotation gating; retained prior state; modpack validation against the accepted snapshot; deterministic duplicates; and stop/cancel/quiescence with no callbacks to disposed dependencies.
- Performance checks preserve result semantics, progress frequency requirements, cancellation behavior, and user-visible diagnostics.

## Future Documentation Cross-References

Accepted result/warning decisions should later be migrated into future application-system documentation and structured result-warning documentation. Scanner/path result behavior should cross-reference future platform/path-detection documentation and UI status/error/progress presentation docs.

## Open Questions

- Which result codes should be public/stable versus internal?
- Should modpack import/save adopt shared results before MVVM extraction?
- Which additional Phase 7 scanner diagnostic codes, beyond the locked Phase 6.A outcomes and fields, should become public without exposing unnecessary filesystem detail to the UI?

## Out Of Scope

- App-wide generic result framework until duplication proves the need.
- Nexus workflow implementation, except preserving future handoff shape.
- Moving Core workflow logic into WPF ViewModels.

The historical [Phase 6.A locked decisions](phase_6a_mod_pipeline_coordinator_locked_decisions_2026-07-21.md) record the implemented baseline. Phase 7 is the narrow locked install outcome, application notification, and naming plan in [the Phase 7 reference](phase_7_locked_decisions_2026-07-24.md).
