# Core Configuration

`ConfigFileManager` owns the JSON key/value store. It initializes an in-memory
defaults map, overlays valid persisted values, repairs missing/invalid known
settings, ignores obsolete keys, and reports typed load/save outcomes. Malformed
JSON is copied to a collision-safe corruption backup before a replacement is
attempted. Read/write failures make persistence unavailable while keeping the
current session's in-memory settings usable.

`AppSettings` is the typed, bindable facade. It exposes platform, language,
game, Workshop, modpack, launch, debug, EULA, and maintenance settings but does
not own file I/O. `LoggingSettings` supplies the logging-level setting.

The durable data locations are owned by [Data](../../../03_Data/User_Data/Configuration.md),
and cross-cutting recovery expectations are summarized in
[Security](../../../07_Security/Security_Model.md).
