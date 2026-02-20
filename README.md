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

> ⚠️ **Note:** Epic Games and GamePass support is planned for a future release once the developer has access to test on these platforms.

---

## 📦 Supported Mod Archive Formats

| Format | Support                  | Notes |
|--------|--------------------------|-------|
| `.zip` | ✅ Supported              | Recommended format. Fastest extraction. |
| `.rar` | ✅ Supported              | Fully supported. |
| `.7z`  | ⏸️ Temporarily Disabled  | See [FAQ: Why are .7z archives not supported?](#why-are-7z-archives-not-supported) |

---

## ✨ Key Features

### 📦 Mods Management

- Install mods directly from downloaded archives into the Bannerlord `Modules` folder
- Automatic **DLL unblocking** during install to avoid common Windows security issues
- Clear separation of **Active** vs **Inactive** mods
- **Search** across your mods list to quickly find specific entries
- **Drag-and-drop reordering** of mods (within and between lists) to adjust load order

### 🧩 Modpacks (Presets)

- Create named **modpacks** representing complete mod setups
- Save and edit modpacks as your list evolves
- Switch between modpacks from the Mods page via a dedicated modpack selector
- Automatically store the **last used** modpack and restore it on next launch (configurable)
- Import external modpacks/presets, including **Novus Launcher** presets via a built-in converter

### 🎮 Game Launching

- Launch **Mount & Blade II: Bannerlord** directly from CalradiaForge
- Support for multiple platforms:
- Steam
- GOG / Standalone
- Validation checks (e.g., game path presence) before starting to reduce launch errors
- Launch the game with the **exact active mod setup selected by your current modpack**

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

1. **Download & Install**
 - Grab the latest official release from:
   - [![GitHub Repo][CalradiaForge-Github-Shield]][CalradiaForge-Repo]
   - [![Nexus][CalradiaForge-Nexus-Shield]][CalradiaForge-Nexus]

2. **Point CalradiaForge at your game**
 - Open **Settings → Game Config**
 - Select your Bannerlord installation folder (and executable if requested)
 - Use the **Detect Game** option if you installed Bannerlord via other *Game Platforms* and need to redetect the game

3. **Install your mods**
 - Go to the **Mods** page
 - Use the mod installation control to select downloaded archives (`.zip` or `.rar`)
 - CalradiaForge will extract to the `Modules` folder and automatically unblock DLLs

4. **Organize and enable mods**
  - Drag mods between **Active** and **Inactive** lists
  - Drag within the active list to adjust load order
  - Use the search box to quickly find specific mods

5. **Create a modpack**
  - Save your current selection and ordering as a **modpack**
  - Name it (e.g., "Vanilla+ QoL", "Overhaul Build") and save
  - Use the modpack selector on the Mods page to switch between setups

6. **Launch the game**
  - Verify your desired modpack is selected
  - Click **Play** to start Bannerlord with the chosen active mods and order

---

## 📚 FAQ / Help

CalradiaForge includes a built-in **FAQ page** accessible from the navigation menu. Below are some commonly asked questions:

### Why are .7z archives not supported?

CalradiaForge currently uses SharpCompress for archive extraction. The `.7z` format uses **block compression** (LZMA/LZMA2), which SharpCompress must decompress sequentially and entirely in-memory. For large Bannerlord mods with 1,000+ files, this causes extraction times of **~25 minutes** for a single mod — making the install experience unacceptable.

The `.zip` and `.rar` formats use per-file compression, allowing SharpCompress to extract each file individually without this bottleneck.

> **Workaround:** If your mod is only available as a `.7z` file, extract it manually using [7-Zip](https://www.7-zip.org/) and re-archive it as a `.zip` before installing through CalradiaForge.

> **Planned fix:** A future update will integrate native 7-Zip extraction (via the 7-Zip SDK or CLI) to handle `.7z` archives at full speed.

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

| Service | Responsibility |
|---------|---------------|
| `ModService` | Mod discovery, directory scanning, caching, and change detection |
| `ModInstaller` | Batch archive extraction, version comparison, BLSE fallback, format validation |
| `ModpackService` | Modpack CRUD, import/export, Novus conversion, last-used persistence, template creation |
| `GameLauncher` | Platform-aware game launch (Steam auto-start, BLSE support, CLI argument building) |
| `ToastService` | Application-wide toast notifications with auto-dismiss, pause/resume, and progress tracking |
| `Logger` | Singleton file-based logger with level gating (Debug/Info/Warning/Error), 14-day cleanup |

### Config System

| Class | Role | Pattern |
|-------|------|---------|
| `AppConfig` | Low-level JSON key/value storage with thread-safe read/write | Repository |
| `AppConfigSettings` | Strongly-typed properties with `INotifyPropertyChanged` for data binding | Typed Facade / Adapter |

Configuration is created once in `App.xaml.cs`, passed explicitly into services and helpers. `AppConfig` handles persistence and thread safety. `AppConfigSettings` provides typed access — it never performs I/O or saves directly.

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
2. **Extraction** — `ModExtractor.ExtractToTempAsync()` with per-file progress callbacks via SharpCompress
3. **Root detection** — `ModExtractor.FindModRoot()` walks nested folders to locate `SubModule.xml`
4. **BLSE fallback** — `BLSEInstaller` handles non-module archives containing BLSE executables
5. **Version check** — Compares against existing installation (install / upgrade / skip)
6. **File copy** — Recursive copy to `Modules` folder with cancellation support
7. **DLL unblocking** — `DLLUnblocker` strips `Zone.Identifier` ADS via Win32 `DeleteFile`
8. **Progress reporting** — Batch-level cumulative tracking with heuristic file-count estimation and ETA

### Design Principles

- **UI decides *when*, core decides *how***  
 UI triggers actions; core provides deterministic, testable behavior.

- **No global state in helpers or services**  
 All dependencies are received explicitly via constructor parameters or method arguments.

- **Defensive design**  
 Core components validate inputs and are safe to use independently of the WPF application.

- **Service-owned task lifetime**  
 Long-running operations (mod installation) are owned by the service layer, not the UI page. Navigation away does not cancel or orphan background work.

For the complete coding standards, patterns, and contribution rules, see:

- [`CONTRIBUTING.md`](../../CONTRIBUTING.md)

---

## 🤝 Contributing

Contributions are welcome as long as they respect the project's architecture and license.

- Read the contributor rules and patterns in:  
 [`CONTRIBUTIONS.md`](../../CONTRIBUTIONS.md)
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
[CalradiaForge-Nexus]: chrome://network-error/-106
[CalradiaForge-Nexus-Shield]: https://img.shields.io/badge/CalradiaForge-Nexus?style=plastic&label=NexusMods&labelColor=Black&color=orange
[CalradiaForge-License]: https://github.com/ThelianTech-Studios/CalradiaForge/blob/master_docs/LICENSE.md
[CalradiaForge-License-Shield]: https://img.shields.io/badge/CalradiaForge-License?style=plastic&label=LICENSE&labelColor=blue&color=green




