# Changelog

All notable changes to CalradiaForge will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## 0.14.0 - Public Release | 2026-08-02

> Pre-release stabilization: added defaults-first configuration recovery, fail-closed Steam process inspection, centralized beta version metadata, accepted Phase 9 benchmark infrastructure, and updated third-party software notices.

### Added

- Added one canonical configuration-defaults source, typed load/save/persistence outcomes, and a narrow persistence seam for deterministic recovery testing. Configuration durability loss now produces one actionable startup or runtime warning through the existing notification lifecycle.
- Added explicit Steam process states (`Running`, `NotRunning`, and `Unknown`) with injectable process-inspection, process-start, and delay boundaries. Added deterministic coverage for inspection failures, process disposal, bounded startup polling, Bannerlord/BLSE launch blocking, initialization delay, and unchanged Standalone/GOG launching.
- Added `source/Directory.Build.props` as the shared version source for all projects, with application version `0.14.0-beta`, assembly/file version `0.14.0.0`, and informational version `0.14.0-beta` without a source-revision suffix. Added testable Settings/About formatting that preserves prerelease labels and removes build metadata from display.
- Added the authoritative Phase 9 generated-fixture benchmark categories and runners, plus an explicit-consent, offline, read-only real-installation parser/scanner suite with corpus-stability checks and path-redaction controls.
- Added focused configuration, startup-notification, Steam-launch, and application-version tests.

### Changed

- Changed configuration loading to construct a complete in-memory defaults map before overlaying persisted values. Valid and unknown settings survive; missing, null, or invalid recognized values are repaired; obsolete retention data is removed; and malformed JSON is preserved to a collision-safe sibling backup before replacement.
- Changed expected configuration read/write failure handling so inaccessible files remain untouched, startup can continue with in-memory defaults, current-session setting changes survive failed saves, repeated writes stop after persistence becomes unavailable, and failed EULA persistence does not terminate the current session.
- Changed Steam launch readiness so process-enumeration or disposal failures are warning-logged as `Unknown` instead of being treated as success. Steam is started once when appropriate, polling remains bounded, and Steam installations cannot launch Bannerlord or BLSE without a positive `Running` result.
- Changed Core and UI project metadata to inherit the shared beta version and changed Settings/About to read the entry assembly informational version with a numeric fallback. The prior hardcoded `0.12.15` project versions and misleading `1.0.0` display fallback were removed.
- Renamed the current benchmark runner for the Phase 9 baseline, categorized existing benchmark cases accordingly, and added fixture, input-discovery, consent, redaction, and artifact-validation controls for the supplemental owner-installation measurements. No production performance optimization was implemented.
- Organized `TranslationStrings.ApplyTranslationDictionary` under an explicit source region without changing localization behavior.
- Updated the CalradiaForge software license to version 1.3 for 2026 and added individually formatted third-party notices for the previously unlisted Core/UI runtime dependencies. Test and benchmark package notices remain excluded because those packages are not distributed with the application.

### Fixed

- Prevented malformed, unreadable, locked, access-denied, or unwritable configuration storage from turning recoverable persistence failures into fatal startup or typed-setting exceptions.
- Prevented Steam inspection failures from falsely authorizing a game launch or producing an inaccurate Steam-detected claim.
- Fixed application-version drift between Core, UI, inherited project metadata, and the Settings/About display.

### Verification

- The Phase 9 implementation evidence records successful Debug and Release correctness runs with 272 tests passing in each configuration, 14/14 generated-fixture benchmark cases, and 6/6 consent-gated real-installation parser/scanner cases. Those measurements are offline, warm-cache evidence and are not WPF responsiveness or universal performance baselines.
- The accepted stabilization implementation record reports zero-error Debug and Release solution builds and all 300 tests passing in each configuration, including 18 focused configuration/lifecycle tests, 12 focused Steam-launch tests, and application-version display coverage. It also records `0.14.0-beta` informational metadata across the project set and preservation of the Core WPF boundary.
- The owner manually inspected and approved the accepted source endpoint before this documentation-only closeout. This closeout did not rerun application runtime behavior and does not claim live Steam/Bannerlord, manual WPF, packaged-runtime, or release-candidate validation.
- The existing publish profile's `Any CPU` platform and machine-specific output path were not changed in the accepted source range. This build does not claim versioned repository-relative publish-profile alignment.
- Destructive mod-upgrade recovery, archive resource limits, all pending Phase 10 performance findings, Nexus functionality, credentials, polling, and the broader documentation overhaul remain deferred or excluded.

