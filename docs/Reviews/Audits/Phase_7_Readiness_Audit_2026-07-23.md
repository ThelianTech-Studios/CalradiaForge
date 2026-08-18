> Historical audit evidence migrated from the preserved documentation snapshot on 2026-08-02. This report retains its original scope, baseline, and limitations; it is not authoritative for current implementation behavior.

# CalradiaForge Phase 7 Readiness Deep Audit

Status: Post-Phase 6 planning-readiness audit

Audit date: 2026-07-23

Branch / commit: `dev-V0-14-CodeRefactor` / `2fec7c9d416b0012bd95edcae51dbe4c1108a254`

Scope: Read-only audit of the authored source, solution and projects, tests, benchmarks, architecture documentation, refactor documentation from Phase 7 onward, and Phase 6 completion records. No application source, existing documentation, tests, project files, configuration, or Git history was changed. This report is the sole audit artifact created.

## Executive Summary

Phase 7 remains necessary, but it is **not ready to begin from the existing planning documents unchanged**. The correct next action is a focused Phase 7 replan and documentation-reconciliation pass, not a wholesale restart of the Phase 7-12 roadmap.

The original refactor plans entered Git in commit `1695a71` on 2026-07-08. They were generated around the early `v0.13.6` refactor period, not from a `v0.12.15` implementation baseline; `0.12.15` is still the project package metadata. The source has since changed substantially: the `1695a71...HEAD` source range contains 136 changed source/test/benchmark files (`+10,228/-2,738`). Phase 6.A, 6.B, and 6.C are now committed, pushed, and documented as internal builds `v0.13.49`, `v0.13.73`, and `v0.13.83`.

That work created the foundation Phase 7 was intended to rely on: one DI provider, provider-owned Serilog, startup/shutdown coordinators, retained singleton pages, and a Core-owned `ModPipelineManager` with single-operation admission, cancellation, quiescence, accepted snapshots, structured scan results, and an awaitable install path. The remaining Phase 7 need is real but narrower and more concrete: normalize selected workflow outcomes and move install progress/completion orchestration out of page-local code without duplicating the manager's admission, cancellation, or installer authority.

The Phase 8-12 sequence remains directionally sound. Their inputs should be refreshed as part of the Phase 7 replan, particularly the Phase 8 dependency on a settled UI-operation-state contract. The report identifies factual documentation drift, ownership decisions that require approval, and a safe first Phase 7 slice.

## Audit Method And Evidence

- Read `AGENTS.md`, `docs/refactor/task_execution_optimization_policy.md`, the refactor master plan, Phase 7-12 supporting policies, Phase 6 locked-decision records, architecture documents, Phase 6 implementation records, `CHANGELOG.md`, and `MIGRATION_MAP.md`.
- Applied the task-execution policy through three independent, read-only workstreams: live Core/UI architecture and Phase 7 seams; Phase 7-and-later documentation accuracy; and Git/build/test baseline verification. The scopes did not overlap and no workstream changed repository files.
- Inspected the source surfaces that now own pipeline admission, installation, lifecycle, UI notifications, page state, DI registration, and current result models.
- Compared the initial planning commit `1695a71` with the pushed Phase 6 endpoint `2fec7c9` and checked `HEAD`, upstream, and `origin/dev-V0-14-CodeRefactor` alignment.
- Built and tested the solution in Debug. Independent baseline verification also completed the corresponding Release build/test checks.

## Repository Baseline

| Item | Evidence | Audit significance |
|---|---|---|
| Branch and remote state | `dev-V0-14-CodeRefactor`; `HEAD`, upstream, and `origin/dev-V0-14-CodeRefactor` all resolve to `2fec7c9` | The Phase 6 endpoint is pushed and the audit baseline is unambiguous. |
| Working tree before report creation | Clean | There were no owner source or documentation edits to separate from the audit. |
| Current release record | `docs/CHANGELOG.md` and `docs/MIGRATION_MAP.md` record `v0.13.83` for Phase 6.C | This is the current documented internal source endpoint. |
| Planning baseline | `1695a71` (2026-07-08) | The master plan predates Phase 5/6 implementation and should not be treated as a live-source description. |
| Change volume since planning baseline | 136 authored source/test/benchmark paths, `+10,228/-2,738` | A replan is justified by architecture change, not merely wording drift. |
| Authored inventory | 136 C# files, 20 XAML files, 31 test source files, 139 `[Fact]`/`[Theory]` declarations | The review covered the live solution shape, including the added UI/lifecycle test surface. |
| Debug verification | `dotnet build source/CalradiaForge.slnx -c Debug --no-restore`: passed, 0 errors, 14 warnings; `dotnet test source/CalradiaForge.slnx -c Debug --no-restore --no-build`: 156 passed, 0 failed, 0 skipped | The current source is buildable and its automated test suite is executable. |
| Release verification | Independent read-only verification: build passed with 0 errors and 13 warnings; all 156 tests passed | Confirms the Phase 7 starting point is not Debug-only. |

