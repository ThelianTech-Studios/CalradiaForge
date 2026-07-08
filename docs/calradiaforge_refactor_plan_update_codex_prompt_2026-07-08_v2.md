# Codex Prompt — Update CalradiaForge Refactor Plans With Owner Notes, Steam Workshop Scanner Issue, And Documentation Alignment Report

## Role

You are updating CalradiaForge documentation only.

You are not implementing source-code changes in this task.

## Goal

Update the existing CalradiaForge refactor planning documents so they include three owner-directed planning inputs:

1. A future owner decision checkpoint for UI page `.xaml` rename/refactor planning.
2. A known Steam Workshop scanning bug that must be staged into the phased refactor plan.
3. A documentation-alignment workstream based on `calradiaforge_docs_alignment_report_07-08-26.md`, which recommends aligning CalradiaForge documentation with the OniForge documentation model at the organizational and decision-control level.

The current refactor documents already cover cleanup, logging, safety refactors, tests, dependency injection, result/workflow coordination, MVVM extraction, and version/documentation cleanup. Preserve that structure. Add the new information into the existing phase model rather than replacing the current plan.

## Source Documents To Read First

Read the current refactor planning documents before editing:

- `docs/refactor/refactor_master_plan.md`
- `docs/refactor/mvvm_refactor_plan.md`
- `docs/refactor/dependency_injection_plan.md`
- `docs/refactor/logging_policy.md`
- `docs/refactor/testing_strategy.md`
- `docs/refactor/result_and_workflow_policy.md`
- Any other nearby refactor policy documents that need cross-reference updates.

Also read the documentation alignment report if it exists in the repository or has been supplied with this prompt:

- `calradiaforge_docs_alignment_report_07-08-26.md`
- `docs/reviews/calradiaforge_docs_alignment_report_07-08-26.md`
- Or the equivalent owner-supplied report file.

If the repository uses a different refactor-docs folder path, locate the equivalent folder and preserve the existing naming/style conventions.

## Hard Guardrails

- Do not change application source code.
- Do not rename any files, classes, methods, properties, namespaces, or XAML pages in this task.
- Do not invent a rename map.
- Do not claim the Steam Workshop scanner bug is fixed.
- Do not perform the full documentation restructuring described in the alignment report unless the existing refactor-doc task already explicitly authorizes documentation movement.
- Do not turn refactor plans, audits, or review reports into canonical architecture automatically.
- Do not copy OniForge product-specific content into CalradiaForge docs.
- Do not change Nexus API implementation plans.
- Do not move Nexus networking/auth/download responsibilities out of `CalradiaForge.Nexus`.
- Do not move WPF dependencies into `CalradiaForge.Core`.
- Do not add startup update checks, timed polling, silent background scans, or background Nexus polling.
- Do not remove existing refactor phases unless there is a direct duplicate.
- Keep all new content documentation/planning-only.

## Owner Notes To Add

### 1. Future UI Page Rename / Refactor Checkpoint

The owner wants to rename some current UI page `.xaml` files later as a preparation step before future Nexus API development and Nexus-facing UI work.

However, the exact rename targets are not known yet.

Add this as a deferred owner decision checkpoint, not as an implementation task.

The documentation should make clear:

- Some UI page `.xaml` names may need to be renamed/refactored before or during the staged MVVM work.
- The purpose is to prepare the UI structure for future Nexus API development and reduce confusing page naming before new Nexus UI concepts are added.
- The rename list is intentionally not defined yet.
- Codex or a future work agent should generate a small owner-review document/instruction set at the appropriate time.
- No actual UI page rename should happen until the owner provides an explicit approved rename map.
- XAML renames are risky because they can affect code-behind partial classes, `x:Class`, navigation references, resource dictionaries, bindings, design-time tooling, generated files, and documentation references.

Add a planned checkpoint similar to:

```md
### Deferred Owner Checkpoint — UI Page Rename Review

Before broad MVVM extraction or future Nexus UI work begins, create a short owner-review document that inventories current UI pages, current page names, code-behind names, navigation references, and proposed rename candidates. Do not perform any rename until the owner approves an explicit rename map.
```

Recommended documentation placement:

- `refactor_master_plan.md`
  - Add to Phase 1 as a deferred naming/refactor note.
  - Add to Phase 7 as a pre-MVVM/page-extraction checkpoint.
  - Add to global guardrails: do not rename UI pages without owner-approved rename map.
