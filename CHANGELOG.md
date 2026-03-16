# Changelog

All notable changes to CalradiaForge will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## 0.12.15 - 2026-03-16

> EULA packaging hardening — moved from deployed disk file to embedded resource so single-file publish keeps license text internal to the executable.

### Changed

- `CalradiaForge.UI\CalradiaForge.UI.csproj` EULA build item switched from publish-time content copy:
  - removed `Content Include="Resources\EULA.txt"` with `CopyToOutputDirectory`
  - added `EmbeddedResource Include="Resources\EULA.txt"` with logical name `CalradiaForge.Resources.EULA.txt`
- `CalradiaForge.Core\Infra\Eula\EulaService.cs` loading strategy changed from filesystem path (`AppPaths.EulaFilePath` + `File.ReadAllText`) to embedded-resource stream loading via `Assembly.GetEntryAssembly().GetManifestResourceStream(...)`.

### Fixed

- Single-file publish behavior now aligns with release intent: EULA text is bundled inside `CalradiaForge.exe` instead of relying on an external `Resources\EULA.txt` file at runtime.
- Startup EULA prompt is resilient to missing deployed resource files by reading from the executable payload directly.

---

## 0.12.14 - 2026-03-16

> Localization release-note follow-up — language pack inventory and manifest updates documented under a dedicated patch bump.

### Added

- Language pack files now documented in release notes as part of the localization rollout:
  - `Languages\en-US.json`
  - `Languages\es-ES.json`
  - `Languages\de-DE.json`
  - `Languages\ru-RU.json`
  - `Languages\it-IT.json`
  - `Languages\pl-PL.json`
  - `Languages\sv-SE.json`
  - `Languages\tr-TR.json`
  - `Languages\zh-CN.json`

### Changed

- `Languages\languages.json` manifest updated/verified to register all currently shipped language packs and display names.
- Changelog coverage expanded to reflect first-launch language selection + manifest-backed language list behavior.
- Startup localization flow notes aligned with current app behavior (`InitializeLangSelection(TranslationManager)` before translator initialization on first run).

### Fixed

- Patch note completeness gap: prior entry did not explicitly list all language files and manifest scope now present in the repository.
- Release documentation alignment with current `dev-release` state after recent localization commits.

---

## 0.12.13 - 2026-03-15

> First-launch localization completion — pre-EULA language selector shipped, EULA chrome localized, and startup wiring finalized.

### Added

- First-launch language selection window implementation:
  - `CalradiaForge.UI\Views\LanguageSelectWindow.xaml`
  - `CalradiaForge.UI\Views\LanguageSelectWindow.xaml.cs`
  - `CalradiaForge.UI\Resources\LangSelectWindowStyles.xaml`
- Startup language pre-selection flow in `App.InitializeLangSelection(TranslationManager)`:
  - Loads language options from `languages.json`
  - Opens modal selector before EULA on first run (`!AppConfig.EulaAccepted`)
  - Persists selected code to `AppConfigSettings.Language`
- EULA localization key coverage added to translation model:
  - `Eula_WindowTitle`
  - `Eula_CloseTooltip`
  - `Eula_Header`
  - `Eula_VersionLabel`
  - `Eula_AcceptanceText`
  - `Eula_DeclineButton`
  - `Eula_AcceptButton`
- English language file entries added for all new EULA keys in `Languages\en-US.json`

### Changed

- `App.InitializeTranslatorService()` startup wiring updated to:
  - Construct `TranslationManager` first
  - Run first-launch language selection before translator initialization
  - Initialize translator after persisted language selection
- `EulaWindow.xaml` hardcoded chrome text replaced with static translation bindings via `App.Translator.Strings.*`
- EULA window title and close tooltip now use localization bindings instead of literals
- Updated `zh-CN.json` with new EULA translation keys

### Fixed

- Namespace/type wiring for language selector backend corrected to use in-solution localization model (`LanguageOption` in `CalradiaForge.Core.Infra.Localization`)
- Startup method-call mismatch fixed by aligning `InitializeLangSelection` call site with its `TranslationManager` parameter signature
- Modal shutdown safety preserved for language selector (`ShutdownMode.OnExplicitShutdown` during dialog lifetime), preventing premature app termination when the selector closes

---

## 0.11.16 - 2026-03-14

> Localization pipeline modernization — default English generation tooling, translation model rewrite, startup localization flow prep, and new Simplified Chinese language pack.

### Added

- `CalradiaForge.ConsoleUtils` project (net10.0) added to solution for developer-only utility workflows
- Console utility tool to regenerate `Languages\en-US.json` from code defaults using:
  - `TranslationStrings.GetDefaultTranslations()`
  - `TranslationManager.DefaultEnglishLanguageFile(...)`
- `zh-CN` (Simplified Chinese) language support:
  - Added `Languages\zh-CN.json`
  - Added `zh-CN` entry (`简体中文`) in `Languages\languages.json`
- New language path helpers in `AppPaths`:
  - `AppPaths.LanguagesManifestFilePath`
  - `AppPaths.DefaultLanguageFilePath`

### Changed

- `TranslationStrings` rewritten to make hardcoded English defaults the single source of truth via `private const string Default...` fields per key
- `TranslationStrings` now exposes `GetDefaultTranslations()` to build a full key/value English dictionary via reflection
- `TranslationManager` constructor updated to accept explicit paths:
  - `languagesDirectory`
  - `manifestFilepath`
  - `defaultLangFilepath`
