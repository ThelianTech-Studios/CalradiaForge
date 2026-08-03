> Historical audit evidence migrated from the preserved documentation snapshot on 2026-08-02. This report retains its original scope, baseline, and limitations; it is not authoritative for current implementation behavior.

# CalradiaForge Codebase Deep Audit Summary

> Historical audit. Recommendations to add automatic logging redaction, path sanitization, or an AppConfig secret-key denylist are historical findings from 2026-07-07 and were superseded on 2026-07-12. They are not current implementation requirements.

Generated: `2026-07-07 15:14 local time (America/Denver)`
Repository audited: `T:\ThelianTech\ThelianTech-Studios\ProjectsDirectory\CalradiaForge`
Audit folder: `docs/audit_07-07-26/`
Audit report: `docs/audit_07-07-26/calradiaforge_codebase_audit_07-07-26.md`
Scope: Full repository audit covering source code, project structure, docs, tests, build health, security posture, maintainability, architecture, and future development direction.
Mode: Audit-only. No source code or project files were modified.

Primary finding count: Critical 0, High 7, Medium 17, Low 9, Cleanup 7.

## 1. Executive Summary

CalradiaForge is a coherent WPF desktop launcher/mod manager with a clear Core/UI split and unusually strong architecture documentation for a small desktop project. The main workflows are implemented around mod scanning, modpack persistence, archive installation, BLSE special-case installation, launch behavior, localization, logging, and settings. The `CalradiaForge.Nexus` project exists as a reserved boundary, but there is no Nexus runtime implementation yet.

The project is buildable on the audited machine with .NET SDK `10.0.301`, but it is not yet clean: a no-incremental build succeeds with 173 warnings, including nullable warnings, platform-specific API warnings, unreachable-code warnings, and an AMD64/MSIL warning for `SevenZipWrapper`. There are no checked-in tests or CI workflows. That means continued feature work is possible, but major new feature development, especially Nexus download support, should wait until the archive/install trust boundary, persistence safety, warning baseline, and test strategy are improved.

The strongest areas are the documented architecture boundaries, Core ownership of runtime behavior, service/data-helper splits, and the decision to keep Nexus optional and outside Core. The riskiest areas are permissive archive/BLSE installation behavior, non-atomic JSON persistence, page-as-viewmodel UI structure, async lifetime complexity, and roadmap/documentation drift.

| Area | State | Readiness |
|---|---|---:|
| Build health | Builds successfully, but no-incremental build reports 173 warnings and `SevenZipWrapper` architecture mismatch. | Medium |
| Architecture boundaries | UI/Core separation is mostly respected; Nexus remains empty/reserved. | High |
| Maintainability | Services are understandable, but large code-behind pages and static app service access limit testability. | Medium |
| Security posture | No secrets found; archive/install and future Nexus redaction need hardening. | Medium |
| Test coverage | No test project checked in; `dotnet test` runs no tests. | Low |
| Documentation alignment | Architecture docs are strong; README/changelog/TODO have stale or conflicting areas. | Medium |
| Future development readiness | Safe for focused fixes; risky for major Nexus/download expansion without hardening. | Medium |

## 2. Repository Inventory

| Item | Current State |
|---|---|
| Solution | `source/CalradiaForge.slnx` |
| Tracked files | 125 tracked files |
| Application type | WPF Windows desktop app |
| Main target frameworks | `net10.0-windows7.0` for UI, `net10.0` for Core/Nexus/ConsoleUtils |
| Tests | None checked in |
| CI | `.github/ISSUE_TEMPLATE` exists; no `.github/workflows` files found |
| Source style config | `source/.editorconfig` |
| Package management | Per-project `PackageReference`; no central package management or lock file |
| SDK pinning | No `global.json` |

| Project | Type | Target Framework | References | Packages |
|---|---|---|---|---|
| `CalradiaForge.UI` | WPF app | `net10.0-windows7.0` | Core | `gong-wpf-dragdrop 4.0.0`, `MahApps.Metro 2.4.11`, `MahApps.Metro.IconPacks.Material 6.2.1` |
| `CalradiaForge.Core` | Runtime library | `net10.0` | None | `Newtonsoft.Json 13.0.4`, `SevenZipWrapper 0.9.5-beta` |
| `CalradiaForge.Nexus` | Reserved integration library | `net10.0` | Core | `Newtonsoft.Json 13.0.4` |
| `CalradiaForge.ConsoleUtils` | Developer utility | `net10.0` | Core | None |

| Major Folder | Contents / Role |
|---|---|
| `source/CalradiaForge.UI` | WPF startup, windows, pages, resources, toasts |
| `source/CalradiaForge.Core` | Config, paths, logging, localization, mod/modpack services, launch, models |
| `source/CalradiaForge.Nexus` | Empty scaffold folders for future auth/API/downloader |
| `source/CalradiaForge.ConsoleUtils` | Small utility entry point |
| `source/Languages` | Localization manifest and JSON translations |
| `docs/Architecture` | Architecture index, project docs, system docs, Nexus architecture |
| `docs` | EULA, changelog, contributions, Nexus concept plan, TODO |
| `.github/ISSUE_TEMPLATE` | Bug report and feature request templates |

