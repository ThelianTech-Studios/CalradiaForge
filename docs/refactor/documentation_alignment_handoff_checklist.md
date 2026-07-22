# Documentation Alignment Handoff Checklist

Status: planning template
Scope: owner-review support only. This checklist does not make refactor plans canonical architecture.

## Purpose

Track which accepted refactor decisions need to be migrated into canonical CalradiaForge documentation after each refactor phase.

## Planning Artifact Rule

Refactor documents are active planning artifacts. They are not canonical architecture until accepted decisions are migrated into stable docs, ADRs, changelog entries, or release notes.

## Handoff Table

| Refactor phase | Accepted decision | Target canonical doc/ADR/release note | Migration status | Owner review needed? |
|---|---|---|---|---|
| Phase 1 | Current WPF page rename inventory is recorded, but no page rename map is approved. | Keep in `docs/refactor/ui_page_rename_review_checklist.md` until an owner-approved rename map exists. | Planning artifact only; do not migrate as canonical architecture yet. | Yes, before any `.xaml` rename. |
| Phase 1 | The original Steam Workshop scanner/path-resolution report identified legacy `GamePathsHelper`, `AppConfigSettings`, `GamePathValidator`, `ModService`, and `ModScanner` ownership. Phase 5 supersedes that historical state with the resolver/service workflow and automated regressions. | Platform/path-detection architecture and the Phase 5 completion audit. | Automated implementation verification complete; owner real-Steam/WPF smoke and approval remain pending. | Yes, before release acceptance. |
| Phase 1 | Warning cleanup was limited to low-risk nullable/name/comment cleanup and did not change UI/Core/Nexus ownership. | Changelog internal summary only. | Completed cleanup note; no architecture migration required. | No. |
| Phase 2 | Serilog infrastructure is completed, but the legacy logger and callers remain live; active-file and package facts are historical implementation evidence, not lifecycle ownership. The original redaction infrastructure was superseded on 2026-07-12; the remaining custom formatter is neutral. | Logging architecture, security/trust-boundary, testing/QA, and configuration/retention docs after accepted decisions. | Historical phase boundary; do not claim startup wiring or caller migration. | Yes, for lifecycle ownership or future export/telemetry policy. |
| Phase 5 | Game-platform detection/workflow, Steam split-library behavior, manual configuration, startup notifications, scanner safety, test migration, and the completion audit are accepted only after implementation and verification. | Platform/path detection, configuration, launcher, mod-management, testing/QA, and relevant architecture docs. | Automated verification complete; [detailed Phase 5 record](game_platform_detection_and_path_workflow_plan.md) remains the implementation contract and owner real-Steam/WPF smoke remains pending. | Yes, including owner Steam split-library smoke evidence. |
| Phase 6.A | Coordinator completeness, accepted-snapshot, cache-commit, and quiescence behavior require implementation review. | Core workflow, persistence, modpack, and testing docs. | Planning gate; [detailed Phase 6.A ledger](phase_6a_mod_pipeline_coordinator_locked_decisions_2026-07-21.md) is authoritative. | Yes. |
| Phase 6.B | One-provider composition, lifecycle, settings, logger/file policy, and UI lifetime require implementation review. | Lifecycle/startup/shutdown, logging, settings, UI, and testing docs. | Planning gate; [detailed Phase 6.B ledger](phase_6b_di_and_lifecycle_locked_decisions_2026-07-21.md) is authoritative. | Yes. |
| Phase 6.C | Legacy caller migration and the emergency writer require implementation review. | Logging, security/trust-boundary, testing, and relevant Nexus docs. | Planning gate; [detailed Phase 6.C ledger](phase_6c_legacy_logger_migration_locked_decisions_2026-07-21.md) remains authoritative until zero normal legacy callers are verified. | Yes. |
| Phase 4 | Test/benchmark framework, fixture, baseline, analyzer, and threshold decisions are recorded as approved or explicitly unresolved. | Future testing/QA and performance/diagnostics documentation after acceptance. | Planning artifact until choices are approved and infrastructure exists. | Yes, for new packages, fixtures, analyzers, or blocking thresholds. |
| Phase 9 | Performance audit findings and authoritative baselines are recorded in the dated report, but remain review artifacts until the owner decides. | Future performance/diagnostics docs or ADRs for stable accepted decisions. | Pending developer review; no production optimization implied. | Yes, for every proposed finding. |
| Phase 10 | Approved performance changes and their measured outcomes are documented by finding ID. | Relevant canonical system docs or ADRs after stable behavior is accepted. | Requires manual inspection and owner acceptance before changelog/commit workflow. | Yes. |
| Phase 11 | Final verification results, limitations, and unresolved performance findings are recorded. | Testing/QA, performance, architecture, or ADR documentation as appropriate. | Final technical verification artifact. | Yes, for accepted exceptions or remaining risks. |
| Phase 12 | Release documentation reflects only the final verified implementation and accepted stable decisions. | Changelog, release notes, canonical docs, and ADRs as appropriate. | Release/documentation handoff. | Yes, before release closeout. |

## Target Documentation Areas

- Project scope and roadmap.
- Architecture and dependency rules.
- Platform and game path detection.
- Data and persistence.
- Application systems.
- UI/UX and MVVM policy.
- Testing and QA.
- Security and trust boundaries.
- Release/versioning.
- ADRs.
- Reviews.

## Rule

The Phase 6 ledgers are authoritative future implementation contracts, but they are not proof that their behavior is implemented. Only migrate stable behavior proven by source/tests into canonical architecture. Phase 6.A hands off proven coordinator/completeness/accepted-snapshot/quiescence behavior; Phase 6.B hands off proven one-provider, lifecycle, target settings, and logger file-lifecycle behavior; Phase 6.C hands off proven caller migration and emergency-writer behavior.

## Phase 6 Documentation Reconciliation Gate

- Every source-ledger heading is transferred to a canonical phase document without condensing away workflows, exclusions, test cases, exit criteria, or handoff rules.
- Supporting policies are reconciled in their natural sections; no appended “alignment” or “supersedes above” block is used to override contradictory text.
- The reconciliation audit contains complete ledger-to-final-heading traceability.
- Resolved decisions are removed from open-question and decision-register sections.
- Current implemented behavior and planned Phase 6 behavior are explicitly distinguishable.
- Every changed document is reread in full and all relative Markdown links are validated.
- An independent reviewer reads the complete ledgers and every changed document, reports pass/fail with file/section references, and does not rely on the editor summary.
- Every valid reviewer finding is fixed and the affected verification is rerun before completion is claimed.
