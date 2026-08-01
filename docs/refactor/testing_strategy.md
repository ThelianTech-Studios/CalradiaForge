# Testing Strategy

## Purpose

CalradiaForge should move from manual/runtime verification toward phased automated coverage, starting with Core behavior that is risky, file-heavy, or easy to regress. The strategy also establishes reusable benchmark infrastructure without treating early measurements as the final post-refactor performance baseline.

## Current Grounding

- `source/CalradiaForge.Tests` is the owner-approved xUnit project. Core tests live under `Core.Tests`; the Nexus and UI project-level folders remain reserved for their owning layers.
- `AppConfig` and `AppConfigSettings` own JSON-backed settings and typed config behavior.
- `ModsData` owns mod cache file I/O.
- `ModpackData` owns modpack and last-used file I/O.
- `ModpackService` owns modpack workflow behavior, validation, import/export, and default modpack creation.
- `ModInstaller`, `ModExtractor`, and `BLSEInstaller` own install and extraction behavior that needs coverage before Nexus downloads.
- Provider-owned Serilog is the implemented normal logger; `EmergencyStartupLogWriter` is the narrow pre-operational fallback. The formatter is presentation-only and performs no automatic secret or path filtering.
- `source/CalradiaForge.Benchmarks` is the owner-approved BenchmarkDotNet project. Core benchmarks live under `Core.Benchmarks`; raw local results use the ignored `BenchmarkDotNet.Artifacts` directory.
- Additional analyzer tools, retained baseline storage, minimum CI coverage, and CI blocking policy remain unselected.

## Test Layers

| Layer | Timing | Scope |
|---|---|---|
| Core unit tests | First | Parser behavior, persistence helpers, modpack validation, result mapping, BLSE allowlist rules. |
| Regression tests | With each changed risk area | Preserve observed behavior for cleanup, safety, logging, DI, workflow, and MVVM changes. |
| Integration-style filesystem tests | Early | Atomic writes, backup recovery, archive preflight, extraction containment, install destination checks, and realistic file ownership. |
| Benchmark fixtures | Phase 4 | Reusable controlled datasets and setup/cleanup helpers for later measurement. |
| Benchmark execution | Phase 4 infrastructure, Phase 9 authoritative audit, Phase 10 comparison, Phase 11 final verification | Relative speed, throughput, allocations, scaling, and user-visible workflow components. |
| ViewModel tests | After MVVM extraction begins | Commands, state transitions, busy/error/status state, operation results. |
| Manual WPF smoke checks | When UI behavior is in scope | Startup, navigation, responsiveness, dialogs, toasts, and UI-thread-sensitive workflows that CLI tests cannot prove. |
| CI validation | After test projects exist and stability is demonstrated | Build, tests, and later approved analyzer or benchmark checks. |

## Conventional Tests And Performance Benchmarks

Conventional tests verify correctness and deterministic behavior. They should cover initialization, parsing, persistence, recovery, filesystem behavior, validation, dependency ordering, error handling, cancellation, cleanup, and state preservation.

Benchmarks measure relative speed, throughput, allocations, scaling, repeated-operation cost, startup components, extraction orchestration, parsing, scanning, serialization, caching, and logging overhead.

Do not put narrow exact-duration assertions in ordinary unit or integration tests. Timing is sensitive to machine load, virtualization, filesystem caching, antivirus activity, and environment differences. Benchmark results should be reported with variance rather than treated as a universal pass/fail result.

## Benchmark Infrastructure Direction

Phase 4 uses the owner-approved `source/CalradiaForge.Benchmarks` project with BenchmarkDotNet `0.15.2`. The project references Core directly, uses short Release jobs with warmup and repeated iterations, and reports managed allocations through `MemoryDiagnoser`.

The infrastructure should support:

- Release-build execution.
- Warmup and repeated iterations.
- Allocation measurement where technically supported.
- Parameterized small, medium, and representative datasets.
- Stable fixture creation and cleanup.
- Component-only and end-to-end benchmarks.
- Environment metadata capture.
- Reviewable result export and raw-result retention.
- A documented local execution command.
- CI execution only when stability and runtime cost justify it.

The current xUnit and BenchmarkDotNet package sets were selected by the owner before Phase 4 implementation. Any additional package, analyzer, large fixture, or blocking threshold still requires owner approval.

