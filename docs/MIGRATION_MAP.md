# Migration Map

> Historical implementation record: the Phase 2 redaction entries below describe what was previously added. They are not active logging policy; the 2026-07-12 owner decision removed automatic redaction and retained only neutral formatting.

```text
<Metadata>
Last Changelog Version: v0.13.35
Last Git Commit ID: 1889e47861d269b8664752706031c64d9f47dbf7
Last Git Branch Used: dev-V0-14-CodeRefactor(HEAD)
Last Map Compile Date: 2026-07-16
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

Workflow rules for when and how to update this file are owned by `docs/refactor/refactor_master_plan.md`.

## Version Index

| Version | Migration scope | Source comparison | Status |
|---|---|---|---|
| `v0.13.35` | Phase 5 game-platform detection/path workflow, startup notifications, scanner/cache safety, and accepted ancillary source refinements | `1f037b1...1889e47` | Mapped from committed accepted source diff |
| `v0.13.23` | Phase 4 Core tests and provisional benchmarks plus partially reverted Steam multi-library resolver, UI, and regression work | `710881c...1f037b1` | Mapped from committed broken internal build; 13 compile errors acknowledged |
| `v0.13.22` | Atomic persistence, mod-cache recovery, archive/install preflight guardrails, and UI configuration-reference repair | `1b23045...710881c` | Mapped from committed build diff |
| `v0.13.14` | Serilog infrastructure foundation, log-retention configuration, data-helper cleanup, application configuration-property rename, and neutral formatter/redaction-removal follow-up | `37c322e...HEAD` | Mapped from committed build diff |
| `v0.13.6` | Cleanup/nullability/path/logging-message migration rows listed in this document | `dev-release...HEAD` | Mapped from current committed branch diff |

<details open>
<summary><strong>v0.13.35</strong> - Internal build: Phase 5 game-platform detection/path workflow and the full accepted source range.</summary>

**Source comparison:** `1f037b1...1889e47`

**Status:** Mapped from committed accepted source diff; Debug and Release solution builds passed, and all 76 tests passed in both configurations

**Changed implementation/test/language files:** 27

**Scope rule:** Includes every committed Core, UI, test, and language-resource file in this build range. All `docs/` changes are excluded as documentation-only. No project, solution, package-reference, configuration-format, or generated-source file changed in the range.

| Area | Files | Summary |
|---|---:|---|
| Detection, configuration, and legacy-helper migration | 5 | Added Core-owned startup/manual detection, integrated coherent provider/path commits, added the Workshop default, refined provider states, and removed the legacy helper. |
| Steam resolver boundary and validator adaptation | 2 | Preserved the registry boundary and adapted Steam game validation to the boolean-only validator contract. |
| Startup notification handoff | 4 | Added WPF-neutral notification contracts and queue semantics, then adapted them to WPF toast delivery after window readiness. |
| Validation, Settings, and English localization | 5 | Simplified validators, routed Settings actions through Core, exposed the Workshop control name, and aligned fallback/English detection guidance. |
| Mod refresh/cache and DLL maintenance | 2 | Prevented invalid-configuration scans from replacing cache state and removed a redundant DLL-unblock precheck. |
| Detection, resolver, scanner, cache, and Novus tests | 9 | Replaced helper-coupled coverage with service/resolver suites and added queue, scanner, cache-preservation, and split-library regressions. |

<details>
<summary><strong>Detailed file map</strong></summary>

| File | Change | Key Identifiers | Original vs Updated | Summary |
|---|---|---|---|---|
| `source/CalradiaForge.Core/Infra/Config/AppConfigSettings.cs` | Modified | `InitDefaults`; `SteamWorkshopFolderPath` | The typed facade exposed the Workshop property but did not seed its key; missing configuration now receives an empty-string default. | Stabilizes optional Workshop-path reads without changing the persisted configuration shape. |
| `source/CalradiaForge.Core/Infra/GamePlatform/GameDetectionService.cs` | Added | `GameDetectionService`; `InitializeForStartup`; `RedetectGame`; `ApplyManualGameFolder`; `ApplyManualSteamWorkshopFolder` | Startup, re-detection, and manual path changes were helper/UI coordinated; the service now validates and commits complete state, queues one result when startup detection runs, and keeps startup initialization `void` while re-detection returns the provider. | Establishes the Core-owned workflow boundary; invalid manual selections preserve prior settings. |
| `source/CalradiaForge.Core/Infra/GamePlatform/GamePlatformDetectionResolver.cs` | Modified | constructor injection; `DetectGame`; `TryDetectSteam`; `ApplyManualConfigurationFallback`; path helpers | An empty placeholder became the production resolver using injected Steam-root and installation boundaries; it validates and commits game, launcher, optional Workshop and BLSE paths, with gated Epic detection still disabled. | Integrates the existing multi-library resolver, treats missing Workshop as nonfatal, and replaces failed automatic state with `ManualConfiguration`. |
| `source/CalradiaForge.Core/Infra/GamePlatform/GameProvider.cs` | Modified | `GameProvider`; `NotInitialized`; `GOG`; `ManualConfiguration`; `StandAlone` | Implicit values conflated initial/fallback semantics; explicit values now distinguish uninitialized, recognized manual storefronts, manual-required failure, and valid unrecognized standalone installs. | Named configuration values remain readable, but consumers relying on old numeric values for `NotInitialized` or `StandAlone` require review. |
| `source/CalradiaForge.Core/Infra/Paths/GamePathsHelper.cs` | Deleted | `GamePathsHelper`; legacy Steam/Epic/BLSE detection and configured-path accessors | The static single-Steam-root helper and automatic standalone fallback were removed after callers migrated to services and `AppConfigSettings`. | Breaking helper removal eliminates the main-root and missing-Workshop assumptions; use the detection service/resolver and configured path properties. |
| `source/CalradiaForge.Core/Infra/GamePlatform/Steam/ISteamClientRootProvider.cs` | Modified | `WindowsSteamClientRootProvider.GetSteamClientRoot` | Executable behavior and contracts are unchanged; a comment records that another desktop OS would require its own provider. | Comment-only platform-boundary clarification with no runtime migration effect. |
| `source/CalradiaForge.Core/Infra/GamePlatform/Steam/SteamInstallationResolver.cs` | Modified | `TryResolveGameFolder`; `GamePathValidator.ValidateGameFolder` | The resolver passed an `out` validation message; it now calls the boolean-only validator and emits a general invalid-folder diagnostic. | Required call-site adaptation for the validator API change; Steam discovery behavior otherwise remains the existing resolver implementation. |
| `source/CalradiaForge.Core/Infra/GamePlatform/StartupNotificationQueue.cs` | Added | `StartupNotificationQueue`; `Enqueue`; `SignalReady`; `DrainWhenReadyAsync`; `Count`; `IsReady` | No Core-to-UI startup-result handoff existed; the queue waits for readiness, serializes FIFO drains, and removes an item only after successful delivery. | Provides a WPF-free notification boundary with cancellation and failed-delivery retry semantics. |
| `source/CalradiaForge.Core/Models/StartupNotification.cs` | Added | `StartupNotificationSeverity`; `StartupNotification` | Startup results had no framework-neutral payload; the new record carries title, message, and Info/Success/Warning/Error severity. | Defines the shared Core/UI contract without introducing WPF references into Core. |
| `source/CalradiaForge.UI/App.xaml.cs` | Modified | `StartupNotifications`; `GameDetectionService`; `InitializeGameDetectionService`; `OnStartup` | Startup called `GamePathsHelper` during configuration initialization; it now constructs the injected Core resolver/service after EULA acceptance and before mod services, then runs the `void` startup initializer. | Keeps UI composition/timing ownership while moving detection behavior to Core; an expanded future `ModManagerService` comment has no executable effect. |
| `source/CalradiaForge.UI/Views/MainWindow.xaml.cs` | Modified | `ObserveStartupNotificationsAsync`; `DeliverStartupNotificationAsync`; `MapStartupSeverity`; `MainWindow_Loaded`; `MainWindow_Closed` | The window did not consume startup results; it now waits until Loaded, maps Core severity to toast severity, logs delivery failures, and cancels on close. | Adapts the Core queue to WPF without leaking WPF types into Core; future disposal comments do not represent implemented behavior. |
| `source/CalradiaForge.Core/Infra/Paths/GamePathValidator.cs` | Modified | `ValidateGameFolder`; `ValidateGameExecutable`; `ValidateWorkshopFolder` | Each validator returned a Boolean plus an `out string` message; all now return only a Boolean and place detailed failures in conditional debug logging. | Breaking source-signature change requires callers to remove the `out` argument and supply any user-facing message independently. |
| `source/CalradiaForge.UI/Pages/SettingsPage.xaml` | Modified | `SelectWorkshopFolderButton` | The existing Workshop selection button was unnamed; it now has an `x:Name` while retaining the same content, style, and handler. | Exposes the generated control field without changing current user behavior. |
| `source/CalradiaForge.UI/Pages/SettingsPage.xaml.cs` | Modified | `_gameDetectionService`; manual selection handlers; `RedetectGame_Click`; validation and toast paths | The page directly coordinated helper/resolver state; game and Workshop selection and re-detection now delegate to Core and refresh committed game, launcher, Workshop, and BLSE state. Validator call sites use boolean contracts; ancillary logging/messages were simplified. | Preserves UI ownership of dialogs and feedback while Core owns validation, provider inference, and state commits; actual WPF behavior remains a manual check. |
| `source/CalradiaForge.Core/Infra/Localization/TranslationStrings.cs` | Modified | `DefaultSettings_DetectGameHint`; `Settings_DetectGameHint` | The fallback text claimed automatic Steam, Epic, and standalone detection; it now describes supported detection, BLSE discovery, overwrite behavior, and manual fallback. | Aligns fallback copy with the gated platform workflow without changing the localization API. |
| `source/Languages/en-US.json` | Modified | `Settings_DetectGameHint` | The English resource made the same unsupported multi-provider claim; it now matches the supported-detection/manual-fallback wording. | Updates only English in this range; other locale files are unchanged. |
| `source/CalradiaForge.Core/Infra/Mods/ModService.cs` | Modified | `RefreshAsync`; `GameProvider.ManualConfiguration`; `GamePathValidator.ValidateGameFolder` | Refresh could rotate or replace cache state before rejecting invalid configuration; it now exits before refresh state, rotation, backup loading, scanning, or saving when provider/path state is invalid. | Protects in-memory and on-disk cache while setup is unresolved; the existing `false` return remains the skip/failure signal. |
| `source/CalradiaForge.Core/Infra/Mods/DLLUnblocker.cs` | Modified | `UnblockAllAsync`; `UnblockDirectory` | The async caller performed a null/blank/existence precheck; it now delegates directly to the guarded enumeration boundary. | Public API is unchanged; invalid paths still produce an empty result, now through error-logged enumeration handling rather than the former warning path. |
| `source/CalradiaForge.Tests/Core.Tests/GamePlatform/GameDetectionWorkflowTests.cs` | Added | `GameDetectionWorkflowTests`; startup, re-detection, manual game and Workshop cases | No service-level workflow suite existed; nine tests verify valid-state reuse, queued startup results, explicit replacement, provider inference, and atomic invalid-selection behavior. | Locks the `void InitializeForStartup` state/queue contract and keeps provider-return assertions on `RedetectGame`. |
| `source/CalradiaForge.Tests/Core.Tests/GamePlatform/GamePlatformDetectionResolverTests.cs` | Added | `GamePlatformDetectionResolverTests`; `DetectGame` cases | No dedicated application resolver suite existed; six tests cover alternate libraries, missing Workshop, BLSE commit/clear, stale Workshop rejection, and complete failure replacement. | Verifies the provider-returning subordinate boundary and coherent configuration commits. |
| `source/CalradiaForge.Tests/Core.Tests/GamePlatform/GamePlatformTestFixture.cs` | Added | `GamePlatformTestFixture`; fake/throwing providers; Steam and manual-install builders | Detection fixtures were duplicated or coupled to the deleted helper suite; shared builders now create deterministic installs, metadata, Workshop roots, BLSE, and resolver results. | Test-only infrastructure centralizes the new service/resolver contract. |
| `source/CalradiaForge.Tests/Core.Tests/GamePlatform/StartupNotificationQueueTests.cs` | Added | `StartupNotificationQueueTests`; readiness, FIFO, cancellation, retry cases | No queue coverage existed; five tests verify wait-before-ready, ordered removal, idempotent readiness, cancellation preservation, and failed-delivery retry. | Validates Core queue semantics without claiming WPF toast rendering. |
| `source/CalradiaForge.Tests/Core.Tests/GamePlatform/SteamInstallationResolverTests.cs` | Added | `SteamInstallationResolverTests`; 14 metadata/path cases | Resolver cases were mixed into the helper suite; the dedicated suite covers main/alternate libraries, Workshop precedence, malformed or missing metadata, duplicates, deterministic fallback, unregistered roots, and unsafe paths. | Separates Steam mechanics from application workflow and supplies fake-root safety coverage, not real-client runtime proof. |
| `source/CalradiaForge.Tests/Core.Tests/Modpacks/SteamMultiLibraryNovusRegressionTests.cs` | Modified | `ImportedNovusPreset_AfterAlternateSteamLibraryScan_HasNoMissingWorkshopModules` | The regression called a removed helper API; it now enters through `GameDetectionService.RedetectGame`, asserts committed provider/path state, and supplies the required launcher marker. | Verifies the authoritative pipeline discovers eight local plus three Workshop modules with no missing Novus entries. |
| `source/CalradiaForge.Tests/Core.Tests/Mods/ModScannerTests.cs` | Modified | split-library scan; provider-gated Workshop scan; empty-Workshop scan | Prior coverage characterized non-Steam misclassification and lacked empty-path safety; it now asserts unique module IDs, non-Steam Workshop exclusion, and safe local-only Steam scans without a Workshop path. | Locks provider/path gating and guards against duplicates or broad scans from empty Workshop configuration. |
| `source/CalradiaForge.Tests/Core.Tests/Mods/ModServiceTests.cs` | Added | `RefreshAsync_WhenGameConfigurationIsInvalid_PreservesMemoryAndDiskCache`; `RefreshAsync_WhenProviderRequiresManualConfiguration_PreservesValidLookingCache` | Cache preservation during invalid/manual-required configuration was untested; two tests verify no mutation of memory, deltas, current cache, or backup cache. | Directly covers the new non-destructive refresh guard. |
| `source/CalradiaForge.Tests/Core.Tests/Paths/GamePathsHelperTests.cs` | Deleted | `GamePathsHelperTests`; helper integration and Steam metadata cases | The monolithic suite targeted the deleted helper; its coverage moved to workflow, platform-resolver, Steam-resolver, scanner, cache, and Novus suites. | Intentional test migration adds queue/manual/failure-state coverage while removing obsolete helper API dependencies. |

</details>

</details>

<details open>
<summary><strong>v0.13.23</strong> - Internal build: Phase 4 test and provisional benchmark infrastructure plus a partially reverted Steam multi-library implementation.</summary>

**Source comparison:** `710881c...1f037b1`

**Status:** Mapped from committed broken internal build; `dotnet build source/CalradiaForge.slnx -c Debug --no-restore` fails with 13 errors

**Changed implementation/test/build files:** 36

**Scope rule:** Includes every committed source-code, test, benchmark, runner, project, and solution file in this build range. The benchmark README, all `docs/` changes, and `.gitignore` are excluded as documentation-only or repository housekeeping.

| Area | Files | Summary |
|---|---:|---|
| Solution and test-project foundation | 2 | Added the xUnit project and registered the test and benchmark projects in the solution. |
| Core correctness and regression coverage | 14 | Added isolated tests for configuration, persistence, modpacks, parsing/scanning, installers, extraction, logging format, results, and Steam/Novus scenarios. |
| Benchmark project and runner | 3 | Added the BenchmarkDotNet executable project, entry point, and metadata-validating Phase 4 PowerShell runner. |
| Benchmark cases and fixtures | 5 | Added provisional parser, scanner, modpack-validation, and mod-cache benchmarks with managed fixture ownership. |
| Platform namespace and scaffolding | 4 | Moved Epic and provider types under `Infra.GamePlatform` and added an empty future detection-resolver placeholder. |
| Steam resolution components | 4 | Added registry isolation, library/manifest resolution, structured results, and bounded KeyValues parsing. |
| Localization and incomplete UI/path integration | 3 | Added missing-Workshop strings and UI calls to path APIs absent from the committed legacy helper, leaving the build broken. |
| Core test visibility | 1 | Granted the test assembly access to internal Core types. |

<details>
<summary><strong>Detailed file map</strong></summary>

| File | Change | Key Identifiers | Original vs Updated | Summary |
|---|---|---|---|---|
| `source/CalradiaForge.slnx` | Modified | `CalradiaForge.Tests`; `CalradiaForge.Benchmarks`; solution items | The solution contained only application projects and two root files; it now registers the test and benchmark projects and exposes additional documentation as solution items. | Makes the Phase 4 developer projects part of solution build orchestration; the current accepted solution does not compile because of the incomplete Steam integration described below. |
| `source/CalradiaForge.Tests/CalradiaForge.Tests.csproj` | Added | `net10.0`; `Microsoft.NET.Test.Sdk`; `xunit`; `xunit.runner.visualstudio`; `coverlet.collector`; Core `ProjectReference` | No checked-in test project existed; the new non-packable project references Core and reserves Core, Nexus, and UI ownership folders. | Establishes the approved xUnit test surface without adding WPF or Nexus runtime dependencies. |
| `source/CalradiaForge.Tests/Core.Tests/Config/AppConfigTests.cs` | Added | `AppConfigTests`; `Load_WhenFileIsMissing_CreatesReadableJson`; `Settings_WhenConfigIsCorrupt_SeedTypedDefaultsWithoutCrashing`; `Indexer_PersistsUpdatedValuesImmediately` | Configuration behavior lacked automated coverage; the tests use isolated files to cover creation, malformed JSON fallback, typed defaults, and immediate persistence. | Protects accepted configuration persistence behavior without touching real user configuration. |
| `source/CalradiaForge.Tests/Core.Tests/Logging/SerilogTextFormatterTests.cs` | Added | `SerilogTextFormatterTests`; `Format_RendersMessageContextThreadPropertiesAndException` | Neutral formatter output had no automated test; the new test verifies rendered message, context, thread, properties, and exception text. | Covers the existing neutral presentation formatter only; it does not claim Phase 5.A logger lifecycle coverage. |
| `source/CalradiaForge.Tests/Core.Tests/Modpacks/ModpackDataTests.cs` | Added | `ModpackDataTests`; `SaveModpack_RoundTripsThroughSanitizedFileName`; `LoadAllModpacks_SkipsInvalidJsonWithoutDiscardingValidFiles`; `SaveLastUsed_RoundTripsSeparatelyFromNamedModpacks` | Named and last-used modpack persistence lacked isolated regression tests. | Covers filename sanitization, malformed-file isolation, and the separate last-used store. |
| `source/CalradiaForge.Tests/Core.Tests/Modpacks/ModpackServiceTests.cs` | Added | `ModpackServiceTests`; `LoadAll_WhenVanillaIsMissing_CreatesDefaultOnDisk`; `ValidateLoadOrder_ReturnsMissingEntriesWithoutMutatingSavedOrder`; `SaveAs_ClonesCallerEntriesBeforePersisting`; `ImportAndExport_RoundTripThroughServiceWorkflow` | Modpack workflow behavior was unprotected by automated tests. | Covers default creation, validation, cloning, import, and export through the authoritative service. |
| `source/CalradiaForge.Tests/Core.Tests/Modpacks/SteamMultiLibraryNovusRegressionTests.cs` | Added | `SteamMultiLibraryNovusRegressionTests`; `ImportedNovusPreset_AfterAlternateSteamLibraryScan_HasNoMissingWorkshopModules`; `TestSteamClientRootProvider`; `NovusSteamFixture` | No end-to-end fake-root Steam/Workshop-to-Novus regression existed; the new test calls an integration API absent from the committed helper. | Records the intended 8-local plus 3-Workshop Novus contract, but it cannot compile or verify the accepted `HEAD`. |
| `source/CalradiaForge.Tests/Core.Tests/Mods/BLSEInstallerTests.cs` | Added | `BLSEInstallerTests`; `IsBLSEArchive_RequiresStandaloneExecutableMarker`; `InstallAsync_UsesPlatformBinAndUpdatesConfiguredExecutablePath` | BLSE marker detection and platform-bin installation lacked focused tests. | Covers current BLSE detection, copy, and configured executable-path behavior without claiming deferred allowlist enforcement. |
| `source/CalradiaForge.Tests/Core.Tests/Mods/ModExtractorTests.cs` | Added | `ModExtractorTests`; `FindModRoot_FollowsSingleDirectoryWrappers`; `CleanupTempDirectory_RefusesUnmanagedDirectory`; `ExtractToTempResultAsync_*` | Archive root discovery, containment, extraction, and managed cleanup lacked automated coverage. | Exercises the authoritative extractor with generated contained and traversal archive fixtures. |
| `source/CalradiaForge.Tests/Core.Tests/Mods/ModInstallerTests.cs` | Added | `ModInstallerTests`; `IsAcceptedArchive_UsesApprovedExtensionSet`; `StartInstallAsync_*`; `RunInstallAsync` | Normal-module extension, preflight, target preservation, and install flow lacked integration-style tests. | Covers accepted extensions, successful installation, multi-module blocking, and identity mismatch through `ModInstaller`. |
| `source/CalradiaForge.Tests/Core.Tests/Mods/ModParserTests.cs` | Added | `ModParserTests`; `Parse_ReadsAttributesInnerTextAndDeduplicatesDependencies`; `Parse_WhenRootIsNotModule_ReturnsNull` | Module XML parsing behavior lacked focused coverage. | Protects supported XML representations, dependency deduplication, and invalid-root rejection. |
| `source/CalradiaForge.Tests/Core.Tests/Mods/ModScannerTests.cs` | Added | `ModScannerTests`; `ScanForModsAsync_*`; `SplitDriveFixture` | Scanner merging and filtering lacked fake-root regression coverage; the suite now includes 11-module split-root, eight-module known-bug, configured Workshop, filtering, and missing-override cases. | Proves configured-root scanner behavior independently, while retaining a characterization of the unresolved provider/detection failure. |
| `source/CalradiaForge.Tests/Core.Tests/Paths/GamePathsHelperTests.cs` | Added | `GamePathsHelperTests`; `ResolveAndApplySteamGamePaths_*`; `ResolveBannerlord_*`; `TryRepairSteamProviderForGameFolder_*`; `FakeSteamClientRootProvider`; `SteamPathFixture` | No resolver/path test suite existed; the new suite models library discovery, manifest validation, precedence, diagnostics, traversal, and provider repair. | Direct resolver tests are present, but helper-integration tests reference missing methods and are among the compile failures at this `HEAD`. |
| `source/CalradiaForge.Tests/Core.Tests/Persistence/ModsDataTests.cs` | Added | `ModsDataTests`; `SaveAndLoadCurrent_RoundTripsModuleMetadata`; `LoadCurrent_WhenCurrentIsCorrupt_RecoversAndRepairsFromBackup`; `RotateDataFiles_WhenCurrentIsInvalid_PreservesExistingBackup`; `ClearCache_RemovesCurrentAndBackupFiles` | Mod-cache persistence and recovery lacked isolated coverage. | Protects Phase 3 atomic persistence, recovery, rotation, and cleanup behavior. |
| `source/CalradiaForge.Tests/Core.Tests/Results/ModInstallSummaryTests.cs` | Added | `ModInstallSummaryTests`; `Counts_ExcludeBLSEFromNormalInstalledTotal`; `ToSummaryString_SurfacesFirstNormalFailureReason` | Install summary counting and failure detail lacked tests. | Covers BLSE exclusion from normal totals and first normal failure reporting. |
| `source/CalradiaForge.Tests/Core.Tests/Support/TestDirectory.cs` | Added | `TestDirectory`; `CreateDirectory`; `WriteFile`; `CreateModule`; `CreateZip`; `Dispose` | Tests had no shared isolated fixture owner. | Centralizes temporary directory, module XML, zip generation, and cleanup without accessing real installations or user data. |
| `source/CalradiaForge.Benchmarks/CalradiaForge.Benchmarks.csproj` | Added | `net10.0`; `BenchmarkDotNet` `0.15.2`; DiagnosticsHub diagnosers; Core `ProjectReference` | No benchmark project existed; the new executable project references Core and reserves Core, Nexus, and UI benchmark folders. | Establishes the approved developer-only benchmark dependency boundary. |
| `source/CalradiaForge.Benchmarks/Program.cs` | Added | `Program`; `Main`; `BenchmarkSwitcher` | No benchmark entry point existed. | Routes command-line filters and artifacts options into BenchmarkDotNet discovery and execution. |
| `source/CalradiaForge.Benchmarks/run-phase4-benchmarks.ps1` | Added | `Filter`; `ArtifactsPath`; `phase4-environment.txt`; `BenchmarkDotNet.Artifacts`; report validation | No standard Phase 4 benchmark runner existed; the script runs Release benchmarks, records branch/commit/tree/.NET metadata, and rejects missing or `NA` result reports. | Labels output as infrastructure validation/provisional and not comparable to the final post-refactor baseline. |
| `source/CalradiaForge.Benchmarks/Core.Benchmarks/ModParserBenchmarks.cs` | Added | `ModParserBenchmarks`; `DependencyCount`; `Setup`; `ParseModuleXml`; `Cleanup` | Parser scaling had no repeatable benchmark case. | Adds a short-run, memory-diagnosed provisional component benchmark with generated dependency fixtures. |
| `source/CalradiaForge.Benchmarks/Core.Benchmarks/ModScannerBenchmarks.cs` | Added | `ModScannerBenchmarks`; `ModuleCount`; `Setup`; `ScanModules`; `Cleanup` | Module scanning had no controlled filesystem benchmark. | Adds fake-root end-to-end scanner measurements without using real Bannerlord or Steam data. |
| `source/CalradiaForge.Benchmarks/Core.Benchmarks/ModpackValidationBenchmarks.cs` | Added | `ModpackValidationBenchmarks`; `ModuleCount`; `Setup`; `ValidateLoadOrder` | Load-order validation scaling had no benchmark case. | Measures representative validation workloads as a provisional component baseline. |
| `source/CalradiaForge.Benchmarks/Core.Benchmarks/ModsDataBenchmarks.cs` | Added | `ModsDataBenchmarks`; `ModuleCount`; `Setup`; `LoadCurrent`; `SaveCurrent`; `Cleanup` | Mod-cache persistence had no controlled filesystem benchmark. | Measures current load and save operations with generated module data and isolated paths. |
| `source/CalradiaForge.Benchmarks/Core.Benchmarks/Support/BenchmarkFixtureDirectory.cs` | Added | `BenchmarkFixtureDirectory`; `CreateDirectory`; `WriteFile`; `CreateModule`; `Dispose` | Benchmarks had no shared fixture-lifetime helper. | Owns generated benchmark directories and cleanup outside measured operations. |
| `source/CalradiaForge.Core/Infra/GamePlatform/Epic/EpicDetector.cs` | Renamed/Modified | namespace `CalradiaForge.Core.Infra.GamePlatform.Epic`; `EpicDetector` | The type lived under `Infra.Paths`; the file and namespace moved without material detector logic changes. | Begins platform-specific namespace separation; callers require the updated namespace. |
| `source/CalradiaForge.Core/Infra/GamePlatform/Epic/EpicManifestReader.cs` | Renamed/Modified | namespace `CalradiaForge.Core.Infra.GamePlatform.Epic`; `EpicManifestReader` | The type lived under `Infra.Paths`; the file and namespace moved without material manifest-reader logic changes. | Keeps Epic metadata handling grouped under the platform boundary. |
| `source/CalradiaForge.Core/Infra/GamePlatform/GamePlatformDetectionResolver.cs` | Added | `GamePlatformDetectionResolver` | No cross-platform resolver placeholder existed; the new internal class is empty except for planning comments. | This is scaffolding only and must not be interpreted as an implemented detection coordinator. |
| `source/CalradiaForge.Core/Infra/GamePlatform/GameProvider.cs` | Renamed | namespace/path `Infra.GamePlatform`; `GameProvider` | The enum moved from the Paths folder with no content change. | Rehomes provider identity under the new platform namespace, requiring updated imports at call sites. |
| `source/CalradiaForge.Core/Infra/GamePlatform/Steam/ISteamClientRootProvider.cs` | Added | `ISteamClientRootProvider`; `WindowsSteamClientRootProvider`; `GetSteamClientRoot`; registry constants | Steam registry lookup was embedded in `GamePathsHelper`; the new public boundary and Windows implementation isolate the current-user registry read. | Enables fake-root provider substitution without adding a global hook, but is not wired into the committed application helper. |
| `source/CalradiaForge.Core/Infra/GamePlatform/Steam/SteamInstallationResolver.cs` | Added | `ISteamInstallationResolver`; `SteamInstallationResolver`; `ResolveBannerlord`; `DiscoverLibraryRoots`; `TryResolveGameFromLibrary`; `ResolveWorkshopPath`; `WorkshopCandidate` | No defensive multi-library/manifest resolver existed; the new resolver normalizes roots, parses library/app/workshop metadata, validates containment, and selects one Workshop path with diagnostics. | Implements a reusable Core resolver component, but application integration was partially reverted and is absent at the accepted `HEAD`. |
| `source/CalradiaForge.Core/Infra/GamePlatform/Steam/SteamResolutionResult.cs` | Added | `SteamResolutionStatus`; `WorkshopPathSource`; `SteamPathDiagnostic`; `SteamResolutionOptions`; `SteamResolutionResult`; `IsGameResolved` | Steam path discovery previously returned only configuration side effects and a boolean. | Adds structured statuses, options, selected paths, and diagnostic evidence for the standalone resolver. |
| `source/CalradiaForge.Core/Infra/GamePlatform/Steam/ValveKeyValuesParser.cs` | Added | `ValveKeyValuesParser`; `Node`; `TryParse`; `Parser`; `Tokenizer`; `TokenKind`; `Token` | Core had no local VDF/ACF parser. | Adds a bounded internal KeyValues1 parser supporting objects, quoted/unquoted text, comments, escapes, BOM, and format errors. |
| `source/CalradiaForge.Core/Infra/Localization/TranslationStrings.cs` | Modified | `Toast_SteamWorkshopNotFoundTitle`; `Toast_SteamWorkshopNotFoundMessage`; default strings; `Apply` | No localized fallback existed for a resolved Steam game without Workshop content. | Adds English fallback and translation application keys consumed by the Settings warning path. |
| `source/CalradiaForge.Core/Infra/Paths/GamePathsHelper.cs` | Modified | `using CalradiaForge.Core.Infra.GamePlatform.Epic`; legacy `TryAutoDetectGameFolder`; legacy `TryDetectSteam` | The Epic namespace import changed after the file move, but the old registry/single-root Steam implementation otherwise remains. The resolver-application, re-detection, manual-repair, and result APIs expected by UI/tests are absent. | This partial regression/revert is the central compatibility break: Steam resolution is not integrated and dependent projects fail compilation. |
| `source/CalradiaForge.Core/Properties/AssemblyInfo.cs` | Modified | `InternalsVisibleTo("CalradiaForge.Tests")`; `ObfuscateAssembly` | Assembly metadata exposed no internals to tests; it now grants the test project access while preserving obfuscation metadata. | Supports direct testing of internal Core helpers and result contracts. |
| `source/CalradiaForge.UI/Pages/SettingsPage.xaml.cs` | Modified | `GamePlatformDetectionResult`; `ApplyManualGameFolderSelection`; `RedetectGamePaths`; `ShowSteamWorkshopWarningIfNeeded`; `TranslationStrings`; toast severity | Settings previously assigned a selected path directly and called the legacy auto-detect method; it now expects structured manual repair/re-detection and displays localized missing-Workshop warnings. | The UI intent is documented, but its required Core type and methods are absent, producing four of the accepted build's compilation errors. |

</details>

</details>

<details open>
<summary><strong>v0.13.22</strong> - Internal build: Phase 3 persistence and archive/install safety plus the accepted UI configuration-reference repair.</summary>

**Source comparison:** `1b23045...710881c`
**Status:** Mapped from committed build diff
**Changed source files:** 11
**Scope rule:** Includes every committed source-code file in this build range; documentation-only files are excluded.

| Area | Files | Summary |
|---|---:|---|
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
| `source/CalradiaForge.UI/App.xaml.cs` | Modified | `AppConfig` → `AppSettingsInstance`, `ModManagerService`, `InitializeConfiguration`, `InitializeModServices`, `InitializeTranslatorService` | The static configuration facade was named `AppConfig`; it is renamed to `AppSettingsInstance`, the affected references inside `App` are updated, and a nullable `ModManagerService` placeholder is added. | Records the committed UI configuration-surface rename as part of this build's complete source diff. |

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
