<!--
Historical evidence migrated from the preserved legacy documentation set.
This report retains its original claims for traceability; current source and current verification govern present behavior.
Original preserved path: read_only_old_docs/audits/calradiaforge_source_state_deep_audit_2026-07-21.md
-->
# CalradiaForge Source-State Deep Audit

Status: Pre-Phase 6.A readiness audit

Audit date: 2026-07-21

Branch / commit: `dev-V0-14-CodeRefactor` / `53a93dd322f9f252933d3f9323f880e18243e7d0`

Scope: Entire repository source, solution/project configuration, tests, benchmarks, localization assets, documentation, and Phase 6.A planning readiness. This is an audit-only report. No application source, existing documentation, project, test, or configuration file was modified.

## Executive Summary

The repository has a healthy executable baseline: the full Debug solution build passes and all 76 currently discovered tests pass. Phase 5 appears implemented in source, but Phase 6.A has not begun: the WPF application still starts with `StartupUri`, manually constructs services in `App.xaml.cs`, exposes them through static `App.*` properties, uses no dependency-injection package or composition root, and does not own Serilog construction or shutdown.

The principal Phase 6.A risk is lifecycle design rather than registration syntax. The current `ModInstaller` starts fire-and-forget worker work without an awaitable completion/disposal contract, while app shutdown only requests cancellation. The future composition root must establish a single provider, an explicit service/lifetime matrix, a quiescent shutdown sequence, and one logger close/archive path before it starts replacing static access. It must not migrate legacy `Logger.Instance` callers; the plan reserves that work for Phase 6.B.

The documentation is generally useful for current Phase 3 and Phase 5 behavior, but the refactor plan and related documents have material phase-label and status drift. Treat live code and the current Phase 6.A section as authoritative for the implementation task.

## Audit Method And Evidence

- Read `AGENTS.md`, the current refactor master plan, Phase 6.A supporting plans, architecture documents, policy documents, project files, source, tests, benchmarks, localization manifests, and existing audits.
- Applied `docs/refactor/task_execution_optimization_policy.md` to three independent, read-only evidence tracks: Core/Nexus; UI/tests/build/benchmarks; and documentation accuracy. No agent made a repository change.
- Inspected all 98 authored C# files, 18 XAML files, six projects, solution configuration, and authored documentation. `CalradiaForge.Nexus` contains a project boundary but no authored implementation files.
- Ran `dotnet build source/CalradiaForge.slnx -c Debug --no-restore` and `dotnet test source/CalradiaForge.slnx -c Debug --no-restore --no-build`.
- Ran source searches for service composition, static service access, legacy logging, comment markers, and boundary violations. Checked Git status and `git diff --check`.

## Repository Baseline

| Item | Current state | Audit significance |
|---|---|---|
| Branch / HEAD | `dev-V0-14-CodeRefactor` / `53a93dd` | This is the source baseline for the report. |
| Working tree | Pre-existing modification only: `docs/refactor/refactor_master_plan.md` | The owner change marks Phases 3-5 complete. It was preserved and is not attributable to this audit. |
| Solution build | Passed: 0 errors, 420 warnings | The code is buildable. Warnings are dominated by the intentional obsolete legacy `Logger` compatibility path. |
| Test run | Passed: 76 passed, 0 failed, 0 skipped | Current behavioral tests are executable, but are Core-only. |
| Test coverage shape | 72 `[Fact]`/`[Theory]` declarations; no UI/DI/lifecycle/shutdown test suite | Phase 6.A needs targeted composition and lifecycle tests. |
| Benchmarks | Five methods in four Core fixtures | The runner writes artifacts and was not run. UI/Nexus benchmark folders are reserved/empty. |
| Source counts | 98 C#, 18 XAML, 6 project files, 1 solution | Complete authored solution inventory. |
| Dependency baseline | `net10.0` Core/tests/console/benchmarks; UI `net10.0-windows7.0` | No `Microsoft.Extensions.DependencyInjection` package is present. |

No Core WPF reference was found. UI currently references Core only; Nexus references Core only. Nexus networking, authentication, downloader, and API implementation are not present.

## Phase 6.A Readiness

### Current Composition State

Phase 6.A is correctly still unchecked and is not implemented in the source tree.

