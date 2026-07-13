# CalradiaForge Refactor Documentation Deep Audit

Status: Complete
Audit date: 2026-07-12
Audit commit: `1381dda6c30d159e0101699b729c10b4ece30f47` plus working-tree changes; no commit was created
Audit branch: `dev-V0-14-CodeRefactor`
Audit scope: Source-alignment, redaction-removal, AppConfig credential-boundary, refactor-document ownership, phase integrity, references, and verification across the requested CalradiaForge package
Report owner: Codex

## Owner Decision

Automatic secret redaction, secret sanitization, path sanitization, secret-pattern filtering, generic secret-like key blocking, redaction-specific tests and gates, and redaction-performance requirements are removed from the current implementation and future refactor plans.

The logger now renders event values without automatic filtering. Logging callers and credential-owning components remain responsible for not intentionally passing credentials or authentication material to logs. `AppConfig` remains ordinary JSON settings persistence rather than a credential manager; future Nexus credentials remain owned by Nexus components, stored through DPAPI CurrentUser, and decrypted only during explicit operation scope.

## Executive Summary

The audit found the former Serilog redaction implementation active in source and propagated as an active requirement through the refactor plan, logging/security/testing/performance policies, result/MVVM guidance, and canonical logging architecture. The bounded source change removed `LogRedactor`, replaced `RedactingTextFormatter` with the neutral `SerilogTextFormatter`, and updated the factory wiring. Core builds successfully after the change.

The audit also confirmed that AppConfig secret-like key blocking was never implemented; only its planned documentation existed. Those requirements were removed without changing AppConfig persistence. Phase 2 history remains recorded as historical, while its current end state now describes neutral formatting and preserved legacy logger compatibility. Supporting local “Phase” sequences were renamed to “Step” or “Local Implementation Steps” where they were not master-plan phases. A stale UI inventory path, a Phase 1 versioning assignment, duplicate SevenZipWrapper question, and missing document references were corrected or explicitly recorded.

The full solution build restored packages after network escalation but failed on nine pre-existing UI references to missing `App.AppConfig` members. The Core-only build passed. `dotnet test source/CalradiaForge.slnx --no-restore` exited successfully; no test project is checked in and no tests were executed.

## Files Reviewed

### Refactor Documents

Reviewed every current file under `docs/refactor`:

- `archive_installer_safety_policy.md` — supporting safety policy; local implementation steps.
- `dependency_injection_plan.md` — DI/lifecycle plan; Phase 5.A/5.B ownership.
- `documentation_alignment_handoff_checklist.md` — master-phase handoff checklist.
- `logging_policy.md` — logging and neutral formatter policy.
- `mvvm_refactor_plan.md` — MVVM plan; local implementation steps.
- `performance_audit_and_optimization_policy.md` — benchmark/performance ownership and report workflow.
- `persistence_policy.md` — persistence ownership and safe-write plan; local implementation steps.
- `phase_1_cleanup_notes.md` — historical Phase 1 record and deferred risks.
- `refactor_master_plan.md` — canonical refactor phase plan.
- `result_and_workflow_policy.md` — workflow/result policy; local implementation steps.
- `security_and_secret_boundary.md` — credential ownership and AppConfig boundary.
- `task_execution_optimization_policy.md` — extracted canonical task-execution policy; pre-existing uncommitted user change preserved.
- `testing_strategy.md` — correctness-test ownership and verification strategy.
- `ui_page_rename_review_checklist.md` — owner-approval checklist and current inventory.
- `versioning_policy.md` — version source and release policy; local implementation steps.

The user-provided `docs/Codex_Task_Execution_Policy_Documentation_Update_Prompt.md` and untracked `docs/refactor_07-12-26.zip` were also observed and preserved.

### Architecture Documents

Reviewed `docs/Architecture/ARCHITECTURE_INDEX.md`, solution/project architecture documents, Nexus integration architecture, configuration, launcher, logging, mod-management, modpacks, localization, EULA, and related system documents. Directly affected documents were updated in `docs/Architecture/Systems/Logging.md` and `docs/Architecture/Nexus/NEXUS_INTEGRATION_ARCHITECTURE.md`.

