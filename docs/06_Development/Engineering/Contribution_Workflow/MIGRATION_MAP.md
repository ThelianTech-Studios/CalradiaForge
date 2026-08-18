# Migration Map

```text
<Metadata>
Last Changelog Version: v0.14.0
Last Git Commit ID: a8dfbc7c616bdeeb5313ae0addca31b60fa69e32
Last Git Branch Used: dev-V0-14-CodeRefactor(HEAD)
Last Map Compile Date: 2026-08-02
</Metadata>
```

## Map Rules

This document maps all committed source-code changes by changelog build version, including phase work and manually made source changes.

Future migration map sections must use the metadata commit as the comparison base. Use `Last Git Commit ID...HEAD` for the next migration diff.

This map reflects committed branch diffs only, not unrelated local working-tree edits that are still uncommitted.

Documentation-only files are intentionally excluded from the migration map unless the owner explicitly changes that policy. Every source-code file in the comparison range is mapped under its build version.

Each migration update must be grouped by changelog version.

For each version section:

1. Add or update the version row in `Version Index`.
2. Add a collapsible `<details>` block for the changelog version.
3. Add a compact migration-area table grouped by area.
4. Add detailed file rows inside a nested `Detailed file map` block.

Prefer one detailed row per changed source file unless several files changed for the same narrowly scoped reason. If a file has a large number of edits, summarize the change and include approximate line churn instead of listing every edit.

Do not mix rows from different changelog versions in the same detailed table. If a future branch contains work for multiple changelog versions, split the map by version first, then by migration area.

Workflow rules for when and how to update this file are owned by `docs/Decisions/Policies/Git_Publication_Policy.md`.

## Version Index

| Version | Migration scope | Source comparison | Status |
|---|---|---|---|
| `v0.14.0` | Phase 9 benchmark evidence infrastructure, defaults-first configuration recovery, fail-closed Steam process inspection, centralized beta version metadata, and supporting tests | `45ee44b...1611282` | Mapped from committed accepted upstream source diff |
| `v0.13.105` | Phase 8 retained-page MVVM extraction, lifecycle-aware navigation, UI interaction seams, eight-second toast defaults, Settings path feedback, localization/tooling synchronization, and supporting tests | `03856f5...45ee44b` | Mapped from committed accepted upstream source diff |
| `v0.13.90` | Phase 7 install outcomes, correlated notifications, manager-owned finalization/reconciliation, retained Launcher naming, and supporting tests | `3759bf0...03856f5` | Mapped from committed accepted source diff |
| `v0.13.83` | Phase 6.C application-wide legacy logger migration, structured Serilog callers, emergency startup-failure fallback, logger retirement, lifecycle guards, runtime language artifact, and supporting tests | `29ae996...3759bf0` | Mapped from committed accepted source diff |
| `v0.13.73` | Phase 6.B single-provider dependency injection, configuration/logging ownership, coordinated application lifecycle, themed XAML lifecycle dialogs, retained UI injection, and supporting tests | `11c0cd1...29ae996` | Mapped from committed accepted source diff |
| `v0.13.49` | Phase 6.A mod-pipeline manager, structured scan/commit/snapshot lifecycle, awaitable installation, UI integration, archive-progress cleanup, and first-startup atomic JSON persistence hardening | `1889e47...11c0cd1` | Mapped from committed accepted source diff |
| `v0.13.35` | Phase 5 game-platform detection/path workflow, startup notifications, scanner/cache safety, and accepted ancillary source refinements | `1f037b1...1889e47` | Mapped from committed accepted source diff |
| `v0.13.23` | Phase 4 Core tests and provisional benchmarks plus partially reverted Steam multi-library resolver, UI, and regression work | `710881c...1f037b1` | Mapped from committed broken internal build; 13 compile errors acknowledged |
| `v0.13.22` | Atomic persistence, mod-cache recovery, archive/install preflight guardrails, and UI configuration-reference repair | `1b23045...710881c` | Mapped from committed build diff |
| `v0.13.14` | Serilog infrastructure foundation, log-retention configuration, data-helper cleanup, application configuration-property rename, and neutral formatter/redaction-removal follow-up | `37c322e...HEAD` | Mapped from committed build diff |
| `v0.13.6` | Cleanup/nullability/path/logging-message migration rows listed in this document | `dev-release...HEAD` | Mapped from current committed branch diff |

<details open>
<summary><strong>v0.14.0</strong> - Internal build: Phase 9 benchmark evidence, configuration and Steam launch stabilization, centralized beta versioning, and accepted supporting source changes.</summary>

**Source comparison:** `45ee44bd5291e92b9f92818d53210e77874f36ae...16112828a51d435b7b622761497dcf00dd04d88e`

**Status:** Mapped from the committed and pushed owner-approved endpoint `16112828a51d435b7b622761497dcf00dd04d88e` on `origin/dev-V0-14-CodeRefactor`. Phase 9 evidence records 272 passing tests in Debug and Release, 14/14 generated-fixture benchmark cases, and 6/6 consent-gated real-installation cases. The later stabilization record reports zero-error Debug and Release builds and 300 passing tests in each configuration. This documentation-only closeout did not rerun runtime behavior.

