# Security And Secret Boundary

## Purpose

CalradiaForge must keep ordinary settings, diagnostics, and future Nexus credentials separated by explicit boundaries.

## Current Grounding

- Current `AppConfig` and `AppConfigSettings` are non-secret settings managers. Phase 6.B plans the names `ConfigFileManager`, `AppSettings`, and `LoggingSettings`; none is a credential store.
- Existing settings include language, game paths, platform, debug mode, last selected modpack, BLSE path, default launch target, and EULA acceptance.
- The live legacy `Logger` still logs plain messages and structured debug payloads. The future Serilog infrastructure now contains the neutral `SerilogTextFormatter`, which controls presentation only and does not make caller-supplied values safe.
- Nexus integration is optional and future credentials must not be stored in config.
- Nexus architecture requires DPAPI CurrentUser credential storage and explicit operation-scoped decryption.

## Policy

- Current `AppConfig` and planned `ConfigFileManager` must never become credential managers.
- Current `AppConfigSettings` and planned `AppSettings`/`LoggingSettings` may expose only non-secret app settings and non-secret feature flags.
- Secret-bearing data must live in purpose-built auth/credential components.
- Future Nexus credentials must be stored with DPAPI CurrentUser.
- Credentials may be decrypted only for an explicit Nexus operation scope.
- Credential-owning components must not intentionally pass auth headers, bearer tokens, API keys, credential objects, credentials, or secret-bearing URLs to the logger.
- The logger performs no automatic secret or path filtering. Debug and normal logs use the same caller-discipline rule.
- Phase 6.C global `Log.*` calls and the error-only `EmergencyStartupLogWriter` use the same caller-discipline rule. The emergency writer is not a normal injected logger and must not become a filtering or sanitization layer.
- Logs must support diagnostics without becoming a data store for sensitive runtime state.
- Feature flags for experimental Nexus behavior may be stored in normal settings only when they contain no secrets.
- Any future profiler, export, telemetry, or shared-diagnostic feature requires its own approved data-handling policy. This policy does not require automatic filtering of its output.
- Do not use real credentials in tests or diagnostics. Synthetic values may be used to verify caller behavior, but no test is required to prove automatic redaction.

## Nexus Credential Boundary

| Responsibility | Owning boundary |
|---|---|
| Auth flow and credential mechanics | `CalradiaForge.Nexus` |
| DPAPI CurrentUser storage | Nexus credential/auth manager |
| Decryption lifetime | Explicit Nexus operation scope only |
| User-visible auth state | UI presentation state |
| Non-secret feature flags | Current `AppConfigSettings`; planned `AppSettings` when needed |
| Runtime install handoff | Nexus downloads hand off to Core installer pipeline |

## Phased Implementation

| Phase | Scope |
|---|---|
| Step 1 | Document `AppConfig` as ordinary settings storage, not a credential manager. |
| Step 2 | Keep the logger neutral and preserve caller responsibility for diagnostic data. |
| Step 3 | Implement the future Nexus credential/auth boundary in `CalradiaForge.Nexus` using DPAPI CurrentUser. |

## Verification Expectations

- `AppConfig` persists ordinary settings without generic key-name heuristics; source inspection confirms no blocklist is implemented.
- Formatter output preserves event values and includes applicable message, properties, and exception data.
- Credential-owning components do not intentionally pass credentials to logs.
- Nexus auth tests must prove credentials are not written to `config.json`, Nexus metadata, or log files.
- NXM links fail when authentication is missing, per Nexus architecture.
- Future benchmark, profiler, export, or telemetry artifacts follow their own approved data-handling policy when such features are designed.

## Guardrails

- Do not store Nexus credentials in `AppConfig`.
- Do not intentionally pass credential-bearing imported URLs to the logger.
- Do not move Nexus auth or networking into Core.
- Do not add automatic startup auth, polling, or update checks.
- Do not expose decrypted credentials outside explicit operation scope.
- Do not store Nexus metadata in `ModuleModel`.
- Do not introduce a replacement generic secret scanner or key-name blocklist.
- Do not introduce `LogRedactor`, `RedactingTextFormatter`, central sanitization, secret-pattern filtering, or a redactor-removal approval gate.

## Out Of Scope

- Password manager integration.
- Cross-user credential sharing.
- Redesigning Nexus API strategy.
- Telemetry or remote log upload.