- `source/CalradiaForge.UI/App.xaml` retains `StartupUri="Views/MainWindow.xaml"`.
- `App.xaml.cs` manually builds the application graph and exposes static service properties, including settings, mod services, launcher, toast service, and translator (`App.xaml.cs:23-33`, `:93-241`).
- There are 67 static `App.*` service references in authored C#: 28 in `ModsPage`, 21 in `SettingsPage`, 12 in `ModpacksPage`, and 6 in `MainWindow`.
- `MainWindow` directly constructs pages; constructors do not yet form a DI-resolved object graph.
- No `IServiceCollection`, `ServiceProvider`, `AddSingleton`, `AddTransient`, or `Microsoft.Extensions.DependencyInjection` references exist outside the planning documentation.
- Neither Core nor UI references the DI package; there is no `Directory.Build.props`, `Directory.Build.targets`, `global.json`, or `NuGet.config` that currently supplies one indirectly.

This matches the Phase 6.A plan's intended start point. The correct first implementation boundary is one application composition root in `App.xaml.cs`, not a broad service rewrite.

### Logger And Shutdown Readiness

| Finding | Severity | Evidence and impact |
|---|---|---|
| No application-owned Serilog lifecycle | High | `SerilogLoggerFactory.Create()` creates an isolated logger without retaining it; startup does not resolve it. There are 43 `Logger.Instance` references and the legacy path is still active. |
| Shutdown is not quiescent | High | `App.OnExit` requests install cancellation and saves state but does not await installer work, close the logger, release the active handle, or archive a completed session log. |
| Installer lacks an awaitable lifetime contract | High | `ModInstaller` owns fire-and-forget `Task.Run` work, mutable cancellation state, and worker-thread callbacks. Cancellation can race its completion/disposal path. |
| Static/global access survives service constructors | High | Even services with explicit constructor dependencies still use `Logger.Instance`, leaving an implicit dependency that DI cannot replace in Phase 6.A. |
| Retention and archival choices remain unresolved | Planning gate | The setting says `RetainedFileCount`, current cleanup interprets it as days, and active-file size/rolling, logger ownership, global Serilog use, and collision-safe archive naming remain undecided. |

The Phase 6.A implementation must choose factory-owned versus DI-owned logger disposal, not both. It must clean up before opening the active file, quiesce logging-producing work, close exactly once, release the non-shared file handle, archive without overwrite, preserve the active file on archive failure, and ensure provider disposal cannot close a second logger.

### Service And Concurrency Risks To Preserve/Test

- `ModInstaller` and `ModService` expose mutable state consumed by the UI while worker operations can write it. Define event-thread and dispatcher ownership before assigning singleton lifetimes.
- Upgrade installation deletes an installed module before copying the replacement. Cancellation or copy failure can leave the installation absent or incomplete; BLSE direct overwrite has the same rollback limitation. This is not Phase 6.A scope, but its behavior must be preserved and covered when lifetime/shutdown behavior changes.
- Archive preflight has strong path-containment checks and controlled temporary-directory cleanup, but `ArchiveFile.Extract(...)` needs a real late/native extraction-failure test before lifecycle changes rely on it.
- `TranslationManager` is the persistence outlier: default-language generation uses direct `File.WriteAllText` while configuration and cache persistence use atomic helpers.
- Automatic platform detection is intentionally Steam-first: Epic detection is unreachable while `_enableUnsupportedPlatforms` is false, and Epic/GamePass launch-config support remains deferred. Do not portray those flows as complete merely because their types exist.

## Findings

| ID | Severity | Finding | Required Phase 6.A disposition |
|---|---|---|---|
| F-01 | High | No DI composition exists; static `App.*` acts as the live service locator. | Implement one collection/provider and migrate only the approved construction consumers. |
| F-02 | High | `StartupUri` plus direct page/window construction would conflict with a DI-resolved `MainWindow`. | Define the DI-safe EULA/language-dialog/startup sequence; remove or replace `StartupUri` when DI startup activates. |
| F-03 | High | Shutdown cannot await active installer work or establish logger/archive ordering. | Define and test app-lifetime task, cancellation, quiescence, disposal, and archive ownership before implementation. |
| F-04 | High | Serilog factory is unused/non-retaining while the obsolete logger remains widely used. | Register one factory/shared logger only; defer caller migration to 6.B. |
| F-05 | Medium | UI pages/windows are static-service coupled and `MainWindow` owns page construction. | Inventory constructor/lifetime/state needs and use constructor injection in the selected Phase 6.A surface. |
| F-06 | Medium | Core state and callbacks cross background/UI boundaries without an explicit synchronization contract. | Document UI dispatch and singleton/thread-safety expectations; test close-during-work behavior. |
| F-07 | Medium | Existing install operations are destructive on upgrade and lack rollback. | Keep this out of DI scope; add regression coverage around cancellation/failure before changing owner lifetime. |
| F-08 | Medium | Documentation contains Phase 5 status and Phase 5.A/5.B label drift. | Record the current discrepancies and schedule a separate, post-implementation documentation reconciliation. |
| F-09 | Low | Nexus is an empty reserved boundary; platform support is intentionally incomplete. | Do not invent Nexus registrations or treat Epic/GamePass workflows as supported. |
| F-10 | Low | Version metadata is duplicated at `0.12.15` while the latest changelog is `0.13.35`. | Keep out of Phase 6.A unless an owner opens versioning scope; record as release-documentation debt. |

