# Logging

## Currently Implemented
- `Logger` is a singleton session logger.
- Log output is written to the `Logs` directory and mirrored to debug output when enabled.
- Log retention removes older session logs on startup.
- Debug verbosity is controlled by the app's debug mode setting.
- The Core logging folder now contains an isolated Serilog foundation for later composition: one infinite active `CalradiaForge_Latest.log`, asynchronous writes, thread and exception enrichment, custom cleanup, and redaction before sink output.
- `Serilog.Sinks.Debug` is configured only for Debug builds. Runtime DebugMode remains a minimum-level setting and can enable Debug events in rolling file logs in Release builds.

## Architecture Guidance
- Logging must not expose credentials or secret URLs.
- Structured debug payloads are optional and should remain compact.
- Logging should support diagnostics without becoming a second data store.
- `LogRedactor` is the shared redaction boundary for plain messages, exceptions, and recursively rendered structured properties, including debug output.
- `SerilogLoggerFactory` is infrastructure only in Phase 2; it is not initialized from WPF startup and does not replace legacy callers.
- Future Steam/Bannerlord path diagnostics should use structured properties for client paths, library roots, install paths, Workshop candidates, selected paths, skip reasons, and scanner counts. Shared/exported diagnostics must sanitize user-specific path segments as required.

## Key Files
- `source/CalradiaForge.Core/Infra/Logging/Logger.cs`
- `source/CalradiaForge.Core/Infra/Logging/SerilogLoggerFactory.cs`
- `source/CalradiaForge.Core/Infra/Logging/LogRedactor.cs`
- `source/CalradiaForge.Core/Infra/Logging/RedactingTextFormatter.cs`
- `source/CalradiaForge.Core/Infra/Logging/LogRetentionPolicy.cs`

## Deferred / Future Work
- Migrating `Logger.Instance` call sites and retiring the compatibility logger are deferred to Phase 5.B, after DI composition and equivalent-behavior verification.
- Serilog initialization, one-time startup cleanup, logger ownership, close/flush, and active-log archiving through application composition are deferred to Phase 5.A; Phase 2 does not add WPF singleton startup wiring.
- Additional sinks, telemetry, and Host Builder integration are not part of Phase 2.