Ignored local artifacts exist, including `.vs/`, `source/.vs/`, `bin/`, `obj/`, `source/releases/`, and `Calradia Forge Collection/`. They are ignored and untracked, so they are local workspace clutter rather than repository source.

## 3. Build, Test, and Tooling Results

| Command | Result | Notes |
|---|---|---|
| `git status --short` | Pass | Clean before report creation. |
| `rg --files --hidden ...` | Pass | Used for inventory; hidden/ignored generated artifacts inspected separately. |
| `dotnet --info` | Pass | SDK `10.0.301`; no `global.json`. |
| `dotnet build source\CalradiaForge.slnx` | Pass after network approval | Initial sandbox restore failed with `NU1301`; unrestricted rerun passed with 4 repeated `MSB3270` warnings. |
| `dotnet build source\CalradiaForge.slnx --no-restore --no-incremental` | Pass | Passed with 173 warnings, 0 errors. |
| `dotnet test source\CalradiaForge.slnx` | Pass / no tests | Command completed after restore, but no test projects or test results were present. |
| `dotnet list source\CalradiaForge.slnx package` | Pass after network approval | Package inventory completed. |
| `dotnet list ... reference` per project | Pass | UI, Nexus, ConsoleUtils reference Core. |
| `dotnet format source\CalradiaForge.slnx --verify-no-changes --no-restore` | Fail | Verify-only failed on final-newline formatting and platform analyzer warnings; no fixes applied. |

Build warnings worth treating as audit findings:

| Warning Area | Example Location(s) | Severity | Notes |
|---|---|---:|---|
| AMD64/MSIL mismatch | `source/CalradiaForge.Core/CalradiaForge.Core.csproj`, `SevenZipWrapper` | High | `SevenZipWrapper.dll` is AMD64 while projects build as MSIL/AnyCPU. This can fail outside x64 execution. |
| Nullable warnings | `TranslationStrings.cs`, `ModService.cs`, `ModpackService.cs`, `ModInstaller.cs` | Medium | No-incremental build reports many `CS8618`, `CS8601`, `CS8620`, `CS8714` warnings. |
| Windows-only API warnings | `ModExtractor.cs`, `EpicDetector.cs`, `GamePathsHelper.cs` | Medium | Core targets `net10.0`, but registry and SevenZip APIs are Windows-only. The app is Windows-only, but the project target does not communicate that. |
| Unreachable code | `GamePathsHelper.cs` | Low | Disabled/deferred Epic path leaves unreachable-code warnings. |
| Format verify failures | Many `.cs` files | Cleanup | `.editorconfig` has `insert_final_newline = false`, and `dotnet format` reports final-newline differences. |

## 4. Architecture and Project Boundary Audit

| Component / Project | Current Responsibility | Boundary Health | Issues | Recommendation |
|---|---|---|---|---|
| `CalradiaForge.UI` | WPF startup, pages, navigation, resource dictionaries, toasts, user intent | Good with maintainability risk | Large pages also hold viewmodel state and workflow glue. | Keep UI as intent/presentation owner, but gradually extract viewmodels/commands for largest pages. |
| `CalradiaForge.Core` | Runtime logic, models, config, paths, logging, localization, mod/modpack workflows, launch | Good | Core has no WPF references, but Windows-only APIs live in `net10.0` library and Nexus path names are pre-created here. | Preserve Core as runtime owner; document Windows platform assumptions and watch Nexus path ownership. |
| `CalradiaForge.Nexus` | Reserved Nexus boundary | Good but empty | Docs describe dependency direction and locked behavior, but implementation does not exist. | Keep networking/auth/downloader here when implemented; do not move it to Core. |
| `CalradiaForge.ConsoleUtils` | Developer utility reusing Core | Good | No concerns from current audit. | Keep developer-only and Core-consuming. |
| `ModInstaller` / `ModExtractor` | Authoritative archive install/extraction pipeline | Strong ownership, security hardening needed | Path containment and overwrite behavior need stricter contracts before Nexus downloads. | Preserve ownership, harden validation and add tests. |
| `ModsData` / `ModpackData` | File I/O for mod cache and modpack data | Mostly good | Direct writes are non-atomic; `ModpackService.ExportModpack` writes directly. | Add atomic JSON helper and route domain file writes through data helpers. |

Boundary classifications:

| Finding | Classification | Why |
|---|---:|---|
| Core has no WPF references | Strength | Preserves source-of-truth architecture rule. |
| Nexus has no networking/auth/download implementation | Strength / planned gap | No forbidden movement into Core, but no runtime behavior to audit yet. |
| UI does not reference Nexus today | Low | Docs show `UI -> Nexus`; source does not need this until Nexus has implementation. |
| Nexus download/cache path names in Core `AppPaths` | Low | Not a networking violation, but future Nexus metadata/cache ownership should be clarified. |
| Static `App.*` service access | Medium | Simple today, but weaker than explicit dependency passing and harder to test. |