### Source Files

- `source/CalradiaForge.Core/Infra/Logging/Logger.cs`
- `source/CalradiaForge.Core/Infra/Logging/SerilogLoggerFactory.cs`
- `source/CalradiaForge.Core/Infra/Logging/SerilogTextFormatter.cs`
- Former `source/CalradiaForge.Core/Infra/Logging/RedactingTextFormatter.cs` and `LogRedactor.cs`
- `source/CalradiaForge.Core/Infra/Logging/LogRetentionPolicy.cs`
- `source/CalradiaForge.Core/Infra/Config/AppConfig.cs`
- `source/CalradiaForge.Core/Infra/Config/AppConfigSettings.cs`
- `source/CalradiaForge.UI/App.xaml`, `App.xaml.cs`, and current page/navigation files
- Core/UI project files and `source/CalradiaForge.slnx`

### Tests And Benchmarks

The solution contains no checked-in test or benchmark project and no xUnit, NUnit, MSTest, BenchmarkDotNet, or equivalent project reference. The audit reviewed planned test/benchmark requirements in `testing_strategy.md` and `performance_audit_and_optimization_policy.md` and found no redaction-specific source fixture to remove.

### Audits, Reviews, And Workflow Documents

Reviewed the older audit and alignment reports, `docs/MIGRATION_MAP.md`, `docs/CHANGELOG.md` as historical context, the extracted task-execution prompt/policy, and all logging-related paths. The named `docs/Serilog_Logger_Follow_Up_Summary_2026-07-12.md` and `source/CalradiaForge.Core/Infra/Logging/SERILOG_WORKFLOW_GUIDE.md` are not present in the current package; references were corrected or classified as missing.

## Source-Grounded Current State

Verified against the working tree on 2026-07-12, based on commit `1381dda6c30d159e0101699b729c10b4ece30f47` plus the task changes:

- `Logger.Instance` remains the live WPF/Core logging boundary. `App.xaml` still uses `StartupUri`; `SerilogLoggerFactory` is not initialized by WPF startup.
- `SerilogLoggerFactory.Create()` remains non-retaining and uses the current active log path, asynchronous file sink, Debug-only sink, thread enrichment, exception enrichment, and custom cleanup. Lifecycle ownership remains Phase 5.A work.
- `SerilogTextFormatter` is an `ITextFormatter` that renders timestamp, level, message, applicable `SourceContext`, applicable `ThreadId`, other properties, and exception information without rewriting values.
- `AppConfig` accepts generic string keys and persists them; no secret-like key blocklist, heuristic scanner, or generic rejection behavior is implemented. `AppConfigSettings` seeds ordinary typed settings.
- No checked-in tests or benchmarks exist.
- Current project versions are `0.12.15` in UI and Core; `Directory.Build.props` is absent. The latest changelog section is `0.13.14`. Version drift remains a deferred master Phase 11 task.
- The current UI page inventory places `ModsPage.xaml` under `source/CalradiaForge.UI/Pages/` and the class is `CalradiaForge.UI.Pages.ModsPage`.
- Steam Workshop path resolution remains an unresolved known issue; no behavior change was made.

## Redaction And Sanitization Removal

### Source Implementation Removed

- Deleted `source/CalradiaForge.Core/Infra/Logging/LogRedactor.cs`, including generated regexes, secret-like key fragments, recursive property rewriting, and replacement markers.
- Deleted `source/CalradiaForge.Core/Infra/Logging/RedactingTextFormatter.cs`.
- Updated `SerilogLoggerFactory` to instantiate `SerilogTextFormatter`.

### Planned Implementation Removed

Removed active requirements for central redaction, path masking/sanitization, AppConfig secret-like key rejection, synthetic-secret redaction tests, redactor-removal gates, redactor cost measurements, and redacted profiler/export/diagnostic requirements from the active refactor policies.

