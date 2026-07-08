---
Project: CalradiaForge
Document Type: Documentation Alignment Report
Prepared: 2026-07-08
Prepared For: ThelianTech / Thelian Technologies LLC
Source Inputs:
  - docs.zip
  - OniForge_Docs.zip
Status: Review Draft
---

# CalradiaForge Documentation Alignment Report

## 1. Purpose

This report reviews the supplied CalradiaForge documentation package against the supplied OniForge documentation package as a style, structure, and coverage reference.

The goal is **not** to copy OniForge documentation one-for-one. The goal is to determine whether CalradiaForge benefits from the same documentation discipline, where expansion is justified, and what a practical expansion plan should look like.

## 2. Scope and Exclusions

### Reviewed as CalradiaForge source material

The CalradiaForge review used the non-refactor documentation in `docs.zip`, including:

- `docs/Architecture/**`
- `docs/CHANGELOG.md`
- `docs/CONTRIBUTIONS.md`
- `docs/EULA.txt`
- `docs/Nexus_Integration_Plan.md`
- `docs/TODO_v1.md`
- `docs/audit_07-07-26/calradiaforge_codebase_audit_07-07-26.md`

The codebase audit was treated as a **review artifact** that can identify documentation gaps. It was not treated as canonical architecture by itself.

### Explicitly excluded

The following CalradiaForge files were excluded from the canonical documentation assessment because they are active refactor planning material or refactor-generation material:

- `docs/refactor/**`
- `docs/audit_07-07-26/refactor_documentation_instructions.md`

These files may remain useful for future refactor work, but they should not be used as stable source-of-truth documentation until decisions are locked and migrated into canonical docs, ADRs, or release notes.

### OniForge usage boundary

`OniForge_Docs.zip` was used only as a documentation model for:

- folder organization,
- document depth,
- decision capture,
- planning discipline,
- ADR usage,
- review artifact handling,
- development/release documentation patterns.

OniForge product-specific content, ONI data pipeline documents, catalog package documents, and Klei/legal-specific research documents should not be copied directly into CalradiaForge.

## 3. Bottom-Line Assessment

CalradiaForge **should be expanded toward the OniForge documentation model**, but with a smaller and more targeted scope.

The current CalradiaForge docs are useful and already establish the basic architecture skeleton. They correctly identify the layered WPF structure, Core/UI/Nexus boundaries, mod management, modpacks, launcher behavior, configuration, logging, localization, and EULA handling.

However, the current docs are too thin to serve as a durable development-control package for a project that is approaching larger refactors, Nexus integration, scanner/path fixes, installer hardening, and public release readiness.

The recommended approach is:

1. **Do not perform a full OniForge-style expansion blindly.**
2. **Adopt OniForge’s structure and decision discipline.**
3. **Create CalradiaForge-specific docs for project scope, architecture, data/persistence, application systems, UI, development workflow, testing, security, release/versioning, research, ADRs, and review artifacts.**
4. **Keep active refactor plans separate until their decisions become accepted architecture.**

## 4. Current CalradiaForge Documentation State

### 4.1 Strengths

| Area | Current State | Assessment |
|---|---|---|
| Architecture skeleton | `docs/Architecture/Solution/ARCHITECTURE.md` defines the layered WPF application and dependency direction. | Good foundation. Needs more depth and separation. |
| Project boundary docs | `docs/Architecture/Projects/*.md` define UI, Core, Nexus, and ConsoleUtils responsibilities. | Useful and mostly clean. Needs stronger dependency and ownership rules. |
| System docs | Configuration, logging, localization, mod management, modpacks, launcher, and EULA have short system documents. | Good topic coverage. Too brief for future maintainers or coding agents. |
| Nexus architecture | `docs/Architecture/Nexus/NEXUS_INTEGRATION_ARCHITECTURE.md` is much more detailed than most other CalradiaForge docs. | Strongest current architecture document. Needs roadmap/source-of-truth consolidation. |
| Changelog | `docs/CHANGELOG.md` is detailed and preserves project history. | Useful, but should not carry architecture intent by itself. |
| Contribution guidance | `docs/CONTRIBUTIONS.md` exists and gives basic rules. | Needs to become a broader development workflow guide. |
| Audit artifact | `docs/audit_07-07-26/calradiaforge_codebase_audit_07-07-26.md` identifies several missing docs and structural issues. | Valuable review artifact. Should move under a canonical `docs/reviews/` area. |

