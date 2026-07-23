# Modpacks

## Currently Implemented
- `ModpackService` owns modpack CRUD, import/export, validation, and last-used persistence.
- `ModpackData` owns file I/O for modpack JSON and last-used data and writes both atomically.
- `ModpackFileHelper` sanitizes filenames.
- `VanillaModules` provides built-in template load orders.
- `NovusPresetConverter` supports import from Novus Launcher presets.

## Architecture Guidance
- Modpack files are stored separately from the current mod cache.
- The built-in `Vanilla` modpack should exist on disk.
- The "Last Used" load order is persisted separately from named modpacks.
- Validation should report missing entries without mutating the saved modpack.
- Validation accepts one read-only accepted mod-pipeline snapshot so a load-order operation uses one coherent module version.

## Key Files
- `source/CalradiaForge.Core/Infra/Modpacks/ModpackService.cs`
- `source/CalradiaForge.Core/Infra/Modpacks/ModpackData.cs`
- `source/CalradiaForge.Core/Infra/Modpacks/ModpackFileHelper.cs`
- `source/CalradiaForge.Core/Infra/Modpacks/VanillaModules.cs`
- `source/CalradiaForge.Core/Infra/Modpacks/NovusPresetConverter.cs`

## Deferred / Future Work
- Additional import formats and modpack workflows should only be added when the source code requires them.
- Recovery for corrupt named modpacks remains deferred until the owner selects per-file backups, a recovery directory, or both.
