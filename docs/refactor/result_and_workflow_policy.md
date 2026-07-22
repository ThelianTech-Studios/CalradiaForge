# Result And Workflow Policy

## Purpose

Standardize how risky workflows report success, failure, warnings, progress, cancellation, and user-facing messages.

## Current Source Observations

- `ModInstaller` reports progress through events and stores `LastSummary`.
- `ModInstallSummary`, `ModInstallResult`, and `BLSEInstallResult` already provide workflow-specific result shapes.
- `ModsPage.xaml.cs` currently coordinates install completion UI, refresh after install, DLL unblock, status text, and toasts.
- `ModpacksPage.xaml.cs` uses tuple-style service results for import and booleans for save/create.
- `SettingsPage.xaml.cs` uses direct messages/toasts around validation and tool operations.

## Policy

Use workflow-specific result types first. Add a broader shared `Result` / `Result<T>` only if repetition justifies it.

Result objects should support:

- Success/failure.
- Result or error code where useful.
- User-facing message.
- Technical/log message.
- Warnings.
- Affected file, path, module, or operation where useful.
- Cancellation state where useful.

## Phase 6.A Mod-Pipeline Coordinator Foundation

Phase 6.A creates one Core-owned `ModPipelineCoordinator`. It sequences configured Phase 5 paths, `ModScanner`, existing cache helpers, retained module state, and the applicable installer completion/cancellation boundary. `ModScanner` remains discovery-only and never owns detection, workflow sequencing, cache authorization, or accepted-state publication.

Startup initialization and explicit refresh use the same coordinator contract. The coordinator validates preconditions, controls deterministic operation admission and cancellation, evaluates per-root completeness, decides commit eligibility, publishes one coherent accepted snapshot/version, and returns a workflow-specific result. The result identifies success/failure/cancellation, completeness, commit status, local and Workshop outcomes/counts, warnings, duplicate/parse diagnostics, a compact localizable UI summary, and technical logging context. It is not an application-wide `Result<T>` hierarchy.

Only an approved complete result may rotate cache, save current state, and publish the accepted snapshot. Invalid configuration, partial or indeterminate results, cancellation, and unexpected failure never commit; the previous snapshot remains available and cannot be interpreted as module removal. Persistence helpers decide how files are written; the coordinator decides when writing is authorized.

The active-work contract identifies current work, stops new admission, requests cooperative cancellation, exposes idempotently observable completion, and supports awaitable quiescence. It does not replace `ModInstaller`/`ModExtractor`, dispose the provider, close Serilog, terminate WPF, or create an unbounded job queue.

## Deferred Phase 7 Install And General Workflow Coordinator Direction

An install workflow coordinator should eventually own:

- Active install state.
- Progress updates.
- Cancellation state.
- Completion handling.
- Failure handling.
- Cleanup after install attempts.
- UI-facing status/error messages.
- Toast/status integration.
- Future Nexus download-to-install handoff.

Core install services must remain UI-independent.

## Deferred Phase 7 Shared UI State Direction

Initial shared UI state should support:

- `IsBusy`
- `StatusMessage`
- `ErrorMessage`
- `CanCancel`
- `CurrentOperation`
- `LastOperationResult` where useful

The Toast System remains the user-visible notification surface for operation started, operation completed, recoverable warning, operation failed, validation issue, and future Nexus/download/install status events.

## Scanner Result And Warning Planning

`SteamResolutionResult` describes resolver outcomes, and the Phase 5 production coordinator applies accepted results directly to `AppConfigSettings`. `GameDetectionService.InitializeForStartup(...)` is intentionally `void`, explicit re-detection returns `GameProvider`, and manual operations return `bool` rather than introducing a general game-detection result. A compact queued startup notification is a transient UI handoff, not a domain-result hierarchy.

Current `ModScanner` still returns its existing module list. Planned Phase 6.A wraps discovery in a workflow-specific structured completeness/commit result without duplicating the Phase 5 detection workflow. Phase 7 may extend generalized warning/result conventions, but it does not reopen the required Phase 6.A fields.

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
| 1 | Phase 6.A: audit scanner/cache/installer/startup/modpack consumers; define the workflow-specific result and per-root completeness/commit policy. | Current ownership and all no-commit cases are recorded. |
| 2 | Phase 6.A: add `ModPipelineCoordinator`, gate rotation/save, publish the accepted snapshot, and route startup plus refresh through it. | Complete results commit once; rejected results preserve the prior snapshot. |
| 3 | Phase 6.A: add deterministic admission, cancellation, idempotent completion, and awaitable quiescence. | Shutdown integration cannot outlive UI/provider/logger state. |
| 4 | Preserve the implemented Phase 5 detection workflow without adding a general detection-result hierarchy. | Provider/manual outcomes, queued feedback, and technical logs remain covered. |
| 5 | Phase 7: document existing result shapes and normalize naming beyond the 6.A foundation. | No behavior change. |
| 6 | Phase 7: add result types for archive preflight, BLSE validation, persistence recovery, and other high-risk workflows. | Unit tests assert success, warnings, failures, and cancellation where relevant. |
| 7 | Phase 7: wrap current install event flow with an install workflow coordinator. | Navigation-away install behavior still works. |
| 8 | Phase 7/8: move UI status fields into shared UI state and add ViewModel state-transition tests. | Busy/status/error/cancel and start/progress/success/failure/warning/cancel states update predictably. |

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

- Should install cancellation return a distinct status instead of failed?
- Which result codes should be public/stable versus internal?
- Should modpack import/save adopt shared results before MVVM extraction?
- Which additional Phase 7 scanner diagnostic codes, beyond the locked Phase 6.A outcomes and fields, should become public without exposing unnecessary filesystem detail to the UI?

## Out Of Scope

- App-wide generic result framework until duplication proves the need.
- Nexus workflow implementation, except preserving future handoff shape.
- Moving Core workflow logic into WPF ViewModels.

The complete Phase 6.A contract is [Phase 6.A locked decisions](phase_6a_mod_pipeline_coordinator_locked_decisions_2026-07-21.md). Phase 7 retains generalized results, install coordination, broad progress/cancellation/UI state, and future Nexus handoff beyond that foundation.