### 4.2 Weaknesses

| Weakness | Impact |
|---|---|
| No top-level documentation package README in the supplied docs package. | New contributors and coding agents do not have a clear read order or documentation map. |
| No project charter/scope document. | Product identity, v1.0 scope, non-goals, and release boundaries are scattered or implicit. |
| No ADR folder. | Locked decisions are present in prose, but not captured as durable decision records. |
| System docs are very short. | They identify ownership, but usually do not define workflows, invariants, failure handling, test expectations, or edge cases. |
| Nexus docs and `Nexus_Integration_Plan.md` overlap. | Risk of two semi-authoritative Nexus sources diverging. |
| `TODO_v1.md` conflicts with the deferred/planned tone in Nexus architecture. | Nexus timing needs one authoritative roadmap decision: v1.0, post-v1.0, or phased pre/post split. |
| No testing/QA strategy. | Refactors and installer/scanner changes will be harder to validate safely. |
| No release/versioning policy. | Changelog, project versions, and public releases can drift. |
| No security/trust-boundary document. | Archive extraction, installer overwrite behavior, BLSE handling, logging redaction, and Nexus credential storage need explicit rules. |
| No data/persistence specification. | Config, mod cache, modpacks, metadata, download cache, and migration behavior need stable contracts. |
| No UI/UX architecture guide. | MVVM/code-behind boundaries, status states, accessibility, and long-running operation presentation need clearer rules. |
| Review artifacts are stored under an audit-date folder. | OniForge’s `docs/reviews/` model is cleaner and should be reused. |

### 4.3 Documentation depth imbalance

The current CalradiaForge docs are uneven:

- `CHANGELOG.md` is large and detailed.
- `NEXUS_INTEGRATION_ARCHITECTURE.md` is detailed and decision-heavy.
- Most project/system docs are short summaries.
- There is no middle layer between quick summaries and long audit/refactor material.

This creates a source-of-truth problem. Important implementation intent may exist in audits, changelogs, TODOs, or refactor plans instead of stable architecture docs.

## 5. OniForge Documentation Pattern Worth Adopting

OniForge provides a strong model because it separates documentation into clear layers:

| OniForge Pattern | Value for CalradiaForge |
|---|---|
| `docs/README.md` | Gives a documentation package entry point and read order. |
| `00_Project/` | Captures project identity, scope, roadmap, open questions, and documentation state. |
| `01_Architecture/` | Separates architecture design, runtime behavior, solution structure, dependency rules, tech stack, and trust boundaries. |
| `02_Data/` | For CalradiaForge, this should become data/persistence/cache/file-format documentation rather than OniForge-style catalog docs. |
| `03_Application/` | Fits CalradiaForge system workflows: mod discovery, mod installation, modpacks, launcher, Nexus, EULA, localization, logging. |
| `04_UI/` | Fits WPF page/navigation/state/accessibility guidance. |
| `05_Development/` | Fits testing, coding standards, contribution flow, release checklist, versioning, Git workflow. |
| `06_Research/` | Fits deferred platform research, Nexus API notes, Steam workshop/library path research, Epic/GamePass research. |
| `adr/` | Captures accepted decisions separately from plans and audits. |
| `reviews/` | Keeps audits and work orders useful without making them canonical architecture. |
| `templates/` | Useful later, but optional for CalradiaForge unless repeatable audit/update prompts are wanted inside the repo. |

## 6. Recommended CalradiaForge Documentation Model

Recommended target structure:

