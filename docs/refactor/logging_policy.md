# Logging Policy

## Purpose

Logging should provide useful local diagnostics, keep output compact, and avoid hidden business state. It is not an automatic privacy or secret-sanitization layer.

## Current Grounding

- The live application still uses the legacy `Logger.Instance` session logger. Its existing cleanup behavior is separate from the unused Serilog factory path.
- Phase 2 Serilog infrastructure exists under `source/CalradiaForge.Core/Infra/Logging/`, but WPF startup does not currently initialize it and Phase 2 did not migrate callers or establish lifecycle ownership.
- `SerilogLoggerFactory` is an instance class constructed with `AppConfigSettings`; its current public method is `Create()`, not `Build()`. It creates and returns a logger but does not retain it.
- The current Serilog path targets `AppPaths.LogsFilePath`, named `CalradiaForge_Latest.log`, with `RollingInterval.Infinite`, `shared: false`, and asynchronous file output. It does not create daily rolled files.
- `Create()` currently invokes custom cleanup on every factory call. `LogRetentionPolicy.Cleanup(...)` interprets the setting as an age in days, while `AppConfigSettings.RetainedFileCount` is named and documented as a file count; this mismatch is unresolved.
- `retainedFileCountLimit: default` passes null to the nullable Serilog option, so it does not impose the normal file-count limit. `fileSizeLimitBytes` and `rollOnFileSizeLimit` are not configured, so the active-file size policy remains an owner decision.
- As of the 2026-07-12 audit of commit `1381dda6c30d159e0101699b729c10b4ece30f47`, the neutral `SerilogTextFormatter` is the custom `ITextFormatter`. It renders event values without inspecting, masking, redacting, sanitizing, or replacing them. The legacy `Logger.Instance` path remains live and separate from the future Serilog factory path.

## Policy

- The logging pipeline performs no automatic secret filtering, path masking, sanitization, or value replacement.
- Info logs should record lifecycle summaries and important operation outcomes.
- Warning logs should record recoverable problems.
- Error logs should record failures with enough context to diagnose.
- Debug logs may include structured context and full relevant local filesystem paths when they are useful for diagnosis.
- Logging callers and credential-owning components must not intentionally pass credentials, auth headers, bearer tokens, API keys, credential objects, or secret-bearing URLs to the logger.
- Logging should not replace persisted domain data in `AppConfig`, `ModsData`, or `ModpackData`.
- Future Nexus logging code must follow the same caller-discipline rule. Nexus credentials remain in purpose-built credential components and are decrypted only for explicit operations.

## Steam And Bannerlord Path Diagnostics

Phase 5 added structured Steam/Bannerlord path-resolution diagnostics and automated split-library, Workshop-selection, scanner, and Novus regressions. These automated checks verify the implemented path-resolution behavior; owner verification against a real Windows Steam split-library installation and WPF session remains pending before release acceptance.

Useful structured events include:

- Detected platform and path-resolution strategy.
- Detected Steam client path, when available.
- Discovered Steam library roots.
- Detected Bannerlord install path.
- Workshop path candidates composed under `steamapps/workshop/content/261550`.
  - Log game-installation and Steam Workshop folder paths when they are needed to diagnose the scanner; no automatic path masking is applied.
    > Bug Found: Auto Steam workshop mod scanning failure with game installation in separate drive path installed separately from Steam install drive.Needs logging for diagnostics.
- Selected Workshop path, if any.
- Reasons candidate Workshop paths were skipped, missing, empty, invalid, or failed during scan.
- Scanner result counts for local modules and Workshop modules.

User-facing messages should stay concise. Technical details should go to logs as appropriate diagnostic data. Users control whether they share local log files.

## Path Diagnostics And Trust Boundaries

The logger does not redact or sanitize filesystem paths. Relevant Steam, Bannerlord, Workshop, app-data, and selected override paths may appear in local logs. Logging callers remain responsible for not intentionally supplying credentials or authentication material. A future log-export, telemetry, or shared-diagnostic feature requires its own separately approved data-handling policy.

Path logging should stay inside the app's trust boundary:

- Do not log directory listings unrelated to Bannerlord, Steam library discovery, app data, selected override paths, or the active scan.
- Do not intentionally log credential-bearing URLs or credential values.
- Do not treat relevant diagnostic paths as values that the logger must hide.