The warnings are existing platform/architecture observations: MSIL versus AMD64 `SevenZipWrapper` reference warnings and CA1416 Windows-platform API warnings around registry and archive APIs. They are not evidence that Phase 7 must change those systems, but they remain relevant to later Phase 9 performance/compatibility review.

## Verified Phase 6 Baseline

The following implementation is present and must be treated as the immutable starting boundary for Phase 7 planning:

| Area | Current live behavior | Phase 7 consequence |
|---|---|---|
| Composition and lifecycle | `App` owns one validated provider; `ApplicationStartupCoordinator` controls startup; `ApplicationShutdownCoordinator` uses the application-work contract before provider disposal. | Do not introduce a second global scheduler, lifecycle owner, or service locator. |
| Logging | Provider-owned Serilog is constructed once, normal callers use it, and `EmergencyStartupLogWriter` is the narrow pre-operational fallback. | Phase 7 result/log diagnostics must use the established Serilog path rather than recreate logging ownership. |
| Mod pipeline | `ModPipelineManager` owns scan/install admission, stop/cancel, quiescence, accepted snapshot publication, and complete-scan cache authorization. | The managerâ€”not a new coordinatorâ€”remains the exclusive long-running mod-work boundary. |
| Installation | `ModPipelineManager.InstallAsync` admits and awaits `ModInstaller.InstallAsync`; `ModInstaller`/`ModExtractor` retain install/extraction mechanics. | A Phase 7 adapter may compose outcomes and progress but must not duplicate install mechanics or cancellation ownership. |
| Scan result | `ModPipelineResult` already contains status, completeness/commit state, warnings, user summary, technical diagnostic, and accepted snapshot. | Treat this as an implemented workflow-specific model, not as a planned result prototype. |
| UI composition | Primary pages are retained DI singletons; toast delivery and startup notification draining are already UI-owned adapters. | A future UI-operation state contract must account for retained-page lifetime and dispatcher/presentation ownership. |

Canonical architecture documents already reflect this baseline, especially `docs/Architecture/Systems/ApplicationLifecycle.md` and `docs/Architecture/Systems/ModManagement.md`.

## Findings