**Changed implementation/test/tooling files:** 35 (`+1903/-309` source-range aggregate; documentation-only files excluded)

**Scope rule:** Includes every committed implementation, test, project, build-property, runtime-language source, and executable benchmark-runner path in `45ee44bd5291e92b9f92818d53210e77874f36ae...16112828a51d435b7b622761497dcf00dd04d88e`. Repository documentation, audits, `.gitignore`, `LICENSE.md`, and the benchmark README are excluded as documentation-only. The license 1.3 change is recorded in the matching changelog section but is intentionally not a source migration row. No Phase 10 production optimization, destructive mod-upgrade recovery, archive resource limit, Nexus implementation, credential storage, polling, or live Steam/WPF verification is represented.

| Area | Files | Summary |
|---|---:|---|
| Phase 9 benchmark infrastructure | 11 | Reclassified the generated-fixture suite as the authoritative post-Phase-8 baseline and added consent-gated, read-only owner-installation parser/scanner measurement with artifact and corpus safeguards. |
| Configuration recovery and notification | 10 | Centralized defaults, added typed persistence outcomes, made expected storage failures recoverable in memory, and surfaced one lifecycle-owned durability warning with deterministic Core/UI tests. |
| Steam launch readiness | 7 | Replaced fail-open process detection with explicit status, injectable process/start/delay seams, positive-confirmation launch gating, DI registrations, and focused tests. |
| Shared version metadata and display | 6 | Added inherited `0.14.0-beta` build metadata, removed project-local version drift, and moved Settings/About formatting to a testable informational-version helper. |
| Localization source organization | 1 | Added an explicit region around translation-dictionary application without changing behavior. |

<details>
<summary><strong>Detailed file map</strong></summary>

