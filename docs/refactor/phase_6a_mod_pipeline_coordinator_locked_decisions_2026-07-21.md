# Phase 6.A Locked Decisions — Mod Pipeline Coordinator Foundation

## Status

Historical pre-implementation locked-decision record. Phase 6.A is now
implemented as `ModPipelineManager`, which owns admission, accepted snapshots,
cache-commit authorization, cancellation, and quiescence. Preserve the remaining
ledger as rationale; do not read its future-state language as current source.

Owner-approved planning direction for:

```text
Phase 6.A — Mod Pipeline Coordinator Foundation
```

This phase is intentionally placed before dependency injection so Phase 6.B can register and lifecycle-manage the intended Core workflow boundary instead of wiring DI directly to scattered scanner/cache/installer behavior.

## Current Source State

Historical snapshot only: Phase 6.A is now implemented as `ModPipelineManager`,
with accepted snapshots, structured scan results, admission/cancellation, and
awaitable quiescence. This paragraph's former future-state claims are superseded.

---

# 1. Purpose

Establish one Core-owned mod-pipeline coordination boundary for startup initialization, explicit refresh/scan operations, cache-commit safety, current-module snapshot publication, and application-shutdown quiescence.

This is a bounded foundation, not the complete later generalized result/workflow refactor.

---

# 2. Phase Ordering

```text
Phase 5   Game Platform Detection and Path Workflow Rewrite
→ Phase 6.A Mod Pipeline Coordinator Foundation
→ Phase 6.B Dependency Injection and Application Lifecycle Foundation
→ Phase 6.C Legacy Logger Call-Site Migration
```

Phase 6.A consumes the finalized provider/path/settings behavior from Phase 5.

Phase 6.B later registers the finalized coordinator and its dependencies in the sole application provider.

---

# 3. Core Ownership Boundary

Create one Core-owned coordinator boundary, named consistently with:

```text
ModPipelineCoordinator
```

The detailed docs may adapt the exact namespace/file location to the current source layout, but they must not create multiple competing coordinators.

The coordinator owns **workflow sequencing**, not low-level implementation details.

Conceptual dependency direction:

```text
UI / startup caller
→ ModPipelineCoordinator
   → configured AppSettings / game paths
   → ModScanner
   → existing cache/data persistence helper
   → existing ModService state where retained
   → existing ModInstaller completion/cancellation boundary where applicable
```

Core decides how the pipeline executes. UI decides when to request an operation and how to present its returned result.

Core remains free of WPF types, windows, dispatcher objects, toast controls, and page state.

---

# 4. Do Not Redesign Phase 5 Detection

The coordinator consumes finalized Phase 5 configuration.

It must not:

- discover Steam libraries,
- parse Steam metadata,
- infer `GameProvider`,
- repair game-path configuration,
- replace `GameDetectionWorkflow`,
- redesign manual game or Workshop selection.

Invalid or missing game configuration is reported as a pipeline precondition failure. It does not trigger a second detection architecture.

---

# 5. Scanner Boundary

`ModScanner` remains the low-level scanner.

It receives configured roots and returns discovered module data plus the information necessary for the coordinator to evaluate scan completeness.

The scanner must not become the application workflow owner.

The target contract must distinguish at least:

- local game `Modules` root scanned successfully,
- local root missing or invalid,
- local root inaccessible,
- selected Steam Workshop root scanned successfully,
- Workshop root not configured,
- Workshop root absent but optional,
- Workshop root inaccessible,
- module parse failure/warning,
- duplicate module identifier,
- cancellation,
- unexpected scan failure.

Do not collapse these outcomes into an unqualified module list.

---

# 6. Structured Scan/Pipeline Result

The coordinator must return a structured workflow-specific result rather than relying only on mutable service fields or UI events.

The exact type names may be finalized during implementation, but the contract must carry enough information for:

- overall success/failure/cancellation,
- scan completeness,
- whether the current cache/snapshot was committed,
- local-root outcome,
- Workshop-root outcome when applicable,
- discovered module count,
- local/Workshop counts where available,
- warnings,
- duplicate or parse diagnostics,
- user-facing summary suitable for UI mapping,
- technical diagnostic context for logging.

Do not introduce a broad universal `Result<T>` hierarchy solely for this phase.

Do not move every later result-model responsibility into 6.A.

---

# 7. Completeness and Commit Policy

The coordinator is the authority that decides whether scan output is safe to become the active state.

