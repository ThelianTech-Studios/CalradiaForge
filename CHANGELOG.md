# Changelog

All notable changes to CalradiaForge will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---
## 0.9.22 - 2026-02-25

> Open beta release — feature-complete for v1.0 scope with known `.7z` limitation.

### Added
- `.gitignore` updates for beta release packaging
- Beta Section in `README.md` with open beta test instructions, known issues, and contribution guidelines
- `LICENSE.md` updated to version 1.2 (all prior versions voided)
- - FAQ page entry Q9 for `.7z` limitation with workaround
- Updated `README.md` with Supported Mod Archive Formats table, FAQ page reference, and `.7z` limitation entry
### Removed
- `.7z` archive support temporarily disabled — SharpCompress LZMA block-compression caused ~25 min extraction for large mods; accepted formats narrowed to `.zip` and `.rar`
---

## 0.9.19 - 2026-02-20

> Code cleanup, debug logging, finalized `.editorconfig`, and open beta preparation.

### Added
- Bug report and feature request issue templates added to repository
- Debug logging in conditional logic blocks across Core and UI layers when running in debug mode
- XML doc summaries on all public/internal members (Core + UI)
- EULA text file included in build output
- Temporary AI-generated app icon placeholder (pending commissioned artwork)

### Changed
- Finalized `.editorconfig` with project coding conventions
- Ran full code cleanup pass against finalized `.editorconfig` rules
- Updated `.gitignore` for beta release (removed `AssemblyInfo.cs` tracking, suppressed global warning suppressions file)
- Updated build parameters for beta release packaging
- Updated `README.md` with installation instructions and open beta test sections
- Removed duplicate `CONTRIBUTING.md`
- Removed obsolete `Author` key-value pair from configuration

### Fixed
- Mod installer toast ETA not updating — timer display now refreshes correctly during rapid small-mod installs with a small UI delay
- XAML binding error from missing `VerticalContentAlignment` setter in styles
- `RefreshModpackList` missing mods toast firing multiple times — added guard to gate toast to a single authoritative fire per event
- Logger null reference on startup — debug logging calls moved after config initialization
- `AppPaths` getter recursion — `EnsureDirectoryExists()` was cycling the singleton getter; refactored initialization order
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
- All hardcoded `Text="..."` in XAML replaced with `{Binding ..., Source={x:Static local:App.Translator.Strings}}` bindings across ModsPage, ModpacksPage, SettingsPage, FaqPage, and MainWindow
- Code-behind hardcoded status strings in `.xaml.cs` files replaced with `App.Translator.Strings.*` references
- Updated `LICENSE.md` to version v1.2

---

## 0.8.15 - 2026-02-18

> Toast notification system, `.7z` temporary disable, and extraction progress reporting.

### Added
- Toast/notification system — `ToastService`, `ToastViewModel`, `ToastRequest`, `ToastSeverity`, `ToastTemplateKeys`, `ToastStyles.xaml` resource dictionary with Default, InstallProgress, InstallSummary, MissingMods templates
- `App.Toasts` singleton with auto-dismiss timers, pause/resume on hover, persistent progress toasts, max-3 visible cap with oldest eviction, fade-out close animation
- Toast integration in Settings page — replaced all 6 TODO markers (`SelectGameFolder_Click`, `SelectGameExe_Click`, `SelectWorkshopFolder_Click`, `RedetectGame_Click`, `UnblockDlls_Click`, `ClearModCache_Click`)
- Toast integration in ModsPage — install progress, install completion summary, missing mods warning, mod refresh, startup scan, and launch result toasts
- Extraction progress reporting — `ExtractionProgress` model, `ExtractionProgressChanged` event on `ModInstaller`, per-file callback in `ModExtractor.ExtractToTempAsync`, batch-level cumulative tracking with heuristic file-count estimation, ETA calculation
- Archive format validation — `ModInstaller.IsAcceptedArchive()`, `_acceptedExtensions` HashSet, pre-extraction rejection with descriptive error

### Changed
- Updated some Settings page text labels
- Updated `App.xaml` to initialize toast system

---

## 0.7.7 - 2026-02-17

> Create New Modpack split button, Epic/GamePass deferral, and modpack bug fixes.

### Added
- Create New Modpack split button with template selection — conjoined Create New + dropdown, checkmark-selected template, session-only `_pendingTemplate` defaulting to Vanilla
- Dropdown template items: `Vanilla`, `ButterLib`, `VanillaWarSails`, `ButterLibWarSails`
- `SetActiveTemplate()` + `UpdateTemplateCheckmarks()` helpers mirroring Play button's pattern
- Create New left button opens name input panel using selected dropdown template

### Changed
- Epic Games and GamePass platform support deferred — detection disabled via `_enableUnsupportedPlatforms` const gate in `GamePathsHelper`, launch blocked with safety-net validation in `GameLauncher.CanLaunch()`; all underlying code preserved for future re-enablement
- Updated `README.md` to reflect no Epic/GamePass support with developer testing limitations explanation
- Removed obsolete `Load()` method from modpack service