## 5. Code Maintainability Audit

| Finding | Location(s) | Severity | Why It Matters | Suggested Refinement |
|---|---|---:|---|---|
| Large page-as-viewmodel classes | `ModsPage.xaml.cs` (1079+ lines before build-generated counts; no-incremental line ref 1237), `ModpacksPage.xaml.cs`, `SettingsPage.xaml.cs` | High | UI state, event handlers, service calls, and workflow logic are combined, making testing and future changes expensive. | Extract viewmodels/commands incrementally, starting with install/refresh/launch flows. |
| Fire-and-forget installer naming and lifetime | `ModInstaller.StartInstallAsync`, `ModsPage.OnInstallCompleted` | High | A method named async returns `bool`; completion uses `async void` and nested dispatcher async lambda, making exception/lifetime behavior harder to reason about. | Rename or return a tracked task/result, and centralize completion handling with observed async paths. |
| Non-atomic persistence writes | `AppConfig.cs`, `ModsData.cs`, `ModpackData.cs`, `ModpackService.cs` | Medium | Crash or partial write can corrupt config, mod cache, or modpacks. | Write temp file then replace, keep backup, handle corrupt JSON gracefully. |
| Mutable service state exposed | `ModService.CurrentMods`, `ModpackService.CurrentLoadOrderEntries` | Medium | Navigation/refresh/install can observe and mutate shared collections in surprising order. | Expose read-only snapshots or controlled mutation methods. |
| Mixed result/error styles | `GameLaunchResult`, nullable returns, bool saves, tuples, exceptions | Medium | Callers must remember different failure contracts across services. | Standardize result models for workflow services without overengineering simple helpers. |
| File I/O ownership inconsistency | `ModpackService.ExportModpack` direct `File.WriteAllText` | Medium | Architecture says data helpers own file I/O; exceptions make boundaries blur. | Move export write mechanics into `ModpackData` or a shared serialization helper. |
| Raw config values logged in debug mode | `AppConfig`, `AppConfigSettings` | Medium | Safe today only because secrets are forbidden from config; risky for future Nexus. | Add redaction/key blocking before Nexus implementation. |
| Hardcoded visible strings | UI code-behind, `LanguageSelectWindow`, Core result messages | Low | Localization is strong in XAML but incomplete in C# paths. | Localize user-facing strings in code-behind and route Core result keys/messages intentionally. |
| Naming/spelling drift | `SetupExceptionHandeling`, `LogUnhadledException`, `DefaulLanguageFileName`, `_DownloadsDirectory` | Cleanup | Reduces polish and searchability. | Rename opportunistically with tests or low-risk refactor. |

Small cleanup refinements:
- Fix naming/spelling drift and stale comments.
- Add real justifications to suppressions.
- Align README/changelog/repo URLs.
- Bind or remove unused status properties.

Medium refactors:
- Atomic JSON persistence and corruption recovery.
- Result/error contract cleanup.
- Explicit service construction or lightweight DI boundary.
- Read-only snapshots for shared service state.

Larger rewrite candidates:
- `ModsPage.xaml.cs` should be split after behavior is test-covered.
- `ModpacksPage.xaml.cs` should move editable working-copy logic to a viewmodel/service boundary.
- Archive/BLSE validation should be redesigned as a defensive preflight stage before Nexus downloads.

## 6. Professional Coding Standards Review

| Standard Area | Current State | Gap | Recommendation |
|---|---|---|---|
| Separation of concerns | Good between projects; mixed inside UI pages | Page code-behind has workflow and viewmodel state | Extract viewmodels gradually, preserving service ownership. |
| Dependency flow | Mostly clean | UI does not yet reference Nexus despite docs; static `App.*` service locator | Keep source minimal until Nexus exists; introduce explicit passing for new surfaces. |
| Naming | Mostly clear | Several typos/casing inconsistencies | Cleanup pass after tests or as low-risk PR. |
| Error handling | Present and user-facing in many workflows | Mixed result styles; global UI exception swallowing can hide broken paths | Standardize service results and surface UI failures visibly. |
| Logging | Simple and useful | No redaction layer; logs full paths/arguments/config values in debug | Add redaction and secret-key denylist before Nexus. |
| Testability | Core services are closer to testable than UI | No tests; static logger/AppPaths and page code-behind make isolated tests harder | Add Core tests first, then UI-viewmodel tests after extraction. |
| Async boundaries | Service-owned install is a good direction | Fire-and-forget and `async void` completion make failures harder to observe | Make long-running workflow lifetimes explicit. |
| File-system safety | Validators and app paths exist | Recursive copy/delete and archive extraction need containment contracts | Add canonical path checks, reserved names, backups, and tests. |
| Overengineering | Generally avoided | None significant | Preserve the small-service style while hardening contracts. |

## 7. Design Patterns and Structural Consistency

