# CalradiaForge Refactor Documentation Generation Prompt

Use this prompt in Codex to generate the CalradiaForge refactor planning documentation.

## Task

Create or update CalradiaForge refactor planning documentation based on the locked decisions below.

This is a documentation-generation task first. Do not implement source-code changes unless explicitly asked in a later task.

Use the repository source code and existing markdown documentation as source of truth for current names, project structure, existing services, and existing behavior. If the code and this prompt conflict, do not silently rewrite the decision. Document the mismatch and propose a safe follow-up.

## Project Context

CalradiaForge is a WPF desktop app for Mount & Blade II: Bannerlord mod management.

Known projects include:

- `CalradiaForge.UI`
- `CalradiaForge.Core`
- `CalradiaForge.Nexus`
- `CalradiaForge.ConsoleUtils`

Current architecture direction:

- UI depends on Core and Nexus.
- Core should remain free of WPF dependencies.
- Nexus architecture and API strategy are already documented elsewhere and should not be redesigned in this refactor pass.
- The next beta release is a refactor-heavy beta release, not a v1.0 release polish pass.

## Required Documentation Output

Create a master plan plus focused supporting docs.

Recommended files:

- `docs/refactor/refactor_master_plan.md`
- `docs/refactor/testing_strategy.md`
- `docs/refactor/persistence_policy.md`
- `docs/refactor/security_and_secret_boundary.md`
- `docs/refactor/archive_installer_safety_policy.md`
- `docs/refactor/versioning_policy.md`
- `docs/refactor/logging_policy.md`

Optional focused docs may be added only if they materially improve clarity:

- `docs/refactor/mvvm_refactor_plan.md`
- `docs/refactor/dependency_injection_plan.md`
- `docs/refactor/result_and_workflow_policy.md`

Do not create a large number of tiny documents if one supporting document can clearly cover the topic.

## Final Plan Detail Level

Use detailed phase work orders with dependency rules.

Each phase should include:

- purpose
- included work
- excluded work
- affected areas
- implementation notes
- dependency ordering
- do-before / do-after rules
- phase exit criteria
- risk notes
- verification expectations
- Codex guardrails
- documentation updates required

The plan should make phase boundaries explicit so cleanup, Serilog, persistence, installer safety, tests, DI, platform adapters, result types, workflow coordination, and MVVM work do not get mixed into one uncontrolled change set.

## Locked Decisions Summary

### Decision 1 - Nexus Timing

Nexus remains in v1.0 scope.

Early beta builds of Nexus functionality may be available for experimental use while CalradiaForge waits for Nexus Mods approval for the SSO portion of the app.

Rules:

- Nexus is not deferred to post-v1.
- Nexus remains a v1.0 feature target.
- Experimental Nexus builds can exist before full approval.
- SSO-dependent features should be gated, disabled, mocked, or marked experimental until approval is complete.
- Pending SSO approval must not be treated as a reason to remove or defer Nexus architecture.

### Decision 2 - Refactor Scope

CalradiaForge will pursue a phased full architecture refactor.

The refactor must be broken into controlled phases. Each phase should group related changes by size, risk, and dependency order. Large architectural changes must not be bundled together unless they are tightly connected.

Examples:

- MVVM extraction and dependency injection must be treated as separate phases.
- Cleanup-only changes may be grouped into one cleanup phase.
- Installer safety, persistence safety, logging redaction, testing, and UI architecture work should be planned as separate or carefully grouped phases depending on dependency order.
- Final phase ordering should follow the locked phase ordering model below.

Guardrails:

- Keep Core free of WPF.
- Keep Nexus networking/auth/downloader in `CalradiaForge.Nexus`.
- Keep `ModInstaller` and `ModExtractor` as the authoritative install pipeline unless a later implementation phase explicitly changes that design.

### Decision 3 - Archive / Installer Safety

Use a custom hybrid installer safety policy.

The policy must preserve CalradiaForge's ability to work around Bannerlord's mod loading behavior while still improving install safety, overwrite safety, archive validation, and BLSE handling.