Locked rules:

1. A partial or indeterminate scan must not silently replace the current module snapshot.
2. A partial scan must not be interpreted as modules being removed.
3. Cache rotation and `SaveCurrent` occur only after an approved complete result.
4. Cancellation never commits a new active snapshot.
5. Unexpected scan failure never commits a new active snapshot.
6. Invalid game configuration does not start a normal scan and cannot replace the active cache with an empty result.
7. Existing current/cache state remains available when a new result is rejected.
8. The result explicitly reports whether commit occurred.

Provider-specific completeness examples:

- `StandAlone` or another non-Steam provider with a valid local root can complete with local scanning only.
- Steam with no configured Workshop path may still complete as a local-only supported state when Phase 5 intentionally has no Workshop path; the result must distinguish this from an inaccessible configured Workshop path.
- Steam with a configured Workshop path that cannot be scanned is not silently equivalent to “zero Workshop mods.”
- A valid Workshop root containing zero modules is a complete zero-result for that root.

The detailed docs must preserve this distinction.

---

# 8. Atomic Current Snapshot

The application must expose one coherent current module snapshot/version for operations that depend on the installed module set.

At minimum:

- scan completion publishes the accepted module state atomically,
- load-order and modpack validation observe a coherent accepted snapshot,
- a rejected/partial scan does not mutate the accepted snapshot piecemeal,
- install/import/refresh follow-up logic can determine which accepted snapshot it is using.

Do not add a distributed database or broad transaction framework.

Use the smallest architecture that prevents mixed-version observations and false missing-module results.

---

# 9. Startup Initialization

Application startup will use the Phase 6.A coordinator boundary for approved mod initialization before the main window is resolved in Phase 6.B.

Conceptual order within later Phase 6.B startup:

```text
Phase 5 game configuration/detection
→ Phase 6.A mod-pipeline startup initialization
→ modpack loading/validation against the accepted module snapshot
→ MainWindow resolution and display
```

The startup coordinator in Phase 6.B owns when startup initialization runs. `ModPipelineCoordinator` owns how the mod pipeline runs.

Startup must not reproduce scanner/cache logic directly in `App.xaml.cs`.

---

# 10. Explicit Refresh and UI Boundary

Explicit refresh/re-scan calls use the same Core coordinator contract as startup, with operation-specific options where necessary.

The UI:

- requests the operation,
- displays busy/progress state,
- maps the returned result to localized toast/status/error text,
- dispatches UI updates on the WPF dispatcher.

The Core coordinator:

- validates preconditions,
- controls admission/cancellation,
- invokes scanner/cache components,
- decides commit eligibility,
- publishes the accepted snapshot,
- returns the structured result.

Do not put WPF dialogs, toast rendering, or page controls in Core.

---

# 11. Installer Boundary and Awaitable Completion

`ModInstaller` and `ModExtractor` remain the authoritative low-level installation/extraction pipeline.

Phase 6.A does not redesign archive safety, destructive upgrade rollback, BLSE overwrite policy, or Nexus transport.

However, application lifecycle cannot depend on untrackable fire-and-forget work.

The Phase 6.A foundation must provide an awaitable, deterministic operation-lifetime boundary for mod-pipeline work that can still emit state/logging/UI callbacks during shutdown.

Required properties:

- active work can be identified,
- new work admission can be stopped,
- cancellation can be requested,
- completion/quiescence can be awaited,
- completion is idempotently observable,
- worker callbacks do not target disposed UI/provider/logger state,
- the coordinator does not dispose the root provider or terminate WPF.

The exact serialization/rejection policy for simultaneous user requests may be implemented narrowly, but it must be deterministic and documented. Do not create an unbounded generic job queue unless current requirements prove it necessary.

---

# 12. Shutdown Quiescence Contract

Phase 6.A must expose the explicit lifecycle operations required by Phase 6.B’s `ApplicationShutdownCoordinator`.

Conceptual contract:

```text
Stop accepting new pipeline work
→ Request cooperative cancellation
→ Await safe quiescence
→ Persist/commit only states already authorized by result policy
→ Return shutdown outcome
```

Rules:

- methods are idempotent,
- cancellation is cooperative,
- disposal is not used as a substitute for stopping work,
- the coordinator does not close Serilog,
- the coordinator does not call `Application.Shutdown()`,
- the coordinator does not dispose dependencies it does not own,
- timeout and user “Continue Waiting / Exit Anyway” behavior belongs to Phase 6.B’s app lifecycle.