## Serilog Target

| Target behavior | Direction |
|---|---|
| Implementation | Serilog infrastructure in `CalradiaForge.Core`. |
| Source location | Add all new Serilog infrastructure files under the existing Core logging folder, `source/CalradiaForge.Core/Infra/Logging/`. |
| File sink | Use `Serilog.Sinks.File` for app log files. |
| Async sink | Use `Serilog.Sinks.Async` where the logging pipeline benefits from buffered writes. |
| Debug sink | Use `Serilog.Sinks.Debug` only in Debug builds via a conditional `PackageReference` with `PrivateAssets="all"` and `#if DEBUG` sink configuration; exclude it from Release/Public Release artifacts. |
| Console sink | Do not add `Serilog.Sinks.Console`; CalradiaForge is a WPF app and CLI execution must not be treated as interactive runtime verification. |
| File behavior | Current: one infinite-rolling active `CalradiaForge_Latest.log`, custom cleanup, and no time-based Serilog rolling. Planned Phase 6.B: archive the previous `Latest` only at next startup, use no collision suffix, apply fixed seven-day archive retention, and leave `Latest` available after shutdown. |
| Structure | Message templates and structured properties. |
| Context | `SourceContext` or class context where practical. |
| Thread enrichment | Include thread enrichment where it helps session diagnostics. |
| Exception enrichment | Use structured exception enrichment for richer failure context. |
| Minimum level | Current behavior uses `AppConfigSettings.DebugMode`. Planned Phase 6.B loads `LoggingSettings.DebugMode` before logger construction; the selected Debug or Information level remains fixed for the process lifetime. |
| Debug level | Controlled by Serilog configuration/debug setting rather than repeated manual guards. |
| Expensive diagnostics | Guard with level checks only when constructing the diagnostic payload is costly. |
| Formatting | `SerilogTextFormatter` is a neutral presentation template connected to the file and Debug sinks. It renders timestamp, level, message, applicable source context, thread information, properties, and exception information without rewriting event values. |

## Retention And File-Size Policy

Current source still has a configuration/cleanup naming mismatch. Phase 6.B resolves the future policy: remove the configurable retention setting and delete archived CalradiaForge logs older than seven days during startup, after previous-`Latest` archival handling and before opening the new active logger. Never include `CalradiaForge_Latest.log`; ignore missing files and individual deletion failures.

The active sink still has no locked explicit file-size strategy. That file-size/rolling choice remains genuinely unresolved and must not be confused with the resolved archive-retention policy.

## Phase 6.B Logger Construction And Ownership

The bootstrap dependency is:

```text
ConfigFileManager loads LoggingSettings
→ LoggingSettings.DebugMode is available
→ startup log-file lifecycle runs
→ one Serilog logger is constructed
→ the same logger is assigned to Serilog.Log.Logger
→ ApplicationStartupCoordinator may execute Log.* calls
```

`ApplicationStartupCoordinator` depends explicitly on `Serilog.ILogger`, forcing activation before `StartAsync()`. `App` does not separately resolve or sequence the logger, and bootstrap settings code cannot depend on the final logger during construction.

Exactly one DI singleton creates the shared logger and assigns that same instance globally. Global `Log.*` is the normal post-bootstrap API and the explicit exception to the no-global-services rule. `App` owns the root provider; DI owns the factory-created logger; provider disposal is the sole normal flush/dispose path. Prohibit a second pipeline, consumer `Dispose()`, a separate `Log.CloseAndFlush()` path, and logging after provider disposal begins.

## Debug-Mode Process Behavior

At startup, `LoggingSettings.DebugMode` selects Debug or Information and the logger keeps that level for the process lifetime. A changed value is persisted immediately and prompts for restart in both directions. Accepting uses the app-owned restart pipeline; declining retains the persisted value while leaving the current logger unchanged until next start. Do not add runtime switching, rollback on decline, a pending flag, `LoggingSessionState`, or `DebugModeAtStartup`.

## Startup-Only Archival And Shutdown Behavior

On shutdown, quiesce the app, dispose the provider/logger, and leave `CalradiaForge_Latest.log` in place. Do not rename, move, archive, truncate, or delete it.

At the next startup before opening the new logger:

