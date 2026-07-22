# Phase 6.C Locked Decisions — Legacy Logger Call-Site Migration

## Status

Owner-approved planning direction for:

```text
Phase 6.C — Legacy Logger Call-Site Migration
```

Phase 6.C begins only after Phase 6.B has established the final provider-owned Serilog lifecycle and global `Log.*` API.

## Current Source State

This is a future implementation contract; it does not claim Phase 6.C is implemented. Current Core and UI callers still use the general-purpose `Logger.Instance` path, including configuration/bootstrap-sensitive and application-lifecycle code. `EmergencyStartupLogWriter` does not exist yet, and the final provider-owned Serilog pipeline must be established by Phase 6.B before any migration batch begins.

---

# 1. Purpose

Migrate all normal application logging from the custom general-purpose `Logger.Instance` API to the one shared Serilog pipeline, verify equivalent behavior, remove the legacy logger, and retain only a narrow emergency pre-Serilog startup writer.

---

# 2. Prerequisites

Before 6.C begins:

- Phase 6.A mod-pipeline lifecycle is implemented,
- Phase 6.B one-provider architecture is implemented,
- one Serilog logger is constructed after `LoggingSettings.DebugMode` loads,
- the same logger is assigned to `Serilog.Log.Logger`,
- provider disposal is the sole normal close path,
- startup/shutdown/restart tests pass,
- `CalradiaForge_Latest.log` lifecycle is settled.

Do not redesign those foundations in 6.C.

---

# 3. Final Logging API

Use global Serilog calls as the normal application API:

```csharp
Log.Verbose(...)
Log.Debug(...)
Log.Information(...)
Log.Warning(...)
Log.Error(...)
Log.Fatal(...)
```

Use structured message templates and properties where they improve diagnostics.

Do not introduce:

- `Microsoft.Extensions.Logging.ILogger<T>`,
- `Serilog.Extensions.Logging`,
- a second wrapper logger,
- a new service-locator logging abstraction,
- a second Serilog pipeline.

Global Serilog use is already the locked exception to the general no-global-services rule.

---

# 4. Controlled Migration Batches

Do not migrate every call site in one uncontrolled edit.

Inventory all legacy usages and migrate in reviewable batches. A recommended semantic order is:

1. Core leaf utilities and stateless helpers.
2. Core persistence/configuration callers that execute only after Serilog activation.
3. Phase 5 detection/path workflow callers.
4. Phase 6.A mod-pipeline/scanner/cache callers.
5. install/extract/launch/modpack services.
6. UI pages, shell, dialogs, and toast/navigation services.
7. `App.xaml.cs` post-Serilog lifecycle paths.
8. remaining legacy logger internals and tests.
9. remove the general-purpose legacy `Logger` only after zero normal call sites remain.

Adjust batches to current source dependencies, but keep them narrow and independently buildable.

After each batch:

- build,
- run targeted tests,
- inspect log output where relevant,
- search for newly introduced format/template mistakes.

---

# 5. Manual Debug Guards

Remove repeated patterns such as:

```csharp
if (logger.MinimumLevel == Debug)
{
    logger.Debug(...);
}
```

Normal migrated debug events should call:

```csharp
Log.Debug(...);
```

Serilog’s configured minimum level decides emission.

Retain an explicit guard only when constructing the diagnostic payload is materially expensive or has side effects. In that case use the approved Serilog level check without recreating legacy minimum-level state.

Do not add an immutable session-state object solely for these checks.

---

# 6. Structured Logging Rules

During migration:

- prefer message templates over string concatenation,
- preserve exception objects in exception overloads,
- preserve meaningful source/workflow context,
- avoid logging mutable objects when selected scalar properties are clearer,
- do not leak credentials, auth tokens, API keys, or private secrets,
- do not recreate the removed central redaction/sanitization architecture.

The owner rejected automatic secret redaction/sanitization implementation and AppConfig secret-key blocking. Remove stale documentation requiring:

- `LogRedactor`,
- `RedactingTextFormatter`,
- secret-like-key rejection in settings,
- a redactor-removal approval gate.

Ordinary caller discipline remains required.

---

# 7. Bootstrap Logging Restriction

`ConfigFileManager` and `LoggingSettings` participate in constructing the final logger.

They cannot require the final Serilog pipeline before it exists.

After legacy migration:

- successful pre-Serilog config load/default initialization does not emit ordinary `Log.*` events,
- post-bootstrap startup may log loaded configuration state through the startup coordinator,
- failures before final logger activation route to `EmergencyStartupLogWriter`,
- no circular dependency is introduced.

`AppSettings` code that is not needed before logger construction may use `Log.*` only after the provider guarantees logger activation in its resolution path. The implementation must verify resolution ordering rather than assume it.

---

# 8. Remove the General-Purpose Legacy Logger

After all normal call sites migrate and verification passes:

- remove `Logger.Instance` usage,
- remove the singleton/lazy custom logger implementation,
- remove general-purpose `Info`, `Debug`, `Warning`, `Error`, or equivalent legacy APIs,
- remove legacy retention/cleanup behavior superseded by Phase 6.B,
- remove stale obsolete comments and phase references,
- remove tests that validate only the retired logger implementation,
- replace them with tests of final Serilog/lifecycle behavior where needed.

Do not keep the old logger as a compatibility façade after zero callers remain.

---

# 9. EmergencyStartupLogWriter

Add one narrow emergency writer for failures occurring before the final Serilog pipeline is operational.

Conceptual name:

```text
EmergencyStartupLogWriter
```

Primary path:

```text
<AppPaths.LogsDirectory>/CalradiaForge_StartupFailure.log
```

Scope:

- provider build failure before Serilog exists,
- settings/logging bootstrap failure,
- final logger/file-sink initialization failure,
- pre-Serilog previous-`Latest` archive/truncate failure when a diagnostic is useful,
- other unrecoverable startup failures that cannot reach `Log.*`.

It is not a normal logger.

Required behavior:

- invoked only on an error path,
- lazily creates/writes only when needed,
- synchronous best-effort append,
- no persistent stream,
- no background task,
- no async sink,
- no application-wide singleton requirement,
- no `Info`, `Debug`, or general warning API,
- no structured logging framework,
- no caller use after Serilog is operational,
- no interference with normal provider disposal.

Fallback behavior:

- attempt the primary logs-directory path first,
- use a narrow safe fallback path chain only when the primary path cannot be written,
- swallow secondary writer failures after debugger output/best effort,
- do not block fatal shutdown because the emergency log could not be written.

Do not expose the emergency writer through normal service injection to application components.

---

# 10. App and Global Exception Boundaries

After Serilog activation:

- app startup/shutdown/restart and global exception handlers use `Log.*`,
- dispatcher fatal exceptions log before controlled shutdown,
- unobserved task exceptions log and are observed,
- provider disposal remains the sole normal close path.

Before Serilog activation:

- app-owned catch boundaries use `EmergencyStartupLogWriter`,
- fatal startup terminates through the existing controlled/fallback path,
- do not instantiate the retired legacy logger.

---

# 11. No Separate Close Path

Phase 6.C must preserve:

```text
App disposes root provider
→ DI disposes shared Serilog logger
```

Do not add:

- `Log.CloseAndFlush()` in consumers,
- manual logger `Dispose()` calls,
- emergency-writer disposal,
- post-provider `Log.*` calls,
- a shutdown hook that closes the logger twice.

---

# 12. Log File Policy Remains Phase 6.B Architecture

Do not change:

- `CalradiaForge_Latest.log` as active/latest file,
- leaving `Latest` after shutdown,
- archive-on-next-startup,
- archive name `CalradiaForge_yyyy-MM-dd_HH-mm.log`,
- last-write timestamp with creation-time fallback,
- no collision suffixes,
- fixed seven-day archive retention,
- rename-failure overwrite/append fallback.

6.C only migrates callers and adds the emergency writer.

---

# 13. Required Migration Audit

Before editing source in the future implementation task, inventory every legacy caller and classify it:

| Caller | Phase/batch | Current level | Exception use | Structured payload | Expensive debug guard | Pre-Serilog risk | Target call |
|---|---|---|---|---|---|---|---|

Specially review:

- constructors,
- static initializers,
- config/settings load,
- logger factory,
- application startup before provider resolution,
- shutdown after provider disposal begins,
- exception handlers,
- background worker callbacks,
- UI events,
- paths/URLs/user-provided values,
- future Nexus placeholders.

Do not mechanically replace method names without verifying lifecycle timing and templates.

---

# 14. Tests and Verification

Require:

## Inventory

- zero normal `Logger.Instance` references,
- zero general-purpose legacy logger construction,
- zero duplicated minimum-level state,
- no `Log.CloseAndFlush()` outside an explicitly rejected historical comment/example.

## Output behavior

- Information mode excludes ordinary Debug events,
- Debug mode includes Debug events after restart,
- structured properties render correctly,
- exception stack/details are preserved,
- UI/Core logs still reach the same shared file,
- async/file/debug sinks follow current Phase 2/6.B configuration.

## Bootstrap

- missing/malformed settings can regenerate before Serilog without circular logging,
- provider-build failure writes best-effort startup-failure log,
- logger-construction failure uses emergency writer,
- emergency writer does not create a file during successful startup,
- emergency writer failure does not prevent fatal shutdown.

## Lifecycle

- exactly one shared logger,
- provider disposal closes it once,
- no logging after disposal,
- normal shutdown leaves `Latest`,
- next startup archives it correctly,
- restart applies persisted DebugMode.

## Regression

- solution build succeeds,
- all tests pass,
- targeted logging smoke tests pass,
- Phase 5 detection and Phase 6.A pipeline behavior remain unchanged,
- no WPF reference enters Core.

---

# 15. Explicit Exclusions

Do not include:

- DI redesign,
- startup/shutdown architecture redesign,
- runtime log-level switching,
- configurable retention,
- archive naming redesign,
- central secret redaction/sanitization,
- settings architecture redesign,
- broad result/workflow redesign,
- mod-pipeline redesign,
- MVVM rewrite,
- Nexus implementation,
- performance optimization unrelated to obvious removed legacy guards.

---

# 16. Exit Criteria

Phase 6.C is complete only when:

- all normal legacy logger callers use final Serilog `Log.*`,
- migrations were completed in verified batches,
- redundant manual debug guards are removed except justified expensive-payload cases,
- the general-purpose custom `Logger` is removed,
- `EmergencyStartupLogWriter` is the only pre-Serilog fallback,
- no circular bootstrap logging dependency exists,
- one provider-owned logger close path remains,
- documentation/comments use current phase names,
- focused and full tests pass,
- manual log-file smoke checks pass.

---

# 17. Documentation Handoff

After future implementation:

- update canonical logging/lifecycle docs to proven final behavior,
- update changelog only under the existing changelog/version policy,
- record completion evidence and remaining limitations,
- do not retain stale compatibility instructions.

Do not mark Phase 6.C complete in this documentation-only task.

## Implementation Stop Conditions

Stop a future migration batch if a caller may run before Serilog activation or after provider disposal, if structured-template or exception mapping cannot preserve behavior, or if the proposed change creates a second pipeline/close path or revives the rejected redaction/sanitization architecture.
