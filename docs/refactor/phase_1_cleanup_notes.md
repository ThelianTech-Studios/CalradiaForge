# Phase 1 Cleanup Notes

Status: completed
Scope: Phase 1 implementation record and deferred-risk tracking. This document is a refactor planning artifact, not canonical architecture.

## Purpose

Record the low-risk cleanup performed for Phase 1 and the items intentionally deferred because they require tests, owner approval, or a later phase.

## Source And Documentation Read

- `docs/refactor/refactor_master_plan.md`
- `docs/refactor/ui_page_rename_review_checklist.md`
- `docs/refactor/documentation_alignment_handoff_checklist.md`
- `docs/refactor/versioning_policy.md`
- `docs/refactor/testing_strategy.md`
- `docs/refactor/logging_policy.md`
- `docs/refactor/persistence_policy.md`
- `docs/refactor/security_and_secret_boundary.md`
- `docs/refactor/archive_installer_safety_policy.md`
- `docs/refactor/dependency_injection_plan.md`
- `docs/refactor/result_and_workflow_policy.md`
- `docs/refactor/mvvm_refactor_plan.md`
- `docs/Architecture/**`
- `docs/audits/calradiaforge_codebase_audit_07-07-26.md`
- `docs/audits/calradiaforge_docs_alignment_report_07-08-26.md`
- Relevant source files in Core path/scanner/modpack/launch/localization areas and UI startup/page/navigation areas.

## Completed Cleanup

- Removed stale commented-out Steam Workshop detection code from `GamePathsHelper`.
- Kept unsupported Epic/GamePass detection disabled while avoiding a compile-time unreachable-code warning.
- Corrected low-risk spelling and naming drift in private/internal members.
- Added safe nullability annotations or fallbacks where existing code already filters nullable module IDs.
- Added `string.Empty` initializers to translation string properties to reduce fallback-string nullable warnings.
- Corrected small README and changelog typos and stale links without changing release/version metadata.
- Populated the UI page rename review checklist with current page inventory only.
- Updated the documentation handoff checklist with Phase 1 planning-artifact status.

## Steam Workshop Scanner Known Issue

The Steam Workshop scanner/path-resolution report remains unresolved.

Current ownership:

- `AppConfigSettings.IsGameFromSteam`
- `AppConfigSettings.SteamWorkshopFolderPath`
- `GamePathsHelper.TryAutoDetectGameFolder`
- `GamePathsHelper.TryDetectSteam`
- `GamePathsHelper.GetSteamWorkshopFolder`
- `GamePathValidator.ValidateWorkshopFolder`
- `ModService.RefreshAsync`
- `ModScanner.ScanForModsAsync`

Current source observation:

- Bannerlord AppID `261550` is already present in `GamePathsHelper`.
- Steam auto-detection composes the Workshop path under the Steam client registry path.
- The scanner consumes the configured Workshop path; it does not discover Steam library roots.
- No behavior was changed in Phase 1.

Deferred investigation focus:

- Steam client install path versus Steam library roots.
- Bannerlord install library versus Workshop content library.
- Multiple Steam library roots.
- Manual Workshop path override behavior.
- Fake-directory scanner/path tests before implementation.
- Structured scanner warnings and ordinary local diagnostics in later phases; no automatic path or secret filtering is required.

## Deferred Cleanup

| Item | Reason deferred |
|---|---|
| Broad README encoding cleanup | Larger documentation encoding pass outside Phase 1 source cleanup. |
| `TODO_v1.md` Nexus roadmap wording | Requires owner decision on roadmap wording and timing. |
| Version source-of-truth implementation | Deferred to master Phase 12; owner approval is needed before major/minor changes. |
| Platform analyzer warnings | Phase 6.A adapter/platform-targeting work. |
| Archive, BLSE, persistence, logger lifecycle, DI, MVVM, and result/workflow changes | Explicitly excluded from Phase 1. |
| Steam Workshop scanner fix | Requires scoped implementation and verification in a later phase. |
| UI page `.xaml` renames | Requires owner-approved rename map. |

## Verification Expectation

Phase 1 verification is `dotnet build source/CalradiaForge.slnx`.