- `TranslationManager.LoadManifest()` now reads from configured manifest filepath instead of recomputing internally
- `App.InitializeTranslatorService()` now initializes `TranslationManager` with `AppPaths.LanguagesManifestFilePath` and `AppPaths.DefaultLanguageFilePath`
- Startup initialization order adjusted so translation service initialization occurs earlier (prep for first-run language selection flow)

### Fixed

- Translation filename/path wiring issue from prior release cycle (language file typo/path consistency correction)
- Translation manager naming/structure cleanup (legacy naming drift corrected)
- `en-US.json` access coordination logic introduced in `TranslationManager` so default-file read/write paths can use lock-based synchronization

---

## 0.11.5 - 2026-03-09

> EULA window bug fixes — resource dictionary wiring, WPF shutdown mode, title bar drag support, and toggle switch styling.

### Added

- `EulaWindow` title bar — draggable title bar with app icon, branded text, and close button reusing `TitleBarCloseButton` style from `TitleBar.xaml`; close button wired to decline the EULA (`Accepted = false`, `DialogResult = false`)
- `EulaTitleBar` and `EulaTitleBarText` styles added to `EulaWindowStyles.xaml`
- `TitleBar_MouseLeftButtonDown` drag handler and `CloseButton_Click` decline handler added to `EulaWindow.xaml.cs`

### Changed

- `App.xaml` — added `ShutdownMode="OnMainWindowClose"` to `<Application>` element to prevent automatic shutdown when the EULA dialog closes before `MainWindow` is created
- `EulaAcceptance()` — temporarily switches `ShutdownMode` to `OnExplicitShutdown` for the duration of `EulaWindow.ShowDialog()` and restores the previous mode after, preventing WPF from auto-assigning `EulaWindow` as `MainWindow` and terminating on close
- `EulaWindow.xaml` — replaced plain `CheckBox` with `SettingsToggleSwitch` style (orange track / gold thumb) for visual consistency with the Settings page debug mode toggle; window height increased from 580 to 620 to accommodate the new title bar

### Removed

- `EulaAcceptCheckBox` style removed from `EulaWindowStyles.xaml` — no longer referenced after toggle switch replacement

### Fixed

- `EulaWindowStyles.xaml` not loaded at runtime — resource dictionary was missing from `App.xaml` merged dictionaries, causing `XamlParseException: Cannot find resource named 'EulaWindowTitle'` during `EulaWindow.InitializeComponent()`
- App shutdown on EULA acceptance — default `ShutdownMode.OnLastWindowClose` caused WPF to terminate when `EulaWindow` (the only window) closed before `MainWindow` was created by `StartupUri`
- App shutdown on EULA acceptance (second occurrence) — WPF auto-assigned `EulaWindow` as `Application.MainWindow` because it was the first window instantiated; `OnMainWindowClose` then triggered shutdown when it closed; fixed by temporarily switching to `OnExplicitShutdown` during the dialog lifetime
- `EulaWindow` appearing on wrong monitor with no way to reposition — chromeless window with `WindowStyle="None"` had no drag surface; added draggable title bar matching `MainWindow` design
- Unused `EulaAcceptCheckBox` style left in `EulaWindowStyles.xaml` after toggle switch replacement — removed dead resource

---

## 0.11.0 - 2026-03-09

> EULA acceptance gate — first-launch EULA window blocks app until accepted, persisted to config.

### Added

- `EulaService` Core service — reads EULA text from `AppPaths.EulaFilePath`, checks `AppConfigSettings.EulaAccepted` flag, records acceptance on user confirm
- `AppPaths.EulaFilePath` — single const-based path property for the deployed `Resources\EULA.txt` file, following the existing `ConfigFilePath` / `ModsCurrentFilePath` pattern
- `AppConfigSettings.EulaAccepted` — boolean config property persisted to `config.json`; no `OnPropertyChanged` since the value is never bound to live UI
- `EulaWindow` — chromeless modal window matching `MainWindow` gradient background, with scrollable read-only EULA text, acceptance checkbox gating the Accept button, and Decline button
- `EulaWindowStyles.xaml` — dedicated resource dictionary with styles for scroll container, text display, checkbox, accept button (gold/amber), and decline button (surface/red hover), following the existing per-page resource dictionary pattern
- `EulaAccepted` default key seeded in `AppConfigSettings.InitDefaults()`

### Changed

- `App.OnStartup` — EULA gate inserted after `InitializeConfiguration()` and before all service initialization; on decline, logs shutdown reason and calls `Shutdown()` with early `return` to prevent service initialization
- `EulaAcceptance()` helper returns a pure boolean — caller (`OnStartup`) owns the log message and `Shutdown()` decision, following the "UI decides when, core decides how" principle from `CONTRIBUTING.md`
- `App.xaml` — `EulaWindowStyles.xaml` added to merged resource dictionaries

---

## 0.10.8 - 2026-03-08

> Native 7-Zip extraction — SharpCompress replaced with SevenZipWrapper, restoring full `.7z` support. | Third-party software notices added to LICENSE.md for all dependencies.

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

## 0.9.22 - 2026-02-25

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

## 0.9.19 - 2026-02-20

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

## 0.9.3 - 2026-02-19

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

## 0.8.15 - 2026-02-18

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

## 0.7.7 - 2026-02-17

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

## 0.6.18 - 2026-02-16

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

## 0.5.26 - 2026-02-15

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

## 0.4.12 - 2026-02-14

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

## 0.3.9 - 2026-02-13

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

## 0.2.5 - 2026-02-11

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

## 0.1.7 - 2026-02-08

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

## 0.0.1 - 2026-02-04

> Initial project setup.

### Added

- Initial commit — solution and project structure
- `.gitignore`
