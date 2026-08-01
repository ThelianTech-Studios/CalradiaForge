# CalradiaForge Post-Phase-9 Deep Audit

Date: 2026-08-01  
Audited commit: `b3dbf69052f0ac23ff5cc52d023e500f4d428c10` (`dev-V0-14-CodeRefactor`)  
Status: Decision and release-preparation input; no production code changed

## Executive Verdict

CalradiaForge has a sound post-Phase-8 architecture and a healthy automated
baseline, but it is **not ready to declare the 0.14.0 beta release candidate
complete**. The necessary remaining work is focused rather than another broad
refactor:

1. implement one version source and correctly display the prerelease value;
2. resolve four reliability/safety items in the installer, archive, config, and
   launch paths;
3. reconcile substantial Phase-8 and lifecycle documentation drift; and
4. make explicit release decisions for deferred Phase 10 findings and the
   remaining human WPF/runtime evidence.

No confirmed remotely exploitable vulnerability was established in this source
review. Archive resource exhaustion and reparse-point handling remain important
security-hardening follow-ups. This is a static source/documentation audit, not
a penetration test or a completed advisory-database scan.

## Scope And Method

- Read-only review of the current source projects, tests, project files,
  localization, release metadata, architecture/refactor documents, audit
  reports, changelog, migration map, and documentation sitemap.
- Three independent workstreams: runtime/security and ownership, documentation
  accuracy/register integrity, and version/release metadata.
- Debug and Release builds and test suites were run with `--no-restore`.
- A package vulnerability query could not complete because this environment
  could not authenticate its NuGet TLS connection. The Codex Security workflow
  preflight also could not start because the available `python.exe` alias is
  inaccessible. Neither limitation is evidence that dependencies are clean.
- Phase 10 performance changes were intentionally excluded. The current Phase
  9 report remains decision input only.

## Verified Baseline And Strengths

| Area | Evidence | Result |
| --- | --- | --- |
| Build and tests | Debug build: 0 errors, 6 warnings; Release build: 0 errors, 13 warnings; both test runs: 272 passed, 0 failed, 0 skipped. | Automated baseline is healthy. |
| Architecture | `CalradiaForge.Core` remains WPF-free; UI composition/lifecycle ownership is explicit; Nexus has no active HTTP/auth/credential implementation. | Layering remains substantially intact. |
| Installer containment | `ModExtractor.cs:128-176` rejects rooted, parent-traversal, invalid, and escaped archive paths; `ModInstaller.cs:537-603` rechecks module identity and target containment. | Strong initial archive and install controls. |
| Persistence | `AtomicFileWriter.cs:28-76` uses temp write, flush, replacement, and bounded retry. | Resilient persistence foundation. |
| Documentation history | `CHANGELOG.md:10-40` and `MIGRATION_MAP.md:5-22,39-94` agree on `0.13.105`; the architecture index covers the primary system/project documents. | Release history remains traceable. |

## Findings Requiring Decisions Or Work

| ID | Priority | Finding | Evidence | Recommended disposition |
| --- | --- | --- | --- | --- |
| R1 | P1 | An in-place upgrade deletes the installed module before the replacement is copied. Failure, cancellation, low disk space, or power loss can leave a working mod missing or partial. | `source/CalradiaForge.Core/Infra/Mods/ModInstaller.cs:432-450,725-734` | Use sibling staging, validate, atomically swap with a recoverable backup, then add cancellation/I/O-failure tests. |
| R2 | P2 | Archive extraction validates paths but has no entry-count, individual/total uncompressed-size, compression-ratio, or extraction-time budget. A user-selected archive can exhaust disk, CPU, or filesystem capacity. | `source/CalradiaForge.Core/Infra/Mods/ModExtractor.cs:83-93,128-176` | Define conservative limits before extraction and test oversized/bomb-shaped metadata. |
| R3 | P2 | Non-JSON config read failures propagate into DI/startup and lead to fatal close instead of recoverable startup state. | `ConfigFileManager.cs:131-142`; `CalradiaForgeCoreServiceCollectionExtensions.cs:27-32`; `App.xaml.cs:38-61` | Handle I/O/access/security failures safely, preserve known-good/empty in-memory settings, notify the user, and decide corruption-backup policy. |
| R4 | P3 | Steam process enumeration fails open: an inspection exception is treated as Steam running. | `source/CalradiaForge.Core/Infra/Launch/GameLauncher.cs:323-340` | Return an explicit unknown/failure diagnostic; do not silently bypass the launch gate. |
| S1 | Follow-up | Reparse-point behavior is not proven safe. Lexical archive checks are followed by recursive scans/copies, but code does not reject reparse points or track visited targets. | `ModExtractor.cs:128-176`; `ModInstaller.cs:364-365,696-713`; `BLSEInstaller.cs:224-239` | Add supported-Windows symlink/junction attack fixtures. If materialization is possible, reject reparse points and use non-following enumeration. |
| V1 | P1 | Release/application version drift is real: UI and Core declare `0.12.15`, while the release record is `0.13.105`; About cannot display prerelease labels. | `CalradiaForge.UI.csproj:24-25`; `CalradiaForge.Core.csproj:10`; `SettingsViewModel.cs:648-653`; `CHANGELOG.md:10` | Complete a focused version-source implementation before beta release. |
| D1 | P1 | Phase 8 is implemented in source but broad current-facing refactor and architecture records still describe it as future/unimplemented. | `ServiceCollectionExtensions.cs:50-57`; `MainWindow.xaml.cs:22-45,216-301` versus `refactor_master_plan.md:214,478-504`, `mvvm_refactor_plan.md:3-4,26-32`, and `Architecture/Solution/ARCHITECTURE.md:81-85` | Run one documentation-only post-Phase-8 reconciliation. Preserve historical intent, but make implementation, ownership, and deferrals explicit. |
| D2 | P2 | Sitemap rows marked `Exists` point at ten absent temporary instruction files; this conflicts with the sitemap's own verification rules. | `docs/DOCUMENT_SITEMAP.md:7-15,61,69-77` | Reconcile only under the row-level retention/approval rules; do not restore obsolete documents. |
| D3 | P2 | One direct local Markdown link is broken. | `docs/refactor/phase_7_locked_decisions_2026-07-24.md:10` | Replace/remove the link or restore an explicitly retained target. |
| D4 | P2 | Active/historical documentation mixes old and current states: legacy logger status conflicts, platform plan calls implemented behavior a placeholder, AGENTS says no tests, and diagrams imply a UI-to-Nexus reference not present in the project. | `security_and_secret_boundary.md:9-12`; `game_platform_detection_and_path_workflow_plan.md:15-18`; `AGENTS.md:10,27,33-36`; `Architecture/Solution/ARCHITECTURE.md:42-50` | Reframe historical plans and correct live architecture/instruction claims in the same reconciliation. |
| R5 | P2 | Phase-8 runtime evidence language is ambiguous: the runtime audit retains owner-inspection work while the changelog records owner approval. | `calradiaforge_phase_8d_runtime_verification_2026-08-01.md:3,214-261`; `CHANGELOG.md:37-40` | Define whether these statements cover different acceptance gates; retain any unmet manual gate in the beta checklist. |

