# Logging

## Currently Implemented
- Application composition owns one Serilog instance for the validated service-provider lifetime and assigns that same instance to `Serilog.Log.Logger` for compatibility.
- The logger is created only after `ConfigFileManager` and `LoggingSettings` have loaded the persisted `DebugMode` preference.
- Each process uses one infinite active `CalradiaForge_Latest.log` with asynchronous writes, thread and exception enrichment, and a neutral `SerilogTextFormatter`. The formatter controls presentation only and does not inspect, mask, redact, sanitize, or replace event values.
- Startup archives a prior active log and deletes archived logs older than the fixed seven-day retention policy.
- Provider disposal closes and flushes the Serilog graph once during controlled shutdown or restart. Application code does not separately close the global logger.
- `Serilog.Sinks.Debug` is configured only for Debug builds and its runtime asset remains private to each executable/test boundary that constructs or executes the Core logging graph. Runtime `DebugMode` controls the minimum level and can enable Debug events in the file log in Release builds.
- The legacy `Logger` singleton and its existing callers remain temporarily available during the Phase 6.B-to-6.C transition.

## Architecture Guidance
- Credential-owning components must not intentionally pass credentials, authentication material, or secret-bearing URLs to the logger. The logger does not automatically filter them.
- Structured debug payloads are optional and should remain compact.
- Logging should support diagnostics without becoming a second data store.
- Relevant local Steam, Bannerlord, Workshop, app-data, and selected override paths may appear in local logs. Any future export or telemetry feature requires a separate approved data-handling policy.
- New composition-owned services receive `Serilog.ILogger` explicitly. Legacy call-site migration remains separate work.
- Future Steam/Bannerlord path diagnostics should use structured properties for client paths, library roots, install paths, Workshop candidates, selected paths, skip reasons, and scanner counts.

## Key Files
- `source/CalradiaForge.Core/Infra/Logging/Logger.cs`
- `source/CalradiaForge.Core/Infra/Logging/SerilogLoggerFactory.cs`
- `source/CalradiaForge.Core/Infra/Logging/SerilogTextFormatter.cs`
- `source/CalradiaForge.Core/Infra/Logging/LogFileLifecycle.cs`
- `source/CalradiaForge.Core/Infra/Config/LoggingSettings.cs`
- `source/CalradiaForge.UI/App.xaml.cs`

## Deferred / Future Work
- Migrating `Logger.Instance` call sites and retiring the compatibility logger are deferred to Phase 6.C.
- Additional sinks, telemetry, secret-redaction infrastructure, and Host Builder integration are not part of Phase 6.B.