### Neutral Formatter Preserved

`source/CalradiaForge.Core/Infra/Logging/SerilogTextFormatter.cs` retains the custom formatter concept and useful log structure. It is presentation-only, readable, and connected to the existing File and Debug sink configuration. It contains no regular expressions, pattern lists, redactor calls, property-name replacement, path masking, or sanitization logic.

### AppConfig Changes

No AppConfig source change was required. Source inspection proved that generic secret-like key blocking was planned only. Documentation now states that AppConfig is ordinary settings persistence and not the intended Nexus credential manager, without introducing a replacement scanner or allow/deny list.

### Credential Boundaries Preserved

Nexus credential ownership, DPAPI CurrentUser storage, operation-scoped decryption, no credential storage in AppConfig, no credential storage in `ModuleModel` or ordinary Nexus metadata, and manual/auth-gated Nexus behavior remain intact. Credential-owning components must not intentionally pass credentials or authentication material to logs.

## Findings Summary

| ID | Finding | Classification | Severity | Affected documents | Resolution |
|---|---|---|---|---|---|
| RFDOC-001 | Former owner decision remained active in logging/security policies. | Superseded owner decision | High | Master plan, logging, security, architecture | Corrected across active docs; history retained. |
| RFDOC-002 | Phase 2 history mixed completed infrastructure with current redaction claims. | Historical fact incorrectly presented as current | High | Master plan, handoff, logging | Corrected; neutral formatter and superseded history are explicit. |
| RFDOC-003 | AppConfig secret-like key blocking was planned but not implemented. | Conflicting requirement | High | Master plan, persistence, security | Removed active blocklist requirements; source unchanged. |
| RFDOC-004 | Phase 5.B and Phases 8–10 retained redactor gates and costs. | Superseded requirement | High | Master plan, DI, testing, performance | Removed; formatter/lifecycle verification preserved. |
| RFDOC-005 | Redaction/path requirements propagated through workflow and MVVM docs. | Conflicting requirement | High | Result, MVVM, Phase 1, handoff | Corrected to caller responsibility and local diagnostics. |
| RFDOC-006 | Supporting docs used ambiguous local Phase numbering. | Ambiguous phase ownership | Medium | Archive, persistence, security, result, MVVM, versioning | Renamed to local Steps where applicable. |
| RFDOC-007 | Phase 1 assigned version source-of-truth work to Phase 8. | Stale source/planning observation | Medium | Phase 1 notes | Corrected to master Phase 11. |
| RFDOC-008 | UI rename inventory contained stale `ModsPage` path/class text. | Stale source observation | Medium | UI rename checklist | Corrected; rename remains owner-gated. |
| RFDOC-009 | SevenZipWrapper unresolved question was duplicated. | Duplicate open question | Medium | Master plan | Collapsed to one entry. |
| RFDOC-010 | Named logger follow-up/workflow files were missing. | Broken or missing reference | High | Master plan, logging, performance | References corrected or explicitly marked not found. |
| RFDOC-011 | Migration-map references used a missing `docs/refactor/migration_map.md` path. | Broken reference | Medium | Master plan | Corrected to existing `docs/MIGRATION_MAP.md`; file was not otherwise updated. |
| RFDOC-012 | Historical audits still read like active recommendations. | Historical fact incorrectly presented as current | Medium | Older audit reports and migration map | Historical notes added; changelog was intentionally untouched. |
| RFDOC-013 | Audit/review folder and date conventions remain inconsistent. | Deferred documentation-structure decision | Medium | Older alignment reports and master plan | Reported/deferred; no new `docs/reviews` folder created. |
| RFDOC-014 | Testing and performance policies duplicated benchmark ownership. | Redundant content better owned by another document | Medium | Testing and performance policies | Ownership boundary clarified; full benchmark rewrite deferred. |

## Detailed Findings

### RFDOC-001 — Automatic redaction remained an active owner requirement