| File | Change | Key identifiers | Summary |
|---|---|---|---|
| `source/CalradiaForge.Benchmarks/Core.Benchmarks/ModParserBenchmarks.cs` | Modified (`+1/-1`) | `BenchmarkCategory` | Reclassifies parser measurements from the provisional Phase 4 set to the authoritative Phase 9 post-Phase-8 component baseline. |
| `source/CalradiaForge.Benchmarks/Core.Benchmarks/ModScannerBenchmarks.cs` | Modified (`+1/-1`) | `BenchmarkCategory` | Reclassifies scanner measurements as the Phase 9 authoritative end-to-end filesystem baseline. |
| `source/CalradiaForge.Benchmarks/Core.Benchmarks/ModpackValidationBenchmarks.cs` | Modified (`+2/-2`) | `BenchmarkCategory`; fixture description | Reclassifies modpack validation for Phase 9 and updates the synthetic fixture label without changing the measured validation operation. |
| `source/CalradiaForge.Benchmarks/Core.Benchmarks/ModsDataBenchmarks.cs` | Modified (`+1/-1`) | `BenchmarkCategory` | Reclassifies mod-cache persistence measurements as the Phase 9 authoritative filesystem baseline. |
| `source/CalradiaForge.Benchmarks/Core.Benchmarks/RealInstallation/ReadOnlyModuleInventory.cs` | Added (`+30/-0`) | `ReadOnlyModuleInventory.Discover`; `ModuleDescriptor`; `ParserBatchResult` | Discovers direct or one-level nested module descriptors deterministically for read-only parser measurements and exposes aggregate parse-result counts. |
| `source/CalradiaForge.Benchmarks/Core.Benchmarks/RealInstallation/RealInstallationBenchmarkInputs.cs` | Added (`+39/-0`) | `RealInstallationBenchmarkInputs.Load`; `RequireDirectory` | Loads game and Workshop inputs from explicit process-scoped environment variables, validates the Bannerlord root, normalizes paths, and avoids echoing sensitive input values in errors. |
| `source/CalradiaForge.Benchmarks/Core.Benchmarks/RealInstallation/RealInstallationModParserBenchmarks.cs` | Added (`+62/-0`) | `ParseGameModuleCorpus`; `ParseWorkshopModuleCorpus`; `ParseCombinedModuleCorpus` | Measures warm, read-only `ModParser.Parse` operations over local, Workshop, and combined owner-installation descriptors with a temporary silent logger and aggregate result validation. |
| `source/CalradiaForge.Benchmarks/Core.Benchmarks/RealInstallation/RealInstallationModScannerBenchmarks.cs` | Added (`+79/-0`) | `ScanGameModules`; `ScanWorkshopWithEmptySyntheticLocalRoot`; `ScanGameModulesAndWorkshop`; `ValidatePreflight` | Measures warm `ModScanner.ScanAsync` cases using temporary benchmark configuration, validates required roots/counts before measurement, and restores logger and fixture ownership during cleanup. |
| `source/CalradiaForge.Benchmarks/Core.Benchmarks/Support/BenchmarkFixtureDirectory.cs` | Modified (`+7/-0`) | `Dispose` containment guard | Refuses recursive cleanup outside the benchmark-owned temporary root before deleting generated fixtures. |
| `source/CalradiaForge.Benchmarks/run-phase4-benchmarks.ps1` -> `source/CalradiaForge.Benchmarks/run-phase9-benchmarks.ps1` | Renamed/modified (`+12/-6`) | Phase 9 category selection; environment metadata; report validation | Renames the runnable baseline entry point, captures commit/worktree/environment context, selects authoritative Phase 9 categories, and rejects missing or `NA` result reports. |
| `source/CalradiaForge.Benchmarks/run-phase9-real-installation-benchmarks.ps1` | Added (`+261/-0`) | `ConsentToReadRealInstallation`; Steam/library discovery; corpus fingerprinting; `Protect-ArtifactText` | Requires explicit consent, resolves the owner installation, captures descriptor/topology state, runs only real-installation benchmark categories, redacts local literals, rejects invalid reports, and fails if the corpus changes. |
| `source/CalradiaForge.Core/CalradiaForge.Core.csproj` | Modified (`+0/-1`) | inherited `Version` | Removes the hardcoded `0.12.15` package version so Core inherits shared metadata from `Directory.Build.props`. |
| `source/CalradiaForge.Core/Infra/Config/AppSettings.cs` | Modified (`+14/-34`) | canonical enum/EULA fallbacks; removed wrapper default initialization | Removes duplicated wrapper-owned default seeding and resolves recognized fallback values through `ConfigDefaults`; persisted property behavior remains on the shared manager. |
| `source/CalradiaForge.Core/Infra/Config/ConfigDefaults.cs` | Added (`+45/-0`) | `ConfigDefaults.Values`; `ObsoleteKeys`; `IsValidPersistedValue`; `GetEnum`; `GetBool` | Defines the canonical 13-key default map, typed-value validation/helpers, and obsolete `LogFileDaysToKeep` cleanup policy. |
| `source/CalradiaForge.Core/Infra/Config/ConfigFileManager.cs` | Modified (`+294/-116`) | `Load`; `Save`; `this[string]`; `PersistenceBecameUnavailable`; malformed-backup and overlay helpers | Builds defaults before disk overlay, returns typed outcomes, preserves unknown values, repairs recognized values, backs up malformed JSON before replacement, leaves unreadable originals untouched, and retains memory-first changes when persistence fails. |
| `source/CalradiaForge.Core/Infra/Config/ConfigPersistenceResults.cs` | Added (`+47/-0`) | `ConfigLoadStatus`; `ConfigPersistenceStatus`; `ConfigSaveStatus`; result records; unavailable event args | Adds semantic load/save/durability contracts for normal, repaired, malformed, inaccessible, and unavailable-persistence states without UI dependencies. |
| `source/CalradiaForge.Core/Infra/Config/IConfigFilePersistence.cs` | Added (`+32/-0`) | `IConfigFilePersistence`; `ConfigFilePersistence` | Introduces the narrow internal read/copy/atomic-write persistence seam used for deterministic failure coverage while retaining the existing atomic writer in production. |
| `source/CalradiaForge.Core/Infra/Config/LoggingSettings.cs` | Modified (`+1/-5`) | `DebugMode`; centralized initialization | Removes duplicated Debug Mode seeding and obsolete-key cleanup because the shared manager now owns complete default construction and cleanup. |
| `source/CalradiaForge.Core/Infra/DependencyInjection/CalradiaForgeCoreServiceCollectionExtensions.cs` | Modified (`+3/-0`) | `AddCalradiaForgeCore` launch seam registrations | Registers the Steam inspector, launch process starter, and launch delay as singleton Core services for DI-owned `GameLauncher` construction. |
| `source/CalradiaForge.Core/Infra/Launch/GameLauncher.cs` | Modified (`+56/-55`) | injectable constructor; `EnsureSteamRunningAsync`; `LaunchExe`; removed `IsSteamRunning` | Requires positive Steam confirmation, starts Steam once for initial not-running/unknown states, polls boundedly, distinguishes unknown verification failure from timeout, and preserves direct Standalone/GOG launching. Existing one-argument construction remains available. |
| `source/CalradiaForge.Core/Infra/Launch/ILaunchDelay.cs` | Added (`+10/-0`) | `ILaunchDelay`; `LaunchDelay` | Adds an awaitable delay seam for deterministic polling/initialization tests with a production `Task.Delay` implementation. |
| `source/CalradiaForge.Core/Infra/Launch/ILaunchProcessStarter.cs` | Added (`+14/-0`) | `ILaunchProcessStarter`; `LaunchProcessStarter` | Adds the narrow process-start boundary used for Steam protocol and game executable launches. |
| `source/CalradiaForge.Core/Infra/Launch/ISteamProcessInspector.cs` | Added (`+13/-0`) | `SteamProcessStatus`; `ISteamProcessInspector.Inspect` | Defines explicit `Running`, `NotRunning`, and `Unknown` inspection results instead of a fail-open Boolean. |
| `source/CalradiaForge.Core/Infra/Launch/SteamProcessInspector.cs` | Added (`+55/-0`) | `SteamProcessInspector.Inspect` | Enumerates Steam processes, disposes all returned process objects including partial-failure paths, warning-logs inspection failures, and returns `Unknown` without false detection. |
| `source/CalradiaForge.Core/Infra/Localization/TranslationStrings.cs` | Modified (`+2/-2`) | `Apply Translation Dictionary Method` region | Wraps translation dictionary application and its fallback helper in an explicit region; runtime localization behavior is unchanged. |
| `source/CalradiaForge.Tests/Core.Tests/Config/ConfigFileManagerTests.cs` | Modified (`+285/-73`) | defaults/overlay/recovery/failure/concurrency facts | Covers complete defaults, full/partial/null overlays, unknown and obsolete keys, malformed preservation, inaccessible reads, failed writes, memory retention, one durability transition, typed setters, and concurrent access. |
| `source/CalradiaForge.Tests/Core.Tests/Launch/GameLauncherTests.cs` | Added (`+265/-0`) | 12 Steam and Standalone/GOG launch facts | Verifies positive detection, startup/polling, unknown recovery and failure, disposal, timeout distinctions, Bannerlord/BLSE blocking, start failure, initialization delay, and direct-launch compatibility. |
| `source/CalradiaForge.Tests/UI.Tests/Lifecycle/ApplicationStartupCoordinatorTests.cs` | Modified (`+88/-0`) | persistence-unavailable startup/runtime warning and EULA facts | Verifies exactly one durability warning and current-session EULA continuation when configuration persistence fails; updates construction for the manager dependency. |
| `source/CalradiaForge.Tests/UI.Tests/ViewModels/ApplicationVersionTextTests.cs` | Added (`+71/-0`) | informational, metadata-trimming, numeric-fallback, unavailable-version facts | Verifies prerelease preservation, `+metadata` removal, numeric fallback, and the absence of the old hardcoded fallback behavior. |
| `source/CalradiaForge.UI/CalradiaForge.UI.csproj` | Modified (`+1/-2`) | inherited `Version`; `PackageReleaseNotes` interpolation | Removes the hardcoded `0.12.15` version and derives package release-note identity from the shared application version. |
| `source/CalradiaForge.UI/Lifecycle/ApplicationStartupCoordinator.cs` | Modified (`+46/-0`) | `ConfigFileManager` dependency; `PersistenceBecameUnavailable`; one-shot warning | Queues the durability warning before shell readiness or presents it afterward, subscribes to runtime persistence loss, and prevents duplicate notification. Manual construction now supplies the manager; DI resolves it automatically. |
| `source/CalradiaForge.UI/Lifecycle/StartupNotificationDrainCoordinator.cs` | Modified (`+16/-0`) | `PresentAsync` | Adds direct lifecycle-notification presentation after startup queue draining while preserving readiness and cancellation handling. |
| `source/CalradiaForge.UI/ViewModels/ApplicationVersionText.cs` | Added (`+38/-0`) | `FromEntryAssembly`; `FromAssembly`; `Format` | Formats the entry assembly informational version for Settings/About, preserves prerelease labels, strips build metadata only, falls back to numeric version, and reports unavailable metadata honestly. |
| `source/CalradiaForge.UI/ViewModels/SettingsViewModel.cs` | Modified (`+1/-10`) | `VersionText`; removed `BuildVersionText` | Delegates version display to `ApplicationVersionText` and removes the executing-assembly three-part display and hardcoded `Version 1.0.0` fallback. |
| `source/Directory.Build.props` | Added (`+11/-0`) | `VersionPrefix`; `VersionSuffix`; `Version`; assembly/file/informational metadata | Establishes inherited `0.14.0-beta` public metadata, `0.14.0.0` assembly/file versions, and disables automatic source-revision suffixing for all projects beneath `source`. |