### Phase 4 Project Layout And Commands

- Correctness tests: `source/CalradiaForge.Tests/Core.Tests`.
- Performance cases: `source/CalradiaForge.Benchmarks/Core.Benchmarks`.
- Reserved future folders: `Nexus.Tests`, `UI.Tests`, `Nexus.Benchmarks`, and `UI.Benchmarks`.
- Full correctness command: `dotnet test source/CalradiaForge.slnx`.
- Built-in analyzer/compiler-warning command: `dotnet build source/CalradiaForge.slnx -c Release`.
- Authoritative Phase 9 benchmark command on Windows: `powershell -NoProfile -ExecutionPolicy Bypass -File source/CalradiaForge.Benchmarks/run-phase9-benchmarks.ps1`.
- Targeted Phase 9 benchmark command: add `-Filter '*ModpackValidationBenchmarks*'` or another BenchmarkDotNet filter.
- Owner-approved supplemental real-installation command: `powershell -NoProfile -ExecutionPolicy Bypass -File source/CalradiaForge.Benchmarks/run-phase9-real-installation-benchmarks.ps1 -ConsentToReadRealInstallation`.
- The historical `run-phase4-benchmarks.ps1` marker refuses to run the current benchmark project because current output cannot reproduce or inherit the provisional Phase 4 labels. Original Phase 4 artifacts belong to commit `580f3a4e19e383fd701a86b75953a39b69832aad` and are not the Phase 9 decision baseline.
- Raw local results and environment metadata: `source/CalradiaForge.Benchmarks/BenchmarkDotNet.Artifacts`.

The Phase 4 Core suite covers typed configuration and corrupt JSON fallback, atomic mod-cache round trips and backup recovery, named and last-used modpack persistence, modpack workflow validation, parser behavior, manual Workshop-root scanning with fake multi-library roots, archive containment and extraction, authoritative installer preflight, BLSE detection/install behavior, formatter output, and install-summary result mapping. Benchmarks cover parser dependency scaling, load-order validation scaling, mod-cache filesystem persistence, and fake-root module scanning.

The split-root scanner regression models a Steam client root independently from the Bannerlord library, with three Workshop modules and eight game modules. The Phase 5 production resolver selects the alternate registered library, the scanner returns all 11 unique modules, and the Novus regression confirms those Workshop modules are not reported missing. A neutral non-Steam regression still confirms that Workshop scanning is provider-gated.

The Steam resolver and workflow suites cover fake main/alternate libraries, representative `libraryfolders.vdf` and `appmanifest_261550.acf` inputs, Workshop manifests, game markers, launcher paths, duplicate roots, malformed metadata, missing libraries, traversal attempts, direct settings commits, startup reuse, re-detection, manual selection, queued feedback, and cache safety. The metadata fixtures are representative test inputs, not claims that Valve publishes their schemas as stable APIs.

Phase 5 automated evidence now covers resolver/workflow integration, split-root and no-Workshop behavior, scanner safety, startup queue behavior, and the Novus regression. This evidence does not replace owner smoke testing against a real Steam registry and WPF session.

The benchmark artifact directory is ignored because its output is machine- and working-tree-specific. The standard Phase 9 runner selects only deterministic `AuthoritativePostPhase8Baseline` categories and never executes real-installation cases. The separate consent-gated runner selects only `OwnerRealInstallation`, records sanitized dataset and stability metadata, and redacts exact input/repository/user-profile literals from local text artifacts. Reviewed synthetic and real-installation summaries belong in separate tables in the dated Phase 9 audit and remain informational rather than universal thresholds.

### Phase 4 Boundaries Confirmed During Implementation