| Pattern | Classification | Notes |
|---|---|---|
| MVVM | Present but inconsistent | Pages implement `INotifyPropertyChanged` and act as viewmodels. This is practical but not scalable. |
| Commands | Missing and recommended | Most actions are event handlers; commands would help testability for install/refresh/launch/settings. |
| Services | Consistent and useful | Core service/data-helper split is one of the project strengths. |
| Dependency injection | Missing and recommended in lightweight form | `App.*` statics are simple but reduce explicit dependency passing. |
| Options/configuration | Consistent and useful | `AppConfig` plus `AppConfigSettings` is documented and implemented. |
| Result/error types | Present but inconsistent | `GameLaunchResult`, `BLSEInstallResult`, bools, nullables, tuples, and exceptions all coexist. |
| Repository/data helpers | Present but mildly inconsistent | `ModsData`/`ModpackData` are good; export direct writes should be aligned. |
| Event/message patterns | Useful but needs lifecycle care | Installer events keep work service-owned; UI subscriptions are manually managed. |
| Installer/download workflow | Strong current install pipeline | Nexus download pipeline is planned only; should hand off to existing installer. |

## 8. Stability and Reliability Audit

| Risk | Scenario | Current Handling | Severity | Recommended Refinement |
|---|---|---|---:|---|
| Partial JSON writes | App exits or crashes during config/cache/modpack write | Direct `File.WriteAllText` to final path | Medium | Atomic write + backup + corrupt-file quarantine. |
| Archive traversal/overwrite | Malformed archive contains hostile paths or reparse entries | Relies on extraction library behavior | High | Preflight entries and canonicalize destinations under extraction root. |
| Reserved module overwrite | Archive extracts to folder name matching vanilla/critical module | Existing folder may be deleted on upgrade path | High | Deny reserved names, verify parsed module identity, backup before replace, require confirmation. |
| BLSE broad overwrite | BLSE-like archive copies arbitrary flat files into game bin | Marker search plus overwrite copy | High | Strict allowlist, version/hash/source validation, backups. |
| Install completion exceptions | DLL unblock or refresh fails after install completion | Async dispatcher lambda can obscure exceptions | High | Observe inner task and put reset/cleanup in robust finally paths. |
| Global UI exception swallowing | UI exception occurs after handler | Logged and marked handled | Medium | Show user-facing failure state or fail fast for unrecoverable UI paths. |
| Missing game path | User has no valid Bannerlord folder | Settings validation and launch/install guards exist | Low | Keep; add tests around validators. |
| No tests for parser/install/cache | Refactor changes break core workflows | Manual validation only | High | Add focused Core tests before major new features. |
| SDK/package drift | Different machine uses different .NET 10 SDK/package restore | No `global.json` or lock file | Medium | Pin SDK and consider lock file for reproducibility. |

## 9. Security, Privacy, and Trust Audit

| Security Finding | Location(s) | Severity | Risk | Recommended Mitigation |
|---|---|---:|---|---|
| Archive extraction lacks explicit containment preflight | `ModExtractor.ExtractToTempAsync` | High | Trusts third-party extractor to reject traversal/reparse issues. | Validate archive entries before extraction; reject rooted/parent-traversing paths and enforce destination-under-root. |
| Module target overwrite/delete is too permissive | `ModInstaller.ProcessSingleArchiveAsync`, `SafeDeleteDirectory` | High | Malicious/malformed archives could overwrite important module folders. | Deny official module names, verify folder/ModuleId, backup before replace, and require explicit overwrite confirmation. |
| BLSE installer trusts archive content broadly | `BLSEInstaller.IsBLSEArchive`, `InstallAsync`, `CopyBinFiles` | High | Marker-based detection plus overwrite into game bin is powerful. | Add strict file allowlist/manifest, source validation, backup, and post-validation unblock. |
| No logger redaction layer | `Logger`, `AppConfig`, future Nexus logs | High | Future credentials or secret URLs could be logged accidentally. | Redact token-like data centrally and prohibit secret keys in `AppConfig`. |
| Plain JSON stores paths and module URLs | `ModuleModel`, `ModpackEntryModel`, mod cache/modpacks | Medium | Current data is non-secret; future Nexus secret URLs must not land here. | Keep Nexus metadata separate; persist stable public IDs only; DPAPI for credentials. |
| Cleanup helpers lack containment checks | `ModExtractor.CleanupTempDirectory`, `ModpackData.DeleteModpack` | Medium | Reusable delete helpers can delete based on caller-provided paths. | Resolve full path and verify intended root…2147 tokens truncated…Nexus credentials.
- Release/versioning source-of-truth policy.
- Persistence schema/migration policy.

### 14.2 Locked Decisions to Preserve