</details>

</details>

<details open>
<summary><strong>v0.13.105</strong> - Internal build: Phase 8 retained-page MVVM extraction, lifecycle-aware navigation, Settings path feedback, and accepted supporting source changes.</summary>

**Source comparison:** `03856f5...45ee44b`

**Status:** Mapped from the committed and pushed owner-approved endpoint `45ee44bd5291e92b9f92818d53210e77874f36ae` on `origin/dev-V0-14-CodeRefactor`. The implementation evidence records zero-error Debug and Release builds, 272 passing tests in each configuration, and 53 passing focused Phase 8.E tests. This documentation-only closeout did not rerun WPF behavior.

**Changed implementation/test/resource files:** 48 (`+4993/-2471` source-range aggregate)

**Scope rule:** Includes every committed `source/` path in `03856f5e56725fd22d5c41fd38b2ec277196ab5f...45ee44bd5291e92b9f92818d53210e77874f36ae`. Seventeen documentation paths and `.gitignore` are excluded. The later local region-only commit `55b064d5bef7d7fb4e75f8729cad07d34aac23c5` is outside the owner-selected upstream endpoint and is not represented. No Core detection/resolver/scanner algorithm, Nexus behavior, credential storage, polling, page filename, or application-version implementation changed.

| Area …28611 tokens truncated…--|---:|---|
| Atomic persistence foundation | 1 | Added a shared same-directory write, flush, and replace/move helper for JSON persistence owners. |
| Configuration and modpack persistence | 2 | Applied atomic writes to configuration, named-modpack, and last-used data and added malformed configuration fallback. |
| Mod-cache recovery and rotation | 1 | Added validated cache loading, valid-backup recovery, atomic repair, and backup-preserving rotation. |
| Archive containment and managed cleanup | 1 | Added lexical entry containment checks, structured extraction results, and managed GUID-directory cleanup restrictions. |
| Module-install preflight and failure reporting | 4 | Added exact-one-module preflight, target and identity guardrails, deletion confirmation, result models, and actionable summaries. |
| UI configuration-reference repair | 2 | Reconnected Mods and Settings page call sites to the shared application settings facade. |

