# CalradiaForge Documentation Sitemap And Retention Register

This document is the file register for the repository `docs` folder. It is also a protected document and must not be cleaned up or deleted.

## Instructions When Prompted To “Check Docs Cleanup”

When a user prompts for `check docs cleanup` or equivalent wording, use this document as the cleanup task instruction set and execute the following workflow in order:

1. Read the applicable guidance in `Document Rules`, `Audit Report Retention Rules`, `Other Temporary-Document Rules`, and the row-level cleanup criteria before changing anything.
2. Check the existing temporary-document table first. Review rows from oldest to newest for files that meet every applicable cleanup criterion, including age, addressed-content, latest-audit, approval, and keep-record requirements. Treat human-edited rows as read-only overrides.
3. Verify each candidate against the filesystem. Do not delete a row merely because the file is missing; update only the permitted path/result cells unless the row’s keep-record rule allows row removal.
4. Check the repository’s `docs` folder recursively for newly created document files that are not listed in either table. Classify each unlisted document as either protected or cleanup-managed using the rules below, then append a new row at the bottom of the appropriate table. Capture its file name, short path, creation time, last-used time, cleanup criteria, current result, and keep-record choice.
5. After the inventory is complete, perform only the cleanup actions authorized by the applicable rules. Never delete protected documents, Architecture documents, Nexus documents, active refactor documents, canonical application documents, or the sitemap through this workflow.
6. For every deletion, update the existing row’s allowed path/result cells with `Deleted-MM-DD-YY HH:mm:ss` in 24-hour format. Keep or remove the row only according to its keep-record cell and the stated post-cleanup retention period.
7. Recheck the filesystem and both tables. Confirm that every remaining document is registered exactly once, no protected file was touched, new rows were appended rather than reordered, and the final cleanup results accurately state `Exists` or the deletion timestamp.

## Document Rules

- Keep this sitemap at `\repository-root\docs\DOCUMENT_SITEMAP.md`.
- New table entries are always appended to the bottom of their table. Do not reorder existing rows; the oldest tracked files should remain at the top of the temporary-document table.
- `Date Created` is the filesystem creation timestamp. `Date Last Used` is the filesystem last-access timestamp captured when this row was recorded. Windows last-access updates may be disabled or delayed, so developers may override this cell when they have better usage evidence.
- Human Developers may override these rules, alter individual table cells, or remove an entire row. Do not restore or “correct” a human-edited row.
- Existing table rows are read-only by default. A file name or short path may be changed only when the file was renamed or moved and its `Clean up Results` cell still says `Exists`.
- When a file is ready for cleanup, only its `Short File Path` and `Clean up Results` cells may be edited. If `Keep Record in Table after Cleanup` is `No`, the entire row may be removed after the file is deleted.
- If `Keep Record in Table after Cleanup` is `Yes`, retain the row after deletion. The row may be removed only after the required post-cleanup retention period stated below.
- Never delete or modify files under `\repository-root\docs\Architecture`, including Nexus architecture files, as part of temporary-document cleanup.
- Never delete active refactor policy/planning documents or canonical application documentation merely because they are old. They belong in the protected section.
- Cleanup must not occur solely because a file is old. The file must satisfy every applicable criterion in its row and the general rules below.
- Do not touch files outside the `docs` folder through this register.

### Audit Report Retention Rules

- Always keep the latest and most recent audit report until everything it reported on has been fixed, worked on, or documented in implementation-preparation or planning documents.
- Older audit reports must be kept for at least seven days from their creation date and must also satisfy the same “everything addressed” rule before cleanup.
- An audit report older than seven days may be queued for cleanup only after its criteria are satisfied and the latest relevant audit/report record remains available.
- An audit row remains in this table after cleanup when its keep-record cell says `Yes`.
- Audit rows older than 30 days may be queued for removal from this table after the file has been deleted and its historical record is no longer needed.

### Other Temporary-Document Rules

- A prompt, summary, or other temporary document may be cleaned up only after everything it describes has been fixed, completed, superseded, or recorded in implementation-preparation/planning documents.
- A temporary-document row may be deleted after the file is cleaned up when its keep-record cell says `No`.
- When its keep-record cell says `Yes`, the row must remain for at least seven days after the deletion timestamp before it can be removed from this table.
- More specific rules may be added later. New rules must not silently rewrite existing human overrides.