- Classification: Superseded owner decision.
- Severity: High.
- Source evidence: Former `LogRedactor` and `RedactingTextFormatter` source; current neutral formatter and factory wiring.
- Affected files: `docs/refactor/refactor_master_plan.md`, `logging_policy.md`, `security_and_secret_boundary.md`, `docs/Architecture/Systems/Logging.md`.
- Current state: Automatic value filtering has been removed from source.
- Problem: Active documents required the removed behavior.
- Scope impact: Logging policy and future phase gates were inconsistent with the requested owner decision.
- Resolution: Replaced automatic filtering with caller responsibility and neutral formatter language.
- Remaining action: None for this bounded decision.
- Owner decision required: Separate policy approval only if a future export/telemetry feature is designed.
- Verification: Repository searches show no active redactor type or factory reference.

### RFDOC-002 — Phase 2 history and current state were conflated

- Classification: Historical fact incorrectly presented as current.
- Severity: High.
- Source evidence: Phase 2 created the former redaction infrastructure, but the current source retains only neutral formatting and legacy caller compatibility.
- Affected files: Master plan, logging policy, handoff checklist, architecture logging document.
- Resolution: Phase 2 history remains recorded; its end state now states Serilog infrastructure remains, the formatter is neutral, redaction is removed, legacy logger/callers remain, and lifecycle ownership is deferred to Phase 5.A.
- Verification: Phase overview, deliverables, Phase 2 table, and handoff row agree.

### RFDOC-003 — AppConfig blocklist was documentation-only

- Classification: Conflicting requirement.
- Severity: High.
- Source evidence: `AppConfig` indexer and `Load()` accept arbitrary string keys; no blocklist exists. `AppConfigSettings` contains ordinary settings only.
- Affected files: Master plan, persistence policy, security boundary.
- Resolution: Removed key-name heuristics and retained the architectural boundary that Nexus credentials do not belong in AppConfig.
- Verification: Source search found no generic AppConfig secret scanner or blocklist.

### RFDOC-004 — Redaction-specific tests and performance gates were stale

- Classification: Superseded requirement.
- Severity: High.
- Source evidence: No test or benchmark project exists; all affected requirements were planned documentation.
- Affected files: DI, testing, performance, master plan.
- Resolution: Removed synthetic-secret redaction coverage and redactor cost/gate language. Preserved formatter output, minimum-level, sink, cleanup, close, archive, disposal, and caller credential-boundary verification.
- Verification: No test/benchmark source references `LogRedactor` or `RedactingTextFormatter`.

### RFDOC-005 — Diagnostic path guidance contradicted the owner decision

- Classification: Conflicting requirement.
- Severity: High.
- Source evidence: Current path logging callers include game and Workshop paths; no path redactor exists outside the removed generic redactor.
- Affected files: Logging, result/workflow, MVVM, Phase 1, architecture docs.
- Resolution: Relevant local paths may be logged; users control sharing. Future export/telemetry requires a separate data-handling policy.
- Verification: Active path guidance no longer requires masking or sanitization.

### RFDOC-006 — Supporting local steps were mistaken for master phases

- Classification: Ambiguous phase ownership.
- Severity: Medium.
- Resolution: Renamed local tables/headings to `Step` or `Local Implementation Steps` in archive, persistence, security, result/workflow, MVVM, and versioning documents.
- Verification: The handoff checklist and master plan retain master-phase terminology.

### RFDOC-007 — Phase 1 version assignment was stale

- Classification: Stale planning observation.
- Severity: Medium.
- Resolution: Phase 1 notes now defer version source-of-truth work to master Phase 11.
- Verification: Master Phase 11 owns final versioning and documentation cleanup.

### RFDOC-008 — UI inventory path/class was stale

- Classification: Stale source observation.
- Severity: Medium.
- Resolution: Corrected `ModsPage.xaml` to `source/CalradiaForge.UI/Pages/ModsPage.xaml` and class to `CalradiaForge.UI.Pages.ModsPage`.
- Verification: Paths match current files.

### RFDOC-009 — SevenZipWrapper question was duplicated