```text
docs/
  README.md
  CHANGELOG.md
  EULA.txt

  00_Project/
    00_Project_Index.md
    01_Project_Charter.md
    02_Project_Scope.md
    03_Project_Plan.md
    04_Roadmap_and_Release_Strategy.md
    05_Open_Questions_and_Decision_Log.md
    06_Documentation_State.md

  01_Architecture/
    00_Architecture_Design.md
    01_Runtime_Architecture.md
    02_Solution_Structure.md
    03_Dependency_Rules.md
    04_Technology_Stack.md
    05_Security_and_Trust_Boundaries.md
    06_Platform_and_Game_Path_Detection.md

  02_Data_and_Persistence/
    00_Data_and_Persistence_Architecture.md
    01_App_Config_Specification.md
    02_Mod_Cache_Specification.md
    03_Modpack_File_Specification.md
    04_Nexus_Metadata_Specification.md
    05_Download_Cache_and_Partial_File_Policy.md
    06_Persistence_Versioning_and_Migration_Policy.md

  03_Application_Systems/
    00_Application_Systems_Overview.md
    01_Mod_Discovery_and_Scanning.md
    02_Mod_Parsing.md
    03_Mod_Installation_and_Archive_Extraction.md
    04_BLSE_Installation.md
    05_Modpack_Workflows.md
    06_Launcher_Workflow.md
    07_Nexus_Integration_Architecture.md
    08_Localization.md
    09_Logging.md
    10_EULA_Flow.md

  04_UI/
    00_UI_UX_Design_Guide.md
    01_Navigation_and_Page_Model.md
    02_MVVM_and_Command_Policy.md
    03_Status_Error_and_Progress_Presentation.md
    04_Accessibility_Checklist.md

  05_Development/
    00_Software_Design_Standards.md
    01_Testing_and_QA_Plan.md
    02_Repository_and_Release_Workflow.md
    03_Coding_Standards.md
    04_Git_Workflow.md
    05_Contributing.md
    06_Release_Checklist.md
    07_Versioning_Policy.md

  06_Research/
    00_Research_Index.md
    01_Nexus_API_Research.md
    02_Steam_Workshop_and_Library_Path_Research.md
    03_Epic_and_GamePass_Platform_Research.md
    04_Future_Ideas.md

  adr/
    0001-calradiaforge-bannerlord-only-wpf-desktop.md
    0002-layered-ui-core-nexus-boundaries.md
    0003-nexus-integration-is-optional.md
    0004-manual-update-checking-only.md
    0005-modulemodel-does-not-own-nexus-metadata.md
    0006-modinstaller-and-modextractor-remain-authoritative.md
    0007-steam-workshop-discovery-policy.md

  reviews/
    README.md
    calradiaforge_codebase_audit_07-07-26.md

  templates/
    doc-template.md
    adr-template.md
```

This structure follows OniForge’s discipline while staying CalradiaForge-specific.

## 7. Recommended Expansion Level

### What should be expanded

CalradiaForge should expand documentation in areas that control correctness, safety, refactoring, and release readiness:

- architecture boundaries,
- dependency rules,
- mod discovery and scanner behavior,
- Steam Workshop/library path detection,
- mod parsing,
- archive extraction and installer safety,
- BLSE handling,
- modpack persistence,
- config/cache persistence,
- Nexus auth/download/metadata boundaries,
- logging and redaction,
- testing and QA,
- release/versioning,
- UI state and long-running workflow behavior,
- accessibility and public-release polish.

### What should not be expanded heavily yet

Avoid excessive detail for areas that are not active or not implemented:

- Epic/GamePass backend internals beyond current deferred constraints.
- Full Nexus implementation details that have not been locked.
- Large template systems unless you want CalradiaForge to store its own repeatable prompt templates.
- Overly formal diagrams for simple systems unless the system has multiple workflows or failure states.

### Correct detail target

A good CalradiaForge document should be detailed enough that a coding agent or contributor can answer:

- What owns this behavior?
- What must not own this behavior?
- What is implemented today?
- What is planned but not implemented?
- What are the allowed dependencies?
- What are the safety rules?
- What edge cases must be preserved?
- What tests or manual checks prove the behavior still works?

It does not need to be as long as OniForge’s catalog/data documents unless the CalradiaForge subsystem has equivalent complexity.

## 8. Priority Work Plan

### Phase 0 — Documentation Stabilization

| Work Item | Action | Reason |
|---|---|---|
| Create `docs/README.md` | Add package overview, read order, and folder map. | Establishes the documentation entry point. |
| Create `docs/reviews/README.md` | Define audits/work orders as review artifacts, not canonical architecture. | Prevents audits from becoming accidental source of truth. |
| Move audit file into `docs/reviews/` | Move or copy `audit_07-07-26/calradiaforge_codebase_audit_07-07-26.md`. | Aligns with OniForge review organization. |
| Mark refactor docs as excluded/staging | Keep `docs/refactor/**` separate with a clear note that it is active planning. | Prevents unstable refactor plans from overwriting stable docs. |
| Consolidate docs index | Replace or supersede `Architecture/ARCHITECTURE_INDEX.md` with a project-wide index. | Current index only covers architecture. |

