# Modpack Data

Named modpacks are JSON files under `Modpacks/`. A modpack stores its name,
creator/date metadata, and ordered `ModpackEntryModel` values representing the
selected load order and active/inactive state. `last_used_mods.data` stores the
last-used working load order separately.

The data layer is `ModpackData`; orchestration and template/import behavior are
owned by `ModpackService`. The application capability specification is
[Modpacks](../../04_Application/Capabilities/Modpacks.md).