| Locked Direction | Source Doc(s) | Why It Should Be Preserved |
|---|---|---|
| UI decides when; Core decides how | `AGENTS.md`, solution architecture | Keeps presentation separate from runtime behavior. |
| Core remains free of WPF references | `AGENTS.md`, Core project doc | Preserves testability and layering. |
| Nexus networking/auth/downloader belongs in `CalradiaForge.Nexus` | `AGENTS.md`, Nexus architecture | Prevents Core from becoming network/security mixed. |
| Nexus integration is optional | Nexus architecture | Ensures app remains usable offline and without Nexus. |
| Credentials stored only with DPAPI CurrentUser | `AGENTS.md`, Nexus architecture | Correct security baseline for future Nexus. |
| `AppConfig` must never store secrets | `AGENTS.md`, Nexus architecture | Avoids secret leakage in plain JSON and debug logs. |
| `ModuleModel` must not store Nexus metadata | `AGENTS.md`, Nexus architecture | Protects current mod model contract. |
| `ModInstaller` and `ModExtractor` remain authoritative | Mod management docs, Nexus architecture | Prevents duplicate install logic in Nexus. |
| Update checks are manual only | `AGENTS.md`, Nexus architecture | Avoids hidden polling/startup scans. |
| Download queue is single-active-download | Nexus architecture | Sensible for bandwidth, rate limits, and user clarity. |

### 14.3 Future Development Direction

| Future Area | Current Documentation State | Implementation Readiness | Notes |
|---|---|---:|---|
| Nexus auth/API/downloads | Locked architecture, no code | Low | Needs owner decision on v1.0 vs post-v1 timing, security model docs, and tests. |
| NXM handling | Locked architecture, no code | Low | Must stay in Nexus boundary and require authentication. |
| Manual update checks | Locked architecture, no code | Low | Needs metadata model and Nexus-linked mod mapping first. |
| Download cache/metadata | Planned | Low | Existing `AppPaths` creates directories; runtime ownership still needs design. |
| Epic/GamePass backend | Deferred/planned | Low | Source blocks launch; docs say testing access is missing. |
| Modpack save improvements | TODO mentions remaining v1 work | Medium | Existing services/pages provide base, but persistence needs atomicity first. |
| WPF UX polish | README/open beta acknowledges rough edges | Medium | Status bindings, accessibility, and localization are practical next steps. |
| Testing strategy | Missing | Low | Should be created before major feature expansion. |
| Release/versioning | Stale/conflicting | Medium | Decide source of truth for version and changelog. |

### 14.4 Suggested Refinements

| Suggested Refinement | Why It Helps | Affected Area | Requires Owner Decision? |
|---|---|---|---|
| Decide Nexus timing: v1.0 or post-v1 | Resolves TODO vs architecture tension | Roadmap/docs/Nexus | Yes |
| Define archive trust model | Makes Nexus downloads safe before implementation | Core installer/extractor | Yes |
| Add test strategy doc | Provides contributor and release confidence | Docs/QA | No |
| Add release/versioning policy | Prevents changelog/project metadata drift | Docs/project files | Yes |
| Add UI architecture doc | Guides viewmodel/command extraction | UI/docs | No |
| Add logging/security model | Prevents secret leakage when Nexus begins | Core/Nexus/docs | No |

## 15. Code Rewrite / Refactor / Streamline Candidates

### 15.1 Keep As-Is

| Candidate | Type | Location(s) | Reason | Risk of Changing | Recommended Timing |
|---|---|---|---|---|---|
| Core/UI project split | Keep | `source/CalradiaForge.UI`, `source/CalradiaForge.Core` | Clear and working architecture. | High if churned unnecessarily | Keep stable |
| Nexus reserved boundary | Keep | `source/CalradiaForge.Nexus` | Correct future integration boundary. | Medium | Keep until implementation |
| `AppConfig` / `AppConfigSettings` concept | Keep | Core config | Documented source of truth. | Medium | Keep, harden internals |
| `ModInstaller` / `ModExtractor` authority | Keep | Core mods | Correct ownership; avoid duplicate install paths. | High | Keep, harden |
| Toast service structure | Keep | UI toasts | More structured than page-local toasts. | Low | Keep |

### 15.2 Small Refinements

| Candidate | Type | Location(s) | Reason | Risk of Changing | Recommended Timing |
|---|---|---|---|---|---|
| Naming/spelling cleanup | Refactor | `App.xaml.cs`, `AppPaths.cs` | Improves polish/searchability. | Low | After tests or small PR |
| Suppression justification | Document | `GlobalSuppressions.cs` | `<Pending>` is not useful. | Low | Soon |
| Stale comments | Update | `AppPaths.cs`, `ModpackData.cs` | Avoids misleading maintainers. | Low | Soon |
| README links | Update | `README.md`, settings/Faq URLs | Public-facing correctness. | Low | Before release |
| Status property binding audit | Update | UI XAML/pages | Makes progress/errors visible. | Medium | Soon |

### 15.3 Medium Refactors