### Phase 1 — Project Foundation

| Work Item | Action | Reason |
|---|---|---|
| Project charter | Define CalradiaForge as a Bannerlord-focused WPF desktop mod manager/launcher. | Locks product identity. |
| Project scope | Define v1.0 scope, non-goals, deferred areas, and public-release boundaries. | Resolves scattered assumptions. |
| Roadmap/release strategy | Decide Nexus timing, Epic/GamePass deferral, LTS intent, and post-v1 plan. | Resolves `TODO_v1.md` versus Nexus deferred/planned tension. |
| Open questions/decision log | Track unresolved owner decisions. | Keeps planning decisions visible without changing architecture prematurely. |
| Documentation state doc | Summarize current docs, known gaps, and update rules. | Makes future audit follow-up easier. |

### Phase 2 — Architecture Foundation

| Work Item | Action | Reason |
|---|---|---|
| Architecture design | Expand current solution architecture into a primary architecture design doc. | Gives one canonical architecture overview. |
| Runtime architecture | Document startup, EULA, config load, localization, service initialization, scan/refresh, install, launch. | Captures runtime flow instead of only project boundaries. |
| Solution structure | Preserve current project list and expected dependency direction. | Helps refactors stay inside boundaries. |
| Dependency rules | Define allowed/forbidden dependencies for UI, Core, Nexus, ConsoleUtils. | Prevents boundary drift. |
| Technology stack | Document WPF, .NET version, packages, JSON library, extraction dependency, test stack target. | Supports reproducible development. |
| Security/trust boundaries | Document archive trust, file deletion, overwrite, BLSE, Nexus credentials, logging redaction, URL handling. | Required before Nexus/download/installer expansion. |

### Phase 3 — Data, Persistence, and Application Systems

| Work Item | Action | Reason |
|---|---|---|
| Data/persistence architecture | Define all persisted data categories and ownership. | Required for safe refactors and migrations. |
| App config specification | Define keys, allowed values, secret prohibition, and migration rules. | Prevents config sprawl. |
| Mod cache specification | Define cache schema, refresh behavior, backup/corrupt handling. | Supports scanner changes. |
| Modpack file specification | Define modpack JSON shape, import/export, validation, and Last Used behavior. | Supports modpack reliability. |
| Nexus metadata specification | Define `.metadata` ownership, filename rules, public IDs, secret exclusions. | Keeps Nexus data separate from `ModuleModel`. |
| Download cache policy | Define `.unfinished` behavior, resume validation, cleanup, and retention. | Needed before Nexus downloads. |
| Mod discovery/scanning document | Expand current Mod Management doc into a full scanner policy. | Needed for Bannerlord module and Steam Workshop issues. |
| Steam Workshop/library path policy | Define Steam library discovery, workshop content paths, Bannerlord app ID handling, fallback/manual override, and multi-drive cases. | Critical for users with workshop mods outside the main game install path. |
| Archive extraction/install document | Define containment checks, reserved module names, overwrite policy, backups, and validation. | Critical safety area. |
| Launcher workflow document | Expand Steam/current behavior and deferred Epic/GamePass behavior. | Reduces platform support ambiguity. |

### Phase 4 — Nexus Documentation Consolidation

| Work Item | Action | Reason |
|---|---|---|
| Merge `Nexus_Integration_Plan.md` into canonical Nexus architecture | Keep one authoritative Nexus doc. | Avoids drift. |
| Split Nexus roadmap from Nexus architecture | Roadmap decides timing; architecture decides boundaries. | Prevents TODO/planned conflicts. |
| Add Nexus ADRs | Capture SSO, DPAPI, manual updates, metadata separation, NXM registration timing. | Makes security-sensitive decisions durable. |
| Add Nexus implementation readiness checklist | Include auth, credential storage, redaction, queue, metadata, download validation, tests. | Prevents premature implementation. |

### Phase 5 — UI and Development Workflow