<details>
<summary><strong>Detailed file map</strong></summary>

| File | Change | Key Identifiers | Original vs Updated | Summary |
|---|---|---|---|---|
| `source/CalradiaForge.Core/Infra/Persistence/AtomicFileWriter.cs` | Added | `AtomicFileWriter`; `WriteAllText`; `fullDestinationPath`; `temporaryFilePath`; `FileStream`; `StreamWriter` | No shared atomic-write helper existed; the new internal static helper writes a GUID-named temporary file beside the destination, flushes it to disk, replaces or moves it into place, and removes any leftover temporary file. | Centralizes atomic same-directory text persistence without changing public application APIs or creating backup files. |
| `source/CalradiaForge.Core/Infra/Config/AppConfig.cs` | Modified | `Save`; `Load`; `AtomicFileWriter`; `JsonException` | `Save` wrote directly with `File.WriteAllText`, and malformed JSON could escape normal loading; saves now use atomic replacement, while malformed JSON is logged and resets the in-memory settings dictionary to empty. | Preserves the configuration key/value contract while allowing the typed settings facade to seed defaults after malformed-JSON fallback. |
| `source/CalradiaForge.Core/Infra/Modpacks/ModpackData.cs` | Modified | `SaveModpack`; `SaveLastUsed`; `AtomicFileWriter` | Named modpacks and `last_used_mods.data` were written directly; both save paths now use the shared atomic writer while retaining existing locks, serialization, return values, and error handling. | Reduces partial-write exposure without adding named-modpack or last-used backup recovery. |
| `source/CalradiaForge.Core/Infra/Mods/ModsData.cs` | Modified | `SaveCurrent`; `LoadCurrent`; `SaveBackup`; `LoadBackup`; `RotateDataFiles`; `TryLoadMods`; `recoveredJson` | Current/backup writes were direct, loads could propagate handled file/JSON failures, and rotation copied unvalidated current data; writes are now atomic, loads validate non-null arrays and entries, invalid current data can recover from a valid backup, and invalid current data no longer overwrites the backup. | Keeps public signatures and the persisted list shape while adding bounded cache recovery and backup-preserving rotation. |
| `source/CalradiaForge.Core/Infra/Mods/ModExtractor.cs` | Modified | `ExtractToTempAsync`; `ExtractToTempResultAsync`; `TryValidateEntryContainment`; `CleanupTempDirectory`; `TryResolveManagedTempDirectory`; `UnsafeArchiveEntryException` | Extraction previously began without entry-destination inspection and cleanup accepted arbitrary directory paths; entries are now lexically validated before temp creation/extraction, failures carry structured detail, and recursive cleanup is limited to direct GUID children of `AppPaths.ExtractionDirectory`. | Retains the legacy nullable-path extraction API as a compatibility wrapper while adding a safer result-based path for installation. |
| `source/CalradiaForge.Core/Infra/Mods/ModInstaller.cs` | Modified | `ProcessSingleArchiveAsync`; `PreflightModuleInstall`; `PreflightFailure`; `IsPathAtOrBelowRoot`; `IsPathBelowRoot`; `CheckExistingVersion`; `SafeDeleteDirectory` | Installation previously selected the first discoverable module XML, derived targets without identity preflight, could overwrite unknown targets, and continued after failed deletion; normal archives now require exactly one `SubModule.xml`, contained roots/targets, a parseable module ID, matching existing identity, and confirmed target deletion before copy. | Makes valid normal-module installation intentionally stricter while preserving the public installer API, events, and zero-module BLSE fallback. |
| `source/CalradiaForge.Core/Models/ArchiveExtractionResult.cs` | Added | `ArchiveExtractionResult`; `Success`; `TempDirectory`; `Message`; `Ok`; `Fail` | Extraction success/failure was represented only by a nullable path; the additive public result now carries success state, the managed temp path, and failure detail through controlled factory methods. | Enables actionable extraction and validation failures without removing the compatibility API. |
| `source/CalradiaForge.Core/Models/ModuleInstallPreflightResult.cs` | Added | `ModuleInstallPreflightResult`; `ModuleRootPath`; `ModuleFolderName`; `SubModuleXmlPath`; `TargetPath`; `Module`; `ExistingModule` | Validated roots, target paths, and parsed incoming/existing identities were separate installer locals; the additive public init-only model now carries the complete pre-destination state. | Gives the private installer preflight one explicit Core-owned result contract with no WPF dependency. |
| `source/CalradiaForge.Core/Models/ModInstallSummary.cs` | Modified | `ToSummaryString`; `firstNormalFailure`; `label` | Normal archive failures contributed only a generic count; the summary now appends the first normal failure's archive/module label and message while keeping existing BLSE summary handling. | Surfaces extraction and preflight failures through the existing status/toast output without changing the public result collection. |
| `source/CalradiaForge.UI/Pages/ModsPage.xaml.cs` | Modified | constructor; `OnInstallCompleted`; `PopulateModpackList`; `ResolveStartupModpackIndex`; `RefreshModpackList`; `PlayButton_Click`; `SetActiveLaunchTarget`; `App.AppSettingsInstance` | Eight settings reads/writes referenced the nonexistent `App.AppConfig` member; launch-target, module-path, startup-mode, and last-selected-modpack call sites now use the shared `AppSettingsInstance` facade. | Corrects the accepted build-scope UI references without changing settings schema, XAML, or service ownership. |
| `source/CalradiaForge.UI/Pages/SettingsPage.xaml.cs` | Modified | constructor; `_config`; `App.AppSettingsInstance`; localization namespace import | The constructor assigned `_config` from nonexistent `App.AppConfig`, and an unused localization import remained; the field now receives `AppSettingsInstance` and the unused import is removed. | Restores the page's existing `AppConfigSettings` dependency without changing page behavior or configuration ownership. |

