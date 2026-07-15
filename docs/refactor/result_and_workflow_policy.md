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

## Workflow Coordinator Direction

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

## Shared UI State Direction

Initial shared UI state should support:

- `IsBusy`
- `StatusMessage`
- `ErrorMessage`
- `CanCancel`
- `CurrentOperation`
- `LastOperationResult` where useful

The Toast System remains the user-visible notification surface for operation started, operation completed, recoverable warning, operation failed, validation issue, and future Nexus/download/install status events.

## Scanner Result And Warning Planning

Steam path resolution now returns `SteamResolutionResult` with a status, resolved game and Workshop paths, selected Workshop source, and structured candidate diagnostics. It distinguishes complete Steam resolution, Steam game resolution without Workshop content, missing client roots, invalid manifests, and unresolved installs. Settings surfaces the Steam-without-Workshop state as a localized recoverable warning while local scanning remains available.

The scanner itself still returns its existing module list rather than a general structured scan-result contract. Phase 6 should add scanner-level warnings without duplicating or weakening the implemented path-resolution diagnostics.

Scanner results should distinguish:

- Local modules scanned successfully.
- Workshop modules scanned successfully.
- Workshop path detection failed.
- No Workshop path candidates were discovered.
- No Workshop mods were installed or no valid Workshop modules were found.
- Workshop path existed but scan failed.

User-facing messages should be concise and actionable. Technical details, candidate paths, exceptions, and skip reasons should be written to local logs as appropriate. The logger does not automatically redact or sanitize them; callers must not intentionally supply credentials or authentication material.

## Performance Verification

Later performance work may measure result allocation, progress-update frequency, cancellation checks, event dispatch, cleanup, and coordinator overhead where stable boundaries exist. These measurements must preserve progress, cancellation, failure mapping, cleanup, and user-facing diagnostics.

Phase 5.A logger shutdown must not close or archive the active logger while a workflow can still emit progress, cancellation, failure, cleanup, or completion events. Quiescence and completion must be confirmed before logger close; this lifecycle rule does not authorize a Phase 6 result-model redesign.

## Local Implementation Steps

| Step | Work | Verification |
|---|---|---|
| 1 | Document existing result shapes and normalize naming. | No behavior change. |
| 2 | Add result types for archive preflight, BLSE validation, persistence recovery, and other high-risk workflows. | Unit tests assert success, warnings, failures, and cancellation where relevant. |
| 3 | Wrap current install event flow with an install workflow coordinator. | Navigation-away install behavior still works. |
| 4 | Move UI status fields into shared UI state. | Busy/status/error/cancel states update predictably. |
| 5 | Preserve the implemented Steam resolver diagnostics and add structured scanner warnings for no valid mods and scan failure when the broader scanner-result contract is implemented. | Tests assert warning codes, user messages, and useful technical/log messages. |
| 6 | Add ViewModel tests for workflow state transitions. | Tests cover start, progress, success, partial failure, failure, warning, and cancel. |

## Guardrails

- Do not put WPF types in Core results.
- Do not replace all exceptions with result objects blindly.
- Do not hide technical failures; log them with useful diagnostic context.
- User-facing messages must be clear and non-secret.
- Do not duplicate installer mechanics inside the coordinator.
- Do not drop progress, cancellation, cleanup, or result reporting to improve throughput.

## Verification Expectations

- Existing workflows still produce user-visible status and toasts.
- Install success, failure, partial failure, and cancellation are observable.
- Result logs preserve useful diagnostic values; credential-owning callers must not intentionally pass credentials to them.
- ViewModel tests cover operation state as UI extraction begins.
- Future Nexus download-to-install handoff uses the coordinator shape without bypassing `ModInstaller`.
- Scanner warnings distinguish no Workshop mods installed from Workshop path not resolved and Workshop path scan failure.
- Performance checks preserve result semantics, progress frequency requirements, cancellation behavior, and user-visible diagnostics.

## Future Documentation Cross-References

Accepted result/warning decisions should later be migrated into future application-system documentation and structured result-warning documentation. Scanner/path result behavior should cross-reference future platform/path-detection documentation and UI status/error/progress presentation docs.

## Open Questions

- Should install cancellation return a distinct status instead of failed?
- Which result codes should be public/stable versus internal?
- Should modpack import/save adopt shared results before MVVM extraction?
- Which existing resolver diagnostic codes should be promoted into the future scanner result without exposing unnecessary filesystem detail to the UI?

## Out Of Scope

- App-wide generic result framework until duplication proves the need.
- Nexus workflow implementation, except preserving future handoff shape.
- Moving Core workflow logic into WPF ViewModels.
