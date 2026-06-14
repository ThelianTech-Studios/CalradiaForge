# Mod Management

## Currently Implemented
- `ModService` coordinates scans and cache refreshes.
- `ModScanner` discovers installed modules from the game and Steam Workshop directories.
- `ModParser` reads `SubModule.xml` metadata.
- `ModInstaller` runs archive installs as a service-owned background task.
- `ModExtractor` handles archive extraction and mod-root detection.
- `BLSEInstaller` handles BLSE as a special-case install path.
- `ModsData` stores mod cache snapshots.

## Architecture Guidance
- `ModInstaller` and `ModExtractor` remain the authority for install flow.
- Installation work stays owned by the service, not the page.
- Mod scans populate cached data that the UI can consume.
- BLSE is treated separately from normal mod counts and install rules.

## Key Files
- `source/CalradiaForge.Core/Infra/Mods/ModService.cs`
- `source/CalradiaForge.Core/Infra/Mods/ModScanner.cs`
- `source/CalradiaForge.Core/Infra/Mods/ModParser.cs`
- `source/CalradiaForge.Core/Infra/Mods/ModInstaller.cs`
- `source/CalradiaForge.Core/Infra/Mods/ModExtractor.cs`
- `source/CalradiaForge.Core/Infra/Mods/BLSEInstaller.cs`
- `source/CalradiaForge.Core/Infra/Mods/ModsData.cs`

## Deferred / Future Work
- Nexus download handling is not part of the current mod management pipeline.