- Existing SevenZipWrapper performance statements in repository history do not include a controlled archive fixture, source benchmark output, environment, cache state, or full methodology. They remain historical and `Not directly comparable`; Phase 4 does not subtract them from current results.
- No legal representative archive fixture was approved for a reusable SevenZipWrapper versus full-pipeline A/B benchmark. Generated zip fixtures cover correctness only; controlled extraction comparison remains for a later approved benchmark slice.
- Steam multi-library auto-discovery and candidate precedence are implemented with temporary-directory metadata fixtures. Owner Steam split-library smoke testing remains a separate evidence gate.
- BLSE allowlist enforcement remains deferred pending the owner-approved manifest. Current tests cover marker detection, platform-bin selection, copying, and executable-path configuration without claiming allowlist behavior.
- Phase 6.B provider/logger ownership, startup-only archival, fixed seven-day retention, shutdown/restart, and UI lifecycle behavior do not exist yet. Phase 4 covers the current neutral formatter; Phase 6.C later owns caller migration.
- No additional analyzer package, timing assertion, performance threshold, production optimization, WPF automation, Nexus integration test, or network fixture was added.

## Benchmark Fixture Categories

Where applicable, use fixtures for:

- Representative module XML.
- Mod directories with varying module counts.
- Valid, invalid, empty, and duplicate metadata.
- Modpack/load-order graphs of varying sizes.
- JSON persistence payloads of varying sizes.
- Small and representative archives.
- Extraction destinations with controlled state.
- Logging payloads with active and inactive levels.
- Cache-hit and cache-miss paths.

Conventional tests, CI, and deterministic reusable benchmark fixtures must use fake roots or isolated temporary directories. They must not read or write real user configuration, Steam directories, Bannerlord installations, real logs, Nexus credentials, or network services. The separately invoked, owner-approved Phase 9 real-installation benchmark is the only exception: it may read game and Workshop module metadata in place under the consent, no-write, redaction, stability, and non-CI controls in the performance audit policy.

## Benchmark Execution Rules

- Run benchmarks in Release configuration unless explicitly evaluating Debug-only behavior.
- Record operating system, CPU, memory, storage context where relevant, .NET runtime/SDK, commit, branch, build configuration, benchmark framework/version, analyzer/tool versions, and filesystem/antivirus notes.
- Use warmup and repeated iterations where appropriate.
- Record distributions, variance, throughput, scaling, and allocations where supported.
- Separate cold-cache and warm-cache behavior when relevant.
- Keep component setup and cleanup outside the measured region, while providing end-to-end measurements when setup and cleanup are part of user-visible cost.
- Do not compare materially different archives, machines, runtimes, builds, harnesses, or cache states without recording the limitation.
- Keep raw results separate from hand-written report conclusions.

## Baseline Lifecycle

| Phase | Baseline meaning |
|---|---|
| Phase 4 | Infrastructure validation, fixture calibration, or provisional pre-refactor baseline. Not authoritative for final post-refactor claims. |
| Phase 9 | Authoritative post-Phase-8 baseline captured against the settled substantive refactor. |
| Phase 10 | Finding-specific before/after comparison against the Phase 9 baseline. |
| Phase 11 | Final full-suite comparison and verification after approved changes. |

Phase 4 measurements must be labeled explicitly as `Infrastructure validation`, `Provisional pre-refactor baseline`, `Fixture calibration`, or `Not comparable to final post-refactor baseline` where applicable.

## Performance Regression Thresholds

Any proposed threshold must identify:

- The measured workflow and fixture.
- Baseline source and retention rule.
- Threshold or tolerance.
- Why the tolerance is reasonable.
- Expected environment variance.
- Whether the result is informational or blocking.
- How the baseline is updated.
- How regressions are reviewed.

Results remain informational by default until stable across intended environments. Do not make filesystem, startup, or archive benchmarks blocking from one developer-machine baseline.

## SevenZipWrapper Comparison

SevenZipWrapper benchmark values are potential comparison evidence, not automatically valid baselines. Prefer a same-harness A/B comparison between direct SevenZipWrapper extraction and the full CalradiaForge extraction path.

Comparisons must control or record archive format, exact content, compressed size, extracted size, file count, destination state, machine, operating system, runtime, SDK, build configuration, SevenZipWrapper version, warmup, iterations, cache state, antivirus interference, setup/cleanup placement, and progress/cancellation instrumentation.

Do not subtract historical values when their source, archive, environment, or methodology is unknown. Mark them `Not directly comparable` and create or recommend a controlled baseline instead.

The final report must separate total extraction duration, the extraction-library baseline, estimated orchestration overhead, allocations, setup/cleanup, validation, metadata processing, destination preparation, and post-extraction parsing/scanning.

## Report-Driven Test Categories

Maintain coverage targets for:

- Scanner/path behavior using fake Steam libraries and Workshop roots.
- Installer/archive behavior, including extraction, preflight, containment, overwrite safety, and BLSE validation.
- Persistence behavior, including config, mod cache, modpacks, backups, and corrupt-file handling.
- Modpack workflows, including import, export, validation, save, save-as, and last-used behavior.
- Nexus metadata boundaries when implemented, without credentials in `ModuleModel` or `AppConfig`.
- Logging behavior, including minimum levels, formatter output, sink selection, and lifecycle.
- UI/ViewModel behavior, including commands, status, warning, error, progress, and cancellation.

## Scanner And Path Test Categories

Scanner/path tests must use fake temporary directories only. Required categories include:

- Steam client installed on one fake root while Bannerlord is under another fake library root.
- Multiple fake Steam library roots.
- Workshop content under the Bannerlord library root.
- Missing, empty, invalid, or incomplete Workshop content.
- No Workshop path candidates.
- Manual Workshop path override and invalid override if supported.

Tests should distinguish local module results from Workshop results and should produce structured warnings for missing or invalid Workshop paths.

## Phase 6.A Mod-Pipeline Tests

### Complete scans

- A valid local-only provider commits local modules.
- A valid Steam local plus Workshop scan commits one combined unique snapshot.
- A valid Workshop root containing zero modules is complete.
- Module ordering and identity behavior are deterministic where current contracts require it.

### Incomplete and failing scans

- Invalid game configuration does not scan or replace the active cache.
- An inaccessible local root does not commit.
- An inaccessible configured Workshop root does not masquerade as zero modules.
- Parse warnings surface without corrupting accepted state.
- Cancellation and unexpected failure do not commit.
- Duplicate identifiers are handled deterministically and reported.

### Cache and accepted snapshot

- The previous accepted snapshot remains after a rejected result.
- Cache rotation occurs only after an approved complete result.
- A partial scan does not emit false removals.
- Modpack missing-module validation uses the accepted snapshot.
- The split-root Steam fixture does not report existing Workshop modules missing.
- Startup and explicit refresh route through the same coordinator contract and report whether commit occurred.

### Lifecycle and regression

- Only one active operation follows the documented admission policy.
- Stop-admission is idempotent, cancellation reaches active work, and quiescence completes after operation completion.
- Shutdown during active work does not call disposed UI, logging, or provider dependencies.
- Existing Phase 5 resolver/detection tests remain valid, the solution builds, the full suite passes, and Core remains WPF-free.

## Phase 6.B Composition, UI, Logging, And Lifecycle Tests

### Composition and UI startup

- Exactly one validated provider is built; Core/UI modules extend the same collection; no temporary provider exists; intended singleton identities and registered roots resolve.
- `StartupUri` cannot create a second window. `MainWindow` and each retained primary page resolve exactly once.
- EULA rejection does not resolve/show the shell. Language/EULA dialogs create fresh windows.
- Startup notifications wait and drain FIFO only after `MainWindow.Loaded` readiness.

### Settings and logging bootstrap

- Missing, empty, and malformed settings regenerate defaults through the locked object flow.
- `LoggingSettings.DebugMode` is available before one logger is constructed and assigned globally.
- Provider disposal is the only normal close path.
- Debug mode selects the new-process minimum level, and changing it persists immediately and prompts for restart in both directions.

### Log files

- Previous `Latest` archives at startup using last-write time and minute precision; missing `Latest` is a no-op; creation-time fallback works.
- A destination collision or rename failure does not overwrite an archive and proceeds to the locked overwrite/append attempt for the new session.
- Fixed seven-day cleanup excludes active `Latest`, ignores missing files and individual deletion failures, and shutdown leaves `Latest` available.

### Shutdown, restart, and exceptions

- Duplicate shutdown requests are guarded; normal close confirms before commitment; Cancel prevents shutdown.
- A 15-second timeout offers bounded continue-waiting or controlled best-effort exit-anyway without process kill.
- Active mod work is cancelled/quiesced through the Phase 6.A contract, and the provider disposes exactly once.
- Restart launches the current executable only after disposal; launch failure still exits safely.
- Dispatcher exceptions route to fatal shutdown, unobserved task exceptions are logged/observed, provider/startup failure does not show the shell, and AppDomain termination remains best effort.

