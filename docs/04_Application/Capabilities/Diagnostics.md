# Diagnostics Capability

Diagnostics are delivered through structured Serilog logs, startup notifications,
user-facing status/error messages, and typed operation outcomes. Technical
diagnostics retain useful failure context, while user messages remain concise.

The log path and archive lifecycle are defined by [Core Logging](../../02_Systems/CalradiaForge_Core/Logging/README.md).
The evidence boundary is important: static inspection, automated tests, and
benchmark output do not prove live WPF, Steam, Bannerlord, packaged-runtime, or
release-candidate behavior.