| ID | Severity | Finding | Evidence and impact | Required disposition |
|---|---|---|---|---|
| P7-01 | High | The Phase 7 plan uses a pre-Phase-6 ownership model. | `ModPipelineManager` now owns admission, cancellation, quiescence, scan commits, and install invocation; `result_and_workflow_policy.md` and several planning documents still call the component `ModPipelineCoordinator` and describe it as future work. | Rebase all Phase 7 planning on the implemented `ModPipelineManager`; preserve old names only when explicitly labelled historical. |
| P7-02 | High | Install outcome semantics are not sufficient for a UI-facing coordinator. | `InstallAsync` returns `ModInstallSummary?`; `null` represents non-admission and does not distinguish busy from admission stopped. Batch cancellation and partial failure lack one explicit top-level outcome/code/message contract. | Define one narrow install-operation result that wraps existing `ModInstallSummary` rather than replacing installer models. |
| P7-03 | High | The page still owns cross-cutting install orchestration. | `ModsPage.xaml.cs` subscribes directly to `ModInstaller` progress/extraction events, retains the install toast id, maps progress, handles completion, runs DLL unblock/refresh/reapply work, and owns local busy/status state. | Decide the exact Core/presentation-neutral coordinator boundary and move only approved orchestration from the page. Keep WPF dispatcher and toast presentation in UI. |
| P7-04 | High | The plan risks creating competing operation state. | The manager and shutdown controllers already own operation admission/cancellation/quiescence; `ModsPage`, `ModpacksPage`, and `SettingsPage` keep independent page-local state/messages. | Specify a UI-facing state adapter that observes existing work. It must not become a second shutdown controller or job queue. |
| P7-05 | Medium | Result styles are materially more diverse than the Phase 7 plan inventories. | Existing shapes include `ModPipelineResult`, `ModInstallSummary`, `ModInstallResult`, extraction/preflight results, BLSE/DLL/launch results, bool/out-message operations, exceptions, and modpack import tuples. | Create a source-derived result inventory and explicitly classify each candidate as retain, normalize, or defer before writing models. Do not begin with app-wide `Result<T>`. |
| P7-06 | Medium | Result policy and Phase 6 ledgers contain stale current-source claims. | `result_and_workflow_policy.md` says Phase 6.A "creates" a future coordinator and says `ModScanner` returns a list; Phase 6.A/B/C locked-decision files identify their work as unimplemented. Live source uses `ModPipelineManager`, `ModScanResult`, DI, lifecycle ownership, and Serilog. | Update factual current-grounding/status language and cross-links before treating these files as Phase 7 implementation authority. Retain decision detail as historical design evidence. |
| P7-07 | Medium | Phase 8 is still viable but its prerequisite is underspecified. | `mvvm_refactor_plan.md` correctly defers the rewrite, yet it assumes a settled Phase 7 state model without identifying the now-DI-resolved page seams, retained instances, event lifetime, or toast adapter boundary. | Refresh Phase 8's entry criteria after the Phase 7 ownership and state-contract decisions are approved. Do not start MVVM extraction in the replan. |
| P7-08 | Low | Version metadata diverges from the Phase 6 release record. | UI/Core project files still declare `0.12.15`; changelog/migration map declare `v0.13.83`. | Record this as an existing Phase 12 version-source decision. Do not absorb version work into Phase 7 without separate owner authorization. |
| P7-09 | Low | Existing warnings and manual test limits remain. | Build warnings persist; automated tests do not replace manual WPF install, cancellation, navigation-away, toast, and close/restart smoke coverage. | Preserve the warnings for later compatibility review and make the targeted Phase 7 manual matrix an explicit acceptance requirement. |

## Documentation Reconciliation Required Before Phase 7

The following is documentation-only planning work; it is not authorization to implement Phase 7 source changes.

| Document set | Required correction |
|---|---|
| `docs/refactor/refactor_master_plan.md` | Refresh the Phase 7 starting baseline, replace current-plan references to `ModPipelineCoordinator`, and narrow Phase 7 around outcome/progress adaptation instead of a new admission coordinator. Refresh Phase 8's Phase 7 dependency language. |
| `docs/refactor/result_and_workflow_policy.md` | Mark the scan/admission/quiescence contract implemented; rename `ModPipelineCoordinator` to `ModPipelineManager`; inventory actual result types and define the selected Phase 7 normalization slices. |
| `docs/refactor/dependency_injection_plan.md`, `logging_policy.md`, `persistence_policy.md`, `security_and_secret_boundary.md`, `testing_strategy.md`, and `archive_installer_safety_policy.md` | Replace obsolete Phase 6 future-state/current-source observations with concise implemented-baseline references, keeping deferred decisions genuinely deferred. |
| `docs/refactor/phase_6a_*`, `phase_6b_*`, and `phase_6c_*` | Clearly label these as historical locked decision records; cross-link to Phase 6 completion evidence rather than presenting their pre-implementation state as current. |
| `docs/refactor/documentation_alignment_handoff_checklist.md` | Reconcile Phase 6 handoff/status rows and identify the remaining Phase 7 documentation deliverables. |
| `docs/refactor/mvvm_refactor_plan.md` | Retain Phase 8 scope and owner rename checkpoint, but add the approved Phase 7 state/ownership contract as a precondition. |

## Recommended Phase 7 Replan

### Preserve These Boundaries

- `ModPipelineManager` retains admission, cancellation, quiescence, accepted snapshots, cache authorization, and invocation of `ModInstaller`.
- `ModInstaller` and `ModExtractor` retain archive/install mechanics.
- Core remains WPF-free; UI owns WPF dispatch, views, toasts, and presentation mapping.
- Provider/lifecycle/Serilog ownership remains exactly as implemented in Phase 6.
- Nexus work remains contract/handoff planning only. No download implementation, polling, or startup update behavior belongs in this phase.

