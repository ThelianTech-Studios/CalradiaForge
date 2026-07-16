# Phase 5 - Game Platform Detection and Path Workflow Rewrite

## 1. Purpose and status

**Status: planned.** Phase 5 repairs the game-platform detection and path workflow so a valid Bannerlord installation can be recognized independently of Workshop availability. It is pending production implementation, production integration, test migration, build verification, and owner Steam split-library smoke verification. This document is the sole detailed Phase 5 authority; it does not authorize implementation by itself.

## 2. Authority and source-of-truth rules

Current source and the current source-state audit determine what exists. The locked decisions in this document determine the future Phase 5 architecture. Historical prompts, audits, and prior refactor text are evidence only and must not upgrade planned work into implemented behavior. Preserve the UI-decides-when/Core-decides-how boundary and keep Core WPF-free.

## 3. Audited current source state

- `GamePathsHelper` remains the active detection owner. Its legacy automatic flow tries Steam, conditionally tries an unsupported-platform branch, and then classifies failure as `StandAlone`.
- The legacy Steam branch reads the main client root from the registry and assumes both the game and Workshop content are below that root.
- `SteamInstallationResolver`, `SteamResolutionResult`, `ISteamClientRootProvider`, Valve KeyValues support, and Steam diagnostics already exist as reusable multi-library resolution components, but are orphaned from the active flow.
- `GamePlatformDetectionResolver` is an empty or placeholder coordinator. Startup still uses the legacy helper, while Settings and tests refer to reverted or incomplete APIs that are not present in the current Core state.
- `AppConfigSettings` is the authoritative owner of provider/game/launcher/Workshop/BLSE values, persistence, logging, and change notifications. `ModulesDirectoryPath` is derived from `GameFolderPath`.
- `ModScanner` scans Workshop only for `GameProvider.Steam`; `ModService` must receive only the minimum safety adjustment needed to avoid replacing an active cache with an empty result solely because game configuration is invalid.

## 4. Confirmed root-cause chain

The main Steam client root is a metadata discovery entry point, not proof that Bannerlord or its Workshop content lives there. A Bannerlord installation in another registered Steam library fails the legacy main-root assumption. The same branch also treats a missing Workshop folder as Steam failure, so the top-level fallback incorrectly selects `StandAlone`. That provider suppresses Workshop scanning even if valid game and Workshop roots are configured. The scanner diagnostic established that the scanner can combine split roots when given `GameProvider.Steam`, a valid game root, and a valid Workshop root: 8 local modules plus 3 Workshop modules equals 11; the same roots with `StandAlone` return only the 8 local modules. The defect is therefore upstream detection/provider assignment, not basic scanner merging.

## 5. Steam split-library installation model

```text
C:\Program Files (x86)\Steam\
    steamapps\libraryfolders.vdf

D:\SteamLibrary\
    steamapps\appmanifest_261550.acf
    steamapps\common\Mount & Blade II Bannerlord\
    steamapps\workshop\appworkshop_261550.acf
    steamapps\workshop\content\261550\<WorkshopItemId>\<ModuleFolder>\SubModule.xml
```

The client root on `C:` is only the discovery entry point. Neither the game, Workshop content, nor their library may be assumed to be on the client drive. Do not hardcode drives or recursively search mounted drives.

## 6. Existing reusable Steam resolver inventory

Phase 5 must audit and reuse the existing narrow registry/root boundary (`ISteamClientRootProvider`), `SteamInstallationResolver`, `SteamResolutionResult`, Valve KeyValues parsing, and current diagnostic/status types. Inspect every registry location the current root provider supports before changing it. Do not move registry access into the UI, create a competing resolver, a Steam adapter, or generic platform interfaces.

## 7. Target source layout

Keep the current folder organization. The minimal planned Core layout is `Infra/GamePlatform/` containing the rewritten `GamePlatformDetectionResolver`, new `GameDetectionService`, existing `GameProvider`, existing Steam files, and existing platform files in their current locations. A compact Core-safe startup-notification model follows existing `Core.Models` conventions. No broad subsystem tree, candidate records, request/status/config-applier fragmentation, generic result hierarchy, or placeholder symmetry folders are introduced. Phase 5 may normalize `GameProvider`'s stale namespace if source confirms it remains under old `Infra.Paths` ownership.

## 8. Provider semantics

Inspect persisted enum/serialization compatibility before changing numeric order. `NotInitialized` means detection has not run; `Steam`, `EpicGames`, `GamePass`, and `GOG` require positive detection or reliable manual inference; `StandAlone` means a valid manually selected installation has no recognized signature; and `ManualConfiguration` means all enabled automatic detection failed. `StandAlone` is manual-only. Automatic failure must end at `ManualConfiguration`, never `StandAlone` or GOG.