## Internal Code Comment Inventory

The repository has 2,789 source comment lines: 2,410 C# XML documentation lines, 217 ordinary authored C# `//` lines, and 162 XAML layout/section comments. There are no C# block-comment lines. The ordinary comments are 105 Core, 111 UI, and 1 test-fixture line; 13 generated-resource lines are included in the UI total. Most comments explain behavior or group UI/layout code and are not work items.

The three explicit source TODO markers are:

| Location | Comment/debt | Relevance |
|---|---|---|
| `source/CalradiaForge.Core/Infra/Launch/GameLauncher.cs:15-22` | Implement Epic/GamePass launcher configuration after researching the true file contract. | Deferred platform support; keep out of Phase 6.A. |
| `source/CalradiaForge.Core/Models/ModulesModel.cs:40` | Placeholder for advanced load-order/sorting features. | Future product work, not composition scope. |
| `source/CalradiaForge.Core/Models/ModulesModel.cs:47` | Second advanced load-order/sorting placeholder. | Future product work, not composition scope. |

Other actionable or stale internal comments/documentation are:

| Location | Concern | Recommended disposition |
|---|---|---|
| `App.xaml.cs:26` | Unused `object`-typed `ModManagerService`/Nexus placeholder; no other repository reference. | Resolve or explicitly defer before defining the service matrix; do not register an untyped placeholder. |
| `App.xaml.cs:181`; `MainWindow.xaml.cs:90,94` | Startup notification queue comments defer abstraction/disposal, although a queue service already exists. | Specify ownership and drain/disposal behavior in the startup/shutdown design. |
| `TranslationService.cs:12` | XML documentation says DI while `App` manually constructs the service. | Correct only in the future accepted documentation/code change; treat it as currently stale. |
| `SettingsPage.xaml.cs:226,232,248,254,260,533,537,574-575,585-586,601` | Hard-coded UI copy is annotated as needing translation resources. | Defer unless the selected 6.A constructor/UI work touches the same flow. |
| `ModExtractor.cs:11-13` | Obsolete commented SharpCompress imports remain. | Do not remove as part of Phase 6.A unless separately approved. |
| `Logger.cs:9` | Retirement is described as Phase 5.B; the active plan now names 6.B. | Correct in the same approved documentation/code-change scope that establishes the final migration. |
| `ModExtractor.cs:36` | XML comment says a minimum estimate of 10 while valid archive behavior can return 1. | Create a narrow documentation/test follow-up; not DI scope. |

No `FIXME`, `HACK`, or `XXX` markers were found. Searches containing natural-language strings such as â€œbugâ€ were excluded from the tracker count because they are user-facing text or XML documentation rather than action markers.

## Documentation Accuracy Compared With Source

Overall assessment: **partially accurate**. The planning documents correctly identify Phase 6.A as unimplemented and state its core guardrails, but the documentation set contains stale phase labels, conflicting Phase 5 status statements, historical reports that are no longer current-source evidence, and metadata drift.