---

## 0.13.105 - Internal | 2026-08-01

> Refactor Phase 8: moved the retained Launcher, Mod Packs, and Settings presentation workflows into lifecycle-aware ViewModels, completed the approved Settings path feedback and toast-duration follow-up, and retained existing Core workflow ownership.

### Added

- Added retained singleton `LauncherViewModel`, `ModpacksViewModel`, and `SettingsViewModel` presentation owners on a shared awaitable lifecycle foundation, backed by CommunityToolkit.Mvvm commands and observable state.
- Added an awaitable WPF UI-dispatch boundary and narrow UI interaction contracts/adapters for mod-archive selection, modpack import, Settings folder/executable selection, DLL unblocking, and shell folder opening without moving the underlying Core workflows into UI.
- Added focused ViewModel, dispatcher, lifecycle/navigation, composition, toast, and localization coverage, including serialized navigation, accepted-snapshot reconciliation, command availability, manual Workshop selection, language refresh, and dated log-archive behavior.
- Added localized English fallback and generated runtime-language entries for the Launcher ghost-modpack prompt and Steam Workshop valid/invalid indicators.

### Changed

- Moved Launcher install, refresh, launch, filtering, drag/drop, launch-target, and modpack-selection presentation orchestration from `LauncherPage` code-behind into `LauncherViewModel`; moved modpack editing/import/create/save presentation into `ModpacksViewModel`; and moved Settings state, validation, redetection, maintenance, and restart presentation into `SettingsViewModel`.
- Reduced the three retained page code-behind files to one-time `DataContext` wiring and WPF-specific gesture/layout adapters. Their existing `.xaml` filenames and page identities remain unchanged.
- Changed `MainWindow` navigation to serialize initialization, deactivation, display, and activation of retained ViewModels, including deterministic failure handling and one-time startup-ready signaling after successful initial navigation.
- Standardized the fallback duration for non-persistent toasts without an explicit duration to eight seconds. Install terminal notifications now use that shared default, while active install-progress notifications remain persistent until their terminal transition.
- Kept visible Bannerlord and Steam Workshop folder-selection buttons disabled when their configured paths are valid. Steam installations without a valid Workshop folder retain the manual Workshop recovery action, and successful manual selection refreshes the localized validation indicator and command state.
- Changed the visible navigation label and related FAQ text from Launcher to Home, added a localized ghost-modpack label, and regenerated `source/Languages/en-US.json` with the current Launcher, Phase 7 install-notification, and Phase 8 Settings translation keys.
- Changed the ConsoleUtils English-language generator to move the generated file directly into `source/Languages` with overwrite support. The test project now explicitly excludes the reserved `Nexus.Tests` tree, and the solution lists `.gitignore` as a solution item.

### Fixed

- Fixed the successful manual Steam Workshop selection path so it immediately displays the valid green Workshop indicator and continues to use translated validation text after a language change.
- Fixed unnecessary manual path reselection after valid detection by binding button availability to the Settings validation state without hiding the controls.
- Fixed the dated log-lifecycle test so its fixed archive timestamp is evaluated with a matching injected clock instead of the machine's current date.

### Verification

- The accepted implementation record reports zero-error Debug and Release solution builds, all 272 tests passing in each configuration, and all 53 focused Phase 8.E tests passing. It also records successful Core WPF-boundary, toast-duration, affected `async void`, and diff checks; this documentation-only closeout did not rerun runtime behavior.
- The owner manually inspected and approved the accepted source state before this closeout. The underlying Core automatic game-platform-detection and Steam Workshop scanner/path-resolution algorithms, the inherited DLL-unblock result mapping, performance optimization, Nexus functionality, credentials, polling, and application major/minor version metadata were not changed by this build.

---

## 0.13.90 - Internal | 2026-07-24

> Refactor Phase 7: added install-specific terminal outcomes and correlated application notifications, moved required post-install consistency work into the authoritative pipeline, and completed the retained Launcher page naming foundation.

### Added

- Added immutable `ModInstallOperationResult` and `ModInstallProgress` contracts, stable terminal statuses/diagnostic codes, operation and archive correlation, and notification-source interfaces for install progress and completion.
- Added an application-lifetime `InstallNotificationPresenter` and toast sink that translate correlated Core progress and terminal outcomes into one notification lifecycle, including stale-progress rejection and launcher-completion ordering.
- Added manager-owned module DLL-unblocking and install reconciliation seams, plus focused Core/UI tests for terminal classification, cancellation/finalization, progress correlation, presenter behavior, DI activation, startup, and localization.