| Work Item | Action | Reason |
|---|---|---|
| UI/UX guide | Document WPF design goals, navigation, page responsibilities, status display, and dialogs. | Helps reduce page-code-behind drift. |
| MVVM and command policy | Define what stays in code-behind versus ViewModels/commands. | Useful during refactor work. |
| Status/error/progress presentation | Define user-facing progress and failure rules for scans, installs, downloads, and launch. | Improves reliability and UX consistency. |
| Accessibility checklist | Define keyboard, labels, focus, tooltip, and screen-reader baseline. | Public release quality. |
| Testing/QA plan | Define Core tests, scanner tests, persistence tests, archive/install tests, UI-viewmodel tests, and manual QA. | Required before risky changes. |
| Repository/release workflow | Define build/test/release steps and what must be updated per release. | Prevents release drift. |
| Versioning policy | Choose the source of truth for app version, changelog version, and package version. | Resolves version mismatch risk. |
| Release checklist | Add a repeatable pre-release checklist. | Supports open beta/public release. |

### Phase 6 — ADRs and Ongoing Maintenance

| Work Item | Action | Reason |
|---|---|---|
| Add ADR template | Use OniForge-style short decision records. | Makes decision capture repeatable. |
| Backfill key ADRs | Record existing accepted decisions. | Prevents future confusion. |
| Add doc template | Standardize future docs. | Keeps formatting consistent. |
| Add documentation update rule | Stable docs must be updated when code changes locked behavior. | Prevents docs from going stale. |
| Re-audit after major docs pass | Generate a new review report after updates. | Confirms gaps are closed. |

## 9. Proposed Document Template

Use a lightweight version of the OniForge document style:

```markdown
---
Project: CalradiaForge
Document Set: Documentation Foundation
Version: 0.1 Draft
Prepared: YYYY-MM-DD
Owner: ThelianTech / Thelian Technologies LLC
Status: Draft
---

# Document Title

## 1. Purpose

## 2. Scope

## 3. Current Implementation

## 4. Locked Decisions

## 5. Ownership and Boundaries

## 6. Workflow / Data Flow

## 7. Safety Rules / Invariants

## 8. Non-Goals / Deferred Work

## 9. Related Source Files

## 10. Related Documentation

## 11. Test / Validation Expectations
```

For very small docs, sections can be collapsed. For critical systems like mod scanning, archive extraction, Nexus credentials, and persistence, keep the full structure.

## 10. Proposed ADR Template

```markdown
---
Project: CalradiaForge
Document Set: Architecture Decision Records
Version: 0.1 Draft
Prepared: YYYY-MM-DD
Owner: ThelianTech / Thelian Technologies LLC
Status: Accepted
---

# ADR NNNN — Decision Title

## Status

Accepted.

## Context

## Decision

## Consequences

## Related Docs
```

Recommended first ADRs:

| ADR | Decision |
|---|---|
| 0001 | CalradiaForge is a Bannerlord-only Windows desktop mod manager/launcher. |
| 0002 | UI/Core/Nexus layered boundaries are required. |
| 0003 | Nexus integration is optional and must not be required for core app use. |
| 0004 | Nexus update checks are manual only. |
| 0005 | Nexus metadata stays outside `ModuleModel`. |
| 0006 | `ModInstaller` and `ModExtractor` remain authoritative for install flow. |
| 0007 | Steam Workshop discovery must support Steam library/workshop paths independently from the selected Bannerlord game install path. |
| 0008 | App config must not store secrets. |
| 0009 | Nexus credentials use DPAPI CurrentUser and operation-scoped decryption only. |

## 11. Important CalradiaForge-Specific Guidance

### 11.1 Do not let refactor plans become canonical automatically

Refactor plans should remain separate until they produce accepted decisions. After a refactor phase is locked, migrate only stable decisions into:

- architecture docs,
- system docs,
- data/persistence docs,
- development policy docs,
- ADRs,
- changelog entries.

Do not copy the full planning text into canonical docs unless it describes final accepted behavior.

### 11.2 Treat review artifacts as inputs, not architecture

The codebase audit and future documentation audits should live under `docs/reviews/`. They should inform updates, but they should not override canonical docs unless findings are converted into proper documentation changes.

### 11.3 Separate roadmap timing from architecture rules

