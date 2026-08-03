<!--
Historical evidence migrated from the preserved legacy documentation set.
This report retains its original claims for traceability; current source and current verification govern present behavior.
Original preserved path: read_only_old_docs/audits/calradiaforge_steam_multilibrary_fix_verification_2026-07-15.md
-->
# CalradiaForge Steam Multi-Library Fix Verification

Status: Fixed and verified by the required automated acceptance suite; real-install manual smoke remains owner-gated  
Report date: 2026-07-15  
Branch: `dev-V0-14-CodeRefactor`  
Base commit: `798fc3d87d4fa2f9588f8d5b8c4cbb17b7161a75`

## Original Symptom

An end user with the Steam client and Bannerlord in different libraries reported that Workshop modules were not found. The diagnostic fixture reproduced the downstream state: eight local modules were found, but three existing Workshop modules were skipped because failed single-root detection classified the manually selected game as `StandAlone`.

## Confirmed Root Cause

The former `GamePathsHelper` read only the main Steam registry path and derived the game and Workshop folders beneath that one root. It did not parse registered libraries or the Bannerlord app manifest. A valid game outside the client root was therefore missed, and the resulting provider classification disabled the already-correct independent Workshop scanner input. Bannerlord App ID `261550` and `ModScanner` result merging were not the defect.

## Selected Architecture

- `ISteamClientRootProvider` isolates the Windows registry lookup.
- `ISteamInstallationResolver` and `SteamInstallationResolver` discover normalized registered libraries, parse Valve KeyValues metadata, validate Bannerlord manifest identity and install containment, and return `SteamResolutionResult` diagnostics.
- `GamePathsHelper` preserves its startup entry point, applies complete results only after successful game resolution, exposes a distinct explicit re-detection operation, and repairs provider state after manual folder selection.
- `SettingsPage` decides when to re-detect or select paths. Core decides how paths are resolved. Core remains WPF-free.
- `ModScanner` and the Novus import pipeline remain unchanged in production; they consume the corrected configuration.

## Steam Library And Workshop Precedence

1. Include the Steam client root and registered roots from `steamapps/libraryfolders.vdf`; normalize and case-insensitively deduplicate them in discovery order.
2. Identify Bannerlord with `appmanifest_261550.acf`, validate `appid`, use `installdir`, reject rooted/traversal escapes, and validate the derived game folder.
3. During normal detection, preserve a configured Workshop path only when it is a valid existing directory.
4. Otherwise prefer `steamapps/workshop/content/261550` in the Bannerlord library.
5. If absent, select one deterministic alternate library; valid `appworkshop_261550.acf` evidence wins, then discovery order.
6. Do not merge multiple Workshop roots.
7. If no Workshop candidate exists, keep `GameProvider.Steam`, leave the Workshop path empty, scan local modules, retain the Settings selector, and show a localized warning.
8. Explicit re-detection recomputes the Workshop path. An unmatched valid manual game folder is classified as `StandAlone` and stale Workshop state is cleared.

## Source Files Changed

- `source/CalradiaForge.Core/Infra/Paths/ISteamClientRootProvider.cs` â€” registry boundary.
- `source/CalradiaForge.Core/Infra/Paths/SteamInstallationResolver.cs` â€” library, manifest, game, and Workshop resolution.
- `source/CalradiaForge.Core/Infra/Paths/SteamResolutionResult.cs` â€” statuses, selected source, options, and structured diagnostics.
- `source/CalradiaForge.Core/Infra/Paths/ValveKeyValuesParser.cs` â€” bounded internal KeyValues1 parser.
- `source/CalradiaForge.Core/Infra/Paths/GamePathsHelper.cs` â€” startup coordination, application of results, manual provider repair, standalone fallback, diagnostics, and distinct method names.
- `source/CalradiaForge.Core/Infra/Localization/TranslationStrings.cs` â€” localized fallback strings for the missing-Workshop warning.
- `source/CalradiaForge.UI/Pages/SettingsPage.xaml.cs` â€” explicit re-detection, manual classification repair, visibility refresh, and steady-state warning.

## Test Files Changed

- `source/CalradiaForge.Tests/Core.Tests/Paths/GamePathsHelperTests.cs`
- `source/CalradiaForge.Tests/Core.Tests/Mods/ModScannerTests.cs`
- `source/CalradiaForge.Tests/Core.Tests/Modpacks/SteamMultiLibraryNovusRegressionTests.cs`

The active coverage includes main and alternate libraries, missing Workshop content with local scanning, configured-path preservation and rejection diagnostics, primary and deterministic alternate precedence, malformed metadata, wrong/missing manifest values, duplicate and missing libraries, path traversal, manual Steam repair, unmatched standalone classification, exact 11-module integration, and Novus missing-module validation.

## Dependency Changes

No package or framework dependency was added or changed. The resolver uses an internal parser, so no new license, native runtime, Steamworks dependency, or framework compatibility approval is required. `CalradiaForge.Core.csproj` is unchanged.

## Focused Verification

Command:

```powershell
dotnet test source/CalradiaForge.Tests/CalradiaForge.Tests.csproj -c Debug --filter "FullyQualifiedName~GamePathsHelperTests|FullyQualifiedName~ModScannerTests|FullyQualifiedName~SteamMultiLibraryNovusRegressionTests" --no-restore
```

