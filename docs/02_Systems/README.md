# Systems

This domain documents runtime mechanisms by owning project first and capability
second. Use [Architecture](../01_Architecture/README.md) for structural
ownership, [Application](../04_Application/README.md) for user-visible rules,
and [Data](../03_Data/README.md) for durable storage.

| Owner | System area | Current status |
| --- | --- | --- |
| Core | [Configuration](CalradiaForge_Core/Configuration/README.md) | Implemented |
| Core | [Logging](CalradiaForge_Core/Logging/README.md) | Implemented |
| Core | [Game platforms](CalradiaForge_Core/Game_Platforms/README.md) | Implemented; platform evidence remains separately bounded |
| Core | [Mod management](CalradiaForge_Core/Mod_Management/README.md) | Implemented |
| Core | [Modpacks](CalradiaForge_Core/Modpacks/README.md) | Implemented |
| Core | [Localization](CalradiaForge_Core/Localization/README.md) | Implemented |
| Core | [EULA](CalradiaForge_Core/EULA/README.md) | Implemented |
| UI | [Application lifecycle](CalradiaForge_UI/Application_Lifecycle/README.md) | Implemented |
| UI | [Notifications](CalradiaForge_UI/Notifications/README.md) | Implemented |
| UI | [Dialogs and interactions](CalradiaForge_UI/Dialogs_and_Interactions/README.md) | Implemented |
| Nexus | [Nexus boundary](CalradiaForge_Nexus/README.md) | Reserved/optional; feature implementation not present |
| ConsoleUtils | [Language utility](CalradiaForge_ConsoleUtils/README.md) | Implemented developer utility |

Tests and benchmarks are documented under [Development](../06_Development/README.md),
not as runtime systems.
