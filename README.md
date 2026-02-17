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
    - or the NexusMods page: [CalradiaForge on Nexus][CalradiaForge-Nexus]

2. **Point CalradiaForge at your game**
  - Open **Settings → Game Config**
  - Select your Bannerlord installation folder (and executable if requested)
  - Use the **Re-detect** option if you installed via Steam and want auto-detection

3. **Install your mods**
  - Go to the **Mods** page
  - Use the mod installation control to select downloadedarchives
  - CalradiaForge will extract to the `Modules` folder and automatically unblock DLLs

4. **Organize and enable mods**
  - Drag mods between **Active** and **Inactive** lists
  - Dragwithin the active list to adjust load order
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

A dedicated **Help / FAQ** page is planned inside the application.

Until then:

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
 - Navigation (Mods, Modpacks, Settings, future Help/FAQ)
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

### Design Principles

- **UI decides *when*, core decides *how***  
 UI triggers actions; core provides deterministic, testable behavior.

- **No global state in helpers**  
 Helpers accept explicit dependencies (e.g., `AppConfigSettings`, paths, or plain data models).

- **Defensive design**  
 Core components are safe to use independently of the WPF application.

For a deep dive into the patterns, folder layout, and rules, see:

- [`CONTRIBUTIONS.md`](../../Contributor_Guidelines.md)

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