1. If `CalradiaForge_Latest.log` is missing, continue.
2. Read its last-write timestamp, using creation time only when last-write cannot be obtained.
3. Rename it to `CalradiaForge_yyyy-MM-dd_HH-mm.log`; precision stops at minutes.
4. Do not add a numeric suffix and do not overwrite an existing archive.
5. Treat collision or rename failure as nonfatal, leave the existing archive untouched, and emit only best-effort pre-Serilog diagnostics.
6. Attempt to truncate/overwrite `Latest` for the new session; if that fails, allow the sink to attempt append-compatible opening.
7. If the final logger cannot initialize, use the bootstrap-failure path.

Do not invent recovery archive names.

## Formatter Decision

The 2026-07-12 owner decision removes automatic secret redaction, path sanitization, secret-pattern filtering, and generic key-name blocking from the current implementation and future refactor requirements. Preserve the custom formatter concept as a readable presentation template. The named `source/CalradiaForge.Core/Infra/Logging/SERILOG_WORKFLOW_GUIDE.md` and `docs/Serilog_Logger_Follow_Up_Summary_2026-07-12.md` were not found in the current package; current source inspection is authoritative until those documents are created or restored.

## Performance Verification

Later audit work should measure logging behavior without weakening required diagnostics. Applicable comparisons include disabled-level message construction, interpolated strings versus message-template construction, structured payload construction, enrichment and source-context cost, neutral formatter rendering, async sink buffering and file-write behavior, and retention cleanup.

Do not infer that `Serilog.Sinks.Async` is always faster. Measure relevant user-visible or component behavior under comparable Release conditions, and keep expensive diagnostic guards only where payload construction is genuinely costly.

## Approved Package Set

The Serilog refactor must use only the packages already approved for `CalradiaForge.Core` unless the owner explicitly approves a later package change.

```xml
<ItemGroup>
	<PackageReference Include="Serilog" Version="4.3.1" />
	<PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
	<PackageReference Include="Serilog.Sinks.Async" Version="2.1.0" />
	<PackageReference Include="Serilog.Exceptions" Version="8.4.0" />
	<PackageReference Include="Serilog.Enrichers.Thread" Version="4.0.0" />
</ItemGroup>

<ItemGroup Condition="'$(Configuration)' == 'Debug'">
	<PackageReference Include="Serilog.Sinks.Debug" Version="3.0.0" PrivateAssets="all" />
</ItemGroup>
```

Do not add `Serilog.Settings.Configuration`, `Microsoft.Extensions.Configuration.Json`, `Serilog.Extensions.Logging`, `Serilog.Extensions.Hosting`, or `Serilog.Sinks.Console` as part of Phase 2. The app keeps the existing custom JSON configuration manager for now and does not adopt `Microsoft.Extensions.Logging.ILogger<T>` or Host Builder in this phase.

## Migration Direction

Phase 2 creates the Serilog infrastructure only. It must not convert app-wide legacy logger call sites and must not remove the existing `Logger` compatibility path.

- Keep the current `Logger` class file in place.
- Add only a short legacy/deprecated compatibility summary to the current `Logger` file.
- Keep every existing `Logger.Instance` call site in Core and UI intact during Phase 2.
- Do not initialize the new Serilog service through the WPF app service startup path until the dependency-injection refactor establishes the service composition path.
- Phase 5 Core platform branches log technical detection details; the workflow logs validation and fallback details; the UI maps outcomes to toasts. Paths are not subject to newly invented redaction or sanitization rules.
- Phase 6.B establishes the final provider-owned lifecycle but keeps normal legacy callers temporarily.
- Phase 6.C inventories and migrates legacy call sites in narrow batches, removes ordinary manual debug guards except for expensive/side-effecting payloads, and uses structured templates/exception overloads.
- Retire the general-purpose legacy logger only after zero normal callers remain and verification proves equivalent behavior. Do not retain a compatibility facade.
- Phase 6.C adds the error-only, pre-Serilog `EmergencyStartupLogWriter`; it is not a normal injected logger and creates no file during successful startup.

## Phase 6.C Emergency Startup Fallback

`EmergencyStartupLogWriter` exists only for provider-build failure before Serilog, settings/logging bootstrap failure, final logger/file-sink initialization failure, useful pre-Serilog archive/truncate diagnostics, and comparable unrecoverable startup failures.