## 9. Automatic detection workflow

Rewrite `GamePlatformDetectionResolver` in place. Its automatic public operation accepts `AppConfigSettings` and returns `GameProvider`; individual platform branches accept that settings instance and return `bool`. Its order is Steam, then Epic/GamePass/GOG only when the existing unsupported-platform toggle is enabled, then `ManualConfiguration`. Keep that toggle disabled by default. Steam is the supported implementation target; other automatic branches remain preserved but disabled pending direct testing, and mocked tests do not prove real-world support.

Resolve and validate required values in locals before the first settings assignment. A successful branch sets provider, game folder, launcher, provider-specific values, and optional BLSE state directly through `AppConfigSettings`; an expected negative result returns `false` with no changes. Unexpected branch errors are logged and return `false` so the next enabled branch can run. The coordinator may have one final defensive boundary before applying the automatic-failure state; do not add transaction, rollback, or result-contract architecture.

## 10. Steam client, library, and manifest resolution

Start at `<SteamClientRoot>\steamapps\libraryfolders.vdf`. Include the client root as a candidate; parse registered Steam library roots using Valve KeyValues parsing, normalize paths, deduplicate case-insensitively, preserve deterministic discovery order, and safely skip inaccessible/missing roots. Log useful technical diagnostics. Do not guess unrelated folders when Steam metadata is available and do not parse nested VDF/ACF files with regular expressions.

For each registered library, inspect `<LibraryRoot>\steamapps\appmanifest_261550.acf`. Require `AppState.appid = 261550` and a valid `AppState.installdir`, construct `<LibraryRoot>\steamapps\common\<installdir>`, and validate it with the existing game-path validator. The library whose manifest-backed path validates is the Bannerlord library root. A validated Steam game succeeds even if no Workshop content exists.

## 11. Workshop primary/fallback selection

Keep game and Workshop libraries distinct and select exactly one active Workshop root.

1. Prefer `<BannerlordLibraryRoot>\steamapps\workshop\content\261550` when valid; commit it and log that the Bannerlord library supplied it.
2. Only if the primary candidate is unavailable, inspect other registered libraries. Prefer a candidate with valid/parseable `appworkshop_261550.acf`, otherwise a valid `content\261550` directory, breaking ties by stable library order.
3. Log acceptance/rejection reasons and when the selected Workshop root differs from the game library.
4. If no candidate is valid, use no Workshop path. Never merge multiple roots.

Missing, empty, malformed, or inaccessible Workshop content is non-fatal: Steam, game folder, and launcher still succeed and `SteamWorkshopFolderPath` becomes empty. It must not cause `StandAlone`, `ManualConfiguration`, local-scan failure, or BLSE failure.

## 12. Direct configuration commit rules

`AppConfigSettings` remains the sole committed state: `GameProvider`, `GameFolderPath`, `GameLauncherFilePath`, `SteamWorkshopFolderPath`, and `BLSEExePath`. Do not duplicate these values in a detection model. When automatic Steam detection actually runs, replace Workshop with the selected value or `string.Empty`. Steam success replaces game/launcher and replaces or clears BLSE. Epic/GamePass/GOG success replaces provider/game/launcher, clears Workshop, and replaces or clears BLSE. Every-enabled-branch failure must set `ManualConfiguration` and clear all provider-derived paths; it must not restore an earlier configuration.

## 13. Game launcher and modules-path behavior

Every successful newly detected or manually selected game path must resolve and validate the launcher before committing. Startup validity first evaluates the base configured game installation; Workshop and BLSE do not independently invalidate it, and launcher validity only does so if current source proves that requirement. Use `AppConfigSettings.ModulesDirectoryPath`, derived from `GameFolderPath`; do not retain duplicate modules-path helpers.

## 14. Optional BLSE behavior

After accepting the base game folder, check the current-source BLSE executable location under `bin\Win64_Shipping_Client`. Set `BLSEExePath` when present; clear stale state when absent or on an exception, log unexpected errors, and never fail base detection or change provider inference solely because BLSE is absent.

## 15. Failed detection and stale-path clearing

Steam absent, manifest absent, Workshop absent, BLSE absent, and a gated branch skipped are normal negative states. On total enabled-branch failure, clear provider-derived game, launcher, Workshop, and BLSE values and commit `ManualConfiguration`. Never treat automatic failure as `StandAlone`, never fall back to GOG, and never leave stale Steam Workshop state when another provider succeeds.

## 16. Startup initialization behavior