The exact overwrite, backup, validation, and confirmation rules are not fully locked yet. They should be customized later after Bannerlord-specific mod loading constraints are explained and reviewed.

The policy should still address:

- archive containment
- unsafe overwrite/delete behavior
- official/reserved module protection
- BLSE bin-folder overwrite safety
- tests around install behavior

### Decision 4 - Platform Targeting

Use an adapter-based strategy for Windows-specific APIs.

CalradiaForge remains a Windows-first WPF desktop application, but Windows-specific APIs should be isolated behind explicit abstractions where practical.

This should apply especially to:

- game path detection
- Registry access
- external process launching
- URL/file explorer launching
- archive extraction implementation details
- file-system operations that need containment checks
- future OS-specific Nexus handler registration

Do not attempt to make the UI cross-platform in this refactor pass. The goal is to keep Core logic cleaner, more testable, and less tightly coupled to Windows implementation details.

### Decision 5 - Versioning Policy

Use the following owner-provided versioning policy as source of truth.

# CalradiaForge Versioning Policy Refactor Prompt

I want to refactor CalradiaForge’s versioning policy and project version management based on the following locked-in versioning direction.

CalradiaForge is a WPF desktop app for Mount & Blade II: Bannerlord mod management. It currently has version drift because `CHANGELOG.md`, `.csproj` files, UI display locations, GitHub release titles, and Nexus Mods release titles can fall out of sync.

## Locked Versioning Policy Direction

Use a custom SemVer-style policy:

MAJOR.MINOR.PATCH[-prerelease]

No build metadata should be used for now.

## Before v1.0

Before v1.0, versions should use:

0.MILESTONE.PATCH[-prerelease]

Meaning:

- `MAJOR`
  - Always `0` while CalradiaForge is still pre-1.0/open beta.

- `MINOR`
  - Represents a milestone version.
  - A milestone is a meaningful development phase, feature group, module addition, or app capability step.

- `PATCH`
  - Represents an intentional released revision inside the current milestone.
  - Patch can include bug fixes, small code changes, cleanup, stabilization work, or commit/change batches needed to complete or stabilize the milestone.

Important caution:

- Patch numbers must not become raw commit counters.
- Patch should not automatically increase for every individual commit.
- Patch should represent a deliberate released stabilization/update revision.
- This keeps the commit-based flavor without making public versions chaotic or misleading.

Example:

v0.12.15-beta

Meaning:

- `0` = pre-1.0/open beta app
- `12` = milestone 12
- `15` = released patch/revision 15 inside milestone 12
- `beta` = prerelease channel

## After v1.0

After v1.0, the rules become stricter:

- `MAJOR`
  - Complete app-wide alteration.
  - Major architecture change.
  - Major compatibility break.
  - Full visual shell/foundation change.
  - Major platform/UI foundation change.

Examples that can justify a major version bump:

- WPF to AvaloniaUI migration.
- Full application shell redesign.
- App-wide architecture rewrite.
- Major metadata/settings/cache compatibility break.

- `MINOR`
  - New user-facing features.
  - Major modules.
  - New workflows.
  - New integrations.
  - Meaningful capability expansions.

- `PATCH`
  - Bug fixes.
  - Small improvements.
  - UI polish.
  - Compatibility fixes.
  - Stabilization releases.

## Shared App Version vs Assembly Versions

Use both, but with a clear hierarchy:

- Public App Version is shared and authoritative.
- Assembly versions should share the app version by default.
- Separate assembly metadata/versioning is allowed only when justified.

Rule:

CalradiaForge has one public app version used for:

- GitHub releases
- Nexus Mods releases
- changelog entries
- app UI display
- release artifact names
- installer/package names if applicable

Individual assemblies may only have separate metadata if there is a strong technical reason, such as a separately distributed library or tool.

## Source of Truth

Use:

Directory.Build.props

`Directory.Build.props` should become the central source of truth for shared version properties.

The refactor should remove or prevent conflicting hardcoded version values in:

- `.csproj` files
- UI display code
- AssemblyInfo files
- release scripts
- artifact naming scripts

Where possible, app UI should read the version from assembly metadata instead of hardcoding it.