### Changed

- Changed `ModPipelineManager.InstallAsync` to return a non-null terminal result and to own validation/admission, installer execution, bounded consistency finalization, DLL unblocking, accepted-snapshot reconciliation, terminal publication, and quiescence release without replacing `ModInstaller` or `ModExtractor`.
- Renamed the retained primary surface from `ModsPage` to `LauncherPage`, including navigation, styles, DI references, localization bindings, configuration comments, and launcher-specific translation identifiers; `ModsPage` remains reserved for future dedicated mod management.
- Updated launcher, modpack, settings, main-window, language-selection, toast, and startup-composition integration for the new presenter and retained Launcher identity. Shared ComboBox styling now comes from the Launcher resource dictionary.

### Verification

- The accepted source endpoint recorded successful Debug and Release solution builds and 195 passing tests in each configuration, including Phase 7 manager/result/presenter/rename coverage. Owner approval occurred before this documentation-only closeout; no runtime validation is newly claimed here.
- No Nexus implementation, credential persistence, automatic update check, polling, generic result abstraction, MVVM extraction, or Steam Workshop/path-resolution fix is represented by this build.

---

## 0.13.83 - Internal | 2026-07-23

> Refactor Phase 6.C: completed the application-wide migration from the retired legacy logger to the provider-owned Serilog pipeline, added the narrow pre-Serilog startup-failure fallback, and preserved the single provider-disposal close path.

### Added

- Added `EmergencyStartupLogWriter` for fatal failures before the provider-owned Serilog pipeline becomes operational. It lazily appends UTC-stamped exception details to `CalradiaForge_StartupFailure.log`, tries the application Logs directory followed by narrow Local AppData and temporary-directory fallbacks, and contains all secondary write failures so diagnostics cannot block fatal shutdown.
- Added focused tests for primary emergency-log writes, fallback-directory selection, all-candidates-failed containment, persisted Debug Mode after configuration reload, and the absence of an emergency file during successful provider construction.

### Changed

- Migrated every normal Core and UI legacy logger caller to structured `Serilog.Log` events across configuration, EULA, platform detection, launch, localization, paths, modpack persistence/import, scanning, parsing, extraction, installation, BLSE, DLL unblocking, cache management, the accepted pipeline, retained pages, and the main window.
- Replaced interpolated and anonymous diagnostic payloads with named Serilog properties and exception-first overloads. Routine Debug filtering now belongs to the configured Serilog minimum level; explicit guards remain only where diagnostic payload construction is meaningfully expensive.
- Kept `ConfigFileManager` and `LoggingSettings` independent of ordinary logging during pre-provider bootstrap, reduced duplicate exception and routine language-application events, and preserved their persistence, recovery, notification, and localization behavior.
- Added explicit Serilog-operational state to `App`: fatal startup, dispatcher, task, and AppDomain failures use the emergency writer only before the shared pipeline is available, use Serilog while it is operational, and avoid logging through it after provider disposal begins.
- Changed `AppPaths.LogResolvedPaths(Logger)` to the parameterless `AppPaths.LogResolvedPaths()` structured-logging boundary and migrated its application startup caller.
- Regenerated the committed `en-US.json` runtime language artifact with the accepted lifecycle-confirmation and Steam Workshop-not-found strings already represented by the current translation contract.

### Removed

- Removed the obsolete `Logger` singleton, its parallel `LogLevel` and `MinimumLevel` state, custom session-log files, debugger mirroring, legacy retention cleanup, and debug-payload JSON serialization after the zero-caller inventory passed.
- Removed transitional legacy-level synchronization from `SerilogLoggerFactory`; persisted `LoggingSettings.DebugMode` now configures the shared Serilog pipeline as the sole runtime logging-level authority.

### Verification

- Built the full solution in Debug and Release with zero compilation errors and passed all 156 tests in both configurations. The focused logging/composition set passed all 19 tests.
- Confirmed the Release output excludes `Serilog.Sinks.Debug.dll`, the Debug output retains it, Core remains free of WPF references, and provider disposal remains the only normal Serilog close path.
- Confirmed static inventories contain no normal `Logger.Instance`, `Logger.LogLevel`, legacy logger construction, or competing close path. The four retained level guards protect expensive diagnostic construction.
- The owner manually inspected and approved the accepted source state before this documentation closeout. This docs-only workflow did not perform additional runtime smoke testing and does not claim a Steam Workshop scanner/path-resolution fix.
- Confirmed the accepted build adds no Nexus functionality, startup update checks, timed polling, silent scans, package changes, application major/minor version changes, or credential persistence through `AppConfig`.

