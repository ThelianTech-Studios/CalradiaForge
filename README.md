# CalradiaForge

A modern mod launcher for **Mount & Blade II: Bannerlord**.

CalradiaForge streamlines mod installation, load order management, modpacks, and game launching into a single, purpose-built desktop application.

 ```
 CalradiaForge was built by players, for players.
 ```

[![GitHub Repo][CalradiaForge-Github-Shield]][CalradiaForge-Repo]
[![NexusMods][CalradiaForge-Nexus-Shield]][CalradiaForge-Nexus]
[![License.md][CalradiaForge-License-Shield]][CalradiaForge-License]

---

## 🎯 What is CalradiaForge?

CalradiaForge is a standalone WPF launcher focused on **stable, repeatable mod setups** for Bannerlord. 
It helps you install mods from archives, arrange and toggle them quickly, and save your setups as **modpacks** that you can re-use or share.

If you maintain more than one loadout (e.g., vanilla+, hardcore, overhaul), CalradiaForge is designed to keep each setup organized and launchable with minimal friction.

> 🧪 **Open Beta:** CalradiaForge is currently in an open beta phase. Core workflows (mod install, modpacks, launching) are mostly feature-complete and stable enough for everyday use, but you may still encounter UI rough edges or missing quality-of-life improvements.
>
> ⚠️ **BetaNotice — EULA Prompt on First Launch (v0.11.5):**
> Starting with **Beta v0.11.5**, CalradiaForge displays a **EULA (End-User License Agreement) window** on first launch. You must read and accept the agreement before the main application window loads. If declined, the application will close immediately. This prompt only appears once — your acceptance is saved and will not be shown again on subsequent launches.
> Starting with **Beta v0.11.5**, we migrated from SharpCompress to SevenZipWrapper in the backend code for mod archive extraction. This change was made to improve performance and reliability, especially for larger mod archives. If you encounter any issues with mod installation or archive extraction after this update, please refer to the bug reporting instructions below.
>
> If you experience any issues with mod installation or archive extraction, please:
> 1. **File a bug report** on the [GitHub Issues page][CalradiaForge-Repo] with a description of the problem, the archive format used, and any relevant log files from the `Logs` directory.
> 2. **Revert to the last stable release** — **Beta v0.9.22** — available from the [Releases page][CalradiaForge-Repo] until the issue is resolved.

---

## 💻 App Installation (Open Beta)

CalradiaForge is distributed as a **portable desktop application** — no traditional installer is required.

### Operating System

- ✅ **Supported:** Windows 10 (64-bit) and Windows 11  
- ⚠️ **Not tested / not supported:** Windows versions earlier than 10

### Install & Run

1. **Download the latest release**
  - From GitHub:  
    [![GitHub Repo][CalradiaForge-Github-Shield]][CalradiaForge-Repo]
  - From NexusMods:  
    [![Nexus][CalradiaForge-Nexus-Shield]][CalradiaForge-Nexus]

2. **Extract the archive**
  - Extract the downloaded `.zip` to any folder you control (e.g., `C:\Games\CalradiaForge` or another data drive).
  - Avoid protected locations such as `C:\Program Files` when possible to reduce permission issues.

3. **Run the launcher**
  - Double-click `CalradiaForge.exe`.
  - On first run, Windows SmartScreen may warn you because this is a new, unsigned executable. Choose **More info → Run anyway**

4. **Accept the EULA**
  - On first launch, CalradiaForge will display a **EULA window** before the main application loads.
  - Read the End-User License Agreement carefully, then click **Accept** to continue.
  - If you click **Decline** or close the EULA window, the application will shut down.
  - This prompt only appears once — your acceptance is persisted in the app configuration and will not be shown again on future launches.

