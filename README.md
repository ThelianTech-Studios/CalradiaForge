
# CalradiaForge

A modern mod launcher for **Mount & Blade II: Bannerlord**.

CalradiaForge streamlines mod installation, load order management, preset sharing, and game launching into a single purpose-built desktop application.

> *`CalradiaForge was built by players, for players.`*

---

## What CalradiaForge Solves

Managing large mod lists in Bannerlord is tedious, error-prone, and poorly supported by the default launcher.

CalradiaForge provides a reliable workflow for:

- Installing mods from downloaded archives
- Automatically unblocking required DLL files
- Managing active and inactive mods with clear visibility
- Sorting and organizing load order with drag-and-drop
- Creating and sharing preset mod configurations
- Launching the game with the exact mod setup you intend

---

## Core Capabilities

### Mod Installation & Handling
- Archive extraction directly into the Bannerlord modules folder
- Automatic DLL unblocking during install
- Clean separation of active vs inactive mods
- Fast search across mod lists

### Load Order & Presets
- Drag-and-drop load order control
- Enable/disable mods instantly
- Create custom presets
- Import presets from other users
- Export shareable preset files (JSON)

### Application Experience
- Purpose-built WPF desktop interface
- Clear navigation between Mods, Presets, Settings, and Help
- Settings management for game launch configuration
- Integrated FAQ for common guidance

---

## Architecture Overview

CalradiaForge is built using a two-layer architecture with the MVVM design pattern.

**Presentation Layer (WPF)** handles UI, navigation, and user interaction.
**Business Logic Layer (.NET Class Library)** handles file operations, mod processing, preset management, and game launch control.

Primary libraries include MahApps.Metro, Newtonsoft.Json, and SharpCompress.

---

## Development Roadmap — Version 1.0

- WPF MVVM project foundation
- Complete UI navigation and layout
- Drag-and-drop mod list interface
- Mod extraction and installation system
- Automatic DLL unblocking
- Active / Inactive mod management
- Mod list search
- Preset creation, import, and export
- Game launch handler with selected load order
- Settings and configuration page
- Help / FAQ page
- Testing and usability validation
- Release packaging and NexusMods deployment

---

## Post-1.0 Considerations

The following are intentionally outside the scope of the initial release:

- Direct integration with mod hosting APIs for automated downloads
- Automatic dependency detection and load order resolution
- Curated or prebuilt community mod lists
- Advanced mod metadata indexing

---

## Project Status

CalradiaForge is currently in active development toward a Version 1.0 release.

---

## License & Publisher

Copyright © 2026 ThelianTech™

ThelianTech is the trade name of the author and publisher of this software.