---

## 0.13.73 - Internal | 2026-07-23

> Refactor Phase 6.B: established one validated Core/UI dependency-injection provider and an application-owned startup, shutdown, restart, and logging lifecycle, then completed the accepted themed XAML lifecycle-dialog patch while retaining the legacy logger-call migration for Phase 6.C.

### Added

- Added Core and UI service-registration modules with validated storage-path options, one singleton root provider, retained singleton shell/pages, and deferred `MainWindow` resolution so language selection, EULA acceptance, detection, cache loading, the authoritative startup scan, modpack validation, and notification setup finish before the main shell is constructed.
- Added explicit application-lifecycle contracts and coordinators for startup, shutdown, restart, state persistence, tracked-work cancellation and quiescence, startup-notification draining, final provider disposal, replacement-process launch, and best-effort Windows session-ending handling.
- Added a themed, XAML-backed `ConfirmDialogWindow` with its presentation rules isolated in `ConfirmDialogWindowStyles.xaml`. Each invocation creates a fresh owner-assigned modal window with application-theme resources, custom title bar and controls, focus/hover/pressed/disabled visuals, optional scrollable operation details, and center-screen fallback when no usable owner exists.
- Added WPF-neutral confirmation purposes and display models in Core, current-language resolution with English fallback strings for shutdown, restart, delayed shutdown, and active scan/install warnings, plus UI dialog services and policy mapping for those models.
- Added UI-facing seams for deferred shell creation, modal EULA and language selection, application lifetime, startup-notification presentation, and active-work coordination without adding WPF references to Core.
- Added automated UI coverage for validated provider composition, singleton lifetimes, dialog model and policy mapping, lifecycle enum stability, shutdown/finalization behavior, and readiness-gated notification draining. Added Core coverage for logging lifecycle, Serilog construction and disposal, configuration migration, confirmation localization/models, and the already-supported alternate Steam-library layout.

### Changed

- Removed WPF `StartupUri`, set explicit application shutdown mode, and made `App` the sole composition and lifecycle root. It now builds one validated provider, owns ordered startup, routes fatal startup and dispatcher failures through native WPF error presentation, and performs controlled provider disposal exactly once.
- Migrated retained pages, `MainWindow`, EULA, language selection, toast delivery, and their existing Core workflows from static `App` service access and local construction to explicit constructor dependencies. Page localization bindings now use injected `TranslationService` instances while preserving retained-page behavior.
- Moved the initial authoritative mod scan from `ModsPage` into the startup coordinator. The page now consumes the accepted pipeline snapshot while retaining explicit refresh, install, launch, modpack, and settings workflows through injected services.
- Changed normal close to proceed without a prompt while the authoritative mod pipeline is idle and to show one themed warning while tracked work is activ…9391 tokens truncated…ess replaced with SevenZipWrapper, restoring full `.7z` support. | Third-party software notices added to LICENSE.md for all dependencies.

### Added

- `SevenZipWrapper` library — custom 7z.dll COM interop wrapper (`ArchiveFile`, `ArchiveEntry`, `SevenZipHandle`) with format auto-detection from extension and file signature, progress reporting via `onFileExtracted` callback, and `CancellationToken` support that maps to 7z.dll `E_ABORT`
- `.7z` re-added to `ModInstaller._acceptedExtensions` and `FileDialogFilter` — all three formats (`.zip`, `.rar`, `.7z`) now fully supported at native extraction speeds
- Third-Party Software Notices section in `LICENSE.md` — full original license texts for all third-party dependencies: gong-wpf-dragdrop (BSD 3-Clause), MahApps.Metro (MIT), MahApps.Metro.IconPacks Material (MIT), Newtonsoft.Json (MIT), SevenZipWrapper (MIT), ControlzEx (MIT), Microsoft.XamlBehaviors.Wpf (MIT)

### Changed

- `ModExtractor.ExtractToTempAsync` — extraction pipeline rewritten from SharpCompress `IArchive`/`IArchiveEntry` iteration to `SevenZipWrapper.ArchiveFile.Extract()` with overwrite, progress callback, and cancellation token pass-through
- `ModExtractor.EstimateFileCount` — replaced file-size heuristic with exact count via `ArchiveFile.Entries.Count`, falling back to the original size-based estimate only when the archive cannot be opened
- `ModInstaller` extraction callback — `OnFileExtracted` wiring updated for `SevenZipWrapper`'s cumulative file count parameter (same signature, no UI changes needed)
- SharpCompress package reference removed from `CalradiaForge.Core.csproj`; `SevenZipWrapper` project reference added
- `LICENSE.md` — added `# CalradiaForge Software License` title heading for document structure