Its primary path is `<AppPaths.LogsDirectory>/CalradiaForge_StartupFailure.log`. It lazily performs synchronous best-effort append only on error paths, owns no persistent stream, background task, async sink, structured framework, or normal Info/Debug/Warning API, and is not exposed through ordinary service injection. After Serilog activation, callers use `Log.*` and never use the emergency writer.

If the primary path cannot be written, use only a narrow safe fallback path chain. Swallow secondary failures after debugger/best-effort output; emergency-log failure must never block fatal shutdown or add another disposal/close path.

## Phased Implementation

| Phase | Scope |
|---|---|
| 2 | Create the Serilog infrastructure files, neutral formatting, rolling file configuration, retention/archive helpers, source context support, thread enrichment, exception enrichment, and Debug-build-only debug sink configuration under `source/CalradiaForge.Core/Infra/Logging/`. Keep the legacy `Logger` file and all current call sites intact. |
| 6.B | Establish one validated provider, `LoggingSettings` bootstrap, one DI-created/global logger, provider-only disposal, startup-only archival, fixed retention, shutdown/restart, and exception ownership. Keep normal legacy callers temporarily. |
| 6.C | Migrate `Logger.Instance` callers in controlled batches, remove redundant guards, retire the general legacy logger after zero callers, and add `EmergencyStartupLogWriter`. |
| Later verification | Add or expand tests for formatter output, minimum-level behavior, Release artifact exclusion of Debug-only sinks, and legacy compatibility retirement when migration is complete. |

## Verification Expectations

- Logs are created in the expected logs directory.
- Minimum level suppresses lower-priority messages.
- Session logs retain expected lifecycle behavior or have an intentional Serilog replacement.
- Formatter output includes the applicable timestamp, level, message, source context, thread information, properties, and exception details without value rewriting.
- Startup removes archives older than seven days while excluding `Latest`; shutdown leaves `Latest` available.
- Logging callers do not intentionally pass credentials or authentication material to the logger.
- Relevant local Steam/Bannerlord path-resolution diagnostics remain available without automatic path masking.
- Any future performance, profiler, export, or telemetry output policy is defined separately when that feature is designed.
- One factory invocation produces one shared logger instance; repeated factory creation and duplicate providers are detected.
- Previous `Latest` archives only at startup using last-write time with creation fallback, minute precision, no suffix, and no overwrite; rename failure follows the locked truncate/overwrite then append-compatible attempt.
- Shutdown waits for logging-producing work, provider disposal closes exactly once, no logging occurs afterward, and no shutdown archive occurs.
- Missing/empty/malformed settings bootstrap without circular logging; provider/logger construction failure uses the best-effort emergency writer; successful startup creates no emergency file.
- Phase 6.C verification proves zero normal legacy callers/construction/duplicated level state, structured output and exceptions, Debug behavior after restart, one UI/Core sink, and one provider-owned close path.

## Guardrails

- Do not intentionally pass credentials, raw auth headers, raw tokens, or secret-bearing URLs to the logger.
- Do not introduce remote telemetry as part of this refactor.
- Do not rewrite every logging call site in one uncontrolled pass.
- Do not move call-site migration into Phase 2.
- Do not make Core depend on WPF logging APIs.
- Keep future Nexus credential mechanics outside ordinary logging and AppConfig.
- Do not remove required logging or diagnostics solely to improve benchmark results.
- Do not present disabled-level, sink, or enrichment savings as measured without comparable evidence.

## Future Documentation Cross-References

Accepted logging and credential-boundary decisions should later be migrated into future logging, security, and trust-boundary documentation. Scanner/path diagnostics should cross-reference future platform/path-detection documentation so path logging and user-facing scan warnings stay aligned.

## Out Of Scope

- Remote log collection.
- Analytics.
- Full Serilog migration in the same change as Nexus auth.
- Persisting user workflow state in logs.

## Open Questions

- Should the active file preserve the Serilog default approximate size limit, use explicit size rolling, or use another policy?

Full implementation contracts: [Phase 6.B](phase_6b_di_and_lifecycle_locked_decisions_2026-07-21.md) and [Phase 6.C](phase_6c_legacy_logger_migration_locked_decisions_2026-07-21.md).