- `mvvm_refactor_plan.md`
  - Add a checkpoint before page ViewModel extraction.
  - Add verification notes for XAML/code-behind/navigation references if renames are later approved.
- Optional new small planning doc if consistent with repo style:
  - `docs/refactor/ui_page_rename_review_checklist.md`
  - This document should be a checklist/template only, not a completed rename plan.

Suggested checklist contents for the optional new doc:

```md
# UI Page Rename Review Checklist

## Purpose
Prepare an owner-approved rename map for current WPF page `.xaml` files before broad MVVM extraction or future Nexus UI work.

## Do Not Rename Yet
This checklist does not authorize renames. It exists to gather candidates and risks for owner approval.

## Inventory Table
| Current XAML file | Code-behind class | Current navigation key/reference | Proposed name | Reason | Risk | Owner approved? |
|---|---|---|---|---|---|---|

## Review Areas
- `x:Class` names.
- Code-behind partial class names.
- Navigation registration and page construction.
- Resource dictionaries and styles.
- Bindings and commands.
- Tests or smoke-test references.
- Documentation and screenshots.
- Future Nexus UI naming conflicts.

## Approval Rule
No rename may proceed until the owner fills or approves the rename map.
```

### 2. Known Issue — Steam Workshop Mods Not Automatically Scanned

Add a known issue to the refactor plan:

One end user reported that Bannerlord Steam Workshop mods were not automatically scanned. The suspected cause is that the app may be failing to correctly identify the Steam Workshop folder path. The owner is waiting on confirmation about whether that user has Bannerlord installed in a separate Steam library location from the main Steam client install location.

The owner has noted that the app already uses Bannerlord AppID `261550` as part of the Workshop path composition. Therefore, the staged refactor plan should not treat the AppID string as the primary suspected issue. The likely investigation area is Steam library discovery/path resolution.

Document this as a tracked scanner/path-resolution issue.

The documentation should make clear:

- The bug is not confirmed yet, but it is important enough to stage into the refactor plan.
- The issue may involve Steam client install path versus Steam library path versus Bannerlord install path.
- The app should not rely on a single assumed Steam Workshop root if Bannerlord or Workshop content may be stored under a different Steam library root.
- The scanner/path detection should eventually handle multi-library Steam setups.
- Manual override behavior, if present, should remain supported unless explicitly removed later.
- The plan should include diagnostics, tests, adapter boundaries, result/warning surfacing, and UI status improvements.

Recommended phase additions:

#### Phase 1 — Cleanup And Low-Risk Consistency Fixes

Add a planning-only note:

- Record the Steam Workshop scanning bug as a known issue/deferred-risk item.
- Identify current scanner/path-resolution classes and methods without changing behavior.
- Add TODO/reference notes only if that matches repo style.
- Do not implement the fix during general cleanup.

#### Phase 2 — Serilog Migration And Logging Cleanup

Add diagnostics planning:

- Add structured diagnostic events for Steam/Bannerlord path resolution once logging work begins.
- Log detected platform, detected Steam client path if available, discovered Steam library roots, Bannerlord install path, resolved Workshop path candidates, selected Workshop path, and scanner result counts.
- Log reasons why a candidate Workshop path is skipped or missing.
- Redact or sanitize user-specific path segments in shared diagnostic bundles/log exports if path redaction policy requires it.
- Do not log secrets or unrelated personal filesystem data.

#### Phase 3 — Small Safety Refactors

Add scanner stabilization as a targeted safety/bugfix candidate, but keep it staged:

- Investigate Steam Workshop path detection and Bannerlord Workshop mod discovery.
- Do not assume Workshop content is under the main Steam client install folder.
- Prefer resolving Steam library roots first, then checking valid `steamapps/workshop/content/261550` candidates.
- Preserve existing successful behavior for users whose current setup works.
- Keep any fix compact and verifiable.
- If the bug cannot be confirmed yet, keep the work as documented/deferred until end-user setup details are known.

#### Phase 4 — Initial Tests Around Changed Risky Areas

Add tests for Steam path/scanner behavior using fake temp directories only:

- Steam client installed on one root, Bannerlord installed under another Steam library root.
- Multiple Steam library roots.
- Workshop content located under the Bannerlord library root.
- Workshop content missing.
- Workshop content exists but contains no valid modules.
- Manual override path, if supported.
- No test should require a real Steam install or a real Bannerlord install.

#### Phase 5 — DI And Platform Adapter Foundation

Add platform adapter planning:

- Move Steam library discovery, Bannerlord install detection, and Workshop path resolution behind explicit platform/path-resolution abstractions.
- Keep Windows-specific registry/file-system probing behind adapters.
- Make the scanner testable with fake path providers and fake filesystem roots.
- Keep Core free of WPF dependencies.

#### Phase 6 — Result Types And Workflow Coordinator

Add result/warning planning:

- Mod scan/refresh should report structured warnings when Workshop path detection fails, when no Workshop path candidates exist, or when Workshop mods are not found.
- Results should distinguish between “no Workshop mods installed,” “Workshop path could not be resolved,” and “Workshop path exists but scan failed.”
- User-facing messages should be concise; technical details should go to logs.

#### Phase 7 — Staged MVVM Shell/ViewModel Rewrite

Add UI status planning:

- Surface Steam Workshop scan warnings in the relevant page/ViewModel without burying them in logs only.
- The user should be able to understand whether local modules were scanned, Workshop modules were scanned, or Workshop scanning was skipped due to path detection.
- Do not add unrelated Nexus UI in this task.

#### Phase 8 — Final Documentation And Versioning Cleanup

Add release-doc alignment:

- If the scanner bug is fixed before the next beta, mention it in changelog/release notes.
- If it remains deferred, keep it listed as a known issue or tracked follow-up.
- Do not describe the bug as fixed unless implementation and verification happened.

### 3. Documentation Alignment Report Workstream

The owner has completed a documentation alignment report comparing CalradiaForge docs against the OniForge documentation model.

The report's central recommendation is to expand CalradiaForge toward OniForge's documentation discipline, but with a smaller and more targeted scope. The goal is organizational and decision-control alignment, not one-for-one content copying.

Treat the report as a planning input for the refactor documents.

Do not perform the full documentation rebuild in this task unless the existing refactor plan already includes that scope. Instead, update the refactor documents so the phased refactor work coordinates with the documentation alignment work.

The refactor documents should capture these report conclusions:

- CalradiaForge docs should adopt stronger documentation organization, read order, project scope, architecture, data/persistence, application-system, UI, development, testing, security, release/versioning, research, ADR, and review-artifact coverage.
- Current `docs/refactor/**` material should remain active planning/staging material.
- Refactor plans should not become canonical architecture automatically.
- Audits and alignment reports should be treated as review artifacts, preferably under `docs/reviews/`.
- Stable decisions produced by refactor phases should later be migrated into canonical docs, ADRs, changelog entries, or release notes.
- Architecture docs should describe how systems must work; roadmap docs should describe when systems belong in v1.0, post-v1.0, or deferred work.
- Continue preserving the distinction between `Currently Implemented`, `Planned / Locked Architecture`, and `Deferred / Future Work` where existing docs already use that convention.
- Scanner/path documentation should be created or updated before scanner work expands, especially for Steam Workshop and multi-library path detection.

Recommended documentation placement:

- `refactor_master_plan.md`
  - Add the documentation alignment report as a tracked owner input.
  - Add a note that the refactor plan must remain staged/planning-only until accepted decisions are migrated into canonical docs.
  - Add Phase 8 documentation-alignment tasks that hand off accepted refactor decisions into the future numbered docs package, ADRs, changelog, and release notes.
- `testing_strategy.md`
  - Cross-reference the report's requirement for a real testing/QA strategy.
  - Ensure scanner/path, installer/archive, persistence, modpack, Nexus metadata, logging/redaction, and UI/ViewModel behavior are represented as test categories where relevant.
- `dependency_injection_plan.md`
  - Cross-reference future platform/path-detection documentation and adapter boundaries.
- `logging_policy.md`
  - Cross-reference future security/trust-boundary docs and path-redaction expectations.
- `mvvm_refactor_plan.md`
  - Cross-reference future UI/UX, navigation/page model, MVVM command policy, status/error/progress presentation, and accessibility docs.
- `result_and_workflow_policy.md`
  - Cross-reference future application-system docs and structured result/warning documentation.

