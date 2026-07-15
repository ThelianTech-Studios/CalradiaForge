# Migration Map

> Historical implementation record: the Phase 2 redaction entries below describe what was previously added. They are not active logging policy; the 2026-07-12 owner decision removed automatic redaction and retained only neutral formatting.

```text
<Metadata>
Last Changelog Version: v0.13.22
Last Git Commit ID: 710881c7cd336032e48197d20b9135e142625d09
Last Git Branch Used: dev-V0-14-CodeRefactor(HEAD)
Last Map Compile Date: 2026-07-14
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
| `v0.13.22` | Atomic persistence, mod-cache recovery, archive/install preflight guardrails, and UI configuration-reference repair | `1b23045...710881c` | Mapped from committed build diff |
| `v0.13.14` | Serilog infrastructure foundation, log-retention configuration, data-helper cleanup, application configuration-property rename, and neutral formatter/redaction-removal follow-up | `37c322e...HEAD` | Mapped from committed build diff |
| `v0.13.6` | Cleanup/nullability/path/logging-message migration rows listed in this document | `dev-release...HEAD` | Mapped from current committed branch diff |

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
