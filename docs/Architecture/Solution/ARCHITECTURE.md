# CalradiaForge Architecture

## Overview
CalradiaForge is a layered WPF application for Bannerlord mod management and launching.
The repository is organized around a presentation layer, a core runtime library, a reserved Nexus integration assembly, a small developer utility project, and separate correctness-test and benchmark projects.

## Repository Structure
```text
source/
  CalradiaForge.UI/
  CalradiaForge.Core/
  CalradiaForge.Nexus/
  CalradiaForge.ConsoleUtils/
  CalradiaForge.Tests/
  CalradiaForge.Benchmarks/
  Languages/
docs/
  Architecture/
```

## Solution Structure
Currently discovered projects:
- `CalradiaForge.UI`
- `CalradiaForge.Core`
- `CalradiaForge.Nexus`
- `CalradiaForge.ConsoleUtils`
- `CalradiaForge.Tests`
- `CalradiaForge.Benchmarks`

The solution file is `source/CalradiaForge.slnx`.

## Layered Architecture
### Currently Implemented
- UI owns windows, pages, navigation, and user intent.
- Core owns paths, config, logging, localization, mod discovery, mod installation, modpack storage, and launch logic.
- The WPF `App` owns one validated application provider, while Core and UI registration modules extend the same service collection without building providers.
- `ApplicationStartupCoordinator` and `ApplicationShutdownCoordinator` own ordered startup and quiescence; `App` retains final WPF/process and provider-disposal ownership.
- ConsoleUtils owns developer-only tooling that reuses Core services.
- Nexus is reserved as a separate integration boundary.
- Tests and benchmarks are developer-only consumers organized by owning project; the test project now covers Core and the Phase 6.B UI composition/lifecycle boundaries.

### Dependency Direction
```text
CalradiaForge.UI -> CalradiaForge.Core
CalradiaForge.UI -> CalradiaForge.Nexus
CalradiaForge.Nexus -> CalradiaForge.Core
CalradiaForge.ConsoleUtils -> CalradiaForge.Core
CalradiaForge.Tests -> CalradiaForge.Core
CalradiaForge.Tests -> CalradiaForge.UI
CalradiaForge.Benchmarks -> CalradiaForge.Core
```

## Cross-Cutting Systems
Currently implemented cross-cutting systems live mostly in Core:
- Configuration and runtime paths
- Logging
- Application composition, startup, shutdown, restart, and global exception handling
- Localization
- EULA gating
- Mod scanning and installation
- Mod-pipeline completeness, accepted snapshots, cache commit gating, and quiescence
- Modpack persistence and templates
- Game launch and platform detection

## Design Rules
- UI decides when.
- Core decides how.
- Nexus work follows the same rule with Core and Nexus sharing the implementation boundary.
- Core remains UI-agnostic.
- Filesystem state is owned by the appropriate data helper or service.
- Long-running work stays service-owned rather than page-owned.
- DI owns construction and dependency delivery; the root provider is not exposed as a service locator.
- MainWindow and primary pages retain one-instance-per-application behavior until the later MVVM phase.

## Deferred / Planned Systems

- **Phase 7 planned handoff:** `ModPipelineManager` remains the Core operation
  owner; an explicitly activated UI presenter will adapt semantic progress/results
  to generic `ToastService`, and the current page will be renamed `LauncherPage`.
  Phase 8 ViewModel migration remains deferred. See the
  [Phase 7 reference](../../refactor/phase_7_locked_decisions_2026-07-24.md).
- Nexus Mods authentication, download management, metadata caching, and NXM handling are planned in the Nexus boundary.
- Epic Games and Game Pass support remains intentionally deferred in the launcher paths.
- Additional system-specific docs should be added only when the codebase grows enough to justify them.
