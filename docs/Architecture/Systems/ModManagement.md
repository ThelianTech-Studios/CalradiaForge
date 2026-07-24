# Mod Management

## Currently Implemented
- `ModPipelineManager` is the single Core workflow boundary for startup/refresh scans, cache commit authorization, accepted snapshot/version publication, install admission, cancellation, and quiescence.
- `ModScanner` remains the low-level discovery owner and returns structured local/Workshop completeness, counts, warnings, parse diagnostics, and duplicate diagnostics.
- For Steam, scanner inputs are independent resolved game and Workshop roots; split-library discovery is owned by the path-resolution system rather than `ModScanner`.
- `ModParser` reads `SubModule.xml` metadata.
- `ModInstaller` runs archive installs as an awaitable service-owned task; the coordinator admits and tracks that task without replacing install mechanics.
- `ModExtractor` validates archive entry containment before extraction, handles extraction and mod-root detection, and only cleans app-managed GUID extraction directories.
- `BLSEInstaller` handles BLSE as a special-case install path.
- `ModInstaller` performs named module preflight before destination writes: exactly one `SubModule.xml`, parsed identity, target containment, and matching identity for an existing target.
- `ModsData` atomically stores mod cache snapshots, validates rotation input, and can recover a corrupt current cache from its backup.
- Current and backup mod-cache writes use the shared bounded-retry atomic replacement path.
- Complete scans are deterministic: paths are scanned in stable order, module IDs compare case-insensitively, local modules win over Workshop duplicates, and the first stable entry wins within a root.

## Architecture Guidance
- `ModInstaller` and `ModExtractor` remain the authority for install flow.
- Installation work stays owned by the service, not the page.
- Only a complete scan rotates/saves cache data and atomically publishes a new accepted snapshot/version. Invalid configuration, missing/inaccessible configured roots, parse failures, cancellation, and unexpected failure preserve the prior accepted and disk state.
- Steam with no configured Workshop path is an explicit complete local-only state. A configured missing/inaccessible Workshop root is incomplete, while a valid empty Workshop root is a complete zero-result.
- Modpack/load-order consumers capture one accepted snapshot so they cannot observe mixed scan versions.
- The coordinator rejects concurrent scan/install requests as busy, can permanently stop admission, cooperatively cancels active work, and exposes awaitable quiescence for Phase 6.B.
- Application shutdown uses that admission, cancellation, and quiescence contract before persisting the last-used load order and disposing the service provider.
- BLSE is treated separately from normal mod counts and install rules.
- Unknown, unparsable, or identity-mismatched existing module folders are not overwritten automatically.
- Upgrade copying stops when the previous target cannot be removed completely.

## Key Files
- `source/CalradiaForge.Core/Infra/Mods/ModPipelineManager.cs`
- `source/CalradiaForge.Core/Infra/Mods/IModScanner.cs`
- `source/CalradiaForge.Core/Infra/Mods/ModScanner.cs`
- `source/CalradiaForge.Core/Infra/Mods/ModParser.cs`
- `source/CalradiaForge.Core/Infra/Mods/ModInstaller.cs`
- `source/CalradiaForge.Core/Infra/Mods/ModExtractor.cs`
- `source/CalradiaForge.Core/Infra/Mods/BLSEInstaller.cs`
- `source/CalradiaForge.Core/Infra/Mods/ModsData.cs`
- `docs/Architecture/Systems/PlatformAndPathDetection.md`

## Deferred / Future Work
- Nexus download handling is not part of the current mod management pipeline.
- Reserved/official module blocking awaits an owner-approved folder list.
- BLSE allowlist enforcement awaits an owner-approved file/folder manifest.
- Normal-module backup, rollback, and confirmation behavior remains owner-gated.
- Deterministic target naming versus blocking for flat archives with root-level `SubModule.xml` remains owner-gated; the current extraction-GUID target behavior is a known limitation.
- Broader generalized workflow results, install progress/state coordination, and UI-state conventions remain deferred to Phase 7; the workflow-specific Phase 6.A scan result is implemented.
