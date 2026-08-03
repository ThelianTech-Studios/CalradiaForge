# Configuration Data

Configuration is a JSON dictionary managed by `ConfigFileManager` and exposed
through the typed `AppSettings` facade. Defaults are materialized before a disk
overlay. Recognized missing, null, or invalid values are repaired; unknown
values may survive; obsolete keys are removed during repair.

Persistence uses the Core atomic-writing seam. A malformed file is retained as
a collision-safe sibling backup before replacement when possible. If the file
cannot be read or written, the in-memory session continues with defaults/current
values and the persistence status becomes unavailable. The UI receives the
existing warning lifecycle rather than owning recovery.

See [Core Configuration](../../02_Systems/CalradiaForge_Core/Configuration/README.md)
for mechanics and [Durable Serialization Contracts](../Internal_Contracts/Durable_Serialization_Contracts.md)
for compatibility boundaries.
