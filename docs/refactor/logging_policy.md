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

Structured diagnostics should be added when Steam/Bannerlord path-resolution work begins. The known Workshop scanning issue should be logged as a path-resolution investigation area without resolved-status language.

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
| File behavior | Current: one infinite-rolling active `CalradiaForge_Latest.log`, custom cleanup, and no time-based Serilog rolling. Planned Phase 5.A: explicit startup cleanup, close-before-archive, and collision-safe archive. |
| Structure | Message templates and structured properties. |
| Context | `SourceContext` or class context where practical. |
| Thread enrichment | Include thread enrichment where it helps session diagnostics. |
| Exception enrichment | Use structured exception enrichment for richer failure context. |
| Minimum level | Preserve current minimum-level behavior tied to `AppConfigSettings.DebugMode`; Release/Public Release builds may still write Debug-level events to file logs when runtime DebugMode is enabled. |
| Debug level | Controlled by Serilog configuration/debug setting rather than repeated manual guards. |
| Expensive diagnostics | Guard with level checks only when constructing the diagnostic payload is costly. |
| Formatting | `SerilogTextFormatter` is a neutral presentation template connected to the file and Debug sinks. It renders timestamp, level, message, applicable source context, thread information, properties, and exception information without rewriting event values. |

## Retention And File-Size Policy

The current cleanup helper uses the configured integer as an age in days and falls back to seven days for non-positive values. The setting is named `RetainedFileCount` and defaults to 14, so the current code does not establish whether the intended policy is "14 files" or "14 days." Phase 5.A must resolve the meaning, document active-file exclusion, define cleanup timing, and verify cleanup occurs once at startup before the active sink opens. Cleanup must not be inferred from Serilog's nullable `retainedFileCountLimit`, which is currently null.

The active sink has no explicit file-size setting. Before implementation, the owner must choose whether to preserve the library default approximate size limit, set an explicit limit and roll policy, or use another deliberate strategy. Tests must cover the selected behavior; docs must not call the current active file unlimited merely because time-based rolling is infinite.

## Logger Lifecycle

Phase 5.A owns the planned application lifecycle. Startup performs retention cleanup once, before opening `CalradiaForge_Latest.log`, then constructs the one shared logger through the one factory singleton. Runtime callers use that shared instance after DI composition is active. Shutdown must stop new logging-producing work, request cancellation, await or confirm active workflows and asynchronous log production are quiescent, close the logger exactly once, confirm the non-shared file handle is released, attempt a collision-safe archive, and preserve the active file if archiving fails. Provider disposal must not create or close a second logger.

The final implementation must choose factory-owned or DI-owned logger disposal and must separately decide whether the instance is assigned to `Serilog.Log.Logger`. `Log.CloseAndFlush()` is not a substitute for direct ownership unless the global assignment is intentional and there is exactly one global close path.

Archive behavior must never overwrite an existing file. It must distinguish no active file, collision without a safe alternate name, access/move failure, and success, and must retain the active file when the move fails. Timestamp-only names are not sufficient if they can collide; a deterministic sequence or equivalent collision-safe strategy is required.

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
- Migrate legacy logger call sites only in Phase 5.B, after Phase 5.A dependency injection work and the Phase 2 Serilog foundation are verified.
- Retire the legacy logger compatibility path only after the Phase 5.B migration verifies equivalent Serilog behavior.

## Phased Implementation

| Phase | Scope |
|---|---|
| 2 | Create the Serilog infrastructure files, neutral formatting, rolling file configuration, retention/archive helpers, source context support, thread enrichment, exception enrichment, and Debug-build-only debug sink configuration under `source/CalradiaForge.Core/Infra/Logging/`. Keep the legacy `Logger` file and all current call sites intact. |
| 5.A | Establish dependency-injection composition and service initialization patterns. Do not use this phase to migrate all legacy logger callers. |
| 5.B | Migrate legacy `Logger.Instance` call sites in controlled batches after the Serilog foundation and DI work exist. Remove repeated manual debug guards except around expensive diagnostic construction as call sites are migrated. |
| Later verification | Add or expand tests for formatter output, minimum-level behavior, Release artifact exclusion of Debug-only sinks, and legacy compatibility retirement when migration is complete. |

## Verification Expectations

- Logs are created in the expected logs directory.
- Minimum level suppresses lower-priority messages.
- Session logs retain expected lifecycle behavior or have an intentional Serilog replacement.
- Formatter output includes the applicable timestamp, level, message, source context, thread information, properties, and exception details without value rewriting.
- Log cleanup preserves recent files and removes old session logs according to retention rules.
- Logging callers do not intentionally pass credentials or authentication material to the logger.
- Relevant local Steam/Bannerlord path-resolution diagnostics remain available without automatic path masking.
- Any future performance, profiler, export, or telemetry output policy is defined separately when that feature is designed.
- One factory invocation produces one shared logger instance; repeated factory creation and duplicate providers are detected.
- Cleanup runs once before the active sink opens; the active file is preserved on archive failure; archive collisions never overwrite; access/move failures are recoverable and visible.
- Shutdown waits for logging-producing work, closes exactly once, releases the active handle before archive, and does not rely on an unassigned global close.
- The selected retention semantics, file-size behavior, minimum level, Debug-only sink exclusion, formatter output, and caller credential-boundary behavior are verified in relevant Debug/Release paths.

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

- Should `RetainedFileCount` mean file count or file age, and what exact retention/active-file/archive policy should replace or preserve the current cleanup?
- Should the active file preserve the Serilog default approximate size limit, use explicit size rolling, or use another policy?
- Should logger ownership belong to `SerilogLoggerFactory` or the DI provider?
- Should the shared logger be assigned to `Serilog.Log.Logger`, or be directly disposed through DI?
- What is the exact provider/logger disposal order after workflow quiescence?
- What collision-safe archive naming and failure-reporting contract should be used?
