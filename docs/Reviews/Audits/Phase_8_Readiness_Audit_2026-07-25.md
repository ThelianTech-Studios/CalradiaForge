> Historical audit evidence migrated from the preserved documentation snapshot on 2026-08-02. This report retains its original scope, baseline, and limitations; it is not authoritative for current implementation behavior.

# CalradiaForge Phase 8 Readiness Deep Audit

Status: Read-only Phase 8 planning-readiness audit

Audit date: 2026-07-25

Branch / commit: `dev-V0-14-CodeRefactor` / `4de1bd9f512e59a32613c37fc74803c5b3d08bc9`

Scope: Read-only audit of the current Phase 8 planning documents, Phase 7 handoff, UI/source seams, release/version records, current automated-test baseline, and audit/register provenance. No application source, existing planning document, test, project, configuration, or Git history was changed. This report and its sitemap registration are the only artifacts created.

## Executive Summary

**Verdict: conditionally ready for a documentation-and-decision-lock slice, not ready for broad Phase 8 implementation.**

The architecture is viable. Phase 7 established the prerequisite manager-owned install boundary, correlated notification presenter, retained `LauncherPage` identity, DI composition, and an explicit handoff of temporary Launcher presentation reconciliation. The staged `LauncherViewModel` -> Modpacks -> Settings -> shell plan preserves the UI/Core boundary and is a practical implementation route.

However, do not begin a broad MVVM rewrite yet. The owner manually runtime-tested the Launcher label and changed one translation string from `Launcher` to `Home` because the former was too long for the visual font. The resulting two localization-test expectation mismatches are being manually corrected by the owner; the intended Phase 7 baseline remains 195 passing tests. The master plan also has contradictory Phase 7 status claims, and key MVVM ownership/lifetime choices remain explicitly open. The current evidence supports a narrow first planning/execution slice only after the owner locks the decisions in this report.

Version metadata drift is real but belongs to Phase 12 and must not expand this Phase 8 scope without explicit owner approval.

## Audit Method And Evidence

- Read `AGENTS.md`, `docs/refactor/task_execution_optimization_policy.md`, the master plan, MVVM/testing/result policies, Phase 7 decision records, current architecture documents, changelog, migration map, sitemap, relevant UI/project source, and recent Git history.
- Applied the task-execution policy through three independent, read-only workstreams: Phase 8 documentation/contract review; version/release/source inspection; and Phase 7 history/provenance review. The workstreams had non-overlapping scopes and made no edits.
- Ran `dotnet test source/CalradiaForge.slnx -c Debug --no-restore`. Build completed with existing MSIL/AMD64 SevenZipWrapper and Windows-platform warnings, then two localization tests failed. The command stopped before Release because Debug returned failure.
- Compared the current commit with the Phase 7 release record rather than treating pre-implementation planning inventories as current-source evidence.

## Current Baseline

| Area | Current evidence | Audit significance |
|---|---|---|
| Phase 7 implementation | `docs/CHANGELOG.md` records `v0.13.90`, owner-approved Phase 7 implementation, and 195 passing Debug/Release tests at its accepted endpoint. | Phase 8 has a credible architectural starting point. |
| Phase 7 handoff | `phase_7_locked_decisions_2026-07-24.md` reserves temporary Launcher reconciliation for Phase 8 and requires human-driven runtime acceptance. | The next phase must transfer presentation state without recreating Core admission, cancellation, or notification ownership. |
| MVVM implementation | No `LauncherViewModel`, base ViewModel, or command framework currently exists; primary pages set `DataContext = this`. | This is a genuine staged extraction, not a small conversion. |
| Test baseline | The owner manually changed the visible Launcher translation to `Home` after runtime testing showed `Launcher` was too long for the visual font. The two affected test expectations are being manually corrected; all 195 tests are treated as passing for the Phase 7 baseline. | This label adjustment is not a Phase 8 architectural blocker. |
| Versioning | UI/Core project metadata remains `0.12.15`; changelog/migration record is `0.13.90`; no root `Directory.Build.props` exists. | Confirmed Phase 12 debt, not Phase 8 implementation scope. |

## Findings

