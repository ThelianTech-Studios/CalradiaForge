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
| Phase 1 | Steam Workshop scanner/path-resolution report remains unresolved; current ownership is `GamePathsHelper`, `AppConfigSettings`, `GamePathValidator`, `ModService`, and `ModScanner`. Bannerlord AppID `261550` is already present. | Future platform/path-detection doc or ADR after scanner behavior is implemented and verified. | Deferred risk recorded; not described as fixed. | Yes, for multi-library/manual override policy. |
| Phase 1 | Warning cleanup was limited to low-risk nullable/name/comment cleanup and did not change UI/Core/Nexus ownership. | Changelog internal summary only. | Completed cleanup note; no architecture migration required. | No. |
| Phase 2 | Serilog infrastructure is completed, but the legacy logger and callers remain live; current active-file, formatter/redactor, and package facts are historical implementation evidence, not lifecycle ownership. | Logging architecture, security/trust-boundary, testing/QA, and configuration/retention docs after accepted decisions. | Historical phase boundary; do not claim startup wiring or caller migration. | Yes, for any lifecycle or redaction change. |
| Phase 5.A | One logger owner, startup cleanup, shutdown quiescence, exact-once close, active-handle release, collision-safe archive, and provider disposal order are accepted only after implementation review. | Application lifecycle/startup/shutdown, logging architecture, security/trust-boundary, configuration/retention, and testing/QA docs. | Planning gate; unresolved options are not accepted architecture. | Yes. |
| Phase 5.B | Legacy caller migration and secret-safe structured-property/exception review are accepted separately from any formatter/redactor removal decision. | Logging architecture, security/trust-boundary, testing/QA, and relevant Nexus docs. | Requires explicit owner decision before redaction is narrowed or removed. | Yes. |
| Phase 4 | Test/benchmark framework, fixture, baseline, analyzer, and threshold decisions are recorded as approved or explicitly unresolved. | Future testing/QA and performance/diagnostics documentation after acceptance. | Planning artifact until choices are approved and infrastructure exists. | Yes, for new packages, fixtures, analyzers, or blocking thresholds. |
| Phase 8 | Performance audit findings and authoritative baselines are recorded in the dated report, but remain review artifacts until the owner decides. | Future performance/diagnostics docs or ADRs for stable accepted decisions. | Pending developer review; no production optimization implied. | Yes, for every proposed finding. |
| Phase 9 | Approved performance changes and their measured outcomes are documented by finding ID. | Relevant canonical system docs or ADRs after stable behavior is accepted. | Requires manual inspection and owner acceptance before changelog/commit workflow. | Yes. |
| Phase 10 | Final verification results, limitations, and unresolved performance findings are recorded. | Testing/QA, performance, architecture, or ADR documentation as appropriate. | Final technical verification artifact. | Yes, for accepted exceptions or remaining risks. |
| Phase 11 | Release documentation reflects only the final verified implementation and accepted stable decisions. | Changelog, release notes, canonical docs, and ADRs as appropriate. | Release/documentation handoff. | Yes, before release closeout. |

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

Only migrate stable, accepted decisions. Do not migrate speculative planning text as final architecture.
