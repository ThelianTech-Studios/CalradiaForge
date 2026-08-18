# User Data

User data is stored relative to the portable application root. The path model
and file names are authoritative in `source/CalradiaForge.Core/Infra/Paths/AppPaths.cs`.

| Data set | Detail | Owner |
| --- | --- | --- |
| Configuration | `Config/config.json` and corruption backups | [Configuration](../../02_Systems/CalradiaForge_Core/Configuration/README.md) |
| Logs | `Logs/CalradiaForge_Latest.log` and startup archives | [Logging](../../02_Systems/CalradiaForge_Core/Logging/README.md) |
| Modpacks | `Modpacks/` JSON definitions and `Data/last_used_mods.data` | [Modpacks](../../02_Systems/CalradiaForge_Core/Modpacks/README.md) |
| Module inventory | `Data/mods_current.data` and `Data/mods_backup.data` | [Mod management](../../02_Systems/CalradiaForge_Core/Mod_Management/README.md) |
| Language resources | `Languages/` manifest and language JSON | [Localization](../../02_Systems/CalradiaForge_Core/Localization/README.md) |
| EULA | Embedded UI resource `CalradiaForge.Resources.EULA.txt` | [EULA](../../02_Systems/CalradiaForge_Core/EULA/README.md) |

Nexus download directories are path reservations in the current path model;
they do not establish a shipped Nexus feature.