### Decisions Required Before Coding

1. Approve an install-operation status/code matrix that distinguishes admitted success, partial failure, cancellation, busy, admission stopped, validation failure, and unexpected failure.
2. Decide whether post-install DLL unblock, refresh, and modpack reapplication are composite child outcomes or separate reported operations.
3. Decide the source of progress: retain insulated installer events, add a typed Core progress stream, or use another narrow UI-neutral adapter. Define subscriber lifetime and navigation-away cleanup.
4. Define the UI-operation-state contract needed by Phase 8: busy, status, error, cancellation affordance, current operation, last result, and toast mapping. Decide which state is shared and which remains page-local.
5. Select the first result-normalization slice. Recommended first slice: install batch plus directly related extraction/preflight, BLSE, DLL unblock, and modpack import outcomes. Defer broad persistence, save/create/export, and generic result abstractions unless the inventory proves repeated needs.
6. Define localization and technical-diagnostic rules. Existing hard-coded workflow messages must not become a reason to add more unlocalizable messages.

### Suggested Implementation Sequence After Approval

1. Complete the documentation reconciliation and approve the decisions above.
2. Produce the operation/result inventory with ownership, cancellation, progress, UI consumer, persistence effect, tests, and proposed disposition for each selected workflow.
3. Implement and test the narrow Core/presentation-neutral install outcome/progress boundary while preserving manager and installer authority.
4. Add the UI adapter and migrate `ModsPage` away from direct installer subscriptions only after the ownership tests pass.
5. Treat shared UI state as a Phase 8-ready contract; do not perform the ViewModel rewrite in Phase 7.
6. Complete targeted automated tests, then the manual WPF success/failure/partial-failure/cancel/navigation-away/toast matrix before Phase 7 closeout.

## Phase 8-12 Impact

| Phase | Assessment | Required planning action |
|---|---|---|
| 8 - MVVM | Retain. DI and retained pages provide a better starting point than the original plan assumed. | Gate entry on an approved Phase 7 UI-operation-state and event-lifetime contract; retain the owner page-rename checkpoint. |
| 9 - Performance audit | Retain. Benchmark infrastructure is present but its results remain provisional. | Preserve the report-only boundary and capture the authoritative baseline after Phase 8 settles. |
| 10 - approved optimization | Retain. | No change before Phase 9 owner decisions. |
| 11 - final audit | Retain. | Include the existing architecture/platform warnings and the Phase 7/8 manual smoke limits in final verification scope. |
| 12 - documentation/versioning | Retain. | Keep version-source reconciliation here; current `0.12.15` project metadata versus `v0.13.83` internal release documentation is not a Phase 7 change. |

## Readiness Gates

Phase 7 is ready to implement only when all of the following are complete:

- The documentation reconciliation is reviewed and accepted.
- The selected operation/result inventory and status/code matrix are approved.
- The Core/UI ownership boundary for progress, cancellation, completion, cleanup, post-install actions, and toast mapping is explicit.
- Focused tests are planned for admitted, busy, admission-stopped, cancelled, partial-failure, observer-failure, cleanup, post-install refresh failure, and navigation-away subscription cases.
- The Phase 7 manual WPF matrix covers install success/failure/cancellation, progress/toast behavior, navigation away and return, shutdown during active work, and no duplicate work admission.
- The future Nexus handoff remains interface/contract-only and does not broaden into Nexus implementation.

## Verification Limits

This audit verified source structure, Git baseline, planning/documentation alignment, Debug build/test execution, and independent Debug/Release baseline results. It did not run an interactive WPF session, real Steam/Bannerlord discovery, a real archive installation, a real cancellation/shutdown sequence, Nexus networking, or benchmarks. Those remain phase-appropriate manual or later-audit evidence, not completed behavior claims.

## Final Assessment

Proceed with a **focused Phase 7 documentation replan**, then implement the approved first slice. Do not begin from the existing Phase 7 text unchanged, and do not restart Phase 8-12. The architecture changed enough to require refreshed contracts and ownership decisions, but it changed in the direction the roadmap intended: the Phase 6 foundations substantially reduce Phase 7 risk once the plan is rebased on the live code.