### Removed

- Temporary `.7z` limitation FAQ entry (Q9) and README workaround — no longer applicable
- `.7z` known-issue references from beta README section

### Fixed

- `.7z` archive extraction performance — SharpCompress LZMA block-compression caused ~25 min extraction for large mods; SevenZipWrapper delegates to native 7z.dll, completing the same archives in seconds

---

## 0.9.22 - Public Release | 2026-02-25

> Open beta release — feature-complete for v1.0 scope with known `.7z` limitation.

### Added

- `.gitignore` updates for beta release packaging
- Beta section in `README.md` with open beta test instructions, known issues, and contribution guidelines
- `LICENSE.md` updated to version 1.2 (all prior versions voided)
- FAQ page entry Q9 for `.7z` limitation with workaround
- `README.md` updated with Supported Mod Archive Formats table, built-in FAQ page reference, and `.7z` limitation entry

### Removed

- `.7z` archive support temporarily disabled — SharpCompress LZMA block-compression caused ~25 min extraction for large mods; accepted formats narrowed to `.zip` and `.rar`

---

## 0.9.19 - Internal | 2026-02-20

> Code cleanup, debug logging, finalized `.editorconfig`, and open beta preparation.

### Added

- Bug report and feature request issue templates added to repository
- Debug logging in conditional logic blocks across Core and UI layers (gated behind debug mode config)
- XML doc summaries on all public/internal members (Core + UI)
- `CONTRIBUTING.md` with architecture overview and design pattern guidelines
- EULA text file included in build output
- Temporary AI-generated app icon placeholder (pending commissioned artwork)
- Unit test project (`CalradiaForge.UnitTests` — MSTest SDK, net10.0) scaffolded and retained; intentionally left empty due to tight coupling with file system and WPF dispatcher

### Changed

- Finalized `.editorconfig` with project coding conventions
- Full code cleanup pass against finalized `.editorconfig` rules
- Updated `.gitignore` for beta release (removed `AssemblyInfo.cs` tracking, suppressed global warning suppressions file)
- Updated build parameters for beta release packaging
- Updated `README.md` with installation instructions and open beta sections; finalized documentation
- Removed duplicate `CONTRIBUTING.md`
- Removed obsolete `Author` key-value pair from configuration

### Fixed

- Missing mods toast firing multiple times on startup — three independent code paths (constructor `PopulateModpackList`, WPF auto-fired `SelectionChanged`, `StartupRescanAsync`) all raced to call `ApplySelectedModpack` with toasts enabled; fixed with `showToast` parameter gating, `_hasCompletedInitialScan` flag, and `suppressToast` constructor parameter
- Missing mods toast firing multiple times on page navigation — `RefreshModpackList` and `RefreshAvailableMods` each independently calling `ApplySelectedModpack`; fixed with `suppressApply` parameter so only `RefreshAvailableMods` performs the single authoritative apply
- Navigation-triggered refresh race during startup — `RefreshModpackList` and `RefreshAvailableMods` could fire from navigation before the initial startup scan completed; fixed with `_hasCompletedInitialScan` early-return guards
- `StartupRescanAsync` re-entrancy from WPF `Loaded` event — WPF `Frame` layout cycles can fire `Loaded` multiple times; fixed with `_hasCompletedInitialScan` guard at method entry
- `ModParser` case-sensitive dependency attribute bug — `DependedModuleMetadata` uses lowercase `id` per BUTR schema, but parser used `dep.Attribute("Id")`; fixed casing, added `DependentVersion` fallback for legacy `DependedModules` block, silently skips empty IDs
- Mod installer toast ETA not updating — timer display now refreshes correctly during rapid small-mod installs
- XAML binding error from missing `VerticalContentAlignment` setter in styles
- `RefreshModpackList` missing mods toast firing multiple times — guard added for single authoritative fire
- Logger null reference on startup — debug logging calls moved after config initialization
- `AppPaths` getter recursion — `EnsureDirectoryExists()` cycling the singleton getter; refactored initialization order
- Dependency modules parsing returning empty data — case-sensitive JSON key typo corrected
- Config value getter debug logging removed (kept setter logging only)
- Removed unreachable code in titlebar `MouseLeftButtonClick` event handler