Manual WPF smoke coverage includes valid/first-run startup, language, EULA accept/decline, Steam with/without Workshop, manual fallback, startup mod initialization, queued toasts, retained page state, debug toggles and restart choices, normal/active-work close, both timeout choices, controlled restart, fatal dispatcher handling, `Latest` after shutdown, and prior-`Latest` archival at next startup.

## Phase 6.C Logger Migration Tests

### Inventory and output

- Zero normal `Logger.Instance` references, zero general-purpose legacy construction, zero duplicated minimum-level state, and no normal `Log.CloseAndFlush()` remain.
- Information mode excludes ordinary Debug events; Debug mode after restart includes them.
- Structured properties and exception details render correctly, and UI/Core events reach the same shared file and approved sinks.

### Bootstrap and lifecycle

- Missing/malformed settings regenerate before Serilog without circular logging.
- Provider-build and logger-construction failures use the best-effort emergency writer.
- Successful startup creates no emergency file, and emergency-writer failure does not block fatal shutdown.
- Exactly one shared logger closes once through provider disposal; no logging occurs after disposal.
- Normal shutdown leaves `Latest`, next startup archives it correctly, and restart applies persisted `DebugMode`.

### Regression

- The solution builds, all tests and targeted logging smoke tests pass, Phase 5/6.A behavior remains unchanged, and no WPF reference enters Core.

Logger benchmarks may measure construction, disabled-level calls, neutral formatter rendering, structured properties, async sink behavior, startup archive/cleanup scaling, provider-owned flush/close, startup contribution, and allocations. These measurements do not authorize lifecycle or file-policy changes.

## Implemented Phase 7 Deterministic Validation

Phase 7 automated tests cover manager sequencing and status precedence;
busy versus admission-stopped rejection; validation failure, partial failure,
cancellation, bounded finalization, exactly-once cleanup, and retained partial
summary; DLL-unblock and internal scan/commit seams; operation/archive IDs and
transient-progress/terminal-result separation; BLSE classification; presenter
lifecycle, activation, stale-progress rejection, and no duplicate toast;
DI/XAML/navigation/localization rename boundaries; and Core/UI dependency rules.
The tests prove `RefreshAsync` is not recursively admitted from install and that
installer/extractor mechanics remain in their current owners. Debug and Release
solution runs pass 194 tests at the Phase 7 implementation endpoint; owner WPF
runtime inspection remains separate from automated verification.

## Approved, Unimplemented Phase 8 Verification Strategy

Phase 8.A uses focused property, command, lifecycle, dispatcher, and
cancellation-boundary tests. Unified Phase 8.B uses focused Launcher, Modpacks,
and Settings ownership-transfer tests in that internal order; it has no B
subphases. Phase 8.C uses focused shell/navigation/lifecycle/cleanup tests.

Phase 8.D owns comprehensive terminal-result matrices, collection
identity/ordering, activation/subscription idempotence, navigation concurrency,
architecture-boundary checks, clean Debug and Release builds/tests, and the
owner-driven Visual Studio runtime workflow. Tests are deterministic and
task-driven: do not use arbitrary `Task.Delay`, wall-clock assumptions, sleeps,
or live windows for ordinary unit tests. Automated tests do not prove interactive
WPF behavior.

Runtime evidence is classified as **Pass - Directly Observed**, **Pass -
Corroborated**, **Inconclusive**, **Blocked**, or **Fail**. Codex records only
what it directly observes and labels supplied screenshots, recordings, logs, or
checkpoints accurately. Phase 8.E corrects and revalidates only a validated
Phase 8.D production defect. See the [Phase 8 locked decisions](phase_8_locked_decisions_2026-07-29.md).

## Phased Implementation

