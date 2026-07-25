# Phase 7 Locked Decisions Reference

Status: implemented decision reference; automated verification complete, pending owner inspection and approval.

## Purpose

This concise repository reference preserves the 2026-07-24 Phase 7 decisions
without treating the temporary instruction package as current-source
documentation. The supplied full ledger remains the detailed decision record:
[Phase 7 locked decisions report](../TEMP_Codex_Instructions/CalradiaForge_Phase_7_Locked_Decisions_Report_2026-07-24.md).

## Locked decision index

| ID | Locked planning decision |
|---|---|
| P7-D01 | Phase 7 is **Install Outcomes, Application Notifications, and Phase 8 Naming Foundation**; it is not generic workflow, scheduler, MVVM, or Nexus work. |
| P7-D02 | A narrowly scoped, explicitly activated application-lifetime UI presenter observes manager semantics; `ToastService` remains a generic renderer and `MainWindow` the host. |
| P7-D03 | Rename the current `ModsPage` to `LauncherPage` and the visible label to **Launcher**; reserve `ModsPage` for future mod management. |
| P7-D04 | Add a non-null install-specific terminal result around, not instead of, `ModInstallSummary`; do not introduce generic `Result<T>`. |
| P7-D05 | `ModPipelineManager` remains the sole application admission, cancellation, terminal classification, and quiescence owner. |
| P7-D06 | Publish correlated, transient per-archive progress separately from one immutable terminal Core result. |
| P7-D07 | The presenter owns one correlated progress-to-terminal notification lifecycle; page/ViewModel code does not retain raw toast IDs. |
| P7-D08 | The manager-owned admitted pipeline includes installer work, DLL unblocking, authoritative scan/accepted-snapshot reconciliation, result construction, and release. Launcher presentation reconciliation remains temporary until Phase 8. |
| P7-D09 | Phase 7 requires deterministic automated validation. Phase 8 requires human-driven, Codex-observed runtime acceptance evidence. |
| P7-D10 | Completion requires implementation, deterministic validation, reconciliation, an implementation map/report, and cross-document audit; the listed Phase 8 and Nexus work remains deferred. |

## Implemented source boundary

Phase 7 extends the existing `ModPipelineManager` rather than creating another
coordinator. The manager now returns a non-null install-specific terminal
result, relays correlated immutable progress, owns Modules-directory unblocking
and internal authoritative scan/commit reconciliation, and holds admission and
quiescence through required consistency finalization.

The explicitly activated application-lifetime UI presenter owns the correlated
progress-to-terminal toast lifecycle. The behavior-preserving
`ModsPage` -> `LauncherPage` rename is implemented, and `LauncherPage`
temporarily retains accepted-snapshot, selected-modpack, and launch-state
presentation reconciliation before reporting semantic completion. Phase 8
remains responsible for moving that temporary presentation ownership to
`LauncherViewModel` or another approved presentation owner.

See the planning-only [expected Phase 7 migration map](phase_7_migration_map.md)
for the pre-implementation inventory and
[future mod-management context](nexus_future_mod_management_context.md)
for the deliberately separate Nexus handoff.
