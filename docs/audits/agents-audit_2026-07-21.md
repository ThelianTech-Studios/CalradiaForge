# CalradiaForge Agent-Instruction Audit

Status: Read-only inventory and centralization proposal

Audit date: 2026-07-21

Branch / commit: `dev-V0-14-CodeRefactor` / `53a93dd322f9f252933d3f9323f880e18243e7d0`

Scope: Repository-resident Codex/AI-agent instructions, reusable prompts, task policies, workflow guidance, contributor/developer guidance, and adjacent machine-readable instruction surfaces. No existing documentation, source, configuration, or prompt file was changed.

## Executive Summary

CalradiaForge has a solid active root instruction file and several useful reusable policies, but agent guidance is distributed across at least ten surfaces with no central index, status metadata, or routing model. The highest-value reusable material is split between `AGENTS.md`, the task-execution policy, the refactor master plan, and the performance policy. Historical and completed prompts sit beside active documents, including one ignored `TEMP` prompt that can be opened locally but is not tracked or governed as an active instruction.

The recommended future state is a small, version-controlled `.agents/` instruction hub with `AGENTS.md` retained as the concise mandatory entrypoint. The hub should route an agent to the existing source, architecture, refactor, and policy documents; it must not duplicate their technical decisions or become a second architecture source of truth. Reusable operating rules, workflows, templates, active-phase routing, and archival metadata belong centrally. Phase requirements, accepted architecture, contributor documentation, generated-code rules, and benchmark instructions should remain in their current authoritative locations and be linked.

No migration was performed by this audit. The immediate prerequisite is correction of the identified stale and contradictory guidance, particularly the generic prompt's embedded Phase 2 rules and the 5.A/5.B versus 6.A/6.B phase-label drift.

## Audit Method And Coverage

- Read `AGENTS.md` and the full `docs/refactor/task_execution_optimization_policy.md` before delegating work.
- Applied the task-execution policy through three independent, read-only tracks: explicit prompt surfaces; refactor/architecture instruction content; and public/developer/tooling surfaces.
- Searched tracked and ignored files for `Codex`, `agent`, `subagent`, `prompt`, `instruction`, `skill`, and related workflow terms.
- Examined the instruction-bearing Markdown files, root and source developer guidance, project/configuration surfaces, `.gitignore`, `.github`, the empty `.agents` directory, and documentation-retention context.
- Verified that the Phase 5 Codex prompt under `docs/TEMP_Codex_Instructions` is ignored rather than tracked.
- Preserved the pre-existing modification to `docs/refactor/refactor_master_plan.md` and the earlier untracked source-state audit report.

## Current Instruction Inventory