---

# 13. Notifications and Diagnostics

Core results contain compact user-facing outcome data and technical diagnostics.

UI maps the result to the current toast/status surface.

Do not place full paths or internal mutable settings objects in transient UI notification models unless the existing Core-safe model already requires them.

Technical details remain in logs. User-facing text remains concise and localizable.

---

# 14. Cache and Persistence Ownership

Retain existing data-helper/file ownership where it is already appropriate.

The coordinator decides **when** a result is eligible for cache rotation/current save. The persistence helper decides **how** files are written.

Do not duplicate cache serialization inside the coordinator.

Do not broaden this phase into a complete cache transaction framework.

---

# 15. Phase 6.A Explicit Exclusions

Do not include:

- dependency-injection registration or provider construction,
- WPF startup rewrite,
- static `App.*` migration,
- Serilog lifecycle ownership,
- broad legacy logger call-site migration,
- MVVM extraction,
- generalized application-wide result hierarchy,
- complete installer rollback redesign,
- archive extraction redesign,
- Nexus API/download implementation,
- Steam detection redesign,
- Epic/GamePass/GOG support expansion,
- broad performance optimization,
- unrelated UI redesign.

Phase 6.B consumes this phase’s finalized coordinator boundary.

---

# 16. Required Implementation Order

Document this order for the future implementation task:

1. Audit current `ModService`, `ModScanner`, cache helpers, `ModInstaller`, startup callers, and modpack consumers.
2. Define the minimal structured scan/pipeline result.
3. Define completeness and commit policy.
4. Add the coordinator seam around current scanner/cache behavior.
5. Move cache rotation/current-save authorization behind the coordinator result policy.
6. Publish accepted current module state atomically.
7. Route startup mod initialization through the coordinator.
8. Route explicit refresh through the same boundary.
9. Add awaitable active-work/cancellation/quiescence behavior.
10. Add focused Core tests.
11. Build and run existing tests.
12. Record intentional deviations and remaining later-phase work.

Do not begin broad UI/MVVM or DI migration in this phase.

---

# 17. Required Tests

At minimum, require coverage for:

## Complete scans

- valid local-only provider commits local modules,
- valid Steam local plus Workshop scan commits one combined unique snapshot,
- valid Workshop root with zero modules is complete,
- deterministic module ordering/identity behavior where current contracts require it.

## Incomplete/failing scans

- invalid game configuration does not scan or replace active cache,
- inaccessible local root does not commit,
- inaccessible configured Workshop root does not masquerade as zero modules,
- parse warnings are surfaced without corrupting accepted state,
- cancellation does not commit,
- unexpected failure does not commit,
- duplicate IDs are handled deterministically and reported.

## Cache/snapshot safety

- previous accepted snapshot remains after rejected result,
- cache rotation occurs only after approved complete result,
- partial scan does not emit false removals,
- modpack missing-module validation uses the accepted snapshot,
- split-root Steam fixture does not report existing Workshop modules missing.

## Lifecycle

- only one active operation follows the documented admission policy,
- stop-admission is idempotent,
- cancellation reaches active work,
- quiescence completes after operation completion,
- shutdown during active work does not call disposed UI/logging dependencies.

## Regression baseline

- existing Phase 5 resolver/detection tests remain valid,
- solution build succeeds,
- full existing tests pass after new tests are added.

---

# 18. Exit Criteria

Phase 6.A is complete only when:

- one Core-owned mod-pipeline coordinator boundary exists,
- startup and refresh use that boundary,
- structured completeness/commit results exist,
- partial/cancelled/failed scans cannot replace accepted state,
- cache rotation is gated by the result policy,
- accepted module state is coherently published,
- active work exposes stop/cancel/quiescence behavior for Phase 6.B,
- focused tests pass,
- Core remains WPF-free,
- no DI or logger migration scope leaked into the phase.

---

# 19. Documentation Handoff

After future implementation, update canonical architecture only for behavior proven by source/tests.

The Phase 6.A planning document remains the implementation contract until completion evidence exists.

Do not mark the phase complete in this documentation-only task.

## Implementation Stop Conditions

Stop and request owner direction if implementation would require redesigning Phase 5 detection, duplicating low-level persistence or installer authority, introducing WPF into Core, or inventing a broad queue, transaction framework, or application-wide result hierarchy beyond this locked foundation.
