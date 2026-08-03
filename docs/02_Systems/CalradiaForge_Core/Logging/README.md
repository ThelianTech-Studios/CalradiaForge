# Core Logging

`SerilogLoggerFactory` creates one provider-owned Serilog pipeline. It uses the
configured debug/information minimum level, thread and exception enrichment,
the neutral text formatter, and an asynchronous file sink for
`CalradiaForge_Latest.log`. The DEBUG build also has the debug sink package.

`LogFileLifecycle` archives the previous active log at startup and applies the
fixed seven-day archive-retention rule. Provider disposal is the normal close
path. `EmergencyStartupLogWriter` is the pre-provider fallback for fatal
startup/global failures.

Logging does not automatically sanitize arbitrary caller values. Callers must
not intentionally log credentials or authentication material. See the central
[security model](../../../07_Security/Security_Model.md) and
[secrets policy](../../../Decisions/Policies/Security_and_Secrets_Policy.md).