## Experimental Nexus Builds

While Nexus API/SSO approval or implementation is pending, label those builds clearly as experimental.

Preferred example:

CalradiaForge v0.13.0-experimental.nexus.1 - Nexus API Experimental Release

This is preferred over a plain beta label because it clearly tells users that the Nexus integration is experimental and may depend on API/SSO approval or changing behavior.

Acceptable progression:

v0.13.0-experimental.nexus.1
v0.13.0-experimental.nexus.2
v0.13.0-preview.1
v0.13.0-beta
v0.13.0-rc.1
v0.13.0

## Existing Release Title Style

Current release title style example:

CalradiaForge v0.12.15-beta - Third Beta Release

For experimental Nexus releases, use the same title pattern but replace the release phrase:

CalradiaForge v0.13.0-experimental.nexus.1 - Nexus API Experimental Release

## Changelog Mapping Rules

Use this mapping:

| Change Type | Before v1.0 | After v1.0 |
|---|---|---|
| New milestone/module | MINOR | MINOR |
| App-wide rewrite/foundation change | Usually MINOR | MAJOR |
| WPF to AvaloniaUI or major shell redesign | Pre-1.0 milestone | MAJOR |
| Bug fixes | PATCH | PATCH |
| Small UI polish | PATCH | PATCH |
| Docs/internal only | Usually no app version bump | Usually no app version bump |
| Nexus SSO/API experimental work | prerelease label | prerelease label or MINOR |
| Release candidate | rc.N | rc.N |

## Refactor Goals

Design or implement a versioning cleanup that ensures:

1. `Directory.Build.props` is the central version source of truth.
2. Project files inherit version information instead of duplicating it.
3. App UI display reads the version from assembly metadata where possible.
4. `CHANGELOG.md` entries match the selected version.
5. GitHub release titles, Nexus release titles, and release artifacts use the same version.
6. Experimental Nexus builds follow the approved `experimental.nexus.N` label style.
7. Patch numbers are intentional released revisions, not automatic raw commit counters.
8. Codex should not casually change major/minor versions without explicit approval.

## Codex Permission Rules

Codex may change:

- `Directory.Build.props`
- `CHANGELOG.md`
- release notes drafts
- build/release scripts
- version display code if it currently hardcodes versions
- project files only to remove conflicting duplicated version values

Codex should avoid or ask before changing:

- major/minor version numbers
- old changelog history
- unrelated project metadata
- unrelated build configuration
- assembly-specific versioning unless technically justified

The intended outcome is a professional but custom versioning system that fits a solo/open-beta WPF desktop app and leaves room for stricter post-v1.0 versioning later.

### Decision 6 - Testing Strategy

Use a tiered full testing strategy.

A testing strategy document must describe the full intended testing model, including Core unit tests, integration tests, ViewModel tests, CI validation, and possible future WPF UI automation tests.

Implementation must be phased. Do not attempt to add every testing layer at once.

Immediate testing priority:

1. Core unit tests for parser, persistence, install safety, archive validation, BLSE rules, and modpack workflows.
2. Integration-style tests for file-system-heavy workflows where unit tests alone are not enough.
3. ViewModel tests only after MVVM extraction begins.
4. CI test execution after test projects exist.
5. WPF UI automation only later, when specific high-value UI flows justify it.

Runtime/manual testing may still be used during development, but it should no longer be the only verification strategy for risky workflows.

### Decision 7 - Logging Redaction / Secret Safety

Use a strict secret-boundary policy, with explicit recognition that `AppConfig` is only a non-secret settings manager.

`AppConfig` and `AppConfigSettings` are non-secret settings managers. They are allowed to store ordinary app configuration such as:

- game paths
- debug settings
- mod data paths
- UI behavior settings
- other non-sensitive runtime options

`AppConfig` must never be expanded into a credential manager.

Secret-bearing data must be handled by separate purpose-built managers or handlers, such as a future Nexus credential/auth manager.

Rules:

1. A central redaction layer must exist before Nexus authentication is implemented.
2. Logger output must pass through redaction before writing sensitive or user-provided runtime values.
3. `AppConfig` must reject or block secret-like keys to preserve its non-secret boundary.
4. Nexus credentials must never be stored in `AppConfig`.
5. Nexus credentials must be handled only inside the Nexus credential/auth boundary.
6. Nexus credentials must use DPAPI CurrentUser storage.
7. Debug logs must follow the same redaction rules as normal logs.
8. Auth headers, bearer tokens, API keys, credential objects, and secret-bearing URLs must never be logged raw.
9. Tests should verify that common secret patterns are redacted or blocked.

### Decision 8 - Persistence Safety

Use Option B plus selected Option C rules.

CalradiaForge will implement atomic writes and backup recovery, while also creating a broader persistence policy that can grow into schema versions, quarantine handling, and migrations over time.

Immediate implementation direction:

1. Use atomic writes for persisted JSON files.
2. Use backup recovery for important user/app data.
3. Validate written JSON before replacing the current file where practical.
4. Route JSON file writes through data helpers or shared persistence helpers.
5. Avoid direct service-level `File.WriteAllText` persistence for domain data.

Policy-level rules to document:

1. Persisted data should have clear ownership.
2. Important persisted files should have backup behavior.
3. Future persisted models should support `schemaVersion` where useful.
4. Corrupt files should be handled gracefully instead of crashing the app.
5. Corrupt or unreadable files may later be moved to a quarantine/recovery folder.
6. Future migrations should be documented before model-breaking changes.
7. Nexus metadata persistence must follow the same safety model once implemented.

Initial targets:

| File/Data Area | Initial Rule |
|---|---|
| `AppConfig` | atomic save plus reject secrets |
| `ModsData` | atomic save plus backup recovery |
| `ModpackData` | atomic save plus backup recovery |
| last-used load order | atomic save |
| future Nexus metadata | atomic save plus schema/version rules |

### Decision 9 - Async Installer Completion

Use a workflow coordinator model.

The install coordinator should own:

- active install state
- progress updates
- cancellation state
- completion handling
- failure handling
- cleanup after install attempts
- UI-facing status/error messages
- future Nexus download-to-install handoff behavior

The coordinator must not make Core depend on WPF. Core install services should remain UI-independent.

Initial implementation may wrap the current installer/event flow. Later phases may evolve the coordinator to use tracked install operations, install result objects, or task-based completion where useful.

### Decision 10 - UI Status / Busy / Error Visibility

Use a shared UI state pattern now, grow into a fuller operation state system later, and use the existing Toast System for visual notifications.

Initial shared UI state should support:

- `IsBusy`
- `StatusMessage`
- `ErrorMessage`
- `CanCancel`
- `CurrentOperation`
- `LastOperationResult` where useful

The existing Toast System should be used for user-visible notifications such as:

- operation started
- operation completed
- recoverable warnings
- operation failed
- validation issues
- Nexus/download/install status events where appropriate

Long-term, this pattern may evolve into a fuller operation state system covering installs, scans, modpack import/export, Nexus downloads, validation results, and recoverable vs blocking errors.

### Decision 11 - MVVM Refactor Strategy

Use a full shell/viewmodel rewrite, implemented through staged phases.

The goal is to move the WPF UI toward a consistent MVVM architecture across the full app, including the shell, navigation, page state, commands, workflow state, dialogs, and user-visible operation feedback.

This rewrite must be phased by dependency order and risk. Do not attempt to rewrite the entire UI in one uncontrolled pass.

Recommended staging:

1. Define MVVM conventions, base viewmodel patterns, command patterns, and shared UI state.
2. Refactor app shell/navigation structure.
3. Extract high-risk workflow viewmodels, especially install, scan/refresh, BLSE, and modpack workflows.
4. Refactor remaining pages into viewmodels.
5. Remove obsolete code-behind logic after equivalent viewmodel behavior is verified.
6. Add viewmodel tests as viewmodels become stable.

### Decision 12 - Dependency Injection Strategy

Use `Microsoft.Extensions.DependencyInjection`, with room to grow into `Microsoft.Extensions.Hosting` / Host Builder later if justified.

The DI rollout should be staged and should support:

- Core service registration
- platform adapter registration
- ViewModel registration
- workflow coordinator registration
- future Nexus service registration
- test-friendly service replacement

Define clear lifetime rules and avoid using DI as a service locator throughout the UI.

A full Host Builder setup may be considered later if CalradiaForge grows enough to justify richer app lifetime, logging, configuration, and background-service infrastructure.

### Decision 13 - Result / Error Handling Strategy

Use workflow-specific result types first, with growth into a broader app-wide result pattern later if justified.

Initial result types should be added around high-risk workflows such as:

- install operations
- archive validation
- BLSE validation/install
- persistence save/load/recovery
- mod scan/refresh
- modpack import/export
- future Nexus download/auth workflows

Result objects should support:

- success/failure
- error or result code where useful
- user-facing message
- technical/log message
- warnings
- affected file/path/mod where useful

Logging rules:

- Info logs should record normal lifecycle summaries and important operation outcomes.
- Error logs should record failures.
- Debug logs may include fuller result details, but must still pass through redaction rules.

A broader shared `Result` / `Result<T>` pattern may be introduced later if workflow-specific result types become repetitive enough to justify standardization.

### Decision 14 - Nexus Experimental Feature Gating

Use experimental feature flags for Nexus functionality.

Rules:

- Nexus experimental features should be disabled by default unless intentionally enabled for beta/dev testing.
- Nexus UI may exist before full approval, but experimental actions must be clearly labeled.
- SSO/auth-dependent features must be gated separately from non-auth Nexus planning or metadata features.
- Feature flags may be stored in normal non-secret app settings when they do not contain credentials.
- Nexus credentials must still remain inside the Nexus credential/auth boundary.
- Logs should clearly mark experimental Nexus actions.
- Toasts/status messages should tell the user when a Nexus feature is experimental, unavailable, or waiting on approval.
- Codex must not remove Nexus architecture just because SSO approval is pending.

Important scope note:

Nexus experimental gating is a locked future/v1.0-related direction, but it is not part of the next beta refactor phase ordering unless a specific phase later needs to preserve or document the decision.

### Decision 15 - Nexus API Strategy

Do not re-decide Nexus API strategy in this refactor planning pass.

CalradiaForge already has Nexus API planning and strategy documentation. Existing documentation should remain the source of truth for REST/GraphQL/API abstraction decisions unless explicitly reopened later.

Use the existing Nexus architecture/API planning documents as source of truth for:

- REST vs GraphQL usage
- Nexus API abstraction boundaries
- authentication flow assumptions
- downloader/service architecture
- metadata/update-check behavior
- NXM handler planning

This refactor planning documentation may reference those docs, but must not replace or redesign the Nexus API strategy.

### Decision 16 - BLSE Safety / Game Bin File Handling

Use strict BLSE file validation with an allowlist for install/update, while allowing direct overwrite of approved BLSE files.

Refined rule:

BLSE files are additional script extender files, not normal game files that replace official Bannerlord files. CalradiaForge does not need a heavy rollback system for this immediately, but it does need stronger validation before extracting anything into the game bin folder.

Rules:

- BLSE install/update may directly overwrite existing approved BLSE files.
- CalradiaForge must validate extracted filenames before placing files into the game bin folder.
- Only known BLSE-related filenames and expected folder structure should be allowed.
- Unexpected files must be blocked and reported.
- The app should log which BLSE files were accepted, skipped, blocked, or overwritten.
- User-facing status/toast messages should clearly explain blocked unsafe files.
- Tests should verify allowed BLSE files, blocked unexpected files, and update overwrite behavior.

### Decision 17 - Archive Validation / Mod Identity Preflight

Use module identity preflight now, with phased growth into fuller archive safety preflight later.

Initial archive preflight should inspect:

- whether the archive can be opened
- module folder name
- `SubModule.xml`
- mod id/name
- version if available
- whether it looks like a valid Bannerlord module
- whether it targets a reserved or official module folder

Future phased growth may add:

- path traversal checks
- suspicious executable/script checks
- duplicate module detection
- multi-module archive handling rules
- install destination preview
- blocked/reserved folder policy refinement
- warnings before risky overwrite behavior