---

## 0.9.3 - Internal | 2026-02-19

> Full translation/localization system — live language switching without restart.

### Added

- Translation/localization service — `TranslationService` orchestrator singleton, `TranslationStrings` bindable properties with hardcoded English defaults and `PropertyChanged(null)` blanket notify for hot-reload
- `TranslationDataManager` JSON reader for flat key-value language files and `languages.json` manifest
- `LanguageOptionsModel` for Settings language ComboBox binding
- `en-US.json` default language template with full key coverage (Nav, ModsPage, ModpacksPage, SettingsPage, FaqPage, Toasts, Common strings)
- `languages.json` manifest file
- `AppPaths.LanguagesDirectory` with auto-create on first access
- `.csproj` copy rule for `Languages\**` to output directory
- `App.Translator` singleton property initialized on startup
- Settings language ComboBox wired to `App.Translator.AvailableLanguages` with live `SetLanguage()` switching

### Changed

- All hardcoded `Text="..."` in XAML replaced with translation bindings (`{Binding ..., Source={x:Static local:App.Translator.Strings}}`) across ModsPage, ModpacksPage, SettingsPage, FaqPage, and MainWindow
- Code-behind hardcoded status strings in `.xaml.cs` files replaced with `App.Translator.Strings.*` references (scope limited to UI text only — mod/modpack names and dynamic data are not translated)
- Updated `LICENSE.md` to version v1.2

---

## 0.8.15 - Internal | 2026-02-18

> Toast notification system, extraction progress reporting, and archive format validation.

### Added

- Toast/notification system — `ToastService`, `ToastViewModel`, `ToastRequest`, `ToastSeverity`, `ToastTemplateKeys`, `ToastStyles.xaml` resource dictionary with Default, InstallProgress, InstallSummary, MissingMods templates
- `App.Toasts` singleton with auto-dismiss timers, pause/resume on hover, persistent progress toasts, max-3 visible cap with oldest eviction, fade-out close animation
- Toast integration in Settings page — replaced all 6 TODO markers (`SelectGameFolder_Click`, `SelectGameExe_Click`, `SelectWorkshopFolder_Click`, `RedetectGame_Click`, `UnblockDlls_Click`, `ClearModCache_Click`)
- Toast integration in ModsPage — install progress, install completion summary, missing mods warning, mod refresh, startup scan, and launch result toasts
- Extraction progress reporting — `ExtractionProgress` model, `ExtractionProgressChanged` event on `ModInstaller`, per-file callback in `ModExtractor.ExtractToTempAsync`, batch-level cumulative tracking with heuristic file-count estimation, ETA calculation
- Archive format validation — `ModInstaller.IsAcceptedArchive()`, `_acceptedExtensions` HashSet, pre-extraction rejection with descriptive error for unsupported formats

### Changed

- Updated Settings page text labels
- Updated `App.xaml` to initialize toast system

---

## 0.7.7 - Internal | 2026-02-17

> Create New Modpack split button, Epic/GamePass deferral, and modpack template system.

### Added

- Create New Modpack split button with template selection — conjoined Create New + dropdown, checkmark-selected template, session-only `_pendingTemplate` defaulting to Vanilla
- Dropdown template items: `Vanilla`, `ButterLib`, `VanillaWarSails`, `ButterLibWarSails`
- `SetActiveTemplate()` + `UpdateTemplateCheckmarks()` helpers mirroring Play button's `SetActiveLaunchTarget` / `UpdateLaunchTargetCheckmarks` pattern
- Create New left button opens name input panel using whichever template the dropdown currently has selected

### Changed

- Epic Games and GamePass platform support deferred — detection disabled via `_enableUnsupportedPlatforms` const gate in `GamePathsHelper`, launch blocked with safety-net validation in `GameLauncher.CanLaunch()`; all underlying code preserved (`EpicDetector`, `EpicManifestReader`, `TryDetectEpic`, `GameProvider.EpicGames`/`GamePass` enum values, `SettingsPage` visibility branches) for future re-enablement
- Updated `README.md` to reflect no Epic/GamePass support with developer testing limitations explanation
- Removed obsolete `Load()` method from modpack service

---

## 0.6.18 - Internal | 2026-02-16

> BLSE support, Play button split-button, and mod install lifecycle management.

### Added