| Surface | Status | Purpose and audience | Audit assessment |
|---|---|---|---|
| `AGENTS.md` | Active, tracked root entrypoint | Project overview, source-of-truth order, architecture boundaries, forbidden changes, Nexus rules, documentation rules, build commands, and agent working agreement. | The strongest current entrypoint. It should remain the auto-discoverable root baseline, but be shortened to route detailed procedures to a central hub. |
| `docs/refactor/task_execution_optimization_policy.md` | Active, tracked reusable policy | Capability/reasoning selection, justified parallelization, task scope, and runtime fallback for all agent work. | The clearest reusable execution policy; it is suitable to migrate essentially unchanged. |
| `docs/refactor/refactor_master_plan.md` | Active, tracked plan and workflow source | Phase requirements, global guardrails, closeout workflow, subagent/reviewer instructions, and two pasteable prompts. | Valuable but overloaded: it combines phase planning, project constraints, operating procedures, templates, and phase-specific material. |
| `docs/refactor/performance_audit_and_optimization_policy.md` | Active, tracked scoped policy | Performance evidence rules plus audit and optimization prompt templates. | Keep performance constraints local; extract only reusable template mechanics to the central hub. |
| `docs/refactor/versioning_policy.md` | Active, tracked scoped policy | Version semantics and a small Codex/changelog instruction fragment. | Keep version semantics local; relocate generic changelog process into a central workflow. |
| Other `docs/refactor/*.md` policies | Active, tracked scoped references | Security, persistence, archive safety, DI, testing, results, MVVM, and platform workflows. | These are technical/phase policy references, not general agent operating instructions. They should remain local and be selected through a reference map. |
| `docs/audits/refactor_documentation_instructions.md` | Historical, tracked cleanup-managed prompt | Original 943-line refactor-documentation generation work order. | Explicitly historical, but its body includes superseded requirements. It is unsafe as an unlabelled discovery result and should remain archived with prominent metadata. |
| `docs/TEMP_Codex_Instructions/CalradiaForge_Phase_5_Refactor_Docs_Repair_COMPLETE_Codex_Prompt.md` | Completed temporary prompt, ignored/untracked | 1,775-line Phase 5 documentation-repair work order. | Contains detailed, useful history but is not a current authority. It lacks a sitemap/archive record and should not be treated as active guidance. |
| `docs/DOCUMENT_SITEMAP.md` | Active, tracked documentation-governance source | Cleanup procedure, retention rules, and document classification. | A scoped workflow authority. Central instructions should route documentation tasks to it rather than duplicate retention rules. |
| `docs/CONTRIBUTIONS.md` and `README.md` | Active human-contributor documentation | Architecture, build/test commands, contribution workflow, public project orientation. | Not AI-specific. Keep public-facing; reduce duplicated operational claims and link to canonical technical/instruction sources. |
| `source/CalradiaForge.Benchmarks/README.md` and runner | Active task-local developer guidance | Release-only benchmark workflow, artifact handling, fixture provenance, and real-data prohibition. | Strong local instructions; retain beside the benchmark workflow and reference it from central task routing. |
| `source/.editorconfig` and generated-source metadata/comments | Active machine-readable/local rules | Code style and generated-code maintenance boundaries. | Do not copy detailed settings into agent guidance; reference these authoritative local rules. |
| `.github/ISSUE_TEMPLATE/*` | Active issue intake templates | Human bug/feature reporting. | No agent configuration, PR template, Actions workflow, or contributor automation exists here. |

The root `.agents` directory exists but is empty. It is an appropriate future home because it clearly distinguishes agent-operating material from human-facing product documentation. It is not ignored by the current `.gitignore`; a populated folder would be version controlled.

## Current Routing And Authority Risks

### F-01 — No Central Index Or Instruction Lifecycle Model

There is no discoverable map of which instructions are active, phase-scoped, historical, temporary, or superseded. An agent can encounter an audit prompt or ignored completed prompt without an explicit current-use warning.

Impact: work can follow superseded requirements or spend time reconciling documents that were never meant to be current.

Recommendation: create one central index with task routing and require front matter on every agent-targeted file: `status`, `applies_to`, `precedence`, `source_of_truth`, `last_reviewed`, `owner`, `replaces`, and `replaced_by`.

### F-02 — Root `AGENTS.md` Is Strong but Partly Stale and Too Broad

`AGENTS.md` is the appropriate active baseline but states that no test project is checked in, while `source/CalradiaForge.Tests` exists and 76 tests now pass. It also lists generic Markdown above architecture documents in source-of-truth priority, while later wording correctly requires agents to stop on source/architecture conflict. The distinction between the task's requested scope, observed current behavior, accepted architecture, active phase plan, and historical evidence is not explicit enough.

Recommendation: retain root `AGENTS.md` as a short mandatory entrypoint. Correct the test statement and replace duplicated detail with links to central operating rules and the reference map. Define authority by question rather than one undifferentiated rank:

1. System/developer and current user instructions define authorization and scope.
2. Current source defines observed implemented behavior.
3. Accepted architecture and active policy documents define mandatory constraints.
4. The active phase plan defines approved future-state work.
5. Historical audits/prompts provide evidence only unless explicitly reactivated.

### F-03 — Reusable Workflow Content Is Embedded in the Master Plan

The master plan contains global guardrails, a shared implementation/reviewer/closeout workflow, a generic implementation prompt, and a changelog/migration-map prompt. This mixes durable process with phase planning and makes routine instruction changes risk edits to a large planning artifact.

Two execution defects are especially important:

- The generic `Phase x` implementation prompt includes Phase 2-specific language and a Phase 2 logging-constraints block. Pasting it into a Phase 6.A task can falsely constrain the work.
- The changelog/migration-map prompt asks review against Phase 2 constraints instead of the active phase's applicable documentation.