</details>

</details>

<details open>
<summary><strong>v0.13.14</strong> - Internal build: Phase 2 Serilog infrastructure and all other committed source changes in the build range.</summary>

**Source comparison:** `37c322e...091974d`  
**Status:** Mapped from committed build diff  
**Changed source files:** 11  
**Scope rule:** Includes every committed source-code file in this build range; documentation-only files are excluded.

| Area | Files | Summary |
|---|---:|---|
| Serilog package foundation | 1 | Added the approved Serilog packages and made the debugger sink a Debug-build-only dependency. |
| Logging configuration and paths | 2 | Added the persisted log-age setting and the fixed active-log path used by the factory. |
| Serilog logging infrastructure | 4 | Added logger creation, redaction, formatting, and age-based cleanup components. |
| Legacy logger compatibility | 1 | Kept the existing logger while documenting and marking its future staged retirement. |
| Data-helper cleanup | 2 | Removed duplicate directory-creation behavior now owned by `AppPaths`. |
| UI configuration rename | 1 | Renamed the application configuration facade and rewired the references within `App`. |

<details>
<summary><strong>Detailed file map</strong></summary>

| File | Change | Key Identifiers | Original vs Updated | Summary |
|---|---|---|---|---|
| `source/CalradiaForge.Core/CalradiaForge.Core.csproj` | Modified | `PackageReference`; Debug-conditioned `ItemGroup` | The Core project referenced Newtonsoft.Json and SevenZipWrapper only; it now also references Serilog, File, Async, Exceptions, Thread, and a Debug-only `Serilog.Sinks.Debug` package. | Establishes the approved package foundation without adding the Console sink. |
| `source/CalradiaForge.Core/Infra/Config/AppConfigSettings.cs` | Modified | `AddMissingConfigSettings`, `LogFileDaysToKeep` | Configuration defaults did not include a log-age setting; the update registers `LogFileDaysToKeep` with a seven-day default and exposes an integer property that persists changes. | Supplies the non-secret configuration value consumed by log cleanup. |
| `source/CalradiaForge.Core/Infra/Logging/LogRedactor.cs` | Added | `LogRedactor`, `SensitiveKeyFragments`, `Redact`, `RedactValue`, `RenderProperty`; generated regexes | No shared Serilog redaction component existed; the added class redacts secret-like text and recursively renders/redacts structured values, dictionaries, sequences, and public object properties. | Creates the central sink-boundary redaction utility for the Serilog formatter. |
| `source/CalradiaForge.Core/Infra/Logging/LogRetentionPolicy.cs` | Added | `LogRetentionPolicy`, `Cleanup`, `maxAgeDays` | There was no Serilog retention helper; `Cleanup` now finds `CalradiaForge*.log` files, uses a UTC age cutoff, deletes expired files, and contains I/O/access failures. | Provides age-based local log cleanup for the Phase 2 foundation. |
| `source/CalradiaForge.Core/Infra/Logging/Logger.cs` | Modified | `Logger`; `[Obsolete]` compatibility annotation | The logger lacked staged-migration documentation; it remains present, gains a Phase 5.B compatibility summary, and is marked obsolete. | Preserves existing callers while documenting the planned Serilog migration boundary. |
| `source/CalradiaForge.Core/Infra/Logging/RedactingTextFormatter.cs` | Added | `RedactingTextFormatter`, `Format` | No custom Serilog formatter existed; `Format` writes timestamp, level, rendered message, exception, and properties after routing them through redaction. | Ensures rendered Serilog output uses the central redaction behavior. |
| `source/CalradiaForge.Core/Infra/Logging/SerilogLoggerFactory.cs` | Added | `SerilogLoggerFactory`, `_configInstance`, `_logDirectory`, `_logFilePath`, `Create` | No Serilog factory existed; the new instance factory accepts `AppConfigSettings`, runs cleanup, configures minimum level/context/thread/exception enrichment, async file output, infinite rolling, and the Debug-only sink. | Adds the uninitialized Core Serilog foundation for later composition without migrating callers. |
| `source/CalradiaForge.Core/Infra/Modpacks/ModpackData.cs` | Modified | constructor; removed `EnsureDirectoryExists` | The constructor created modpack and last-used directories locally; those calls and the helper were removed. | Leaves directory ownership to the resolved `AppPaths` boundary. |
| `source/CalradiaForge.Core/Infra/Mods/ModsData.cs` | Modified | constructor; removed `EnsureDirectoryExists` | The constructor created current/backup file directories locally; those calls and the helper were removed. | Removes duplicate directory creation from the mods data helper. |
| `source/CalradiaForge.Core/Infra/Paths/AppPaths.cs` | Modified | `LogsFileName`, `LogsFilePath` | `AppPaths` had no dedicated active-log filename/path; it now resolves `CalradiaForge_Latest.log` under `LogsDirectory`. | Gives the Serilog factory a stable application-owned active-log path. |
| `source/CalradiaForge.UI/App.xaml.cs` | Modified | `AppConfig` â†’ `AppSettingsInstance`, `ModManagerService`, `InitializeConfiguration`, `InitializeModServices`, `InitializeTranslatorService` | The static configuration facade was named `AppConfig`; it is renamed to `AppSettingsInstance`, the affected references inside `App` are updated, and a nullable `ModManagerService` placeholder is added. | Records the committed UI configuration-surface rename as part of this build's complete source diff. |

