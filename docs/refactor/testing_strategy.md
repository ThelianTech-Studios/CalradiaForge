# Testing Strategy

## Purpose

CalradiaForge should move from manual/runtime verification toward phased automated coverage, starting with Core behavior that is risky, file-heavy, or easy to regress. The strategy also establishes reusable benchmark infrastructure without treating early measurements as the final post-refactor performance baseline.

## Current Grounding

- There is no test project checked in today.
- `AppConfig` and `AppConfigSettings` own JSON-backed settings and typed config behavior.
- `ModsData` owns mod cache file I/O.
- `ModpackData` owns modpack and last-used file I/O.
- `ModpackService` owns modpack workflow behavior, validation, import/export, and default modpack creation.
- `ModInstaller`, `ModExtractor`, and `BLSEInstaller` own install and extraction behavior that needs coverage before Nexus downloads.
- `Logger` currently writes session logs directly. The future Serilog formatter is presentation-only and performs no automatic secret or path filtering.
- Test framework, benchmark framework, analyzer tools, benchmark project path, result storage, and CI blocking policy are not selected.

## Test Layers

| Layer | Timing | Scope |
|---|---|---|
| Core unit tests | First | Parser behavior, persistence helpers, modpack validation, result mapping, BLSE allowlist rules. |
| Regression tests | With each changed risk area | Preserve observed behavior for cleanup, safety, logging, DI, workflow, and MVVM changes. |
| Integration-style filesystem tests | Early | Atomic writes, backup recovery, archive preflight, extraction containment, install destination checks, and realistic file ownership. |
| Benchmark fixtures | Phase 4 | Reusable controlled datasets and setup/cleanup helpers for later measurement. |
| Benchmark execution | Phase 4 infrastructure, Phase 8 authoritative audit, Phase 9 comparison, Phase 10 final verification | Relative speed, throughput, allocations, scaling, and user-visible workflow components. |
| ViewModel tests | After MVVM extraction begins | Commands, state transitions, busy/error/status state, operation results. |
| Manual WPF smoke checks | When UI behavior is in scope | Startup, navigation, responsiveness, dialogs, toasts, and UI-thread-sensitive workflows that CLI tests cannot prove. |
| CI validation | After test projects exist and stability is demonstrated | Build, tests, and later approved analyzer or benchmark checks. |

## Conventional Tests And Performance Benchmarks

Conventional tests verify correctness and deterministic behavior. They should cover initialization, parsing, persistence, recovery, filesystem behavior, validation, dependency ordering, error handling, cancellation, cleanup, and state preservation.

Benchmarks measure relative speed, throughput, allocations, scaling, repeated-operation cost, startup components, extraction orchestration, parsing, scanning, serialization, caching, and logging overhead.

Do not put narrow exact-duration assertions in ordinary unit or integration tests. Timing is sensitive to machine load, virtualization, filesystem caching, antivirus activity, and environment differences. Benchmark results should be reported with variance rather than treated as a universal pass/fail result.

## Benchmark Infrastructure Direction

Phase 4 should establish a benchmark project or approved equivalent harness only after the framework, project placement, package set, and repository naming conventions are selected. The exact path and package choice remain open.

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

Do not add a test framework, benchmark framework, analyzer package, project, or package dependency merely because this strategy mentions it. Any new package, analyzer, large fixture, or blocking threshold requires owner approval.

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

Fixtures must use fake roots or isolated temporary directories. They must not read or write real user configuration, Steam directories, Bannerlord installations, real logs, Nexus credentials, or network services.

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
| Phase 8 | Authoritative post-Phase-7 baseline captured against the settled substantive refactor. |
| Phase 9 | Finding-specific before/after comparison against the Phase 8 baseline. |
| Phase 10 | Final full-suite comparison and verification after approved changes. |

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

## Logger Lifecycle Test And Measurement Coverage

Conventional tests must cover the planned Phase 5.A logger lifecycle without depending on wall-clock timing:

- one DI factory registration and one shared logger identity;
- factory creation exactly once, cleanup exactly once, and cleanup before the active sink opens;
- preservation of the active file when no file exists, a destination collision occurs, an archive name cannot be resolved, or access/move fails;
- collision-safe archive naming with no overwrite;
- the selected retention semantics after the count-versus-age decision is approved;
- minimum-level behavior, Debug sink exclusion from Release, and expected formatter output;
- formatter output with message, source context, thread information, properties, and exception details;
- exactly-once logger close/disposal, shutdown waiting for logging-producing work, and active-handle release before rename.