Result: passed, 25; failed, 0; skipped, 0. This includes the exact eight-local plus three-Workshop result and the Novus no-missing-module regression.

## Full Debug Verification

```powershell
dotnet build source/CalradiaForge.slnx -c Debug --no-restore
dotnet test source/CalradiaForge.slnx -c Debug --no-build --no-restore
```

Result: final build succeeded with 0 errors and 162 warnings; tests passed, 57; failed, 0; skipped, 0. Warnings are the repository's existing legacy logger, Windows-platform, and project/reference warnings plus the isolated Windows registry platform warning on the new provider.

## Full Release Verification

```powershell
dotnet build source/CalradiaForge.slnx -c Release --no-restore
dotnet test source/CalradiaForge.slnx -c Release --no-build --no-restore
```

Result: final build succeeded with 0 errors and 425 warnings; tests passed, 57; failed, 0; skipped, 0.

Additional checks passed: `git diff --check`; no WPF reference was found in Core; the Core project package file is unchanged; and `docs/CHANGELOG.md`, `docs/MIGRATION_MAP.md`, and `docs/refactor/migration_map.md` have no task diff.

## Method Naming Audit

Every method declaration in the changed C# files was inventoried for ambiguous reuse. Overloaded coordination/test-seam methods were renamed to distinct operations such as `RedetectSteamGamePaths`, `ResolveAndApplySteamGamePathsWithProvider`, `ResolveSteamInstallationAndApply`, and `TryRepairSteamProviderWithResolver`. Test helper names were made domain-specific. The only repeated declarations are `GetSteamClientRoot` and `ResolveBannerlord`, each required by its interface and implementation; both are intentional interface conformance rather than competing behavior.

## Reviewer Workflow, Findings, And Corrections

Subagent work was bounded and non-overlapping:

- `steam_resolution_audit` inspected registry/config/UI ownership, dependency options, and performed the independent final diff review.
- `detection_test_seam` implemented the fake-root resolver suite in `GamePathsHelperTests.cs` only.
- `scanner_pipeline_analysis` traced scanner/Novus behavior and added the Novus regression file only.

The independent reviewer found four required issues:

1. An unmatched manual folder could retain stale Steam state. The Core helper now applies `StandAlone`, the selected paths, and clears Workshop state before attempting reverse match; Settings refreshes all path visibility. A regression begins from Steam state and verifies fallback.
2. Missing configured and primary Workshop candidates lacked explicit skip diagnostics. `ExistingWorkshopPathRejected` and `PrimaryWorkshopPathUnavailable` plus focused assertions were added.
3. Steam-without-Workshop warnings were limited to manual/re-detect flows and duplicated English. Settings now checks the normal loaded state and uses `TranslationStrings` fallback keys.
4. Planning, architecture, and historical reports were stale. Canonical path architecture and refactor documents were updated, historical reports were labeled pre-fix, and this verification report was added.

The final re-review found that standalone fallback cleared a configured Workshop override before a successful manual Steam match. The helper now snapshots the prior override for resolution, and the matched-manual-selection regression verifies its preservation.

The owner then requested a changed-file method naming audit; ambiguous overload/helper names were corrected and the full verification matrix was rerun.

## Documentation Updated

- `docs/Architecture/ARCHITECTURE_INDEX.md`
- `docs/Architecture/Projects/CalradiaForge.Core.md`
- `docs/Architecture/Systems/Configuration.md`
- `docs/Architecture/Systems/Launcher.md`
- `docs/Architecture/Systems/ModManagement.md`
- `docs/Architecture/Systems/PlatformAndPathDetection.md`
- `docs/refactor/refactor_master_plan.md`
- `docs/refactor/testing_strategy.md`
- `docs/refactor/result_and_workflow_policy.md`
- `docs/refactor/dependency_injection_plan.md`
- The two earlier 2026-07-15 Steam diagnostic reports were retained as historical pre-fix snapshots and linked to this report.

## Manual Smoke Status

Not performed: this environment does not provide a representative real Steam client/library installation and the task must not modify owner Steam data. Owner verification remains required for real C/D library detection, Settings displayed paths and warning, manual Steam repair and unmatched standalone selection, mod refresh, Novus selection without a false toast, and alternate Workshop fallback warning.

## Remaining Limitations And Deferred Work

- Phase 5.A still owns registration of the implemented interfaces in the application DI composition root; the current static ownership preserves existing startup behavior.
- Phase 6 still owns a general structured scanner-result contract. This fix provides structured path-resolution diagnostics without redesigning `ModScanner`.
- Real Steam metadata and Windows UI behavior are covered by manual smoke only; automated tests intentionally use isolated temporary roots.
- Epic/Game Pass work, recursive drive searches, multi-root Workshop merging, Steamworks, Nexus changes, broad logger migration, and unrelated scanner refactors remain excluded.

## Issue Status And Owner Closeout

The reported Steam multi-library path-resolution defect is fixed and verified by the required automated acceptance suite. Manual real-install approval remains required before release documentation.

`docs/CHANGELOG.md` was not updated. `docs/MIGRATION_MAP.md` was not updated. The owner must manually inspect and approve the implementation. Accepted source changes must be committed and pushed to `origin` before the separate changelog and migration-map workflow.