| Candidate | Type | Location(s) | Reason | Risk of Changing | Recommended Timing |
|---|---|---|---|---|---|
| Atomic persistence | Refactor | `AppConfig`, `ModsData`, `ModpackData` | Prevents corruption/data loss. | Medium | Before major features |
| Result contract standardization | Refactor | Core services | Simplifies callers and tests. | Medium | Before broad refactors |
| Explicit service construction | Refactor | `App.xaml.cs`, pages | Reduces global service coupling. | Medium | Before UI expansion |
| Archive overwrite policy | Refactor | `ModInstaller` | Prevents destructive installs. | Medium/High | Before Nexus downloads |
| SDK/package reproducibility | Update | root/project config | Reduces machine drift. | Low/Medium | Before public release |

### 15.4 Major Rewrite Candidates

| Candidate | Type | Location(s) | Reason | Risk of Changing | Recommended Timing |
|---|---|---|---|---|---|
| Mods page viewmodel/commands | Rewrite/refactor | `ModsPage.xaml.cs` | Largest workflow surface; install/refresh/launch logic is hard to test. | High | After Core tests and behavior freeze |
| Modpacks page viewmodel/commands | Rewrite/refactor | `ModpacksPage.xaml.cs` | Working-copy logic and persistence actions should be testable. | Medium/High | After modpack tests |
| Archive validation preflight | Rewrite/refactor | `ModExtractor`, `ModInstaller`, `BLSEInstaller` | Trust boundary needs a defensive model before Nexus downloads. | High | Priority 1 |

## 16. Cross-Cutting Consistency Issues

| Issue | Location(s) | Severity | Recommendation |
|---|---|---:|---|
| Version drift between changelog and project metadata | `docs/CHANGELOG.md`, `.csproj` files | Medium | Establish version source of truth and update release process. |
| Nexus timing conflict | `docs/TODO_v1.md`, Nexus architecture docs | High | Owner decision: v1.0 target, post-v1, or locked/deferred. |
| Result/error style inconsistency | Core services | Medium | Standardize workflow result contracts. |
| Logging redaction absent | Logger/config/future Nexus | High | Add central redaction before Nexus. |
| Localization incomplete in C# | UI code-behind/Core result messages | Low | Audit visible strings and migrate high-use messages. |
| UI status state not consistently bound | `ModsPage`, `ModpacksPage` | High | Bind or remove status properties; make progress/errors persistent. |
| Platform targeting mismatch | Core `net10.0`, Windows APIs | Medium | Document Windows-only Core or retarget where appropriate. |
| File I/O ownership drift | `ModpackService.ExportModpack` | Medium | Route writes through data helpers. |
| Naming/spelling drift | Multiple files | Cleanup | Opportunistic cleanup. |

## 17. Development-Blocking Issues

| Blocker | Why It Blocks Development | Recommended Next Step |
|---|---|---|
| No tests for archive/install/cache/modpack workflows | Major new features can regress core data and file-system behavior silently. | Add a Core test project focused on parser, persistence, installer validation, and modpack workflows. |
| Archive/BLSE trust boundary not hardened | Nexus downloads would feed untrusted archives into a permissive installer. | Implement archive preflight, reserved-name protection, overwrite policy, and BLSE allowlist before Nexus downloads. |
| Nexus roadmap conflict unresolved | Team may build against conflicting v1/post-v1 expectations. | Project owner decides Nexus timing and updates TODO/architecture/README accordingly. |
| No CI | Build/test health is manual and local only. | Add Windows CI for build, format verify, and tests once tests exist. |

## 18. Priority Work Order

### Priority 1 - Must Resolve Before Major Feature Development

| Priority | Item | Severity | Why It Matters | Recommended Action | Affected Area |
|---|---|---:|---|---|---|
| 1 | Harden archive extraction and install containment | High | Prevents unsafe extraction/overwrite before Nexus downloads. | Refactor | Core mods |
| 1 | Add Core test project for parser/persistence/install/modpacks | High | Creates regression safety for risky workflows. | Test | Core |
| 1 | Resolve Nexus v1.0 vs deferred decision | High | Avoids building against conflicting roadmap. | Discuss First | Docs/Nexus |
| 1 | Add logging redaction and `AppConfig` secret guardrails | High | Prevents future secret leakage. | Refactor | Core/Nexus |
| 1 | Fix async install completion observability | High | Prevents stale UI state and hidden exceptions. | Refactor | UI/Core |

### Priority 2 - Should Resolve Before Expanding Features

| Priority | Item | Severity | Why It Matters | Recommended Action | Affected Area |
|---|---|---:|---|---|---|
| 2 | Atomic JSON persistence | Medium | Reduces config/cache/modpack corruption risk. | Refactor | Core data |
| 2 | Reduce no-incremental warning backlog | Medium | Improves confidence and keeps new warnings visible. | Update | Core/UI |
| 2 | Clarify Windows-only platform targeting | Medium | Aligns analyzer behavior with product reality. | Document/Update | Projects |
| 2 | Make UI status/busy/empty states visible | High | Users need persistent feedback, not only toasts. | Update | UI |
| 2 | Start viewmodel/command extraction for largest workflows | Medium | Improves testability before UI grows. | Refactor | UI |