## Temporary Documents — Cleanup Only When Criteria Are Met

| File Name | Short File Path | Date Created (MM-DD-YY HH:mm:ss 24h) | Date Last Used | Clean up Criteria (unorder list of criteria if there are multiple criteria) | Clean up Results (Exists/Deleted-DATETIME24h format) | Keep Record in Table after Cleanup (Yes/No) |
| --- | --- | --- | --- | --- | --- | --- |
| `calradiaforge_codebase_audit_07-07-26.md` | `\repository-root\docs\audits` | 07-07-26 15:17:58 | 07-12-26 17:32:19 | <ul><li>Keep at least 7 days from creation.</li><li>Everything reported is fixed, worked on, or documented in implementation-preparation/planning documents.</li><li>It is not the latest relevant audit report.</li><li>Human Developer approval is available when cleanup is queued.</li></ul> | Exists | Yes |
| `refactor_documentation_instructions.md` | `\repository-root\docs\audits` | 07-07-26 20:29:49 | 07-12-26 17:32:19 | <ul><li>All instructions are implemented, superseded, or captured in current planning/policy documents.</li><li>No active workflow still depends on this prompt.</li><li>Human Developer approval is available when cleanup is queued.</li></ul> | Exists | No |
| `calradiaforge_docs_alignment_report_07-08-26.md` | `\repository-root\docs\audits` | 07-08-26 13:15:29 | 07-12-26 17:32:19 | <ul><li>Keep at least 7 days from creation.</li><li>Everything reported is fixed, worked on, or documented in implementation-preparation/planning documents.</li><li>It is not the latest relevant audit report.</li><li>Human Developer approval is available when cleanup is queued.</li></ul> | Exists | Yes |
| `calradiaforge_refactor_documentation_deep_audit_2026-07-12.md` | `\repository-root\docs\audits` | 07-12-26 14:36:10 | 07-12-26 17:32:26 | <ul><li>Always retain while it is the latest relevant audit report.</li><li>After it is no longer latest, keep at least 7 days from creation.</li><li>Everything reported is fixed, worked on, or documented in implementation-preparation/planning documents.</li><li>Human Developer approval is available when cleanup is queued.</li></ul> | Exists | Yes |
| `calradiaforge_source_state_deep_audit_2026-07-15.md` | `\repository-root\docs\audits` | 07-15-26 16:42:49 | 07-15-26 16:45:24 | <ul><li>Always retain while it is the latest relevant audit report.</li><li>After it is no longer latest, keep at least 7 days from creation.</li><li>Everything reported is fixed, worked on, or documented in implementation-preparation/planning documents.</li><li>Human Developer approval is available when cleanup is queued.</li></ul> | Exists | Yes |
| `calradiaforge_steam_multilibrary_scanner_diagnostic_2026-07-15.md` | `\repository-root\docs\audits` | 07-15-26 13:44:58 | 07-16-26 15:17:02 | <ul><li>Always retain while it is the latest relevant audit report.</li><li>After it is no longer latest, keep at least 7 days from creation.</li><li>Everything reported is fixed, worked on, or documented in implementation-preparation/planning documents.</li><li>Human Developer approval is available when cleanup is queued.</li></ul> | Exists | Yes |
| `calradiaforge_steam_multilibrary_autodetection_test_report_2026-07-15.md` | `\repository-root\docs\audits` | 07-15-26 14:11:20 | 07-16-26 15:17:02 | <ul><li>Always retain while it is the latest relevant audit report.</li><li>After it is no longer latest, keep at least 7 days from creation.</li><li>Everything reported is fixed, worked on, or documented in implementation-preparation/planning documents.</li><li>Human Developer approval is available when cleanup is queued.</li></ul> | Exists | Yes |
| `calradiaforge_steam_multilibrary_fix_verification_2026-07-15.md` | `\repository-root\docs\audits` | 07-15-26 15:09:56 | 07-16-26 15:17:02 | <ul><li>Always retain while it is the latest relevant audit report.</li><li>After it is no longer latest, keep at least 7 days from creation.</li><li>Everything reported is fixed, worked on, or documented in implementation-preparation/planning documents.</li><li>Human Developer approval is available when cleanup is queued.</li></ul> | Exists | Yes |
| `calradiaforge_phase_5_implementation_2026-07-16.md` | `\repository-root\docs\audits` | 07-16-26 15:07:00 | 07-16-26 16:07:07 | <ul><li>Always retain while it is the latest relevant implementation/audit report.</li><li>After it is no longer latest, keep at least 7 days from creation.</li><li>Everything reported is fixed, worked on, or documented in implementation-preparation/planning documents.</li><li>Human Developer approval is available when cleanup is queued.</li></ul> | Exists | Yes |
| `calradiaforge_source_state_deep_audit_2026-07-21.md` | `\repository-root\docs\audits` | 07-21-26 12:28:27 | 07-21-26 12:28:27 | <ul><li>Always retain while it is the latest relevant source-state audit report.</li><li>After it is no longer latest, keep at least 7 days from creation.</li><li>Everything reported is fixed, worked on, or documented in implementation-preparation/planning documents.</li><li>Human Developer approval is available when cleanup is queued.</li></ul> | Exists | Yes |
| `agents-audit_2026-07-21.md` | `\repository-root\docs\audits` | 07-21-26 12:51:10 | 07-21-26 12:51:10 | <ul><li>Always retain while it is the latest relevant agent-instruction audit report.</li><li>After it is no longer latest, keep at least 7 days from creation.</li><li>Everything reported is fixed, worked on, or documented in implementation-preparation/planning documents.</li><li>Human Developer approval is available when cleanup is queued.</li></ul> | Exists | Yes |
| `CalradiaForge_Phase_5_Refactor_Docs_Repair_COMPLETE_Codex_Prompt.md` | `\repository-root\docs\TEMP_Codex_Instructions` | 07-16-26 09:53:10 | 07-16-26 10:23:19 | <ul><li>The completed prompt is superseded or its useful decisions are captured in current planning/policy documents.</li><li>No active workflow depends on the prompt.</li><li>Human Developer approval is available when cleanup is queued.</li></ul> | Exists | No |

