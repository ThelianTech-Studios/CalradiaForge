# Security And Secret Boundary

## Purpose

CalradiaForge must keep ordinary settings, diagnostics, and future Nexus credentials separated by explicit boundaries.

## Current Grounding

- `AppConfig` and `AppConfigSettings` are non-secret settings managers.
- Existing settings include language, game paths, platform, debug mode, last selected modpack, BLSE path, default launch target, and EULA acceptance.
- The live legacy `Logger` still logs plain messages and structured debug payloads without the Phase 2 Serilog formatter. The completed but not yet wired Serilog infrastructure includes `RedactingTextFormatter` and `LogRedactor`; their presence does not make all current callers safe.
- Nexus integration is optional and future credentials must not be stored in config.
- Nexus architecture requires DPAPI CurrentUser credential storage and explicit operation-scoped decryption.

## Policy

- `AppConfig` must never become a credential manager.
- `AppConfigSettings` may expose only non-secret app settings and non-secret feature flags.
- Secret-bearing data must live in purpose-built auth/credential components.
- Future Nexus credentials must be stored with DPAPI CurrentUser.
- Credentials may be decrypted only for an explicit Nexus operation scope.
- Auth headers, bearer tokens, API keys, credential objects, and secret-bearing URLs must never be logged raw.
- Debug logs must follow the same redaction rules as normal logs.
- Logs must support diagnostics without becoming a data store for sensitive runtime state.
- Feature flags for experimental Nexus behavior may be stored in normal settings only when they contain no secrets.
- Performance benchmarks, profiler output, environment metadata, and exported result files must follow the same secret boundary as application logs.
- Use synthetic Nexus-like values for redaction and logging-performance tests; never use real credentials or secret-bearing URLs.
- Phase 5.B must review migrated templates, structured/destructured properties, exception data, URLs/query strings, and future Nexus request/response details for secret safety.
- Removing or narrowing the central formatter/redactor is a separate owner decision. It requires caller-discipline evidence, synthetic-secret tests, exception-rendering review, residual-risk documentation, and explicit approval; performance evidence alone is insufficient.

## Blocked Config Content

`AppConfig` should reject or block secret-like keys, including names containing:

- `password`
- `secret`
- `token`
- `apikey`
- `api_key`
- `authorization`
- `bearer`
- `credential`
- `refresh_token`
- `access_token`

## Nexus Credential Boundary

| Responsibility | Owning boundary |
|---|---|
| Auth flow and credential mechanics | `CalradiaForge.Nexus` |
| DPAPI CurrentUser storage | Nexus credential/auth manager |
| Decryption lifetime | Explicit Nexus operation scope only |
| User-visible auth state | UI presentation state |
| Non-secret feature flags | `AppConfigSettings` when needed |
| Runtime install handoff | Nexus downloads hand off to Core installer pipeline |

## Phased Implementation

| Phase | Scope |
|---|---|
| 1 | Document and enforce `AppConfig` as non-secret storage. |
| 2 | Add central redaction before any Nexus auth implementation. |
| 3 | Add secret-like key blocking to `AppConfig`. |
| 4 | Add tests for blocked config keys and redacted log output. |
| 5 | Implement future Nexus credential/auth boundary in `CalradiaForge.Nexus` using DPAPI CurrentUser. |

## Verification Expectations

- Attempts to persist secret-like config keys fail or are rejected.
- Logger redacts common token, key, auth header, and secret URL patterns.
- Debug structured payloads pass through the same redaction path.
- If central redaction is ever removed, synthetic tests prove caller-safe templates/properties and exception rendering across Debug and Release paths before approval.
- Nexus auth tests must prove credentials are not written to `config.json`, Nexus metadata, or log files.
- NXM links fail when authentication is missing, per Nexus architecture.
- Benchmark and profiler artifacts do not expose credentials, secret URLs, auth headers, or sensitive imported values.

## Guardrails

- Do not store Nexus credentials in `AppConfig`.
- Do not log raw imported URLs if they may contain secret query parameters.
- Do not move Nexus auth or networking into Core.
- Do not add automatic startup auth, polling, or update checks.
- Do not expose decrypted credentials outside explicit operation scope.
- Do not store Nexus metadata in `ModuleModel`.
- Do not bypass redaction in production or benchmark code to improve logging performance.
- Do not describe the planned caller-discipline option as implemented, and do not weaken redaction merely because rendered-property tests pass.

## Out Of Scope

- Password manager integration.
- Cross-user credential sharing.
- Redesigning Nexus API strategy.
- Telemetry or remote log upload.

## Open Questions

- Should secret-like config writes throw exceptions or return structured failures?
- What redaction marker should be standard: `<redacted>`, `[REDACTED]`, or typed placeholders?
- Should paths be partially redacted in shared diagnostic bundles?
