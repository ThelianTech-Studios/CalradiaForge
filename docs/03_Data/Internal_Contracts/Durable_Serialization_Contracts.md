# Durable Serialization Contracts

The current durable JSON/data contracts are:

- configuration key/value data handled by `ConfigFileManager`;
- `ModpackModel` and ordered `ModpackEntryModel` values handled by
  `ModpackData`;
- module inventory data handled by `ModsData`;
- last-used load-order data handled by `ModpackData`;
- language manifest and translation dictionaries handled by Core localization.

These contracts are implementation-shaped rather than a public schema version
system. Compatibility expectations are therefore conservative: preserve
unknown configuration keys when the current loader allows them, repair known
settings through defaults, and do not assume a transient result object is a
durable contract.

Atomic persistence and recovery behavior is owned by [Configuration Data](../User_Data/Configuration.md)
and [Persistence Security](../../07_Security/Security_Model.md).
