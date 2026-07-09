# Refactor Migration Map

```text
<Metadata>
Last Changelog Version: v0.13.6
Last Git Commit ID: 37c322e
Last Git Branch Used: dev-V0-14-CodeRefactor
Last Map Compile Date: 2026-07-09
</Metadata>
```

## Map Rules

This document maps completed source-code changes by changelog version.

Future migration map sections must use the metadata commit as the comparison base. Use `Last Git Commit ID...HEAD` for the next migration diff.

This map reflects committed branch diffs only, not unrelated local working-tree edits that are still uncommitted.

Documentation-only files are intentionally excluded from the migration map unless the owner explicitly changes that policy.

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
| `v0.13.6` | Cleanup/nullability/path/logging-message migration rows listed in this document | `dev-release...HEAD` | Mapped from current committed branch diff |

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