| ID | Severity | Finding | Evidence and impact | Required disposition |
|---|---|---|---|---|
| P8-01 | Resolved by owner | The visible Launcher label was manually shortened after runtime testing. | The owner changed the default `Nav_LauncherTab` from `Launcher` to `Home` because `Launcher` was too long for the visual font. The two affected test expectations are being manually corrected; the established baseline is 195 passing tests. | Retain `Home` as the approved visible label and complete the ownerâ€™s test-expectation edit. No Phase 8 scope change is required. |
| P8-02 | High | The master plan contradicts the accepted Phase 7 record. | `refactor_master_plan.md` says Phase 7 is implemented in its decision/Phase 7 sections but labels it `Planned` in the phase list and leaves its checklist unchecked. Changelog and Phase 7 decision reference record implementation. | Reconcile Phase 7 status/current-source wording before using the master plan as an execution prompt. |
| P8-03 | High | The Phase 7 expected migration map is historical planning inventory, not current evidence. | `phase_7_migration_map.md` labels itself planning-only and uses a pre-Phase-7 baseline, while `CHANGELOG.md`/`MIGRATION_MAP.md` record the accepted `v0.13.90` source range. | Label/archive it as historical planning material or replace its current-plan references with the accepted implementation record. Do not plan Phase 8 from its future-tense rows. |
| P8-04 | High | Core Phase 8 decisions are unresolved. | `mvvm_refactor_plan.md` leaves helper/toolkit choice, dialog abstraction timing, navigation order, and possible reusable presentation service open. It also does not lock retained-page/DataContext/ViewModel lifetime or dispatcher/collection-update rules. | Create a short owner-approved Phase 8 decision record before code changes. No package should be added until the helper/toolkit decision is approved. |
| P8-05 | High | The proposed `LauncherPage` -> `LauncherView` rename is not approved. | The Phase 7 rename was explicitly behavior-preserving to `LauncherPage`; the MVVM plan proposes `LauncherView` while the master-plan guardrail requires an owner-approved XAML rename map. | Either retain `LauncherPage` for the first ViewModel slice, or approve an exact rename map and validation inventory first. Recommended: retain the existing page name for the first slice. |
| P8-06 | Medium | Runtime acceptance is specified but not operationally bound to slices. | The testing strategy lists the necessary install/navigation/cancellation/toast scenarios, while Phase 8 acceptance is human-driven. | Convert the matrix into a per-slice checklist with expected result, evidence source, pass/fail, defect, and owner signoff. Do not defer all runtime evidence to the end. |
| P8-07 | Medium | Documentation evidence has count and baseline drift. | `testing_strategy.md` says 194 Phase 7 tests; `CHANGELOG.md` says 195. The master plan assigns authoritative performance baselines to Phase 9 after Phase 8, while benchmark material uses Phase 8 wording. | Correct counts after a green rerun and make Phase 9 the sole authoritative post-Phase-8 performance-baseline owner. |
| P8-08 | Low | Versioning documentation is stale but intentionally deferred. | `versioning_policy.md` describes future `Directory.Build.props`; project files hardcode `0.12.15`; Settings displays assembly major/minor/build and omits prerelease metadata. | Track as Phase 12 debt only. Do not mix version-source cleanup into MVVM extraction without explicit scope expansion. |
| P8-09 | Low | The documentation sitemap still describes the Phase 7 temporary ledger as unimplemented. | `docs/DOCUMENT_SITEMAP.md` Phase 7 temporary-instruction row has stale retention wording. | Reconcile the retention/status wording during the documentation pass; preserve the historical record. |

## What Is Sound And Should Be Preserved

- `ModPipelineManager` remains the sole Core owner of admission, cancellation, accepted snapshots, reconciliation, and quiescence.
- `ModInstaller` and `ModExtractor` retain installer/extraction mechanics.
- The application-lifetime presenter owns correlated progress-to-terminal notification lifecycle; ViewModels must not retain raw toast IDs.
- Core stays WPF-free. ViewModels consume Core outcomes and do not become a second scheduler, workflow manager, or install service.
- The documented migration order is sound: Launcher workflow first, then Modpacks, Settings, shell/navigation, and only then removal of verified obsolete code-behind.

## Required Phase 8 Decision Lock

The following owner decisions should be captured in one small Phase 8 decision record before implementation:

1. **First slice and rollback boundary.** Approve `LauncherViewModel` only as the first independently reviewable change; retain `LauncherPage` naming unless a separate rename map is approved.
2. **ViewModel/command approach.** Choose hand-written base/command classes or an approved toolkit/package. Define whether package introduction is permitted.
3. **Lifetime and binding model.** Define retained page, ViewModel, and `DataContext` ownership; construction/DI registration; activation/deactivation; and disposal/unsubscription rules.
4. **Launcher completion ordering.** Define the exact order for progress, terminal result, accepted-snapshot synchronization, selected-modpack reapplication, launch-state recalculation, semantic completion, and the presenterâ€™s final notification.
5. **Threading and state contract.** Define UI-dispatcher ownership, collection synchronization, command enablement, `IsBusy`, `CanCancel`, `StatusMessage`, `ErrorMessage`, current operation, last result, cancellation, and error semantics.
6. **Navigation and dialog boundary.** Decide whether shell/navigation changes follow or precede the Launcher extraction and whether dialog/file-picker abstractions are created now or only when a page needs them.
7. **Acceptance evidence.** Name the owner of the per-slice human-driven Visual Studio matrix and the evidence to collect.

## Recommended Implementation Sequence After Approval

1. Complete the ownerâ€™s test-expectation edit for the approved `Home` label; retain the established 195-test baseline.
2. Reconcile P8-02, P8-03, P8-07, and P8-09 in a documentation-only pass. Keep versioning work deferred.
3. Approve the Phase 8 decision lock above and freeze a Launcher behavior inventory.
4. Implement only the `LauncherViewModel` slice, preserving the current page name and Core/presenter ownership. Add focused ViewModel state/command tests.
5. Run the relevant automated tests and the Launcher subset of the runtime matrix; stop for owner review.
6. Repeat the same bounded cycle for Modpacks, Settings, and shell/navigation. Remove code-behind only after equivalent behavior is verified.

## GPT-Web Planning Questions

- Which command/ViewModel pattern best fits this WPF solution: hand-written primitives or a toolkit, and what package/lifecycle risks does each introduce?
- Should `LauncherPage` remain the view name in Phase 8 to avoid unnecessary XAML churn, with a later explicitly approved rename if desired?
- What exact state machine and ordering should govern install progress, cancellation, terminal result, Launcher reconciliation, and final toast presentation?
- What retained-page/ViewModel lifetime and event-subscription strategy avoids duplicate handlers, stale updates, and navigation-away leaks?
- Which human-driven acceptance scenarios belong to the Launcher slice versus later page/shell slices?
- What documentation changes are sufficient to make Phase 7 records authoritative without rewriting historical ledgers?

## Final Readiness Decision

Do **not** start the broad Phase 8 rewrite today. The owner-approved `Home` label and 195-test baseline are not blockers. First reconcile the contradictory Phase 7 evidence and lock the seven Phase 8 decisions above. Once complete, start the narrow `LauncherViewModel` slice; the current architecture and staged plan are otherwise viable.