`GameDetectionService` owns startup initialization. Its startup operation is `void` and commits through `AppConfigSettings`. It first validates existing configured game state. A valid existing configuration is reused without rerunning automatic detection, preserving any existing Workshop setting and emitting no repetitive success toast. Missing or invalid configuration runs automatic detection and queues one appropriate startup notification. Startup must not rely on a manual Workshop override during a detection run: automatic Steam detection replaces it with the resolver result or empty.

## 17. Settings re-detection behavior

The thin Core workflow also owns explicit Settings re-detection. UI decides when to invoke it and maps the returned provider/current settings to immediate user feedback; Core owns validation, order, fallback, commits, and technical logs. Explicit re-detection runs automatic detection and therefore replaces or clears Workshop. Do not restore missing helper APIs solely to satisfy stale Settings callers.

## 18. Manual game-folder configuration

Manual selection is a separate `bool` operation, not a full automatic pass. Validate and normalize the selected game folder, resolve a valid launcher, infer provider only from reliable normalized path signatures, set provider/game/launcher directly, clear old Workshop, run optional BLSE enrichment, and return `true`. Invalid selections return `false` without changes. Infer `Steam` from a reliable `steamapps/common` segment, the gated providers from reliable platform signatures, and `StandAlone` only when no reliable signature exists. Regex or normalized segments may support manual classification only; never use regex to parse VDF/ACF metadata. Do not reverse-match the folder through the full Steam resolver and do not auto-resolve Workshop.

## 19. Manual Workshop configuration

After manual Steam inference, enable the Workshop control but leave the value empty until the user chooses it. `ApplyManualSteamWorkshopFolder(AppConfigSettings config, string selectedWorkshopFolder)` returns `bool`: it requires current Steam provider, validates the selected Workshop root, updates only `SteamWorkshopFolderPath`, and preserves the previous value on invalid input. It must not change provider. Disable this action for all non-Steam, `StandAlone`, `ManualConfiguration`, and `NotInitialized` states.

## 20. Startup notification queue

Add only a compact Core-safe notification model containing toast-relevant message and existing-compatible severity/type, plus a focused Core queue/service. It contains no paths, settings, WPF type, window reference, toast control, or resolver diagnostics. Detection queues before UI readiness; the `MainWindow` constructor starts the asynchronous drain waiter immediately; `MainWindow.Loaded` only signals generic readiness; the waiter then drains FIFO notifications through the existing toast system and removes delivered items. No polling, arbitrary delay, background thread, or disk persistence. Repeated readiness is harmless; cancellation is handled; Settings uses immediate toasts. Inspect existing Core-safe notification types before adding an enum.

## 21. UI/Core ownership boundary

`GameDetectionService` owns startup initialization, re-detection, manual game selection, and manual Steam Workshop selection. Startup initialization is `void`; explicit re-detection returns `GameProvider`; manual operations return `bool`. It contains no WPF dialogs, toast rendering, event subscriptions, navigation, view state, or Steam metadata parsing. UI calls it, maps provider/settings plus whether detection actually ran to toasts, and signals queue readiness. No general `GameDetectionResult` is introduced.

## 22. Scanner/cache safety boundary

`ModScanner` consumes configured paths; it does not discover Steam libraries, infer provider, repair configuration, parse metadata, or own detection. It scans Workshop only for `GameProvider.Steam` with a present valid Workshop root. Steam without Workshop scans local `Modules`, logs the skip, and succeeds locally; non-Steam never scans Workshop. If game configuration is missing/invalid, do not begin a normal scan or replace an active cache with an empty result solely due to invalid configuration. Keep this minimum adjustment narrow: no scanner-result, cache-transaction, load-order, concurrency, or performance redesign.

## 23. Caller migration

Migrate production callers in order: startup initialization; Settings re-detection; manual game selection; manual Workshop selection; remaining helper callers; modules access to `AppConfigSettings.ModulesDirectoryPath`; and Workshop access to `AppConfigSettings.SteamWorkshopFolderPath`. Then remove `GamePathsHelper` after all responsibilities and callers move. Do not retain an obsolete facade unless a specific caller cannot migrate without broadening scope; report that as a stop condition.

## 24. Test migration order and required coverage

Production comes first: provider semantics, resolver rewrite, Steam integration, workflow, manual flows, queue, caller migration, helper removal, minimum scanner/cache safety, production builds and scoped fixes, then obsolete-test migration, focused regressions, and the full suite. Keep valid Steam resolver tests; rewrite stale tests only after final production APIs exist.