## Never Delete These Documents

The following documents are canonical architecture, application, refactor-policy, or sitemap records. They are not cleanup-managed by the temporary-document rules above.

| File Name | Short File Path | Date Created (MM-DD-YY HH:mm:ss 24h) | Date Last Used | Clean up Criteria (unorder list of criteria if there are multiple criteria) | Clean up Results (Exists/Deleted-DATETIME24h format) | Keep Record in Table after Cleanup (Yes/No) |
| --- | --- | --- | --- | --- | --- | --- |
| `ARCHITECTURE_INDEX.md` | `\repository-root\docs\Architecture` | 06-12-26 19:44:26 | 07-12-26 14:08:14 | <ul><li>Never delete through cleanup.</li></ul> | Exists | No |
| `NEXUS_INTEGRATION_ARCHITECTURE.md` | `\repository-root\docs\Architecture\Nexus` | 06-12-26 19:40:26 | 07-12-26 17:32:19 | <ul><li>Never delete through cleanup.</li><li>Nexus architecture is explicitly protected.</li></ul> | Exists | No |
| `Nexus_Integration_Plan.md` | `\repository-root\docs\Architecture\Nexus` | 06-12-26 19:07:12 | 07-12-26 17:37:27 | <ul><li>Never delete through cleanup.</li><li>Nexus planning is explicitly protected.</li></ul> | Exists | No |
| `CalradiaForge.ConsoleUtils.md` | `\repository-root\docs\Architecture\Projects` | 06-12-26 19:40:24 | 07-12-26 14:08:14 | <ul><li>Never delete through cleanup.</li></ul> | Exists | No |
| `CalradiaForge.Core.md` | `\repository-root\docs\Architecture\Projects` | 06-12-26 19:40:24 | 07-12-26 14:08:14 | <ul><li>Never delete through cleanup.</li></ul> | Exists | No |
| `CalradiaForge.Nexus.md` | `\repository-root\docs\Architecture\Projects` | 06-12-26 19:40:24 | 07-12-26 14:08:14 | <ul><li>Never delete through cleanup.</li><li>Nexus architecture is explicitly protected.</li></ul> | Exists | No |
| `CalradiaForge.UI.md` | `\repository-root\docs\Architecture\Projects` | 06-12-26 19:40:24 | 07-12-26 14:08:14 | <ul><li>Never delete through cleanup.</li></ul> | Exists | No |
| `ARCHITECTURE.md` | `\repository-root\docs\Architecture\Solution` | 06-12-26 19:40:24 | 07-12-26 14:08:14 | <ul><li>Never delete through cleanup.</li></ul> | Exists | No |
| `Configuration.md` | `\repository-root\docs\Architecture\Systems` | 06-12-26 19:40:25 | 07-12-26 14:08:14 | <ul><li>Never delete through cleanup.</li></ul> | Exists | No |
| `EULA.md` | `\repository-root\docs\Architecture\Systems` | 06-12-26 19:40:25 | 07-12-26 14:08:14 | <ul><li>Never delete through cleanup.</li></ul> | Exists | No |
| `Launcher.md` | `\repository-root\docs\Architecture\Systems` | 06-12-26 19:40:25 | 07-12-26 14:08:14 | <ul><li>Never delete through cleanup.</li></ul> | Exists | No |
| `Localization.md` | `\repository-root\docs\Architecture\Systems` | 06-12-26 19:40:25 | 07-12-26 14:08:14 | <ul><li>Never delete through cleanup.</li></ul> | Exists | No |
| `Logging.md` | `\repository-root\docs\Architecture\Systems` | 06-12-26 19:40:25 | 07-12-26 17:32:19 | <ul><li>Never delete through cleanup.</li></ul> | Exists | No |
| `ModManagement.md` | `\repository-root\docs\Architecture\Systems` | 06-12-26 19:40:25 | 07-12-26 14:08:14 | <ul><li>Never delete through cleanup.</li></ul> | Exists | No |
| `Modpacks.md` | `\repository-root\docs\Architecture\Systems` | 06-12-26 19:40:25 | 07-12-26 14:08:14 | <ul><li>Never delete through cleanup.</li></ul> | Exists | No |
| `CHANGELOG.md` | `\repository-root\docs` | 06-12-26 18:00:49 | 07-12-26 16:24:47 | <ul><li>Never delete through cleanup.</li><li>Preserve historical release records.</li></ul> | Exists | No |
| `CONTRIBUTIONS.md` | `\repository-root\docs` | 06-12-26 20:22:06 | 07-12-26 14:08:14 | <ul><li>Never delete through cleanup.</li></ul> | Exists | No |
| `EULA.txt` | `\repository-root\docs` | 06-12-26 18:00:49 | 07-12-26 14:19:22 | <ul><li>Never delete through cleanup.</li></ul> | Exists | No |
| `MIGRATION_MAP.md` | `\repository-root\docs` | 07-08-26 23:43:27 | 07-12-26 17:32:19 | <ul><li>Never delete through cleanup.</li><li>Preserve source-change history and migration metadata.</li></ul> | Exists | No |
| `TODO_v1.md` | `\repository-root\docs` | 06-12-26 18:00:49 | 07-12-26 17:37:43 | <ul><li>Never delete through cleanup.</li><li>Retain until a human developer explicitly replaces or removes it.</li></ul> | Exists | No |
| `archive_installer_safety_policy.md` | `\repository-root\docs\refactor` | 07-07-26 20:46:35 | 07-12-26 17:32:19 | <ul><li>Never delete through cleanup.</li><li>Active refactor policy.</li></ul> | Exists | No |
| `dependency_injection_plan.md` | `\repository-root\docs\refactor` | 07-11-26 20:01:27 | 07-12-26 17:32:19 | <ul><li>Never delete through cleanup.</li><li>Active refactor plan.</li></ul> | Exists | No |
| `documentation_alignment_handoff_checklist.md` | `\repository-root\docs\refactor` | 07-08-26 13:42:36 | 07-12-26 17:32:19 | <ul><li>Never delete through cleanup.</li><li>Active refactor checklist.</li></ul> | Exists | No |
| `logging_policy.md` | `\repository-root\docs\refactor` | 07-10-26 21:20:09 | 07-12-26 17:32:19 | <ul><li>Never delete through cleanup.</li><li>Active refactor policy.</li></ul> | Exists | No |
| `mvvm_refactor_plan.md` | `\repository-root\docs\refactor` | 07-07-26 20:46:38 | 07-12-26 17:32:19 | <ul><li>Never delete through cleanup.</li><li>Active refactor plan.</li></ul> | Exists | No |
| `performance_audit_and_optimization_policy.md` | `\repository-root\docs\refactor` | 07-11-26 20:03:08 | 07-12-26 17:32:19 | <ul><li>Never delete through cleanup.</li><li>Active refactor policy.</li></ul> | Exists | No |
| `persistence_policy.md` | `\repository-root\docs\refactor` | 07-07-26 20:46:31 | 07-12-26 17:32:19 | <ul><li>Never delete through cleanup.</li><li>Active refactor policy.</li></ul> | Exists | No |
| `phase_1_cleanup_notes.md` | `\repository-root\docs\refactor` | 07-08-26 17:27:42 | 07-12-26 17:32:19 | <ul><li>Never delete through cleanup.</li><li>Retain the Phase 1 implementation record.</li></ul> | Exists | No |
| `refactor_master_plan.md` | `\repository-root\docs\refactor` | 07-10-26 21:20:36 | 07-12-26 17:32:19 | <ul><li>Never delete through cleanup.</li><li>Canonical refactor plan.</li></ul> | Exists | No |
| `result_and_workflow_policy.md` | `\repository-root\docs\refactor` | 07-07-26 20:46:41 | 07-12-26 17:32:19 | <ul><li>Never delete through cleanup.</li><li>Active refactor policy.</li></ul> | Exists | No |
| `security_and_secret_boundary.md` | `\repository-root\docs\refactor` | 07-07-26 20:46:33 | 07-12-26 17:32:19 | <ul><li>Never delete through cleanup.</li><li>Active refactor security policy.</li></ul> | Exists | No |
| `task_execution_optimization_policy.md` | `\repository-root\docs\refactor` | 07-12-26 14:10:26 | 07-12-26 17:32:26 | <ul><li>Never delete through cleanup.</li><li>Canonical reusable task-execution policy.</li></ul> | Exists | No |
| `testing_strategy.md` | `\repository-root\docs\refactor` | 07-11-26 20:04:09 | 07-12-26 17:32:19 | <ul><li>Never delete through cleanup.</li><li>Active refactor testing policy.</li></ul> | Exists | No |
| `ui_page_rename_review_checklist.md` | `\repository-root\docs\refactor` | 07-08-26 18:32:27 | 07-12-26 17:32:19 | <ul><li>Never delete through cleanup.</li><li>Owner-review checklist.</li></ul> | Exists | No |
| `versioning_policy.md` | `\repository-root\docs\refactor` | 07-07-26 20:46:37 | 07-12-26 17:32:19 | <ul><li>Never delete through cleanup.</li><li>Active versioning policy.</li></ul> | Exists | No |
| `DOCUMENT_SITEMAP.md` | `\repository-root\docs` | 07-12-26 17:42:16 | 07-12-26 17:42:24 | <ul><li>Never delete through cleanup.</li><li>Protected document register.</li></ul> | Exists | No |
| `CalradiaForge.Benchmarks.md` | `\repository-root\docs\Architecture\Projects` | 07-15-26 10:38:39 | 07-16-26 15:17:02 | <ul><li>Never delete through cleanup.</li><li>Architecture project documentation.</li></ul> | Exists | No |
| `CalradiaForge.Tests.md` | `\repository-root\docs\Architecture\Projects` | 07-16-26 15:07:00 | 07-16-26 15:55:41 | <ul><li>Never delete through cleanup.</li><li>Architecture project documentation.</li></ul> | Exists | No |
| `PlatformAndPathDetection.md` | `\repository-root\docs\Architecture\Systems` | 07-16-26 15:07:00 | 07-16-26 16:05:10 | <ul><li>Never delete through cleanup.</li><li>Architecture system documentation.</li></ul> | Exists | No |
| `game_platform_detection_and_path_workflow_plan.md` | `\repository-root\docs\refactor` | 07-16-26 15:07:00 | 07-16-26 16:24:34 | <ul><li>Never delete through cleanup.</li><li>Active refactor plan.</li></ul> | Exists | No |