Recommendation: move reusable procedure and templates to `.agents/workflows` and `.agents/templates`. Leave concise links in the master plan, and keep phase requirements/guardrails beside their phase.

### F-04 — Phase Labels Differ Across Agent-Facing Documents

The master plan identifies the current work as Phase 6.A/6.B. `docs/refactor/logging_policy.md` and `docs/Architecture/Systems/Logging.md` still use Phase 5.A/5.B labels. This is a practical execution risk because a template or agent can select the wrong scope.

Recommendation: reconcile these labels before migrating text. The central reference map should provide one active phase-to-document mapping and flag historical labels rather than silently normalizing them.

### F-05 — Historical and Temporary Prompts Are Discoverable Without Enough Guardrails

`docs/audits/refactor_documentation_instructions.md` has a good historical preamble, but its large body contains superseded central-redaction and AppConfig key-blocking requirements. `docs/TEMP_Codex_Instructions/...COMPLETE_Codex_Prompt.md` is ignored by Git, marked `TEMP`/`COMPLETE` only in its path/name, and has no lifecycle record in `DOCUMENT_SITEMAP.md`.

Recommendation: archive prompt artifacts under one governed archive path with required status metadata and a `replaced_by` pointer. Do not use filename conventions alone as lifecycle state. The temporary prompt should remain read-only evidence until a separately approved archival/retention action handles it.

### F-06 — Scope-Local Guidance Should Not Be Duplicated Centrally

Architecture documents contain technical truth but no broad Codex operating procedure. The benchmark README, `.editorconfig`, generated-resource comments, issue templates, and documentation sitemap are all authoritative inside narrow contexts.

Recommendation: central instructions should route agents to these documents. Copying their rules into a new hub would create drift and duplicate authority.

### F-07 — Human Contributor Documentation Has Drift

`docs/CONTRIBUTIONS.md` still describes the removed `GamePathsHelper` as a pure/injected helper. `README.md` duplicates developer architecture/service-ownership material and links only to contributions guidance. This is not an agent-system defect by itself, but it means central routing should not treat either document as the authority for live implementation behavior.

Recommendation: in a later, separately approved documentation task, update the stale contributor claim and make README link to canonical contributor and agent-routing documents rather than restating operational details.

## Recommended Central Instruction Structure

The proposed structure is intentionally small. It centralizes agent operations, not product architecture.

```text
.agents/
  README.md
  reference-map.md
  core/
    operating-rules.md
    architecture-and-security-boundaries.md
  workflows/
    implementation.md
    audit-and-report.md
    documentation-only.md
    changelog-and-migration-map.md
  templates/
    phase-implementation-prompt.md
    performance-audit-prompt.md
    performance-optimization-prompt.md
  phase/
    active-phase.md
  archive/
    README.md
    prompts/
```

| File/group | Owns | Must not own |
|---|---|---|
| `.agents/README.md` | Entry point, lifecycle/status convention, authority model, task classification, required-reading matrix. | Detailed architecture, phase requirements, duplicated build rules. |
| `reference-map.md` | Task-type/phase routing to AGENTS, source, architecture, refactor policies, benchmark README, sitemap, and current audit evidence. | New technical decisions or future-state architecture. |
| `core/operating-rules.md` | Dirty-worktree preservation, scope discipline, verification/reporting honesty, subagent/reviewer protocol, and no-Git-mutation rules where applicable. | Phase-specific constraints. |
| `core/architecture-and-security-boundaries.md` | Concise routing links to authoritative architecture/security constraints and non-negotiable cross-project boundaries. | A duplicate of Architecture or Nexus documents. |
| `workflows/*.md` | Reusable sequence and exit criteria for each task class. | Implementation details belonging to an active phase. |
| `templates/*.md` | Parameterized prompt bodies using `{PHASE}`, `{APPLICABLE_POLICIES}`, and `{VERIFICATION}` placeholders. | Hard-coded Phase 2 or other phase-specific rules. |
| `phase/active-phase.md` | A short pointer to the current phase, its master-plan section, required policy documents, status, and owner gates. | A second master-plan phase specification. |
| `archive/` | Clearly historical/completed agent artifacts with status and replacement pointers. | Unlabelled active instructions. |

