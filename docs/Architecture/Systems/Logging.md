# Logging

## Currently Implemented
- `Logger` is a singleton session logger.
- Log output is written to the `Logs` directory and mirrored to debug output when enabled.
- Log retention removes older session logs on startup.
- Debug verbosity is controlled by the app's debug mode setting.
- The Core logging folder now contains an isolated Serilog foundation for later composition: one infinite active `CalradiaForge_Latest.log`, asynchronous writes, thread and exception enrichment, custom cleanup, and a neutral `SerilogTextFormatter` connected to the file and Debug sinks. The formatter controls presentation only and does not inspect, mask, redact, sanitize, or replace event values.
- `Serilog.Sinks.Debug` is configured only for Debug builds. Runtime DebugMode remains a minimum-level setting and can enable Debug events in rolling file logs in Release builds.

## Architecture Guidance
- Credential-owning components must not intentionally pass credentials, authentication material, or secret-bearing URLs to the logger. The logger does not automatically filter them.
- Structured debug payloads are optional and should remain compact.
- Logging should support diagnostics without becoming a second data store.
- Relevant local Steam, Bannerlord, Workshop, app-data, and selected override paths may appear in local logs. Any future export or telemetry feature requires a separate approved data-handling policy.
- `SerilogLoggerFactory` is infrastructure only in Phase 2; it is not initialized from WPF startup and does not replace legacy callers.
- Future Steam/Bannerlord path diagnostics should use structured properties for client paths, library roots, install paths, Workshop candidates, selected paths, skip reasons, and scanner counts.

## Key Files
- `source/CalradiaForge.Core/Infra/Logging/Logger.cs`
- `source/CalradiaForge.Core/Infra/Logging/SerilogLoggerFactory.cs`
- `source/CalradiaForge.Core/Infra/Logging/SerilogTextFormatter.cs`
- `source/CalradiaForge.Core/Infra/Logging/LogRetentionPolicy.cs`

## Deferred / Future Work
- Migrating `Logger.Instance` call sites and retiring the compatibility logger are deferred to Phase 5.B, after DI composition and equivalent-behavior verification.
- Serilog initialization, one-time startup cleanup, logger ownership, close/flush, and active-log archiving through application composition are deferred to Phase 5.A; Phase 2 does not add WPF singleton startup wiring.
- Additional sinks, telemetry, and Host Builder integration are not part of Phase 2.