</details>

</details>

<details open>
<summary><strong>v0.13.14 follow-up</strong> - Internal source-only follow-up: removed automatic logging redaction and retained neutral Serilog text formatting.</summary>

**Source comparison:** `091974d...1b23045`
**Status:** Mapped from committed source-only follow-up diff
**Changed source files:** 4
**Scope rule:** Documentation-only files, Nexus files, and other non-source changes are excluded.

| Area | Files | Summary |
|---|---:|---|
| Logging redaction removal and neutral formatting | 4 | Removed the automatic redaction layer, replaced the sink formatter with neutral event rendering, and kept the Serilog factory wired to the new formatter. |

<details>
<summary><strong>Detailed file map</strong></summary>

| File | Change | Key Identifiers | Original vs Updated | Summary |
|---|---|---|---|---|
| `source/CalradiaForge.Core/Infra/Logging/LogRedactor.cs` | Deleted | `LogRedactor`, `Redact`, `RedactValue`, `RenderProperty` | The shared regex and structured-value redaction component was removed. | Eliminates automatic secret-like message, URL, exception, and property filtering from the logging pipeline. |
| `source/CalradiaForge.Core/Infra/Logging/RedactingTextFormatter.cs` | Deleted | `RedactingTextFormatter`, `Format` | The sink formatter that routed rendered messages, exceptions, and properties through `LogRedactor` was removed. | Removes formatter-level redaction and preserves ordinary Serilog event rendering. |
| `source/CalradiaForge.Core/Infra/Logging/SerilogLoggerFactory.cs` | Modified | `SerilogTextFormatter` construction; file/Debug sink formatter arguments | The factory instantiated `RedactingTextFormatter`; it now instantiates `SerilogTextFormatter` for both configured text sinks. | Keeps the existing Serilog sink composition while removing the formatter security boundary. |
| `source/CalradiaForge.Core/Infra/Logging/SerilogTextFormatter.cs` | Added | `SerilogTextFormatter`, `Format`, `AppendPropertyIfPresent` | No neutral formatter existed; the new formatter renders timestamps, levels, messages, selected context properties, structured properties, and exceptions without filtering. | Provides the active presentation formatter for local Serilog text output. |

</details>

</details>

<details open>
<summary><strong>v0.13.6</strong> - Refactor Phase 1: cleanup for nullability, localization fallback behavior, documentation alignment, and scanner/path-resolution planning.</summary>