Use temporary-directory metadata fixtures for same-library and alternate-library installs, primary and fallback Workshop cases, no Workshop, malformed VDF/ACF, wrong App ID, missing `installdir`, duplicate/inaccessible libraries, deterministic fallback, no drive-wide search, and no false `StandAlone`. Cover direct commits/no partial commits, BLSE clearing, total failure, toggle ordering, manual provider inference, manual Workshop validation, startup reuse/re-detection, queue lifecycle, scan/cache safety, and the 8-local plus 3-Workshop equals 11 unique-module fixture. After contracts exist, confirm a Novus preset containing existing Workshop modules is not reported missing. Owner split-library smoke testing remains a separate evidence gate.

## 25. Documentation alignment after implementation

After production and tests reflect finalized behavior, align the affected architecture/refactor docs and write a completion audit covering the original defect, changed files, final architecture, builds, focused/full tests, unsupported-platform limitations, deviations, and remaining work. Historical audits remain unchanged. This planning task does not claim those future actions are complete.

## 26. Explicit exclusions

Phase 5 excludes full DI migration, broad adapters, MVVM rewrite, generic result contracts, general scanner-result/cache-transaction/load-order redesign, Nexus work, installer/extractor changes, logger-format redesign, secret redaction/sanitization, broad performance work, real-world Epic/GamePass/GOG support claims, and broad source reorganization. Phase 6.A consumes the finalized Phase 5 design; Phase 6.B retains broad logger migration.

## 27. Stop conditions

Stop and report a bounded finding if current Steam resolver contracts materially conflict, a caller requires broad scope expansion, queue work requires toast-framework redesign, minimum safety requires cache redesign, enum persistence blocks provider changes, disabled platform code cannot be preserved, builds expose unrelated failures, source materially differs from the audit, or a required change would introduce WPF into Core.

## 28. Verification matrix

| Scenario | Required result |
|---|---|
| Same Steam library | Steam provider, valid game/launcher, primary Workshop when present. |
| Client and Bannerlord in different registered libraries | Steam provider; alternate-library game; no `StandAlone`. |
| Bannerlord library contains Workshop | Select its primary Workshop root. |
| Bannerlord library lacks Workshop; another library has it | Select one deterministic fallback. |
| Primary and fallback both exist | Select primary; do not merge roots. |
| Valid Steam game without Workshop | Steam succeeds; Workshop empty; local modules scan. |
| Automatic branch throws | Log and continue safely. |
| Unsupported toggle disabled | Epic/GamePass/GOG branches are skipped. |
| All enabled branches fail | `ManualConfiguration`; all provider-derived paths cleared. |
| Valid startup config | Reuse without auto-detection or repetitive toast. |
| Invalid startup config | Detect and queue feedback. |
| Manual Steam path | Steam; Workshop cleared; control enabled. |
| Manual unknown valid path | `StandAlone`. |
| Invalid manual game path | No setting changes. |
| Manual Workshop | Commit only valid Steam selection; reject non-Steam. |
| BLSE absent | Base detection succeeds and stale BLSE clears. |
| Invalid config before scan | No normal scan and no destructive empty-cache replacement. |
| Split-root scanner fixture | 8 local + 3 Workshop = 11 exact unique modules. |
| Novus preset with existing Workshop modules | No false missing-module result. |
| Window not loaded / loaded | Notification remains queued / drains FIFO after readiness. |

## 29. Completion and changelog rules

Future implementation follows this exact order:

1. Re-read the source-state audit and this detailed plan.
2. Inspect current source and working-tree changes.
3. Audit `SteamInstallationResolver` before editing it.
4. Confirm actual Steam result property names and resolver behavior.
5. Inspect `GameProvider` persistence and serialization compatibility.
6. Update provider semantics.
7. Rewrite `GamePlatformDetectionResolver`.
8. Integrate the existing Steam resolver through direct configuration commits.
9. Add `GameDetectionService`.
10. Implement startup validation and reuse.
11. Implement Settings re-detection.
12. Implement manual game-folder classification and commits.
13. Implement separate manual Workshop configuration.
14. Implement optional BLSE enrichment.
15. Add the compact startup-notification model and queue.
16. Start the asynchronous drain waiter during Main Window construction.
17. Signal readiness from `MainWindow.Loaded`.
18. Migrate production callers.
19. Remove `GamePathsHelper`.
20. Apply the minimum scanner/cache safety adjustment.
21. Build the Core and UI production projects.
22. Fix production integration failures within scope.
23. Rewrite obsolete tests against finalized APIs.
24. Add focused resolver, workflow, queue, and scanner regressions.
25. Run focused tests.
26. Run the full test suite.
27. Perform owner Steam split-library smoke verification when available.
28. Align architecture and refactor documentation to verified source.
29. Write the Phase 5 completion audit.
30. Update the changelog only after verification.

This documentation-only repair does not change the changelog.
