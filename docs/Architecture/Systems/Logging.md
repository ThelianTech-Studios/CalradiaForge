# Logging

## Currently Implemented
- `Logger` is a singleton session logger.
- Log output is written to the `Logs` directory and mirrored to debug output when enabled.
- Log retention removes older session logs on startup.
- Debug verbosity is controlled by the app's debug mode setting.

## Architecture Guidance
- Logging must not expose credentials or secret URLs.
- Structured debug payloads are optional and should remain compact.
- Logging should support diagnostics without becoming a second data store.

## Key Files
- `source/CalradiaForge.Core/Infra/Logging/Logger.cs`

## Deferred / Future Work
- Additional sinks or telemetry are not currently part of the architecture.
