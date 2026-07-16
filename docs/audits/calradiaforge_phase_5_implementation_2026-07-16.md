# CalradiaForge Phase 5 Implementation Audit

Status: Automated implementation verification complete; owner smoke verification and approval pending

Audit date: 2026-07-16

Scope: Phase 5 game-platform detection and path workflow implementation, UI integration, minimum scanner/cache safety, test migration, and supporting documentation alignment.

## Original Defect

The legacy detection path read only the main Steam client root, assumed Bannerlord and Workshop content shared that root, and treated a missing Workshop directory as Steam detection failure. A valid split-library or Steam-without-Workshop installation could therefore become `StandAlone`, suppressing Workshop scanning.

## Implemented Architecture

- `GamePlatformDetectionResolver` reuses the existing Steam metadata resolver and commits provider/game/launcher/Workshop/BLSE settings only after required paths validate.
- A resolved Steam game succeeds without Workshop. Automatic detection replaces or clears Workshop, and total failure clears provider-derived paths before setting `ManualConfiguration`.
- `GameDetectionService` owns startup reuse/detection, Settings re-detection, manual game selection, and Steam-only manual Workshop configuration. Startup initialization is `void`; explicit re-detection returns the detected provider.
- `StartupNotificationQueue` provides a WPF-free FIFO handoff. MainWindow waits from construction, signals readiness from `Loaded`, maps toasts, and handles cancellation.
- The legacy `GamePathsHelper` was removed. Modules access uses `AppConfigSettings.ModulesDirectoryPath`.
- `ModService` rejects invalid base configuration before scan, cache rotation, or save. The broader structured scanner-result/cache contract remains deferred.

## Changed Production Files

- `source/CalradiaForge.Core/Infra/Config/AppConfigSettings.cs`
- `source/CalradiaForge.Core/Infra/GamePlatform/GameProvider.cs`
- `source/CalradiaForge.Core/Infra/GamePlatform/GamePlatformDetectionResolver.cs`
- `source/CalradiaForge.Core/Infra/GamePlatform/GameDetectionService.cs` (added)
- `source/CalradiaForge.Core/Infra/GamePlatform/StartupNotificationQueue.cs` (added)
- `source/CalradiaForge.Core/Models/StartupNotification.cs` (added)
- `source/CalradiaForge.Core/Infra/Mods/ModService.cs`
- `source/CalradiaForge.Core/Infra/Paths/GamePathsHelper.cs` (removed)
- `source/CalradiaForge.Core/Infra/Localization/TranslationStrings.cs`
- `source/CalradiaForge.UI/App.xaml.cs`
- `source/CalradiaForge.UI/Views/MainWindow.xaml.cs`
- `source/CalradiaForge.UI/Pages/SettingsPage.xaml`
- `source/CalradiaForge.UI/Pages/SettingsPage.xaml.cs`
- `source/Languages/en-US.json`
- `source/Languages/de-DE.json`, `es-ES.json`, `it-IT.json`, `pl-PL.json`, `ru-RU.json`, `sv-SE.json`, `tr-TR.json`, and `zh-CN.json` (removed the stale unsupported-platform claim so the accurate default text is used until reviewed translations are supplied)

## Changed Test Files

- `source/CalradiaForge.Tests/Core.Tests/Paths/GamePathsHelperTests.cs` (removed)
- `source/CalradiaForge.Tests/Core.Tests/GamePlatform/GamePlatformTestFixture.cs` (added)
- `source/CalradiaForge.Tests/Core.Tests/GamePlatform/SteamInstallationResolverTests.cs` (added)
- `source/CalradiaForge.Tests/Core.Tests/GamePlatform/GamePlatformDetectionResolverTests.cs` (added)
- `source/CalradiaForge.Tests/Core.Tests/GamePlatform/GameDetectionWorkflowTests.cs` (added)
- `source/CalradiaForge.Tests/Core.Tests/GamePlatform/StartupNotificationQueueTests.cs` (added)
- `source/CalradiaForge.Tests/Core.Tests/Mods/ModServiceTests.cs` (added)
- `source/CalradiaForge.Tests/Core.Tests/Mods/ModScannerTests.cs`
- `source/CalradiaForge.Tests/Core.Tests/Modpacks/SteamMultiLibraryNovusRegressionTests.cs`

## Verification Results

| Verification | Outcome |
|---|---|
| `dotnet restore source/CalradiaForge.slnx` | Passed after approved NuGet network access. The first sandboxed attempt failed with NuGet SSL/authentication errors. |
| Core Debug build | Passed, 0 errors. |
| UI Debug build | Passed, 0 errors. |
| Debug solution build | Passed, 0 errors; 3 existing SevenZipWrapper architecture warnings in the final incremental run. |
| Focused Phase 5 tests | Passed 44/44. |
| Full Debug tests | Passed 76/76. |
| Release solution build | Passed, 0 errors; existing legacy logger, Windows-platform reachability, and SevenZipWrapper architecture warnings remain. |
| Full Release tests | Passed 76/76. |
| Core WPF dependency inspection | No WPF reference introduced. |
| Removed-helper inspection | No `GamePathsHelper` or missing helper API references remain under `source`. |
| Package/project inspection | No package, target framework, project-reference, solution, or version change was made by Phase 5. |

Automated fixtures cover same-library and split-library Steam resolution, Workshop primary/fallback/no-Workshop cases, malformed and unsafe metadata, direct commits and clearing, BLSE set/clear, startup reuse and queued feedback, manual configuration, queue FIFO/cancellation/retry behavior, invalid-cache preservation, an exact 8-local plus 3-Workshop scan, and a Novus preset with no false missing Workshop modules.

## Deviations And Limitations

- Current source contains an Epic detection branch but keeps unsupported automatic platforms disabled. No Game Pass or GOG automatic detector was fabricated, and no real-world Epic/Game Pass/GOG support is claimed.
- Manual non-Steam inference uses conservative normalized path signatures only. It does not reverse-match through Steam metadata.
- Steam resolver option/result types were retained for compatibility, but Phase 5 production automatic detection explicitly disables configured-Workshop preservation.
- The existing structured scanner-result, provenance, cache-transaction, duplicate policy, and load-order redesign remain deferred to Phase 7.
- Logger initialization, ownership, migration, disposal, and archive behavior remain Phase 6 work and were not changed.
- No benchmark run was required because Phase 5 changes correctness contracts and adds no approved performance optimization.

## Required Owner Verification

Before release approval, the owner must manually inspect the source diff and perform a real Windows/Steam split-library smoke test covering startup reuse/detection, Settings re-detection, manual Steam/standalone selection, Workshop selection enablement, startup/immediate toast behavior, local plus Workshop scanning, and the Novus no-false-missing outcome.

This audit does not claim that owner smoke verification passed. It does not update `docs/CHANGELOG.md` or `docs/MIGRATION_MAP.md`.