### Priority 3 - Should Resolve Before Public Release

| Priority | Item | Severity | Why It Matters | Recommended Action | Affected Area |
|---|---|---:|---|---|---|
| 3 | Add Windows CI | Medium | Release confidence should not depend on one machine. | Test | GitHub |
| 3 | Pin SDK and consider package lock file | Medium | Reproducible builds. | Update | Build |
| 3 | Resolve version/changelog/source metadata drift | Medium | Public release metadata must be trustworthy. | Document/Update | Docs/projects |
| 3 | Add accessibility basics | Medium | Professional WPF release quality. | Update | UI |
| 3 | Fix public README/repo/license links | Low | Public-facing trust and usability. | Update | Docs/UI |

### Priority 4 - Cleanup / Polish

| Priority | Item | Severity | Why It Matters | Recommended Action | Affected Area |
|---|---|---:|---|---|---|
| 4 | Naming/spelling cleanup | Cleanup | Professional polish and searchability. | Refactor | Source |
| 4 | Stale comments cleanup | Cleanup | Avoids misleading future work. | Update | Source/docs |
| 4 | Localize remaining C# visible strings | Low | Improves language consistency. | Update | UI/Core |
| 4 | Add UI architecture doc | Low | Guides future contributors. | Document | Docs |

## 19. Items Requiring Project Owner Decisions

### Nexus Release Timing

Decision needed: Is Nexus Mods integration required for v1.0, or is it a locked post-v1 architecture target?

Why it matters: `docs/TODO_v1.md` says NexusModsAPI will be implemented for 1.0, while architecture docs emphasize no runtime implementation yet and defer several decisions.

Options:
- Option A - Nexus is v1.0 scope, but only after security/test hardening.
- Option B - Nexus is post-v1 scope; v1.0 focuses on stable current workflows.
- Option C - Nexus remains architecture-locked but implementation date is undecided.

Affected files/areas:
- `docs/TODO_v1.md`
- `docs/Architecture/Nexus/NEXUS_INTEGRATION_ARCHITECTURE.md`
- `docs/Nexus_Integration_Plan.md`
- `source/CalradiaForge.Nexus`

### Archive Overwrite Policy

Decision needed: Should CalradiaForge overwrite existing mod/game-bin files automatically, require confirmation, or backup and replace?

Why it matters: Current install and BLSE flows can overwrite important folders/files.

Options:
- Option A - Require explicit user confirmation for every overwrite.
- Option B - Backup existing folder/files automatically, then replace.
- Option C - Block overwrites for reserved/official folders and allow normal mod upgrades.

Affected files/areas:
- `source/CalradiaForge.Core/Infra/Mods/ModInstaller.cs`
- `source/CalradiaForge.Core/Infra/Mods/BLSEInstaller.cs`
- `source/CalradiaForge.UI/Pages/ModsPage.xaml.cs`

### Platform Targeting

Decision needed: Should Core remain `net10.0` despite Windows-only APIs, or should projects be retargeted/documented as Windows-only?

Why it matters: Analyzer warnings and package architecture mismatch will continue until product/platform assumptions are encoded.

Options:
- Option A - Keep Core `net10.0`, add supported-platform attributes and suppression rationale.
- Option B - Retarget Core to Windows-specific TFM.
- Option C - Abstract platform APIs behind Windows-only adapters.

Affected files/areas:
- `source/CalradiaForge.Core/CalradiaForge.Core.csproj`
- `source/CalradiaForge.Core/GlobalSuppressions.cs`
- `source/CalradiaForge.Core/Infra/Mods/ModExtractor.cs`
- `source/CalradiaForge.Core/Infra/Paths/*`

### Version Source Of Truth

Decision needed: Which file/process owns release version truth?

Why it matters: Changelog is at `0.13.1`, project metadata is `0.12.15`, and settings fallback says `1.0.0`.

Options:
- Option A - Project files own version; changelog must match.
- Option B - A central props file owns version.
- Option C - Release pipeline stamps version during publish.

Affected files/areas:
- `docs/CHANGELOG.md`
- `source/CalradiaForge.UI/CalradiaForge.UI.csproj`
- `source/CalradiaForge.Core/CalradiaForge.Core.csproj`
- `source/CalradiaForge.UI/Pages/SettingsPage.xaml.cs`

## 20. Suggested Follow-Up Documentation

| Document | Create / Update | Why It Is Needed | Priority |
|---|---|---|---:|
| Testing strategy | Create | Defines Core/unit/integration/UI test scope. | 1 |
| Archive and installer security model | Create | Locks containment, overwrite, backup, and BLSE trust rules. | 1 |
| Nexus implementation roadmap | Update | Resolves v1.0/deferred conflict. | 1 |
| Logging and redaction policy | Create | Prevents future secret leakage. | 1 |
| Persistence schema and migration policy | Create | Defines atomic writes, backup, schema versions, corrupt recovery. | 2 |
| UI/WPF architecture guide | Create | Establishes viewmodel/command and status-state patterns. | 2 |
| Release/versioning policy | Create | Prevents changelog/project metadata drift. | 2 |
| Accessibility checklist | Create | Improves public release quality. | 3 |
| Contributor setup/build docs | Update/Create | No CI/tests today; contributors need exact SDK/build guidance. | 3 |