| Documentation area | Assessment | Evidence |
|---|---|---|
| `refactor_master_plan.md` Phase 6.A section | Accurate as planned architecture | Its unchecked status, single-provider requirement, no-`StartupUri` rule, and 6.B logger-migration boundary match source. |
| Phase 5 status in the master plan | Contradictory | The current owner change marks Phase 5 complete, while other plan sections still call it planned/not implemented. Source and the Phase 5 implementation record support completion. |
| `dependency_injection_plan.md` | Accurate as a future-state plan | It explicitly says DI is planned and correctly describes the current static graph/factory limitations. |
| Logging plan/policy and architecture logging doc | Partially stale | The plan understands factory/lifecycle gaps, but the architecture log document and changelog/migration-map retain obsolete â€œPhase 5.A/5.Bâ€ labels rather than 6.A/6.B. |
| Architecture dependency diagram | Partially stale | It depicts `UI -> Nexus`, but `CalradiaForge.UI.csproj` currently references Core only; the Nexus edge is planned, not implemented. |
| Core project architecture document | Incomplete | Installed Serilog package dependencies are omitted. |
| README/version surface | Stale | Project version metadata remains `0.12.15`; the newest changelog section is `0.13.35`. |
| Historical July 7 audit | Historical only | Its redaction and non-atomic-persistence recommendations were superseded by later decisions and implementation. It must not guide Phase 6.A. |
| Phase 5 plan/handoff checklist | Stale status | They still label completed Phase 5 work as planned. |
| Markdown links and localization docs | Accurate | No unresolved local inline Markdown links were found; the language manifest exactly matches the nine language JSON files. |
| `DOCUMENT_SITEMAP.md` | Incomplete inventory | It omits three current Steam multi-library reports and `calradiaforge_phase_5_implementation_2026-07-16.md`. This report is also intentionally not added because existing docs were read-only for this audit. |

The audit does not alter the existing documents. Post-Phase 6.A documentation work should reconcile accepted implemented behavior only, rather than retrospectively rewriting historical reports.

## Required Pre-Phase 6.A Decisions And Checklist

1. Approve one service/lifetime matrix for `AppConfig`, `AppConfigSettings`, `ModService`, `ModInstaller`, `GameDetectionService`, `StartupNotificationQueue`, `TranslationManager`, toast/navigation services, legacy logging, and the shared Serilog logger.
2. Decide the logger lifecycle: factory-owned or provider-owned disposal, direct DI ownership or intentional `Serilog.Log.Logger` use, retention semantics, active-file size behavior, collision-safe archive format, and failure reporting.
3. Define the shutdown sequence: stop new work, request cancellation, await or confirm installer/workflow completion, dispose log-producing services, close the logger once, release the active handle, archive safely, then dispose the provider.
4. Define the DI-safe startup sequence for the language selector, EULA, settings bootstrap, one DI-resolved `MainWindow`, and WPF shutdown mode. Do not leave `StartupUri` active when DI starts the window.
5. Select the initial Core/UI registration module filenames and registration ownership without creating a second provider or registering WPF types from Core.
6. Add focused tests before declaring completion: one provider/singleton identity, constructor resolution, no duplicate window, factory invocation/cleanup timing, logger exact-once close, handle release/archive collision/failure preservation, close during an active install, and adapter substitution.
7. Inventory the 67 static service accesses and classify each as Phase 6.A constructor injection, an explicitly deferred migration, or an intentional static application concern. Keep all legacy `Logger.Instance` caller migration in Phase 6.B.
8. Preserve current Phase 5 platform behavior and its test coverage. Register finalized dependencies; do not redesign Steam detection, scan behavior, Nexus, or Epic/GamePass flows as part of this phase.

## Verification Matrix For Phase 6.A

| Scenario | Required evidence before completion |
|---|---|
| Provider construction | Exactly one `IServiceCollection` and one application `ServiceProvider`; Core/UI registration modules extend the same collection. |
| Main window startup | `MainWindow` resolves once through DI; `StartupUri` cannot create a second unmanaged window. |
| Singleton identity | Settings, configuration, Core app services, factory, shared logger, and app-wide UI services resolve to the intended single instances. |
| Adapter replacement | Tests replace selected platform/path dependencies without WPF leaking into Core. |
| Logger startup | Cleanup occurs once before the active sink opens; factory constructs/returns exactly one shared logger. |
| Normal shutdown | Logging work is quiescent, close occurs exactly once, the handle releases, and provider disposal does not cause another close. |
| Archive failures | Missing active file, collision, access/move failure, and success are distinguishable; collisions never overwrite and failures preserve the active file. |
| Install during exit | Cancellation and completion semantics are deterministic; no worker-thread callback reaches disposed UI/logging infrastructure. |
| Regression baseline | Solution build, all tests, Core-boundary check, and targeted lifecycle tests pass. |

## Conclusion

Phase 6.A can begin after the owner resolves lifecycle and ownership decisions listed above. The codebase is buildable and the Phase 5 platform work has a passing Core test baseline, but there is no existing DI implementation to extend. The safe implementation sequence is composition/lifetime design, narrowly scoped registration and startup changes, lifecycle tests, then manual WPF startup/shutdown validation. Legacy logger caller migration, unrelated installer rollback changes, Nexus implementation, and full MVVM conversion remain out of scope.

This report is the latest source-state audit and should be retained under the existing audit-retention rules until its findings are addressed, worked, or explicitly dispositioned.

