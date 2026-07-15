# Mod Management

## Currently Implemented
- `ModService` coordinates scans and cache refreshes.
- `ModScanner` discovers installed modules from the game and Steam Workshop directories.
- `ModParser` reads `SubModule.xml` metadata.
- `ModInstaller` runs archive installs as a service-owned background task.
- `ModExtractor` validates archive entry containment before extraction, handles extraction and mod-root detection, and only cleans app-managed GUID extraction directories.
- `BLSEInstaller` handles BLSE as a special-case install path.
- `ModInstaller` performs named module preflight before destination writes: exactly one `SubModule.xml`, parsed identity, target containment, and matching identity for an existing target.
- `ModsData` atomically stores mod cache snapshots, validates rotation input, and can recover a corrupt current cache from its backup.

## Architecture Guidance
- `ModInstaller` and `ModExtractor` remain the authority for install flow.
- Installation work stays owned by the service, not the page.
- Mod scans populate cached data that the UI can consume.
- BLSE is treated separately from normal mod counts and install rules.
- Unknown, unparsable, or identity-mismatched existing module folders are not overwritten automatically.
- Upgrade copying stops when the previous target cannot be removed completely.

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
- Reserved/official module blocking awaits an owner-approved folder list.
- BLSE allowlist enforcement awaits an owner-approved file/folder manifest.
- Normal-module backup, rollback, and confirmation behavior remains owner-gated.
- Deterministic target naming versus blocking for flat archives with root-level `SubModule.xml` remains owner-gated; the current extraction-GUID target behavior is a known limitation.
- Steam Workshop multi-library resolution and scanner stabilization remain deferred pending owner-approved candidate precedence, manual-override semantics, and fake-root verification.