Architecture should say **how a system must be built** when implemented.

Roadmap should say **when or whether that system belongs in v1.0**.

This matters most for Nexus integration, Epic/GamePass support, and post-v1.0/LTS planning.

### 11.4 Preserve “currently implemented” versus “planned” labels

CalradiaForge already uses `Currently Implemented`, `Planned / Locked Architecture`, and `Deferred / Future Work`. Keep this convention. It is one of the strongest parts of the current docs.

Use it consistently in every system document.

### 11.5 Add scanner/path documentation before scanner work expands

The current Mod Management doc states that `ModScanner` discovers installed modules from the game and Steam Workshop directories, but it does not define the exact path policy.

Before or alongside scanner fixes, create a dedicated document that defines:

- Bannerlord game install path discovery,
- Steam installation path discovery,
- Steam library folder discovery,
- Steam Workshop `content` path discovery,
- Bannerlord Workshop app ID handling,
- multi-drive Steam library behavior,
- fallback behavior when workshop paths cannot be detected,
- manual override behavior,
- logging/redaction behavior for detected paths,
- expected user-facing error/warning text.

This should be canonical system documentation, not just a refactor note.

## 12. Recommended Migration Mapping

| Current CalradiaForge File | Recommended Target |
|---|---|
| `docs/Architecture/ARCHITECTURE_INDEX.md` | Replace with `docs/00_Project/00_Project_Index.md` plus `docs/README.md`. |
| `docs/Architecture/Solution/ARCHITECTURE.md` | Split into `01_Architecture/00_Architecture_Design.md`, `01_Runtime_Architecture.md`, `02_Solution_Structure.md`, and `03_Dependency_Rules.md`. |
| `docs/Architecture/Projects/*.md` | Preserve content under `01_Architecture/02_Solution_Structure.md` or individual project docs if desired. |
| `docs/Architecture/Systems/Configuration.md` | Expand into `02_Data_and_Persistence/01_App_Config_Specification.md`. |
| `docs/Architecture/Systems/Logging.md` | Move/expand into `03_Application_Systems/09_Logging.md` and cross-reference security boundaries. |
| `docs/Architecture/Systems/Localization.md` | Move/expand into `03_Application_Systems/08_Localization.md`. |
| `docs/Architecture/Systems/ModManagement.md` | Split into scanning, parsing, installation, extraction, and BLSE docs. |
| `docs/Architecture/Systems/Modpacks.md` | Expand into modpack workflow and modpack file specification docs. |
| `docs/Architecture/Systems/Launcher.md` | Expand into launcher workflow plus platform/path research docs. |
| `docs/Architecture/Systems/EULA.md` | Move/expand into `03_Application_Systems/10_EULA_Flow.md`. |
| `docs/Architecture/Nexus/NEXUS_INTEGRATION_ARCHITECTURE.md` | Move/rename into `03_Application_Systems/07_Nexus_Integration_Architecture.md` or keep under `01_Architecture/Nexus/` and link from systems. |
| `docs/Nexus_Integration_Plan.md` | Merge into the canonical Nexus architecture and roadmap docs; then retire or mark superseded. |
| `docs/TODO_v1.md` | Convert into roadmap/release strategy and open questions. |
| `docs/CONTRIBUTIONS.md` | Expand into `05_Development/05_Contributing.md`. |
| `docs/audit_07-07-26/calradiaforge_codebase_audit_07-07-26.md` | Move into `docs/reviews/`. |
| `docs/refactor/**` | Keep separate as active planning; later migrate locked decisions only. |

## 13. Final Recommendation

Adopt the OniForge documentation style at the **organizational and decision-control level**, not at the full content-volume level.

CalradiaForge does not need OniForge’s exact amount of data-pipeline documentation. It does need OniForge’s stronger documentation foundation because CalradiaForge has several safety-sensitive and release-sensitive areas:

- mod scanning and Steam Workshop discovery,
- archive extraction and install safety,
- modpack persistence,
- Nexus credentials/downloads/metadata,
- launcher/platform detection,
- versioning and release readiness,
- testing strategy,
- refactor control.

The best path is a phased documentation rebuild that converts the current short architecture docs into a stable, numbered documentation package while keeping active refactor planning isolated until decisions are final.