- Classification: Duplicate open question.
- Severity: Medium.
- Resolution: Removed the duplicate master-plan entry; methodology remains open once.

### RFDOC-010 — Missing logger follow-up documents

- Classification: Broken or missing reference.
- Severity: High.
- Resolution: The master plan and performance policy now direct agents to inspect current logger source; logging policy records both named files as not found in this package.
- Remaining action: Create those guides only in a separately scoped documentation task if desired.

### RFDOC-011 — Migration-map path drift

- Classification: Broken reference.
- Severity: Medium.
- Resolution: References now point to existing `docs/MIGRATION_MAP.md`. The migration map itself was not updated as requested.

### RFDOC-012 — Historical audit recommendations needed classification

- Classification: Historical fact incorrectly presented as current.
- Severity: Medium.
- Resolution: Added historical notes to the 2026-07-07 codebase audit, 2026-07-08 alignment report, former documentation-generation prompt, and migration map. `docs/CHANGELOG.md` was left unchanged by explicit scope.

### RFDOC-013 — Audit/review structure remains a deferred decision

- Classification: Deferred documentation-structure decision.
- Severity: Medium.
- Resolution: Recorded the folder/date drift in this report; did not create `docs/reviews` or rename historical artifacts.
- Owner decision required: Whether a future documentation-rebuild task should establish `docs/reviews` and normalize historical filenames.

### RFDOC-014 — Testing/performance ownership overlap

- Classification: Redundant content better owned by another document.
- Severity: Medium.
- Resolution: Testing now owns correctness and formatter/lifecycle checks; performance owns measurement methodology and neutral formatter cost. A larger deduplication pass remains optional.

## Cross-Document Decision Matrix

| Decision or claim | Source evidence | Documents containing it | Classification | Required action |
|---|---|---|---|---|
| Logger performs automatic secret redaction | Former redactor source; neutral formatter now current | Logging, master, architecture, security | Superseded owner decision | Remove active requirement; preserve history. |
| Caller must not intentionally log credentials | Legacy logger and AppConfig debug call sites | Security, logging, Nexus architecture, master | Current and consistent | Preserve and verify caller ownership. |
| Relevant local paths may appear in local logs | `GamePathsHelper`, `AppConfig`, logger paths | Logging, result, MVVM, architecture | Current and consistent | Do not add masking requirement. |
| AppConfig is not a credential manager | AppConfig source; Nexus architecture | Security, persistence, master, Nexus | Current and consistent | Preserve without key heuristics. |
| AppConfig rejects secret-like keys | No source evidence | Security, persistence, master, old audits | Superseded/planned-only | Remove active requirement; mark historical. |
| Neutral formatter remains connected to sinks | `SerilogLoggerFactory` and `SerilogTextFormatter` | Logging architecture/policy | Current and consistent | Preserve. |
| Legacy Logger remains live | `Logger.Instance`, App startup | Master, logging, handoff | Current and consistent | Preserve until Phase 5.B. |
| Phase 5.A owns lifecycle | Factory and startup source | Master, DI, logging | Current but planned | Keep as later lifecycle work. |
| Testing owns correctness; performance owns measurement | No test project; duplicated policies | Testing/performance | Current but duplicated | Clarify ownership; defer broad dedupe. |
| UI rename requires owner map | Current XAML/navigation inventory | Master, MVVM, checklist | Current and consistent | Keep deferred. |

## Master-Plan Phase And Dependency Review

- Phase 1 remains completed/deferred as recorded; version source-of-truth work is correctly assigned to master Phase 11.
- Phase 2 remains completed as infrastructure history. Its current end state is neutral formatting, approved package usage, retention helper presence, and legacy compatibility; it does not claim startup or lifecycle ownership.
- Phase 3 no longer includes AppConfig key blocking.
- Phase 5.A retains DI, singleton identity, cleanup-before-open, quiescence, exact-once close, handle release, archive, and disposal ownership.
- Phase 5.B retains staged legacy caller migration and caller credential-boundary review without redactor-removal gates.
- Phases 8–10 retain neutral formatter, logger lifecycle, and logging-performance verification without redactor cost or preservation requirements.
- Phase 11 remains the owner for versioning and final documentation cleanup.
- The migration-map policy references the actual `docs/MIGRATION_MAP.md` path and was not executed in this task.

