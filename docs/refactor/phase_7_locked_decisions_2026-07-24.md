# Phase 7 Locked Decisions Reference

Status: owner-locked planning reference; Phase 7 is not implemented.

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

## Current-source boundary

At `203464d`, none of the Phase 7 contracts in this reference are implemented.
`ModsPage` still owns direct installer observation, toast identity, DLL unblocking,
and post-install refresh/presentation reconciliation. The current source baseline
is documented in the [Phase 7 reconciliation audit](../audits/calradiaforge_phase_7_documentation_reconciliation_2026-07-24.md).

See the [expected Phase 7 migration map](phase_7_migration_map.md) for the
implementation inventory and [future mod-management context](nexus_future_mod_management_context.md)
for the deliberately separate Nexus handoff.