### Decision 18 - Documentation Output

Use a master-plan-plus-supporting-docs structure.

Required:

- one main refactor master plan
- focused supporting documents for detailed policies
- clear marking of deferred, skipped, or future/v1.0 topics

### Decision 19 - Logger Modernization

Move CalradiaForge to Serilog with a file sink, using a staged migration.

The logger modernization should use:

- Serilog as the logging implementation
- Serilog file sink for app log files
- rolling log files
- retention limits
- structured message templates
- `SourceContext` / class context where practical
- redaction before logging sensitive or user-provided runtime values
- debug-level logs controlled by Serilog configuration instead of repeated manual `if debug enabled` checks

The migration should reduce patterns like:

```csharp
if debug enabled, then log debug message
```

Normal debug logging should use:

```csharp
logger.Debug(...)
```

Only expensive diagnostic construction should be guarded with:

```csharp
logger.IsEnabled(LogEventLevel.Debug)
```

The migration must preserve the strict secret-boundary policy:

- no raw auth headers
- no raw bearer tokens
- no raw API keys
- no raw credential objects
- no secret-bearing URLs
- Nexus credentials remain inside the Nexus credential/auth boundary
- debug logs must still be redacted

The existing custom logger may be temporarily adapted or bridged during migration, but the final target is Serilog-based logging.

### Decision 20 - Refactor Phase Ordering Model

Use a compact-refactors-first hybrid model.

Phases should be ordered by:

1. implementation size and blast radius
2. risk reduction value
3. architecture dependency order
4. speed of implementation

The plan should prefer cleanup and compact improvements before large architecture rewrites.

Major refactors such as DI and MVVM should be staged after smaller cleanup, logging, persistence, installer safety, and initial testing work where practical.

Nexus experimental gating and v1.0 release-readiness polish are not part of this phase ordering model. Those belong to later v1.0-focused planning.

Suggested high-level order:

1. Cleanup and low-risk consistency fixes
2. Serilog migration and logging cleanup
3. Small safety refactors: BLSE allowlist, module preflight, simple persistence atomic writes
4. Initial tests around changed risky areas
5. DI and platform adapter foundation
6. Result types and workflow coordinator
7. Staged MVVM shell/viewmodel rewrite
8. Final documentation/versioning cleanup for the next beta release

### Decision 21 - Next Beta Release Scope

Use refactor plus architecture plus selected UX improvements.

The next CalradiaForge beta release will focus on refactor, architecture, safety, testing, persistence, logging, and selected UX improvements.

Included:

- architecture refactor
- Serilog migration
- cleanup and consistency fixes
- persistence safety improvements
- BLSE allowlist validation
- archive/module identity preflight
- initial testing coverage
- DI foundation
- platform adapter foundation
- workflow coordinator/result type improvements
- staged MVVM shell/viewmodel rewrite
- Toast System/status/busy/error visibility improvements where needed
- version display cleanup
- documentation updates

Excluded:

- new Nexus implementation beyond preserving existing docs/architecture
- v1.0 release polish
- unrelated major new features

### Decision 22 - Final Phase Plan Detail Level

Use detailed work orders plus dependency rules.

Each phase should include the detail level defined earlier in this prompt.

### Decision 23 - Final Output Format

Final planning output should include both:

1. A locked decisions summary.
2. A pasteable Codex prompt for generating or updating the CalradiaForge refactor planning documentation.

Since this file is the pasteable Codex prompt, the generated documentation should include the locked decisions summary inside the appropriate master plan or appendix.

## Required Phase Plan Shape

Use this high-level phase order unless the repository scan reveals a strong reason to propose a minor adjustment. If an adjustment is needed, document the reason before changing the order.

### Phase 1 - Cleanup And Low-Risk Consistency Fixes

Purpose:

- Reduce noise before deeper refactoring.
- Clean obvious naming, stale comments, warnings, and simple duplication.

Include:

- naming/spelling fixes
- stale comments
- warning cleanup where low-risk
- README/changelog consistency notes
- simple dead-code review if clearly safe