## 21. File-Level Notes

| File | Note | Severity / Priority |
|---|---|---:|
| `source/CalradiaForge.Core/Infra/Mods/ModInstaller.cs` | Strong service ownership, but overwrite/delete and async lifetime need hardening. | High / P1 |
| `source/CalradiaForge.Core/Infra/Mods/ModExtractor.cs` | Clear extraction abstraction; needs archive-entry containment preflight. | High / P1 |
| `source/CalradiaForge.Core/Infra/Mods/BLSEInstaller.cs` | Useful special-case flow; trusts marker/content broadly and overwrites game bin. | High / P1 |
| `source/CalradiaForge.UI/Pages/ModsPage.xaml.cs` | Largest UI file; central workflow surface and major viewmodel extraction candidate. | High / P2 |
| `source/CalradiaForge.UI/Pages/ModpacksPage.xaml.cs` | Functional but large; status strings and working-copy logic should be made more testable. | Medium / P2 |
| `source/CalradiaForge.UI/Pages/SettingsPage.xaml.cs` | Good settings coverage; has hardcoded links/strings and direct global service access. | Low / P3 |
| `source/CalradiaForge.Core/Infra/Config/AppConfig.cs` | Simple config source of truth; direct writes and raw debug values need hardening. | Medium / P1 |
| `source/CalradiaForge.Core/Infra/Logging/Logger.cs` | Simple useful logger; needs redaction before Nexus. | High / P1 |
| `source/CalradiaForge.Core/Infra/Localization/TranslationStrings.cs` | Centralized localization fallback design, but many nullable warnings. | Medium / P2 |
| `source/CalradiaForge.Core/Infra/Paths/AppPaths.cs` | Good central path ownership; comments stale and Nexus path ownership needs watch. | Low / P2 |
| `source/CalradiaForge.Nexus/CalradiaForge.Nexus.csproj` | Reserved scaffold only; no runtime to audit. | Cleanup / P1 when Nexus starts |
| `docs/Architecture/Nexus/NEXUS_INTEGRATION_ARCHITECTURE.md` | Strong locked architecture document. | Keep |
| `docs/TODO_v1.md` | Useful but conflicts with Nexus deferred/planned state. | High / P1 |
| `docs/CHANGELOG.md` | Detailed, but ahead of project metadata and contains stale notes. | Medium / P2 |
| `README.md` | Good user-facing coverage, but public links and some text are stale/garbled. | Low / P3 |

## 22. Subagent Findings Summary

| Audit Track | Summary | Highest Severity Finding |
|---|---|---|
| A/B - Repository Inventory, Build Health, Architecture Boundaries | Build passes; no critical boundary violations; no CI/tests; SDK/package reproducibility loose; UI->Nexus doc edge not implemented yet. | High: `SevenZipWrapper` AMD64 vs MSIL mismatch. |
| C/D - Maintainability, Code Quality, Design Patterns | Core boundaries are strong; largest risk is page-as-viewmodel structure, static service access, inconsistent result patterns, and async lifetime complexity. | High: UI pages and `App.*` statics limit testability. |
| E/F - Security, Privacy, Data, Persistence, Installer Workflows | No secrets found; main risks are archive containment, overwrite behavior, BLSE trust, non-atomic persistence, and future Nexus redaction. | High: archive/install trust boundary needs hardening. |
| G/H - UI/UX/WPF, Documentation, Future Direction | UI needs persistent status/busy/empty states, accessibility basics, localization completion, and roadmap cleanup. Docs are strong but some are stale/conflicting. | High: Nexus roadmap conflict and UI status visibility gaps. |

The main agent waited for and integrated all four subagent tracks before writing this report.

## 23. Bottom Line

CalradiaForge is in a promising and maintainable open-beta state, not a throwaway prototype. The project has a clean high-level shape, useful documentation, and a good instinct for keeping runtime work in Core and Nexus work out of Core. Development can continue safely for focused fixes and stabilization.

The top 3 things to fix or document first:
1. Harden archive extraction/install/BLSE overwrite behavior and add tests around it.
2. Resolve Nexus timing and roadmap conflicts before building Nexus runtime code.
3. Add a Core test project and CI so warning/test/build health stops being purely manual.

The top 3 things that should remain stable:
1. Keep Core free of WPF and preserve the UI/Core boundary.
2. Keep Nexus networking/auth/downloader in `CalradiaForge.Nexus`.
3. Keep `ModInstaller` and `ModExtractor` as the authoritative install pipeline.

A refactor phase is recommended before major new feature work, especially before Nexus download/update/NXM features. That refactor should be focused, not broad churn: archive safety, persistence hardening, tests, warning cleanup, async completion safety, and selected UI viewmodel extraction.