- Play button split-button with BLSE support — conjoined Play + dropdown, Bannerlord/BLSE launch targets, checkmark selection, persisted `DefaultLaunchTarget`
- `LaunchTarget` enum (`Bannerlord`, `BLSE`) used by `GameLauncher.LaunchAsync`
- BLSE exe path config — `AppConfigSettings.BLSEExePath`, persisted to config JSON
- BLSE installation from archive — `BLSEInstaller` with platform-aware detection, bin folder resolution, auto-set `BLSEExePath` on install
- BLSE file unblocking — `DLLUnblocker.UnblockBLSEFilesAsync` targeting all file types in temp source folder before copy to game bin
- BLSE install summary tracking — `ModInstallSummary` separates BLSE from mod counts via `IsBLSEResult` sentinel
- Settings: BLSE exe path selector (Game Config tab — Select File button for manual BLSE exe selection)
- `UpdateCanStart()` validates BLSE exe when BLSE launch target is selected (disables Play + shows warning if invalid)
- Mod installer service-owned task lifetime — `ModInstaller.StartInstallAsync` fire-and-forget on thread pool, survives page navigation
- Install progress/completion events — `InstallProgressChanged`, `InstallCompleted` for UI subscribe/unsubscribe on page load/unload
- Install re-entrance guard — `SemaphoreSlim` in `ModInstaller`, `IsInstalling` check in UI before opening dialog
- Install cancellation support — `CancellationTokenSource` in `ModInstaller`, `CancelInstall()` public method
- Cancel install on app exit — `App.OnExit` calls `ModInstaller.CancelInstall()` when `IsInstalling` is true

### Changed

- Updated Play button styling template for split-button design
- Refactored installation process to handle BLSE via `BLSEInstaller`
- Updated `ModInstallSummary` model for BLSE installation data

---

## 0.5.26 - Internal | 2026-02-15

> Settings page, FAQ page, game launcher, modpack startup modes, and major bug fixes.

### Added

- Settings page — full UI + code-behind with General, Game Config, Tools, WIP, and About panels
- Settings nav bar — custom horizontal ListBox with DockPanel, icon + label items, themed styles
- Settings: Game path selection, validation, re-detect, platform-aware visibility
- Settings: Modpack startup mode (LastUsed / AlwaysDefault / AlwaysAsk) with auto-save
- Settings: Debug mode toggle (MahApps ToggleSwitch, themed to CalradiaForge palette)
- Settings: DLL unblock tool with persisted run status
- Settings: Data management tools (clear cache, open config/logs/modpacks folders)
- Settings: About panel (version, publisher, license + GitHub links)
- Wired navigation for Settings and FAQ pages in `MainWindow.xaml.cs`
- `SettingsPageStyles` resource dictionary added to App resources
- F.A.Q. / Help page — `FaqPage.xaml` with 9 Q&A sections covering mods, DLLs, modpacks, platforms, imports, cache, and bug reporting
- Game launch handler — `GameLauncher` service with Steam, Epic, StandAlone support + `CanStart` validation
- Game path validation helpers — `GamePathsHelper` for manual input validation on Settings page
- Modpack startup modes — AlwaysDefault (Vanilla by name), LastUsed (persisted config), AlwaysAsk (ghost sentinel modpack at index 0 with auto-removal on first selection)
- `ResolveStartupModpackIndex()` + `FindModpackIndexByName()` helpers; `RefreshModpackList()` preserves ghost sentinel across nav-back refreshes
- Steam process check — verifies `steam.exe` is running before launching; starts Steam if not running
- Modpack templates for WarSails DLC — `VanillaWarSails` and `ButterLibWarSails`
- Active Load Order ListBox on ModpacksPage showing current load order for modpack validation
- Debug mode logging wired to app startup
- `App.OnExit` saves last selected modpack index to config

### Changed

- Updated `README.md` with Supported Game Platforms section
- ComboBox watermark removed (replaced by ghost sentinel approach — no XAML watermark triggers)
- Logger refactored — minimum log level for debug logging mode tied to config setting
- Added `GamePass` enum value to game provider enums
- Removed obsolete modpack code in prep for new models

### Fixed

- Steam game launch passing CLI args to vanilla launcher instead of game engine executable — now launches directly via engine `.exe`
- Multiplayer mods incorrectly populating mod lists — filtered to singleplayer-only
- `SubModule.xml` parsing failures on modder variations — fixed element vs. elements getter and case-sensitive metadata key; URL data now populates correctly
- Settings page debug mode toggle wiring
- Data persistence issues and stale data refreshing across page navigation
- Play button launching issues