Exclude:

- MVVM rewrite
- DI conversion
- installer behavior rewrites
- Nexus implementation

### Phase 2 - Serilog Migration And Logging Cleanup

Purpose:

- Replace the custom logger direction with Serilog.
- Reduce manual debug-level conditionals.
- Prepare structured logging for future Nexus and installer work.

Include:

- Serilog file sink plan
- rolling file and retention policy
- structured logging conventions
- `SourceContext` usage
- redaction integration requirements
- compatibility bridge if needed

Exclude:

- rewriting every call site in one uncontrolled pass
- logging secrets
- Nexus auth implementation

### Phase 3 - Small Safety Refactors

Purpose:

- Improve safety in compact, high-value areas before larger architecture work.

Include:

- BLSE allowlist validation
- module identity preflight
- simple persistence atomic writes
- backup recovery for important JSON files
- blocked/reserved module folder planning

Exclude:

- full installer policy finalization if Bannerlord-specific constraints still need explanation
- full archive security scanner
- full persistence migration framework

### Phase 4 - Initial Tests Around Changed Risky Areas

Purpose:

- Add tests around workflows touched by earlier safety changes.

Include:

- Core unit tests
- integration-style tests for file-system-heavy workflows
- BLSE allowlist tests
- persistence save/load/recovery tests
- archive preflight tests

Exclude:

- full WPF UI automation
- exhaustive test suite rewrite

### Phase 5 - DI And Platform Adapter Foundation

Purpose:

- Create a cleaner composition model for services, adapters, ViewModels, workflows, and future Nexus services.

Include:

- `Microsoft.Extensions.DependencyInjection`
- service registration conventions
- lifetime rules
- platform adapter boundaries
- test-friendly service replacement

Exclude:

- Host Builder unless justified later
- service locator misuse
- cross-platform UI rewrite

### Phase 6 - Result Types And Workflow Coordinator

Purpose:

- Make important operations report success, failure, warnings, and user-facing messages consistently.
- Give long-running install workflows one clear coordination layer.

Include:

- workflow-specific result types
- install workflow coordinator
- status/error/toast integration
- cancellation/progress/completion model

Exclude:

- app-wide generic result pattern unless repetition justifies it
- WPF-specific dependencies in Core

### Phase 7 - Staged MVVM Shell/ViewModel Rewrite

Purpose:

- Move the WPF UI toward a consistent MVVM architecture across the full app.

Include:

- MVVM conventions
- base ViewModel patterns
- command patterns
- shared UI state
- shell/navigation refactor
- high-risk workflow ViewModels
- remaining page ViewModels
- ViewModel tests as ViewModels stabilize

Exclude:

- uncontrolled full UI rewrite in one pass
- unrelated visual redesign
- unrelated new features

### Phase 8 - Final Documentation And Versioning Cleanup For Next Beta

Purpose:

- Prepare the next beta release documentation and version consistency.

Include:

- documentation updates
- version display cleanup
- `Directory.Build.props` version source-of-truth planning
- changelog/release-note alignment
- release-title pattern verification

Exclude:

- v1.0 release polish
- new Nexus implementation
- major/minor version bump without owner approval

## Codex Guardrails

Codex must:

- read the codebase and existing docs before writing final docs
- treat this prompt as locked planning direction
- preserve existing Nexus API strategy docs
- avoid source-code implementation unless explicitly asked
- keep Core independent from WPF
- keep secrets out of AppConfig
- preserve strict secret-boundary rules
- avoid changing major/minor version values without explicit owner approval
- avoid bundling multiple major architecture changes into one phase
- clearly mark future/v1.0 topics as future/v1.0 topics

Codex should:

- use markdown tables for mappings and phase plans
- write junior-friendly explanations where useful
- include verification expectations for each phase
- list open questions separately from locked decisions
- include "out of scope" sections so future tasks do not accidentally expand

## Deliverable Expectations

After generating or updating the docs, Codex should report:

- files created or changed
- any existing docs that were used as source of truth
- any source-code observations that changed the plan
- any unresolved questions
- any topics intentionally deferred