**Source comparison:** `dev-release...HEAD`  
**Status:** Mapped from current committed branch diff  
**Changed source files:** 11  

| Area | Files | Summary |
|---|---:|---|
| Core cleanup | 1 | Removed the standalone Core suppression file after the warning no longer needed a separate exception. |
| Launch nullability | 1 | Tightened launch module selection around validated non-null module IDs. |
| Localization fallback cleanup | 1 | Hardened translation fallback/default behavior and reduced stale translation state. |
| Modpack metadata cleanup | 2 | Tightened modpack import/load-order metadata assumptions after validation. |
| Mod parsing/install cleanup | 3 | Made parser root validation explicit and aligned installer/mod-service nullability with module metadata guarantees. |
| Path detection cleanup | 2 | Cleaned app path naming/comments and path-detection dead code/error messaging without changing the path layout. |
| UI exception logging cleanup | 1 | Corrected global exception-handler naming and diagnostics text. |

<details>
<summary><strong>Detailed file map</strong></summary>

| File | Change | Key Identifiers | Original vs Updated | Summary |
|---|---|---|---|---|
| `source/CalradiaForge.Core/GlobalSuppressions.cs` | Deleted | `SuppressMessage` for `GamePathsHelper.TryAutoDetectSteamWorkshopFolder` | The file was removed; the assembly-level code-analysis suppression is no longer carried in a separate file. | Removes the Core project suppression file because the warning no longer needs a standalone exception. |
| `source/CalradiaForge.Core/Infra/Launch/GameLauncher.cs` | Modified | `ModuleId` projection in launch list building | `ModuleId` was treated as nullable; the projection now uses `ModuleId!` after upstream validation. | Tightens nullability for launch module selection without changing launch flow. |
| `source/CalradiaForge.Core/Infra/Localization/TranslationStrings.cs` | Modified | `Apply`, `GetOrDefault`, many `Default*` property initializers; about 153 additions / 156 deletions | Missing or blank translations could persist and many properties started unset; `Apply` now reseeds defaults, falls back on whitespace, and initializes properties eagerly. | Large localization cleanup that hardens fallback behavior and reduces stale translation state. |
| `source/CalradiaForge.Core/Infra/Modpacks/ModpackService.cs` | Modified | `Import`, `ValidateLoadOrder`, `BuildEntryListFromModules` | Null or failed imports were less explicit; now null import parsing fails fast, and all modpack entry projections use non-null module metadata. | Improves modpack import validation and removes nullable friction in load-order conversion. |
| `source/CalradiaForge.Core/Infra/Modpacks/VanillaModules.cs` | Modified | LINQ `GroupBy(m => m.ModuleId!, StringComparer.OrdinalIgnoreCase)` | `ModuleId` was nullable in the grouping key; it is now asserted non-null. | Keeps vanilla module grouping aligned with the earlier parsing guarantees. |
| `source/CalradiaForge.Core/Infra/Mods/ModInstaller.cs` | Modified | `Install`, `CheckExistingVersion`, `CompareModVersions` | Blank IDs and versions now use whitespace checks, the redundant `Directory.Exists` early return was removed, and non-null module fields are asserted on assignment and comparison. | Tightens installer version checking and removes a dead directory gate. |
| `source/CalradiaForge.Core/Infra/Mods/ModParser.cs` | Modified | `Parse`, `moduleElement` | `doc.Root` was assumed non-null; it is now stored as `XElement?` and validated explicitly. | Makes the XML parser's root-element nullability explicit. |
| `source/CalradiaForge.Core/Infra/Mods/ModService.cs` | Modified | `DetectChanges`, `currentIds`, `previousIds` | `ModuleId` projections were treated as nullable; they are now asserted non-null after filtering. | Keeps change detection aligned with the parser's non-empty module-id guarantees. |
| `source/CalradiaForge.Core/Infra/Paths/AppPaths.cs` | Modified | `DefaultLanguageFileName`, `_downloadsDirectory`, `_downloadsMetadataDirectory`, `_extractionDirectory` | A typo in the default language constant name was fixed, private lazy fields were renamed to lower camel case, and the comments now describe app-root paths instead of LocalAppData. | Clarifies app-owned path names and logging without changing the underlying path layout. |
| `source/CalradiaForge.Core/Infra/Paths/GamePathsHelper.cs` | Modified | `TryAutoDetectGameFolder`, `TryDetectSteam`, `TryDetectEpic`, `GetModulesFolder`, `GetSteamWorkshopFolder` | `_enableUnsupportedPlatforms` became `static readonly`, exception text was cleaned up, and the old commented-out Steam detection code was removed. | Keeps path detection behavior the same while cleaning dead code and error messaging. |
| `source/CalradiaForge.UI/App.xaml.cs` | Modified | `SetupExceptionHandling`, `LogUnhandledException` | Typo-ridden method names and one log message were corrected. | Pure naming and diagnostics cleanup for global exception handling. |

</details>

</details>