## Completed-History Versus Planned-Work Review

Phase 2’s former redaction infrastructure is historical implementation evidence. It is not described as current behavior in active policy. `docs/CHANGELOG.md` and the detailed migration-map entries remain unchanged historical records; they are not active requirements. The current source and active docs describe the neutral formatter and preserve the legacy logger/caller boundary.

No future Nexus authentication, DI composition, logger lifecycle ownership, UI rename, Steam Workshop fix, version-source consolidation, test project, benchmark project, or performance optimization is described as implemented.

## Scope-Creep Findings

- No unrelated refactor phase was implemented.
- No Nexus authentication, telemetry, export feature, DI conversion, UI rename, Steam Workshop behavior change, version bump, changelog update, or migration-map update was performed.
- Existing legitimate filename sanitization in `ModpackFileHelper` was preserved; it is not secret/path-log sanitization.
- Existing archive containment and persistence safety requirements were preserved.

## Stale Source Observations

- The UI/Core version values remain duplicated at `0.12.15`; `Directory.Build.props` is absent and the changelog is at `0.13.14`. Deferred to Phase 11.
- The named logger follow-up and workflow-guide files are missing in this package.
- The live WPF startup still uses `StartupUri` and `AppSettingsInstance`; the future Serilog factory is not startup-wired.
- `AppConfig` debug logging still supplies raw keys, values, and paths to the legacy logger; caller discipline is now explicit and no automatic safety is claimed.
- The Steam Workshop scanner/path-resolution issue remains unresolved.

## Duplicate And Conflicting Requirements

Corrected conflicts included central redaction versus caller responsibility, AppConfig generic key blocking versus source behavior, redacted paths versus diagnostic path visibility, redactor-removal approval versus the owner’s already-made decision, and redactor performance ownership versus neutral formatter measurement. The testing/performance benchmark overlap was clarified but not fully rewritten.

## Supporting-Document Ownership And Phase-Number Review

Local implementation sequences now use `Step` or `Local Implementation Steps` in archive safety, persistence, security, result/workflow, MVVM, and versioning policies. Master-phase references remain in the master plan and handoff checklist. The extracted task-execution policy is a separate canonical supporting document and the master plan contains only its lightweight reference.

## Missing, Broken, Or Obsolete References

- Corrected missing `docs/refactor/migration_map.md` references to `docs/MIGRATION_MAP.md`.
- Corrected or explicitly recorded missing `docs/Serilog_Logger_Follow_Up_Summary_2026-07-12.md` and `SERILOG_WORKFLOW_GUIDE.md` references.
- Historical alignment-report references to absent `docs/reviews`, `docs/README.md`, and the old `docs/audit_07-07-26` layout remain historical structure findings and were not expanded into a separate documentation-rebuild task.

## Documentation Structure And Location Findings

Older files use `07-07-26`/`07-08-26` filenames while the current report uses `YYYY-MM-DD`. The report and current master plan retain both historical evidence and current conventions. Normalizing historical folders or filenames requires owner approval and is deferred.

## Changes Applied

- Removed `LogRedactor.cs` and `RedactingTextFormatter.cs`.
- Added `SerilogTextFormatter.cs` and rewired `SerilogLoggerFactory`.
- Updated all directly affected active refactor policies and architecture docs.
- Removed AppConfig blocklist planning without changing AppConfig source.
- Corrected local step numbering, UI inventory path/class, Phase 1 versioning ownership, duplicate SevenZipWrapper question, and migration-map references.
- Added historical/superseded notes to affected older audits and migration records.
- Preserved the user’s uncommitted task-execution policy prompt, standalone policy, and archive without overwriting or reverting them.
- Did not update `docs/CHANGELOG.md` or the migration entries in `docs/MIGRATION_MAP.md`; its header received only a historical-status note.