---

## 0.4.12 - Internal | 2026-02-14

> Modpacks page, mod installer/extractor, Novus Launcher import, and core infrastructure.

### Added

- ModpacksPage — full UI with create, edit, save, and import functionality wired to Core library
- ModpacksPage styling resource dictionary
- Novus Launcher preset import — `NovusPresetConverter` + Import button on ModpacksPage
- Mod installer, extractor, and DLL unblocker — `ModInstaller`, `ModExtractor`, `DLLUnblocker` services
- Models for modpack items and mod install result summaries
- `ModsData` and `ModService` updated for manual mod cache refresh from ModsPage
- Game path validation for manual Settings page input
- `GameLauncher` service created
- App config settings for Settings page enums
- Last Selected Modpack config entry and data filepath

### Changed

- Refactored ModsPage into `Pages` directory
- Updated `App.xaml` to add new resource dictionaries; `App.xaml.cs` initializes Mods and Modpacks services
- Updated `README.md` to reflect current app state
- Updated `LICENSE.md` to version 1.1 (all prior versions voided)
- Removed artifacts from ModsPage code left over from AI agent additions
- Icon pack cleanup — `MahApps.Metro.IconPacks` narrowed to `MahApps.Metro.IconPacks.Material` only

---

## 0.3.9 - Internal | 2026-02-13

> Modpacks backend, mod scanner/parser, drag-and-drop reorder, and modpack ComboBox on ModsPage.

### Added

- Modpacks backend — `ModpackService` with create, save, edit, and ComboBox population
- ModsPage ↔ ModpacksPage sync — `RefreshModpackList` called on navigation
- Drag-and-drop reorder — `IDropTarget` with reorder-within and move-between lists on ModsPage
- Modpack ComboBox on ModsPage for quick modpack switching
- Save Last Used modpack on launch/exit (`PlayButton_Click` + `App.OnExit`)
- Modpack delete obsolete code removed (`Delete()` method and region cleaned from `ModpackService`)

### Changed

- Updated `App.xaml.cs` to streamline `InitializeConfiguration()` using DI rules for `AppConfig` instance
- Logger refactored to create a new log file per app session
- Modified and deleted old `ModList` code files in prep for new Modpack models
- Code styling updates with new editor rules
- Updated `.gitignore`
- Removed unused file-drop auto-install from ModsPage (`Page_Drop` cleanup)

---

## 0.2.5 - Internal | 2026-02-11

> Core infrastructure — mod scanning/parsing, centralized paths, theme system, and main window navigation.

### Added

- `AppPaths` — centralized management of file and directory paths (config, logs, modpacks, data storage)
- `ModParser` — XML-to-C# model parsing for `SubModule.xml` files
- `ModsData` — reading/writing mod module models to JSON data files
- `ModScanner` — game mods folder detection, Steam Workshop folder auto-detection, scanned mod parsing
- `ModsPage` — semi-polished UI with active/inactive mod management, mod list search, and extraction-based mod installation
- Automatic DLL unblocking for downloaded mod files
- `Theme.xaml` — global theming and styling resource dictionary for controls and UI
- Updated styling resource dictionaries (`TextStyles.xaml`, etc.) used across views
- Modules directory property added to game folder path config

### Changed

- Removed `Fonts.xaml` in favor of `Theme.xaml` and `TextStyles.xaml`
- Updated `App.xaml` resource dictionary references
- Removed redundant config folder path creation (handled by `AppPaths` on first save)

---

## 0.1.7 - Internal | 2026-02-08

> WPF app foundation — main window, navigation, models, and core services.

### Added

- WPF app foundation with MahApps Metro HamburgerMenu navigation
- `MainWindow` wireframe with nav menu controller
- Basic JSON-serializable models for mod data storage
- `GamePathsHelper` — auto-detection of game installation paths (Steam, StandAlone) with manual selection fallback
- `Logger` class with `Log.Error` overload for exception logging
- `AppConfig` and `AppConfigSettings` — configuration model with JSON persistence
- Search functionality separated for future `ModListService` absorption
- EULA acceptance window requirement (config-gated, shown before `MainWindow` loads)
- WPF-specific properties file for HamburgerMenu implementation
- App resource files for styling and control triggers
- `App.xaml.cs` — Logger and global config instance initialization

---

## 0.0.1 - Internal | 2026-02-04

> Initial project setup.

### Added

- Initial commit — solution and project structure
- `.gitignore`