Root `AGENTS.md` should remain, but after migration it should contain only the project overview, non-negotiable boundary summary, direct links to `.agents/README.md` and core documents, build/test command links, and a clear instruction to read the reference map before task-specific work.

## Migration Boundaries

### Move or Extract into `.agents/`

- Task-execution policy text, without semantic change.
- Reusable subagent/reviewer, verification, reporting, closeout, and dirty-worktree practices.
- Generic audit, documentation-only, implementation, and changelog/migration workflow templates.
- A maintained phase/task reference map.
- Clear lifecycle metadata and archive/index guidance for agent prompts.

### Keep in Existing Authoritative Locations

- `AGENTS.md` as root discovery and short mandatory baseline.
- Phase scope, phase guardrails, dependency ordering, and accepted decisions in `docs/refactor/`.
- Current technical behavior and accepted design in `docs/Architecture/` and source.
- Security, persistence, archive safety, DI, testing, logging, versioning, and performance semantics in their existing scoped policy documents.
- Human contributor material in README and `docs/CONTRIBUTIONS.md`.
- Benchmark workflow in its source-local README and runner.
- `.editorconfig`, project files, generated-source metadata, and `.gitignore` as local machine-readable authorities.
- Audit reports as evidence; do not convert them into live policy.

## Safe Migration Sequence

1. Approve the `.agents/` authority statement, metadata schema, and ownership boundaries. Do not move files yet.
2. Correct existing instruction hazards: stale `AGENTS.md` test wording, generic Phase 2 hardcoding, changelog prompt's Phase 2 comparison, and 5.A/5.B versus 6.A/6.B label drift.
3. Create `.agents/README.md`, `reference-map.md`, and core operating rules. Keep the root file as the mandatory entrypoint and link to the new hub.
4. Extract reusable workflows/templates without changing their intended scope. Use placeholders for phase-specific information and leave phase rules in the master plan.
5. Add `active-phase.md` as a pointer, not a duplicated plan. It should identify the active section, required supporting documents, source/architecture areas, verification expectations, and owner gates.
6. Classify every legacy/temporary prompt as active, historical, completed, or superseded. Move only after the owner approves retention handling; retain historical content and replacement pointers.
7. Update cross-references, run Markdown-link checks, verify all required workflow controls remain reachable, and verify that no central file claims planned behavior is implemented.
8. Only then update `DOCUMENT_SITEMAP.md` and any relevant solution/document indexes in a separate approved documentation-maintenance change.

## Minimum Metadata Contract

Every central or archived agent instruction should begin with a short metadata block such as:

```yaml
status: active # active | phase-scoped | historical | completed | superseded
applies_to: audit-and-report
precedence: repository-agent-guidance
source_of_truth: docs/refactor/task_execution_optimization_policy.md
last_reviewed: 2026-07-21
owner: CalradiaForge maintainers
replaces: []
replaced_by: []
```

Historical/completed files must state their current non-authoritative status before any substantive prompt body. An active file must state the task class and links to the source/architecture/policy documents it requires.

## Migration Acceptance Criteria

- One active entrypoint and one reference map route every agent task type.
- Root `AGENTS.md` remains concise, current, and points to the hub.
- Reusable procedures have one canonical copy; phase-specific rules remain in their phase documents.
- Generic templates have no hard-coded phase constraints.
- Active, completed, historical, and superseded prompts are distinguishable without relying on filenames.
- Architecture, source, scoped policy, human-contributor, and machine-readable local rules retain their current authorities.
- All links resolve and the reference map selects the current phase documents correctly.
- Documentation cleanup/sitemap governance captures any moved or newly created tracked files.
- No historical prompt or audit can be mistaken for proof of current implementation.

## Conclusion

The project already contains most of the content needed for a disciplined agent-instruction system. The gap is organization and lifecycle control, not a lack of guidance. A future `.agents/` hub should centralize routing, reusable process, templates, and prompt status while linking to—not duplicating—the existing source, architecture, refactor, and policy authorities. Resolve the current stale/contradictory instructions before migration so centralization reduces risk instead of preserving it in a new location.