Logger benchmarks may measure construction, disabled-level calls, neutral formatter rendering, structured-property rendering, async sink throughput and backpressure, cleanup scaling, flush/close, archive move, startup contribution, and allocations. These are measurements, not permission to remove validation, diagnostics, or lifecycle waits. Use representative fixtures and report environment-sensitive filesystem results without fragile exact-time unit assertions or single-machine blocking thresholds.

## Phased Implementation

| Phase | Scope | Notes |
|---|---|---|
| 1-3 | Establish initial Core correctness coverage around changed behavior. | Preserve completed Phase 1 history and add tests as safety work is implemented. |
| 4 | Add Core unit/regression tests, integration-style filesystem tests, reusable benchmark fixtures, benchmark harness infrastructure, analyzer/measurement readiness, and provisional baselines. | Separate correctness tests from benchmarks. Do not claim final post-refactor performance results. |
| 5.A-7 | Expand tests and benchmark cases as DI, logging, workflow, and MVVM changes alter stable boundaries. | Verify one-provider/singleton rules and use manual UI checks where required. |
| 8 | Run the report-only performance audit and capture authoritative post-Phase-7 baselines. | Do not edit production code. Stop for developer decisions. |
| 9 | Test and benchmark explicitly approved `PERF-NNN` findings. | Compare before/after under comparable conditions and record ineffective or harmful changes. |
| 10 | Run the full bounded build/test/benchmark/analyzer/architecture/manual-smoke loop. | Update the same audit report and stop when practical criteria are met. |
| 11 | Use final verified results for release and documentation closeout. | Do not describe recommendations as shipped changes. |

## Verification Expectations

- `dotnet build source/CalradiaForge.slnx` succeeds when the relevant phase is implemented.
- `dotnet test source/CalradiaForge.slnx` runs meaningful deterministic tests once a test project exists.
- Tests use isolated temporary paths and do not touch real installations or user data.
- Timing-sensitive claims remain in benchmarks rather than normal tests.
- Relevant Release benchmarks run with documented environment and fixture metadata.
- Allocation results are captured where supported and interpreted with environment limitations.
- WPF startup and responsiveness are verified through manual smoke checks when CLI execution cannot prove interactive behavior.
- SevenZipWrapper evidence is either comparable with metadata or explicitly recorded as unavailable/incomparable.
- Benchmark failures or unavailable commands are reported honestly rather than fabricated.

## Guardrails

- Keep Core tests free of WPF references.
- Do not require Nexus credentials, network access, real user paths, Steam, or Bannerlord.
- Do not bypass `ModInstaller`, `ModExtractor`, `ModsData`, or `ModpackData` to make tests easier.
- Preserve observable behavior, safety, caller credential boundaries, cancellation, cleanup, and ownership boundaries.
- Do not add timing assertions to ordinary unit tests.
- Do not add packages, analyzers, large fixtures, or blocking thresholds without approval.
- Do not perform speculative production optimization in Phase 4.
- Do not treat provisional Phase 4 measurements as authoritative Phase 8 baselines.
- Do not weaken archive containment, module identity, persistence atomicity, recovery, or logging to improve benchmark results.

## Future Documentation Cross-References

Accepted testing and benchmark decisions should later be migrated into canonical testing/QA documentation and linked from platform/path, installer/archive, persistence, modpack, logging/security, performance, and UI/MVVM documentation as those documents are accepted.

## Open Questions

- Which test framework should be standard: xUnit, NUnit, or MSTest?
- Which benchmark framework and project path should be approved?
- Which allocation/analyzer tools are acceptable without adding unnecessary dependencies?
- Where should raw benchmark results be retained?
- What fixture archives can be checked in legally and without unreasonable repository cost?
- What minimum coverage should block CI once tests exist?
- Which performance thresholds, if any, should become blocking after stability is demonstrated?

## Out Of Scope

- Full WPF UI automation in the initial test phase.
- Nexus API integration tests before Nexus boundaries exist.
- Large MVVM test coverage before ViewModels exist.
- Requiring contributors to install Bannerlord.
- Selecting unapproved test or benchmark packages by implication.