---

## 0.6.18 - 2026-02-16

> BLSE support, Play button split-button, and install cancellation.

### Added
- Play button split-button with BLSE support — conjoined Play + dropdown, Bannerlord/BLSE launch targets, checkmark selection, persisted `DefaultLaunchTarget`
- `LaunchTarget` enum (`Bannerlord`, `BLSE`) used by `GameLauncher.LaunchAsync`
- BLSE exe path config — `AppConfigSettings.BLSEExePath`, persisted to config JSON
- BLSE installation from archive — `BLSEInstaller` with platform-aware detection, bin folder resolution, auto-set `BLSEExePath` on install
- BLSE file unblocking — `DLLUnblocker.UnblockBLSEFilesAsync` targeting all file types in temp source folder before copy to game bin
- BLSE install summary tracking — `ModInstallSummary` separates BLSE from mod counts via `IsBLSEResult` sentinel
- Settings: BLSE exe path selector (Game Config tab — Select File button for manual BLSE exe selection)
- `UpdateCanStart()` validates BLSE exe when BLSE launch target is selected (disables Play + shows warning if invalid)
- Install cancellation support — `CancellationTokenSource` in `ModInstaller`, `CancelInstall()` public method
- Cancel install on app exit — `App.OnExit` calls `ModInstaller.CancelInstall()` when `IsInstalling` is true
- Mod installer service-owned task lifetime — `ModInstaller.StartInstallAsync` fire-and-forget on thread pool, survives page navigation
- Install progress/completion events — `InstallProgressChanged`, `InstallCompleted` for UI subscribe/unsubscribe on page load/unload
- Install re-entrance guard — `SemaphoreSlim` in `ModInstaller`, `IsInstalling` check in UI before opening dialog

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
- Wired navigation for Settings page — `OptionsItemClick` in `MainWindow.xaml.cs`, standalone `SettingsPage` instance
- `SettingsPageStyles` resource dictionary added to App resources
- F.A.Q. / Help page — `FaqPage.xaml` with 9 Q&A sections covering mods, DLLs, modpacks, platforms, imports, cache, bug reporting, and `.7z` limitation
- Wired navigation for FAQ page — `_pages[2]` placeholder replaced with `FaqPage` instance
- FAQ page visible scrollbar — `ScrollViewer.VerticalScrollBarVisibility` set to `Visible`
- Game launch handler — `GameLauncher` service with Steam, Epic, StandAlone support + `CanStart` validation
- Game path validation helpers — `GamePathsHelper` for manual input validation on Settings page
- Modpack startup mode: AlwaysDefault selects Vanilla modpack by name via `VanillaModules.DefaultModpackName`
- Modpack startup mode: LastUsed restores `AppConfig.LastSelectedModpack` by name
- Modpack startup mode: AlwaysAsk with ghost sentinel modpack at index 0 (empty load order, prompt text, auto-removed on first selection)
- `ResolveStartupModpackIndex()` + `FindModpackIndexByName()` helpers for startup mode resolution
- `RefreshModpackList()` preserves ghost sentinel across nav-back refreshes when AlwaysAsk is active
- Steam process check — verifies `steam.exe` is running before launching Steam-based games; starts Steam if not running
- Modpack templates for WarSails DLC — `VanillaWarSails` and `ButterLibWarSails` templates for Create New
- Active Load Order ListBox on ModpacksPage showing current load order for modpack validation
- `GameLauncher` service initialization on app startup
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
- Game path validation for manual Settings page input — rejects invalid folders
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

> Modpacks backend, mod scanner/parser, drag-and-drop, and modpack ComboBox on ModsPage.

### Added
- Modpacks backend — `ModpackService` with create, save, edit, and ComboBox population
- ModsPage ↔ ModpacksPage sync — `RefreshModpackList` called on navigation
- Drag-and-drop reorder — `IDropTarget` with reorder-within and move-between lists on ModsPage
- Modpack ComboBox on ModsPage for quick modpack switching

### Changed
- Updated `App.xaml.cs` to streamline `InitializeConfiguration()` using DI rules for `AppConfig` instance
- Logger refactored to create a new log file per app session
- Modified and deleted old `ModList` code files in prep for new Modpack models
- Code styling updates with new editor rules
- Updated `.gitignore`

---

## 0.2.5 - 2026-02-11

> Core infrastructure — mod scanning/parsing, centralized paths, theme system, and main window navigation.

### Added
- `AppPaths` — centralized management of file and directory paths (config, logs, modpacks, data storage)
- `ModParser` — XML-to-C# model parsing for `SubModule.xml` files
- `ModsData` — reading/writing mod module models to JSON data files
- `ModScanner` — game mods folder detection, Steam Workshop folder auto-detection, scanned mod parsing
- `ModsPage` — semi-polished UI connected to nav bar and frame display
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
- `GamePathsHelper` — auto-detection of game installation paths with manual selection fallback
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