| Phase | Scope | Notes |
|---|---|---|
| 1-3 | Establish initial Core correctness coverage around changed behavior. | Preserve completed Phase 1 history and add tests as safety work is implemented. |
| 4 | Add Core unit/regression tests, integration-style filesystem tests, reusable benchmark fixtures, benchmark harness infrastructure, analyzer/measurement readiness, and provisional baselines. | Separate correctness tests from benchmarks. Do not claim final post-refactor performance results. |
| 5 | Preserve and expand implemented detection/path/scanner regressions. | Keep split-library, no-Workshop, queue, and Novus coverage. |
| 6.A | Add coordinator completeness, commit, accepted-snapshot, startup/refresh, and quiescence tests. | Preserve Phase 5 and Core WPF-free behavior. |
| 6.B | Add one-provider, retained UI, settings/bootstrap, startup-archive, shutdown/restart/exception, and manual WPF coverage. | Prove one provider-owned logger close path and retained `Latest`. |
| 6.C | Add caller-inventory, structured output, emergency writer, and one-close-path regression tests. | Remove the general legacy logger only after zero normal callers. |
| 7 | Add locked install-result, progress, manager-reconciliation, presenter, and Launcher-rename deterministic tests. | Preserve manager ownership; Phase 7 does not implement MVVM. |
| 8 | 8.A focused foundation, unified 8.B workflow, 8.C shell, 8.D comprehensive/runtime verification, and conditional 8.E revalidation. | Preserve manager/presenter ownership; do not describe Phase 8 as implemented before verified source exists. |
| 9 | Run the report-only performance audit and capture authoritative post-Phase-8 baselines. | Do not edit production code. Stop for developer decisions. |
| 10 | Test and benchmark explicitly approved `PERF-NNN` findings. | Compare before/after under comparable conditions and record ineffective or harmful changes. |
| 11 | Run the full bounded build/test/benchmark/analyzer/architecture/manual-smoke loop. | Update the same audit report and stop when practical criteria are met. |
| 12 | Use final verified results for release and documentation closeout. | Do not describe recommendations as shipped changes. |

## Verification Expectations

- `dotnet build source/CalradiaForge.slnx` succeeds when the relevant phase is implemented.
- `dotnet test source/CalradiaForge.slnx` runs meaningful deterministic tests once a test project exists.
- Tests and CI use isolated temporary paths and do not touch real installations or user data; only the separately consented local Phase 9 supplemental benchmark may read installed module metadata under the documented no-write controls.
- Timing-sensitive claims remain in benchmarks rather than normal tests.
- Relevant Release benchmarks run with documented environment and fixture metadata.
- Allocation results are captured where supported and interpreted with environment limitations.
- WPF startup and responsiveness are verified through manual smoke checks when CLI execution cannot prove interactive behavior.
- SevenZipWrapper evidence is either comparable with metadata or explicitly recorded as unavailable/incomparable.
- Benchmark failures or unavailable commands are reported honestly rather than fabricated.

## Guardrails

- Keep Core tests free of WPF references.
- Do not require Nexus credentials, network access, real user paths, Steam, or Bannerlord for tests, CI, or the deterministic benchmark baseline. The opt-in real-installation supplement remains local-only and must fail closed without consent and valid discovered roots.
- Do not bypass `ModInstaller`, `ModExtractor`, `ModsData`, or `ModpackData` to make tests easier.
- Preserve observable behavior, safety, caller credential boundaries, cancellation, cleanup, and ownership boundaries.
- Do not add timing assertions to ordinary unit tests.
- Do not add packages, analyzers, large fixtures, or blocking thresholds without approval.
- Do not perform speculative production optimization in Phase 4.
- Do not treat provisional Phase 4 measurements as authoritative Phase 9 baselines.
- Do not weaken archive containment, module identity, persistence atomicity, recovery, or logging to improve benchmark results.

## Future Documentation Cross-References

Accepted testing and benchmark decisions should later be migrated into canonical testing/QA documentation and linked from platform/path, installer/archive, persistence, modpack, logging/security, performance, and UI/MVVM documentation as those documents are accepted.

## Open Questions

- Which allocation/analyzer tools are acceptable without adding unnecessary dependencies?
- Which reviewed benchmark results, if any, should be promoted from ignored local artifacts into the Phase 9 audit evidence?
- What fixture archives can be checked in legally and without unreasonable repository cost?
- What minimum coverage should block CI once tests exist?
- Which performance thresholds, if any, should become blocking after stability is demonstrated?

## Out Of Scope

- Full WPF UI automation in the initial test phase.
- Nexus API integration tests before Nexus boundaries exist.
- Large MVVM test coverage before ViewModels exist.
- Requiring contributors to install Bannerlord.
- Selecting unapproved test or benchmark packages by implication.