## Deferred Findings

- Full solution UI compile errors caused by pre-existing `App.AppConfig` references versus current `AppSettingsInstance` naming.
- No checked-in test or benchmark project.
- Logger lifecycle ownership, retention count-versus-age semantics, active-file size/rolling policy, and archive contract.
- Version source consolidation and changelog/version alignment.
- Historical audit/review folder and filename normalization.
- Optional full deduplication between testing and performance benchmark policies.
- Creation or restoration of the missing logger follow-up/workflow documents.

## Owner Decisions Still Required

- Whether to establish `docs/reviews` and normalize historical audit filenames in a separate documentation task.
- Whether to create the missing logger follow-up and workflow guide documents.
- Future log-export, telemetry, or shared-diagnostic data-handling policy, if such a feature is designed.
- Phase 5.A logger ownership/disposal and retention/file-size decisions already tracked in the master plan.

## Verification Results

### Repository Searches

- No source type named `LogRedactor` remains.
- No source type named `RedactingTextFormatter` remains.
- `SerilogTextFormatter` is connected to the File and Debug sinks through `SerilogLoggerFactory`.
- No generic AppConfig secret-like blocklist exists in source.
- No active refactor requirement now mandates automatic redaction or sanitization; remaining uses are negative statements, historical notes, or unrelated filename sanitization.
- No checked-in test or benchmark requires automatic redaction.

### Build

- `dotnet build source/CalradiaForge.Core/CalradiaForge.Core.csproj --no-restore`: passed, one existing SevenZipWrapper architecture warning.
- `dotnet build source/CalradiaForge.slnx`: restore succeeded after network escalation, then failed with 9 pre-existing UI compile errors because `SettingsPage.xaml.cs` and `ModsPage.xaml.cs` reference missing `App.AppConfig` while `App.xaml.cs` exposes `AppSettingsInstance`; 347 existing warnings were also reported.

### Tests

- `dotnet test source/CalradiaForge.slnx --no-restore`: exited successfully with no test execution output; no test project is present.

### Markdown And Link Review

- Changed Markdown files were searched for removed type names, old active redaction requirements, missing named logger documents, task-policy references, local phase ambiguity, and known path drift.
- The directly affected current references resolve to existing files; intentionally historical missing-folder references remain documented as deferred findings.

### Reviewer Pass

An independent reviewer pass was completed after the implementation changes. It found one report wording mismatch: `docs/MIGRATION_MAP.md` received a historical-status header note, although its migration entries were not changed. The report was corrected to state that distinction. The reviewer found no remaining required correction. Deferred items confirmed by review are the missing logger documents, historical folder/date normalization, existing full-solution UI compile failures, absent test/benchmark projects, broader benchmark-policy deduplication, Phase 5.A lifecycle decisions, and Phase 11 version consolidation.

### Diff Cross-Check

The final diff was cross-checked against the source-boundary and docs-only scope. The only production source changes are the bounded formatter rename/removal and factory reference update. No changelog, migration-map entries, version, DI, Nexus, installer, or UI behavior change was introduced; the migration-map header was classified as historical.

## Residual Risks

- Legacy `Logger.Instance` and AppConfig debug payloads can still receive caller-supplied sensitive values; the owner decision intentionally places responsibility on callers and credential-owning components.
- The full solution remains non-green for pre-existing UI naming errors.
- No automated regression suite exists yet for formatter output, logger lifecycle, or AppConfig persistence.
- Users may expose local filesystem paths by sharing logs; future export/telemetry policy is intentionally not invented here.

## Final Assessment

The bounded redaction-removal and refactor-documentation deep audit is complete. The active implementation and planning documents now agree that the logger is not a security or privacy filter, the custom formatter is presentation-only, relevant local paths may remain visible in local logs, AppConfig has no generic secret-key blocklist, and Nexus credential ownership boundaries remain intact. Remaining issues are explicitly historical, deferred, pre-existing, or owner-decision items rather than silently left as active requirements.