Do not create the entire proposed folder tree from the report in this refactor-plan update unless explicitly authorized by the repository's current instructions. It is acceptable to add a small planning note or checklist that says this documentation alignment work should be handled in a separate documentation-rebuild task.

Suggested wording for `refactor_master_plan.md`:

```md
### Documentation Alignment Report Integration

The owner has produced `calradiaforge_docs_alignment_report_07-08-26.md`, which recommends aligning CalradiaForge documentation with the OniForge documentation model at the organizational and decision-control level. Refactor plans remain active planning artifacts and must not become canonical architecture automatically. As refactor phases produce accepted decisions, only stable decisions should be migrated into canonical architecture docs, application-system docs, data/persistence docs, development/testing docs, ADRs, changelog entries, or release notes.
```

Suggested Phase 8 addition:

```md
#### Documentation Alignment Handoff

At the end of each refactor phase, identify which decisions are now accepted behavior and need migration into the future CalradiaForge documentation structure recommended by `calradiaforge_docs_alignment_report_07-08-26.md`. Do not copy full planning text into canonical docs. Convert final decisions into concise architecture, system, data/persistence, testing, ADR, changelog, or release-note updates.
```

Optional small document if consistent with repo style:

- `docs/refactor/documentation_alignment_handoff_checklist.md`

Suggested contents:

```md
# Documentation Alignment Handoff Checklist

## Purpose
Track which accepted refactor decisions need to be migrated into canonical CalradiaForge documentation after each refactor phase.

## Planning Artifact Rule
Refactor documents are active planning artifacts. They are not canonical architecture until accepted decisions are migrated into stable docs or ADRs.

## Handoff Table
| Refactor phase | Accepted decision | Target canonical doc/ADR/release note | Migration status | Owner review needed? |
|---|---|---|---|---|

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
```

## Suggested New Section For `refactor_master_plan.md`

Add a concise section near locked decisions, risk notes, or phase overview:

```md
## Tracked Owner Additions

### Deferred UI Page Rename Review

Some current WPF page `.xaml` names may need to be renamed before broad MVVM extraction or future Nexus UI/API work. The exact rename targets are not approved yet. Before any page rename occurs, create an owner-review checklist that inventories current page names, code-behind classes, navigation references, proposed names, reasons, risks, and owner approval status. Do not rename UI pages without an explicit owner-approved rename map.

### Known Issue — Steam Workshop Mods Not Automatically Scanned

An end user reported that Bannerlord Steam Workshop mods were not automatically discovered. The suspected area is Steam Workshop path resolution, especially setups where Bannerlord may be installed in a different Steam library from the main Steam client install. The app already uses Bannerlord AppID `261550`, so the staged investigation should focus on Steam library discovery, Workshop path candidate resolution, diagnostics, tests, result warnings, and UI surfacing. Do not mark this issue fixed until implementation and verification are complete.

### Documentation Alignment Report

The owner has produced `calradiaforge_docs_alignment_report_07-08-26.md`, which recommends aligning CalradiaForge documentation with the OniForge documentation model at the organizational and decision-control level. Refactor documents must remain active planning artifacts. As refactor phases produce accepted decisions, migrate only stable decisions into canonical docs, ADRs, changelog entries, or release notes. Do not copy speculative planning text into canonical architecture.
```

## Required Output From Codex

After editing, provide a concise summary with:

1. Files changed.
2. Sections added or updated.
3. Confirmation that no source code was changed.
4. Confirmation that no rename map was invented.
5. Confirmation that the Steam Workshop scanner issue was staged across the relevant phases.
6. Confirmation that the documentation alignment report was integrated as a planning input.
7. Confirmation that refactor docs were kept separate from canonical architecture.
8. Any follow-up questions for the owner, especially:
   - Which UI page `.xaml` files should be considered for rename later?
   - Should the rename review happen during Phase 1 cleanup or immediately before Phase 7 MVVM extraction?
   - Should Workshop scanning check all Steam libraries by default or prefer the Bannerlord install library first?
   - Should a manual Workshop path override be exposed or preserved in Settings?
   - Should the documentation alignment report be moved into `docs/reviews/` in a separate documentation-rebuild task?
   - Should the full numbered documentation structure from the report be created before, during, or after the active refactor phases?
