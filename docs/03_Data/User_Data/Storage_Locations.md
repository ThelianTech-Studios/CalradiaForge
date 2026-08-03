# Storage Locations

The portable root is derived from the executing assembly location. Core lazily
resolves and creates the configured directories:

```text
<application root>/Config/config.json
<application root>/Logs/CalradiaForge_Latest.log
<application root>/Modpacks/<modpack>.json
<application root>/Data/mods_current.data
<application root>/Data/mods_backup.data
<application root>/Data/last_used_mods.data
<application root>/Languages/languages.json
<application root>/Languages/en-US.json
<application root>/Nexus/Downloads/
<application root>/Nexus/ModsMetadata/
<application root>/.extraction/
```

The EULA is currently embedded in the UI assembly as the manifest resource
`CalradiaForge.Resources.EULA.txt`; it is not read from a deployed user-data
file. `AppPaths` also exposes a `Resources/Eula.txt` path reservation, so the
relationship between that reservation and the embedded resource remains an
unresolved source-level discrepancy.

The final two locations are path reservations in `AppPaths`; current Nexus
feature behavior is not implied. The extraction directory is created as a
hidden directory by Core's path helper.