5. **Portable behavior**
  - CalradiaForge stores its configuration, logs, and modpack definitions alongside the app in dedicated subfolders (see [Directories & Data Locations](#-directories--data-locations)).
   - You can move the app folder or keep multiple copies without breaking your existing configuration.

### .NET / Runtime Requirements

CalradiaForge targets **.NET 10 Windows Desktop** and is published as a **framework-dependent** app for **Windows x64**:

- **Target framework:** `net10.0-windows7.0`
- **Runtime identifier:** `win-x64`
- **Deployment mode:** Framework-dependent (`SelfContained = false`)

This means you must have the **.NET 10 Desktop Runtime (x64)** installed on your system.

> **Install the required runtime:**
> - Download and run the official **".NET 10 Desktop Runtime 10.0.3 (x64)"** installer from Microsoft:  
>   https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-10.0.3-windows-x64-installer
> - Follow the installer steps, then restart CalradiaForge.

If the required runtime is missing, CalradiaForge will fail to start and Windows will display a message indicating that a compatible .NET Desktop Runtime is required.

---

## 🎮 Supported Game Platforms

| Platform         | Support       | Notes |
|------------------|---------------|-------|
| Steam            | ✅ Full        | Auto-launches Steam if not running. Full overlay & achievement support. |
| GOG / Standalone | ✅ Full        | No platform client required. |
| Epic Games       | ❌ None        | Not currently supported. See note below. |
| GamePass         | ❌ None       | Not currently supported. See note below. |

**Why aren't Epic Games and GamePass supported?**

The developer does not currently have access to Bannerlord through Epic Games or Xbox GamePass, making it impossible to test CalradiaForge's functionality on these platforms. Without the ability to verify mod installation, load order management, and game launching, there is no way to guarantee compatibility.

Rather than ship an untested experience, Epic and GamePass support has been **disabled for now**. The underlying detection and configuration code is preserved in the codebase and will be re-enabled once proper testing on these platforms becomes possible.

> ⚠️ **Note:** Epic Games and GamePass support is planned for a future release once proper testing on these platforms becomes possible.

---

## 📦 Supported Mod Archive Formats

| Format | Support         | Notes |
|--------|-----------------|-------|
| `.zip` | ✅ Supported     | Recommended format. Fastest extraction. |
| `.rar` | ✅ Supported     | Fully supported. |
| `.7z`  | ✅ Supported     | Extracted via native 7-Zip wrapper at full speed. |

---

## ✨ Key Features

### 📦 Mods Management

- Install mods directly from downloaded archives into the Bannerlord `Modules` folder
- Automatic **DLL unblocking** during install to avoid common Windows security issues
- Clear separation of **Active** vs **Inactive** mods
- **Search** across your mods list to quickly find specific entries
- **Drag-and-drop reordering** of mods (within and between lists) to adjust load order
- Automatic detection and refresh of installed modules when the `Modules` folder changes
- Basic validation of installed mods (e.g., presence of `SubModule.xml`)

### 🧩 Modpacks (Presets)

- Create named **modpacks** representing complete mod setups
- Save and edit modpacks as your list evolves
- Switch between modpacks from the Mods page via a dedicated modpack selector
- Automatically store the **last used** modpack and restore it on next launch (configurable)
- Import external modpacks/presets, including **Novus Launcher** presets via a built-in converter
- Preserve mod load order, active/inactive state, and related metadata per modpack

### 🎮 Game Launching

- Launch **Mount & Blade II: Bannerlord** directly from CalradiaForge
- Support for multiple platforms:
 - Steam
 - GOG / Standalone
- Validation checks (e.g., game path presence) before starting to reduce launch errors
- Launch the game with the **exact active mod setup selected by your current modpack**
- Optional BLSE (Bannerlord Script Extender) launch support where applicable

### 🛠 Power Tools & Settings

A dedicated **Settings** page provides:

- **Game configuration**
 - Game installation path selection and validation
  - Platform-aware visibility and re-detection of Bannerlord
- **Startup behavior**
 - Choose how CalradiaForge selects modpacks on launch:
   - Last used modpack
   - A specific default modpack
   - Always ask at startup
- **DLL unblock tool**
 - Run a focused DLL unblocking pass and persist its "has been run" status
- **Data management**
 - Clear mod cache
 - Open configuration, logs, and modpack folders directly from the UI
- **Debug options**
 - Toggle debug mode for more detailed behavior while testing
- **About panel**
 - App version, publisher information, and direct links to the project and license

---

## 🚀 Quick Start

1. **Download & Extract**
  - Grab the latest official release from:
    - [![GitHub Repo][CalradiaForge-Github-Shield]][CalradiaForge-Repo]
    - [![Nexus][CalradiaForge-Nexus-Shield]][CalradiaForge-Nexus]
  - Extract the `.zip` to a folder of your choice.

2. **Run CalradiaForge**
  - Launch `CalradiaForge.exe`.
  - On first run, grant any necessary SmartScreen permission.

3. **Accept the EULA**
  - On the very first launch, a **EULA window** will appear before the main application loads.
  - Read the agreement and click **Accept** to proceed. Declining or closing the window will exit the application.
  - You will only see this prompt once.

4. **Point CalradiaForge at your game**
  - Open **Settings → Game Config**
  - Select your Bannerlord installation folder (and executable if requested)
  - Use the **Detect Game** option if you installed Bannerlord via other *Game Platforms* and need to re-detect the game

5. **Install your mods**
  - Go to the **Mods** page
  - Use the mod installation control to select downloaded archives (`.zip` or `.rar`)
   - CalradiaForge will extract to the `Modules` folder and automatically unblock DLLs

6. **Organize and enable mods**
  - Drag mods between **Active** and **Inactive** lists
  - Drag within the active list to adjust load order
  - Use the search box to quickly find specific mods

7. **Create a modpack**
  - Save your current selection and ordering as a **modpack**
  - Name it (e.g., "Vanilla+ QoL", "Overhaul Build") and save
  - Use the modpack selector on the Mods page to switch between setups

8. **Launch the game**
  - Verify your desired modpack is selected
  - Click **Play** to start Bannerlord with the chosen active mods and order

---

## 📁 Directories & Data Locations

CalradiaForge stores its data in a small set of directories relative to the application's root folder. These are resolved and created at runtime by the core path helper:

- Source:  
 `CalradiaForge.Core\Infra\Paths\AppPaths.cs`

Key directories (as defined in `AppPaths`):

- **Config directory**
 - Logical location: `Config` subfolder next to `CalradiaForge.exe`
 - Contains: `config.json` and other configuration data
- **Logs directory**
 - Logical location: `Logs` subfolder next to `CalradiaForge.exe`
 - Contains: rotating diagnostic logs written by the `Logger` service
- **Modpacks directory**
 - Logical location: `Modpacks` subfolder next to `CalradiaForge.exe`
 - Contains: user-created modpack definitions and related metadata
- **Data directory**
 - Logical location: `Data` subfolder next to `CalradiaForge.exe`
 - Contains:
   - `mods_current.data`
   - `mods_backup.data`
   - `last_used_mods.data`
- **Languages directory**
 - Logical location: `Languages` subfolder next to `CalradiaForge.exe`
 - Reserved for localization / language resources

If you are troubleshooting or preparing a bug report, attaching the relevant log files from the **Logs** directory can be very helpful.

---

## 🐛 Bug Reports & Feedback

During the open beta, all bug reports, issues, and feature requests are tracked through the GitHub Issues system:

- Repository: [CalradiaForge-Repo]

Please:

- Search existing issues before creating a new one.
- When opening a new issue, include:
 - A clear description of the problem or request
 - Steps to reproduce (if applicable)
 - Your Windows version and game platform (Steam / GOG / Standalone)
 - Relevant log files from the `Logs` directory (see [Directories & Data Locations](#-directories--data-locations))

> **Note:** A dedicated issue template will be added in a future update to guide you through providing the most useful information.

---

## 📚 FAQ / Help

CalradiaForge includes a built-in **FAQ page** accessible from the navigation menu. Below are some commonly asked questions:

### Other questions

- Check the in-app **FAQ** page for answers to common questions about mod detection, DLL unblocking, modpacks, importing presets, and more.
- Check the repository's **Issues** tab on GitHub for known problems and workarounds.
- Open a new issue if you run into:
 - Game not detecting mods launched via CalradiaForge
 - Installation / extraction errors
 - Unexpected behavior when switching modpacks

---

## 🧬 Under the Hood (For Developers)

CalradiaForge is built around a **layered architecture** and a strict separation between UI and core logic.

### Architecture at a Glance

- **UI Layer (WPF Application)**
 - Navigation (Mods, Modpacks, Settings, FAQ)
 - Visual styling, theming, and interaction
 - No direct file-system or game-specific logic

- **Core Infrastructure (Class Library)**
 - Paths, configuration, detection, and logging
 - Services for:
   - Mod discovery and classification (active/inactive)
   - Modpack creation, storage, import/export
   - Game launch handling across Steam/Standalone

- **Helpers**
 - Stateless, pure-logic utilities such as path helpers
 - Receive all required data via parameters (no globals)

- **Config System**
 - `AppConfig` implements the repository pattern:
   - JSON read/write
   - Thread-safe access
   - Directory safety/validation
 - `AppConfigSettings` provides strongly-typed, bindable access to configuration
   - No direct IO
   - No save logic

### Service Layer

All core services are instantiated once in `App.xaml.cs` on startup and exposed as static properties for page access. Each service receives its dependencies explicitly via constructor parameters — no service accesses global state.

| Service         | Responsibility |
|-----------------|----------------|
| `ModService`   | Mod discovery, directory scanning, caching, and change detection |
| `ModInstaller` | Batch archive extraction, version comparison, BLSE fallback, format validation |
| `ModpackService`| Modpack CRUD, import/export, Novus conversion, last-used persistence, template creation |
| `GameLauncher` | Platform-aware game launch (Steam auto-start, BLSE support, CLI argument building) |
| `ToastService` | Application-wide toast notifications with auto-dismiss, pause/resume, and progress tracking |
| `EulaService`   | EULA text loading, acceptance checking, and persistence via `AppConfigSettings` |
| `Logger`        | Singleton file-based logger with level gating (Debug/Info/Warning/Error), 14-day cleanup |

### Config System

| Class               | Role                                                   | Pattern       |
|---------------------|--------------------------------------------------------|---------------|
| `AppConfig`         | Low-level JSON key/value storage with thread-safe access | Repository    |
| `AppConfigSettings` | Strongly-typed properties with `INotifyPropertyChanged` for data binding | Typed Facade / Adapter |

Configuration is created once in `App.xaml.cs`, passed explicitly into services and helpers. `AppConfig` handlespersistence and thread safety. `AppConfigSettings` provides typed access — it never performs I/O or saves directly.

### Toast System

A purpose-built notification overlay rendered in `MainWindow.xaml` via an `ItemsControl` bound to `ToastService.VisibleToasts`. Supports:

- **Severity levels** — Info, Success, Warning, Error (each with distinct accent color and icon)
- **Auto-dismiss timers** with pause on hover and resume on leave
- **Persistent progress toasts** for long-running operations (mod installation with ETA)
- **Template selection** — Default, InstallProgress, InstallSummary, MissingMods
- **Max 3 visible** with oldest-eviction and fade-out close animation

### Mod Installation Pipeline

Archives are processed by a service-owned background task that survives page navigation:

1. **Format validation** — `ModInstaller.IsAcceptedArchive()` rejects unsupported extensions before extraction
2. **Extraction** — `ModExtractor.ExtractToTempAsync()` with per-file progress callbacks
3. **Root detection** — `ModExtractor.FindModRoot()` walks nested folders to locate `SubModule.xml`
4. **BLSE fallback** — `BLSEInstaller` handles non-module archives containing BLSE executables
5. **Version check** — Compares against existing installation (install / upgrade / skip)
6. **File copy** — Recursive copy to `Modules` folder with cancellation support
7. **DLL unblocking** — `DLLUnblocker` strips `Zone.Identifier` ADS via Win32 `DeleteFile`
8. **Progress reporting** — Batch-level cumulative tracking with heuristic file-count estimation and ETA

### EULA Acceptance Flow

On startup, `App.OnStartup` calls `EulaAcceptance()` before any services or the main window are initialized:

1. **Check persisted state** — `EulaService.RequiresAcceptance()` reads the `EulaAccepted` flag from `AppConfigSettings`
2. **Show modal window** — If acceptance is required, `EulaWindow` is displayed as a modal dialog with the full agreement text loaded from the `Resources` directory
3. **Accept or decline** — The user can **Accept**, **Decline**, or close the window
4. **Persist decision** — On acceptance, `EulaService.RecordAcceptance()` writes the flag to config. On decline, the application shuts down immediately
5. **One-time prompt** — Once accepted, the EULA window is never shown again on subsequent launches

### Design Principles

- **UI decides *when*, core decides *how***  
 UI triggers actions; core provides deterministic, testable behavior.

- **No global state in helpers or services**  
 All dependencies are injected explicitly via constructor parameters or method arguments.

- **Defensive design**  
 Core components validate inputs and are safe to use independently of the WPF application.

- **Service-owned task lifetime**  
 Long-running operations (mod installation) are owned by the service layer, not the UI page. Navigation away does not cancel or orphan background work.

For the complete coding standards, patterns, and contribution rules, see:

- [`CONTRIBUTIONS.md`](docs/CONTRIBUTIONS.md)

---

## 🤝 Contributing

Contributions are welcome as long as they respect the project's architecture and license.

- Read the contributor rules and patterns in: 
 [`CONTRIBUTIONS.md`](docs/CONTRIBUTIONS.md)
- Fork the repository and create a feature branch:
 ```git
 git checkout -b feature/my-feature
 ```
- Implement your changes and add tests where it makes sense.
- Open a pull request with:
  - A short summary of the change
  - Any relevant screenshots for UI tweaks
  - Notes about behavior or breaking changes (if any)

By submitting a contribution, you agree to the project’s Contributor License terms described in the main `LICENSE.md`.

---

## 📄 License & Publisher

Copyright © 2026 ThelianTech™

ThelianTech is the trade name of the author and publisher of this software.

CalradiaForge is distributed under a custom license.  
A copy of the software license can be found here:

###### [![CalradiaForge License][CalradiaForge-License-Shield]][CalradiaForge-License]

---

[CalradiaForge-Repo]: https://github.com/ThelianTech-Studios/CalradiaForge
[CalradiaForge-Github-Shield]: https://img.shields.io/badge/CalradiaForge-Repo?style=plastic&logo=github&logoColor=%23181717&label=GitHub&color=blue
[CalradiaForge-Nexus]: https://www.nexusmods.com/mountandblade2bannerlord/mods/10332
[CalradiaForge-Nexus-Shield]: https://img.shields.io/badge/CalradiaForge-Nexus?style=plastic&label=NexusMods&labelColor=Black&color=orange
[CalradiaForge-License]: https://github.com/ThelianTech-Studios/CalradiaForge/blob/master_docs/LICENSE.md
[CalradiaForge-License-Shield]: https://img.shields.io/badge/CalradiaForge-License?style=plastic&label=LICENSE&labelColor=blue&color=green
