# Logging Policy

## Purpose

Logging should provide useful local diagnostics while protecting secrets, keeping output compact, and avoiding hidden business state.

## Current Grounding

- `Logger` is a singleton session logger.
- Logs are written to `AppPaths.LogsDirectory`.
- A new session log file is created per app session.
- Old logs are cleaned after 14 days.
- `MinimumLevel` controls verbosity and is tied to `AppConfigSettings.DebugMode`.
- Structured debug payloads are serialized with Newtonsoft.Json.
- Current logging has no central redaction layer.

## Policy

- All log output must pass through central redaction before file or debug output.
- Info logs should record lifecycle summaries and important operation outcomes.
- Warning logs should record recoverable problems.
- Error logs should record failures with enough context to diagnose.
- Debug logs may include structured context but must remain compact and redacted.
- Logging must not store credentials, auth headers, bearer tokens, API keys, credential objects, or secret-bearing URLs.
- Logging should not replace persisted domain data in `AppConfig`, `ModsData`, or `ModpackData`.
- Future Nexus logs must pass through the same redaction layer as Core/UI logs.

## Steam And Bannerlord Path Diagnostics

Structured diagnostics should be added when Steam/Bannerlord path-resolution work begins. The known Workshop scanning issue should be logged as a path-resolution investigation area without resolved-status language.

Useful structured events include:

- Detected platform and path-resolution strategy.
- Detected Steam client path, when available.
- Discovered Steam library roots.
- Detected Bannerlord install path.
- Workshop path candidates composed under `steamapps/workshop/content/261550`.
- Selected Workshop path, if any.
- Reasons candidate Workshop paths were skipped, missing, empty, invalid, or failed during scan.
- Scanner result counts for local modules and Workshop modules.

User-facing messages should stay concise. Technical details should go to logs after redaction.

## Path Redaction And Trust Boundaries

Logs must not expose credentials, secret URLs, or unrelated personal filesystem data. Path diagnostics should record enough to diagnose Steam library and Workshop detection problems, but shared diagnostic bundles or exported logs should redact or sanitize user-specific path segments when policy requires it.

Path logging should stay inside the app's trust boundary:

- Do not log directory listings unrelated to Bannerlord, Steam library discovery, app data, selected override paths, or the active scan.
- Do not log raw secret-bearing URLs.
- Do not treat paths as secrets by default, but handle them as user-specific diagnostic data.
- Apply the same redaction rules to debug payloads and normal logs.

## Serilog Target

| Target behavior | Direction |
|---|---|
| Implementation | Serilog. |
| Sink | File sink for app log files. |
| File behavior | Rolling logs with retention limits. |
| Structure | Message templates and structured properties. |
| Context | `SourceContext` or class context where practical. |
| Debug level | Controlled by Serilog configuration/debug setting rather than repeated manual guards. |
| Expensive diagnostics | Guard with level checks only when constructing the diagnostic payload is costly. |
| Redaction | Apply before writing sensitive or user-provided runtime values. |

## Phased Implementation

| Phase | Scope |
|---|---|
| 1 | Add central redaction to the current `Logger`. |
| 2 | Reduce raw path/value logging where it is noisy or sensitive. |
| 3 | Plan Serilog migration with file sink, rolling files, retention, message templates, and `SourceContext`. |
| 4 | Migrate call sites in controlled batches. |
| 5 | Remove repeated manual debug guards except around expensive diagnostic construction. |
| 6 | Add tests for redaction and minimum-level behavior. |

## Verification Expectations

- Logs are created in the expected logs directory.
- Minimum level suppresses lower-priority messages.
- Session logs retain expected lifecycle behavior or have an intentional Serilog replacement.
- Redaction applies to plain messages, exception details, and structured payloads.
- Log cleanup preserves recent files and removes old session logs according to retention rules.
- Sample Nexus-like secrets are redacted even in debug logs.
- Steam/Bannerlord path-resolution diagnostics redact user-specific path details in shared/exported diagnostics where required.

## Guardrails

- Do not log secrets, raw auth headers, raw tokens, or secret-bearing URLs.
- Do not introduce remote telemetry as part of this refactor.
- Do not rewrite every logging call site in one uncontrolled pass.
- Do not make Core depend on WPF logging APIs.
- Do not allow future Nexus auth logs to bypass redaction.

## Future Documentation Cross-References

Accepted logging and redaction decisions should later be migrated into future logging, security, and trust-boundary documentation. Scanner/path diagnostics should cross-reference future platform/path-detection documentation so path logging, redaction, and user-facing scan warnings stay aligned.

## Out Of Scope

- Remote log collection.
- Analytics.
- Full Serilog migration in the same change as Nexus auth.
- Persisting user workflow state in logs.

## Open Questions

- Should Serilog migration keep a temporary adapter matching the current `Logger` API?
- What retention policy should replace or preserve the current 14-day cleanup?
- Should diagnostic bundles redact or omit user-specific filesystem paths?
- Which path components should remain visible when diagnosing multi-library Steam Workshop detection?