## Central Version Source: Recommended Design

The existing `docs/refactor/versioning_policy.md:51-64,131-154` already selects
`Directory.Build.props` as the intended authority. Implement that design as a
focused release-metadata phase, not as a second application configuration
system.

1. Place one `Directory.Build.props` at the repository root or `source/` (choose
   based on the desired scope), with only two human-edited release inputs:
   `CalradiaForgeVersionPrefix=0.14.0` and a prerelease suffix such as
   `beta.1`.
2. Derive `Version`, `AssemblyVersion`, `FileVersion`, and
   `InformationalVersion` from those inputs. Assembly/file versions must use a
   four-part numeric value, for example `0.14.0.0`; that is derived metadata,
   not a second public version.
3. Remove the UI/Core hardcoded `<Version>` properties. Let solution projects
   inherit shared version metadata unless a documented exception is necessary;
   make UI release notes interpolate the shared value.
4. Keep product identity (`Title`, `Product`, copyright) project-specific. Do
   not duplicate the version in a C# constant. Have the About view read the
   entry assembly's `AssemblyInformationalVersionAttribute`, which preserves and
   displays `0.14.0-beta.1` correctly.
5. Decide the exact public prerelease form before implementation. A bare
   `0.14.0` looks stable under SemVer; use an explicit suffix if it is a beta.

Nexus and ConsoleUtils currently inherit the MSBuild default `1.0.0`; that is
another reason shared metadata must be deliberately scoped and verified. The
publish profile also writes to the fixed unversioned
`releases\\CalradiaForge` directory (`FolderProfile.pubxml:7-14`), so beta
release work should add a versioned artifact/traceability decision.

## Release-Preparation Gaps

- Record the owner's explicit deferral of Phase 10 in the master plan/decision
  record. The Phase 9 audit is correctly in-review and has no approved
  production optimization; deferral is a disposition, not completion.
- Decide what replaces the originally sequential Phase 10/11/12 path: a
  release-specific verification gate must still cover real Steam installation,
  WPF startup/navigation, install/cancel/close/restart, localization, and
  packaged output. Automated tests cannot prove those interactions.
- Treat the recurring `SevenZipWrapper` MSIL/AMD64 warning as release risk
  until target architecture is explicitly chosen/tested. Release also emits
  CA1416 Windows-platform reachability warnings for registry/archive calls;
  the app is Windows-targeted, but Core's platform contract should be
  documented or guarded deliberately.
- Refresh outward-facing release guidance immediately before publishing:
  `README.md:26-32` presents historical release notices as active guidance, and
  the pinned .NET Desktop Runtime link at `README.md:73-86` requires live
  verification. No release automation exists; this should be an explicit
  owner-managed checklist rather than an assumed process.
- Upgrade dependency/advisory verification from a blocked environment check to
  a release gate: successfully restore and run `dotnet list ... package
  --vulnerable --include-transitive`, then resolve or consciously accept every
  advisory.

## Proposed Closeout Sequence

1. Owner decides exact beta identifier and formally records the Phase-10
   deferral/disposition.
2. Implement and test R1-R4 and decide S1 after a focused archive/reparse-point
   test. These are safety/reliability work, not performance optimization.
3. Implement the single version source and validate every solution assembly,
   About display, release notes, artifact directory, and changelog draft.
4. Perform documentation-only Phase-8/release reconciliation, including D1-D4
   and the manual-evidence wording.
5. Run a release candidate verification matrix: clean Debug/Release build and
   tests, dependency advisory scan, Windows architecture/package validation,
   manual WPF/Steam install workflows, update README/runtime requirements, and
   final changelog/migration-map reconciliation against the accepted source
   range.

## Audit Limitations

- Tests exercised 272 automated cases in each configuration but did not
  substitute for human WPF or real Steam/mod-library validation.
- No dependency advisory result is reported because NuGet connectivity failed.
- No penetration test, signed installer verification, code-signing review, or
  live Nexus operation was in scope; Nexus networking/auth remains unimplemented
  in this checkout.
- The archive reparse-point issue is a credible hardening candidate, not a
  confirmed vulnerability until supported Windows extraction behavior is
  demonstrated.

