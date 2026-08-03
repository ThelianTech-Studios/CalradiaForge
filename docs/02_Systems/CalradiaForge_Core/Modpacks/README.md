# Core Modpacks

`ModpackData` persists named modpacks and last-used state. `ModpackService`
orchestrates load, default-Vanilla creation, refresh, save, create, import,
export, and validation. `NovusPresetConverter` supports the implemented Novus
XML import path; built-in templates are supplied by `VanillaModules`.

Modpacks are durable load-order definitions and are distinct from the accepted
installed-module snapshot owned by `ModPipelineManager`. See [Modpack data](../../../03_Data/User_Data/Modpacks.md)
and [Modpacks capability](../../../04_Application/Capabilities/Modpacks.md).
