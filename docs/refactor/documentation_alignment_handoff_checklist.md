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
