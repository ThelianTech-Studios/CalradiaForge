# Persistence Policy

## Purpose

CalradiaForge persisted data must have clear ownership, safe writes, graceful recovery, and documented migration rules as stored models evolve.

## Current Grounding

| Area | Owner | Current behavior |
|---|---|---|
| Config JSON | `AppConfig` | Thread-safe key/value JSON store using direct `File.WriteAllText`. |
| Typed settings | `AppConfigSettings` | Facade over `AppConfig`; seeds defaults and raises change notifications. |
| Runtime paths | `AppPaths` | Resolves and creates app working directories. |
| Mod cache | `ModsData` | Reads/writes current and backup `ModuleModel` lists. |
| Modpacks | `ModpackData` | Reads/writes `ModpackModel` JSON files and last-used data. |
| Modpack workflow | `ModpackService` | Coordinates modpack behavior and delegates most persistence to `ModpackData`; export currently writes directly. |
| Future Nexus metadata | Nexus/Core model boundary | Planned separate metadata files, never stored in `ModuleModel`. |

## Policy

- `AppConfig` remains the only owner of config JSON persistence.
- `AppConfigSettings` remains the typed facade and must not perform direct file I/O.
- `ModsData` owns mod cache persistence.
- `ModpackData` owns modpack and last-used persistence.
- `ModpackService` may orchestrate saves but should not directly write domain JSON files.
- Important JSON writes should use atomic write-and-replace behavior.
- Important persisted files should support backup recovery.
- Corrupt files should be handled gracefully and should not crash normal startup.
- Future persisted model-breaking changes should document migration behavior before implementation.
- Future Nexus metadata must follow this policy and must not store credentials.

## Initial Targets

| File/data area | Initial rule |
|---|---|
| `AppConfig` | Atomic save plus secret-like key rejection. |
| `ModsData` current cache | Atomic save plus backup recovery. |
| `ModsData` backup cache | Atomic save; used as recovery input. |
| `ModpackData` modpacks | Atomic save plus recovery policy for important user-created files. |
| `ModpackData` last-used load order | Atomic save. |
| `ModpackService.Export` | Move write mechanics into a data helper or shared persistence helper in a later implementation phase. |
| Future Nexus metadata | Atomic save plus schema/version rules; no credentials. |

## Phased Implementation

| Phase | Scope |
|---|---|
| 1 | Add shared atomic JSON write helper or equivalent owner-specific atomic write pattern. |
| 2 | Apply atomic saves to `AppConfig`, `ModsData.SaveCurrent`, `ModsData.SaveBackup`, `ModpackData.SaveModpack`, and `ModpackData.SaveLastUsed`. |
| 3 | Add recovery behavior for `ModsData` current/backup files and important modpack files. |
| 4 | Add validation before replacing current files where practical. |
| 5 | Move direct domain JSON writes, such as modpack export, behind an owning helper when implementation work is requested. |
| 6 | Add `schemaVersion` only where model evolution justifies it. |
| 7 | Consider quarantine/recovery folders for corrupt persisted files. |

## Performance Verification

Later performance work may measure JSON read/write, serialization, atomic replacement, backup recovery, cache hit/miss behavior, and modpack export costs. Correctness and recovery tests must pass or have documented dispositions before performance comparisons are interpreted.

Measurements should distinguish app-owned serialization and I/O work from filesystem, antivirus, storage, and cache variance. Component benchmarks should separate setup and cleanup from the measured operation while end-to-end measurements retain the user-visible persistence cost.

## Verification Expectations

- Valid writes produce readable JSON and preserve expected model fields.
- Failed writes do not destroy the last known good file.
- Missing files fall back to safe defaults or empty lists as appropriate.
- Corrupt files are reported and recovered or skipped without app crash.
- Services continue to delegate persistence to their owning data helpers.
- Future Nexus metadata tests verify credentials are not written to metadata files.

## Guardrails

- Do not store credentials, auth tokens, API keys, or secret URLs in `AppConfig`.
- Do not move persistence ownership into UI code-behind.
- Do not duplicate `ModsData` or `ModpackData` behavior in services.
- Do not introduce WPF dependencies into Core persistence helpers.
- Do not introduce a migration framework before a real persisted model change needs it.
- Do not weaken atomicity, backup recovery, corruption handling, or safe defaults to improve throughput.

## Out Of Scope

- Full database migration.
- Cloud sync.
- Nexus credential storage.
- Broad schema-version framework before a breaking persisted model change exists.

## Open Questions

- Should modpack recovery use per-file `.bak` files, a recovery folder, or both?
- Should corrupt user modpacks be quarantined automatically or left in place with warnings?
- Which persisted files require schema versions immediately?
- Should exported modpacks use the same backup behavior as app-owned modpack files?
