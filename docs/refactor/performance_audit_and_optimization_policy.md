# Performance Audit And Optimization Policy

Status: planned policy
Scope: performance testing, audit, approved optimization, and final verification guidance only. This document does not authorize production-code changes by itself.

## Purpose

Define an evidence-driven process for measuring CalradiaForge workflows, producing a report-only performance audit, implementing only explicitly approved optimization findings, and completing bounded post-refactor verification.

Performance work must preserve current behavior, safety, ownership, diagnostics, and maintainability. The existence of a benchmark or a finding does not imply that a target is inefficient or that an optimization is approved.

## Scope

Applicable areas may include:

- Application startup and composition.
- DI registration, provider construction, resolution, singleton identity, and disposal.
- Configuration and localization loading.
- Paths, filesystem checks, Steam library and Workshop discovery.
- Mod scanning, parsing, version comparison, modpack validation, and persistence.
- Archive inspection, extraction orchestration, validation, destination preparation, parsing, scanning, and cleanup.
- Logging construction, disabled-level work, enrichment, neutral formatter rendering, and sink behavior.
- Logger lifecycle targets include duplicate construction, factory invocation count, active-sink startup, startup-only previous-`Latest` archival and fixed retention, formatter cost, disabled-level work, async sink throughput/backpressure, provider-owned flush/close, shutdown races, file-size behavior, and allocations. Startup archive/retention measurements remain separate from shutdown quiescence/provider disposal measurements.
- Async work, cancellation, locking, contention, collections, serialization, allocations, and memory retention.
- UI-thread-sensitive workflows and manual responsiveness observations.

Do not presume that a named file, static field, lazy initialization path, or third-party operation is inefficient before measuring or documenting strong evidence.

## Evidence And Measurement Rules

- Use Release configuration unless the behavior under evaluation is explicitly Debug-only.
- Record operating system, CPU, memory, storage context where relevant, .NET SDK/runtime, build configuration, commit, branch, benchmark framework/version, analyzer/tool versions, and filesystem or antivirus interference notes.
- Use warmup and repeated iterations where appropriate.
- Record distributions, variance, throughput, scaling, and allocations where supported rather than relying on one stopwatch value.
- Separate cold-cache and warm-cache behavior when the distinction affects the result.
- Use isolated temporary directories and controlled fixture data.
- Keep setup and cleanup outside a component's measured region, while also providing end-to-end measurements when setup and cleanup are part of user-visible cost.
- Do not compare materially different archives, machines, runtimes, build configurations, harnesses, or cache states without clearly recording the limitation.
- Keep raw benchmark output separate from hand-written conclusions and retain enough metadata to reproduce the comparison.

## Conventional Tests And Benchmarks

Conventional tests and benchmarks have different purposes.

### Conventional Tests

Use unit, regression, and integration-style filesystem tests for correctness, deterministic output, parsing, persistence, recovery, validation, dependency ordering, error handling, cancellation, cleanup, state preservation, and architecture invariants.

Do not put narrow exact-duration assertions in ordinary tests. Timing is sensitive to machine load, virtualization, filesystem caching, and antivirus activity.

### Benchmarks

Use benchmarks to measure relative speed, throughput, allocations, scaling, repeated-operation cost, startup components, extraction orchestration, parsing, scanning, serialization, caching, and logging overhead.

Benchmarks should report results and variance. They should not become blocking checks solely because one developer-machine result exceeded a narrow wall-clock value.

## Benchmark Lifecycle

| Phase | Performance responsibility |
|---|---|
| Phase 4 | Establish reusable fixtures, benchmark/measurement infrastructure, and provisional baselines. |
| Phase 9 | Capture authoritative post-Phase-8 baselines and produce the report without production-code edits. |
| Phase 10 | Compare explicitly approved finding changes against the Phase 9 baseline. |
| Phase 11 | Run the final full verification loop and update the same audit report. |

Phase 4 measurements must be labeled `Infrastructure validation`, `Provisional pre-refactor baseline`, `Fixture calibration`, or `Not comparable to final post-refactor baseline` as appropriate. The authoritative baseline for optimization decisions is captured in Phase 9 against the settled Phase 8 implementation.

Phase 8 may record implementation/runtime observations that inform Phase 9, but
it does not create `PERF-NNN` findings or perform speculative optimization.

## Benchmark Infrastructure Requirements

Phase 4 should establish, after framework and package approval:

- Release-build execution.
- Warmup and repeated iterations.
- Allocation reporting where technically supported.
- Parameterized small, medium, and representative datasets.
- Stable fixture creation and cleanup.
- Separate component and end-to-end benchmarks.
- Environment metadata capture.
- Reviewable result export and retention.
- Documented local execution commands.
- CI execution only when stability and runtime cost justify it.

The owner approved `source/CalradiaForge.Benchmarks`, BenchmarkDotNet `0.15.2`, and the project's existing diagnostics package for Phase 4. Local raw results use the ignored `source/CalradiaForge.Benchmarks/BenchmarkDotNet.Artifacts` path. Additional frameworks, analyzer packages, result-retention paths, or blocking thresholds remain unapproved.

## Fixture Categories

Where applicable, fixtures should cover:

- Representative module XML.
- Mod directories with varying module counts.
- Valid, invalid, empty, and duplicate metadata.
- Modpack/load-order graphs of varying sizes.
- JSON payloads of varying sizes.
- Small and representative archives.
- Controlled extraction destinations.
- Logging payloads with active and inactive levels.
- Cache-hit and cache-miss paths.

Fixtures must not require real user configuration, Steam directories, Bannerlord installations, Nexus credentials, or network access.

## Performance Regression Thresholds

Any threshold must document:

- Measured workflow and fixture.
- Baseline source and retention rule.
- Threshold or tolerance.
- Reason for the tolerance.
- Expected environmental variance.
- Whether the result is informational or blocking.
- Baseline update approval.
- Regression review procedure.

Performance results remain informational by default until stability is demonstrated across intended environments. Filesystem, startup, and archive results must not become blocking from one developer-machine baseline alone.

## SevenZipWrapper Comparison Rules

SevenZipWrapper values are potential comparison evidence, not automatically valid baselines. Prefer a controlled A/B comparison in the same harness and environment:

- Benchmark A calls the narrowest technically valid SevenZipWrapper extraction operation.
- Benchmark B runs the full CalradiaForge extraction orchestration.
- Both use the same archive bytes/content, destination conditions, machine, runtime, build, warmup, iterations, and cache conditions.

Control or record:

- Archive format, exact content, compressed size, extracted size, file count, and compression characteristics.
- Destination medium and existing state.
- Operating system, machine, runtime, SDK, build configuration, and SevenZipWrapper version.
- Warmup, iteration, filesystem-cache state, antivirus interference, setup/cleanup placement, and progress/cancellation instrumentation.

Only when the cases are genuinely comparable may the report show this as an estimate:

```text
Estimated CalradiaForge orchestration overhead
  = median CalradiaForge end-to-end duration
  - median controlled SevenZipWrapper baseline duration
```

This is not proof that every difference is CalradiaForge-owned code. If historical values lack archive, environment, runtime, build, cache, or methodology metadata, preserve them as historical context and mark them `Not directly comparable`; do not subtract them.

The final report must distinguish, where measurable:

- Total CalradiaForge extraction duration.
- Controlled extraction-library baseline.
- Estimated CalradiaForge orchestration overhead.
- Allocations.
- Setup and cleanup.
- Validation.
- Metadata processing.
- Destination preparation.
- Post-extraction parsing and scanning.
- Measurement limitations.

Do not remove validation, path containment, cancellation, cleanup, or result reporting merely to reduce an overhead value. `ModInstaller` and `ModExtractor` remain the authoritative install pipeline.

## Finding Model

### Classifications

- `Measured performance problem`: repeatable benchmark, profiler, analyzer, or trace evidence demonstrates a material cost or regression.
- `Strongly supported likely inefficiency`: code and workflow evidence strongly indicate avoidable cost, but end-to-end impact is not isolated.
- `Low-confidence concern requiring measurement`: plausible issue requiring focused evidence.
- `Theoretical micro-optimization`: possible savings without demonstrated meaningful impact.
- `Maintainability-only suggestion`: clarity or structure improvement that is not a performance claim.
- `Correctness issue discovered during performance review`: behavioral defect handled through correctness/safety workflow, not disguised as optimization.

### Finding IDs

Use stable identifiers in the form `PERF-NNN`, assigned once and retained through audit, decision, implementation, and final verification.

### Priority

Use `Critical`, `High`, `Medium`, `Low`, or `Informational`, considering frequency, scale, latency, allocations, user impact, complexity, correctness risk, and maintainability.

### Confidence

Use `High` for repeatable controlled measurement or clear analyzer/profiler evidence, `Medium` for strong code/workflow evidence with partial measurement, and `Low` for hypotheses requiring focused evidence.

### Developer Decision

Each proposed production change must have one of:

- `Pending Developer Review`.
- `Approved`.
- `Approved With Modification`.
- `Rejected`.
- `Deferred`.
- `Needs More Evidence`.
- `Out Of Scope`.

Phase 10 cannot implement findings that remain pending, rejected, deferred, out of scope, needs-more-evidence, or theoretical-only unless the developer separately approves them.

### Implementation Status

Use `Not Started`, `In Progress`, `Implemented`, `Verified Improved`, `Verified No Material Change`, `Regressed`, `Inconclusive`, `Reverted`, or `Blocked`.

Each finding should record its affected files and workflow, current behavior, suspected cost, evidence, baseline, cause, proposed optimization, expected benefit, complexity, correctness risk, maintainability impact, verification plan, developer decision, implementation status, final measurement, and final disposition.

## Performance Guardrails

Performance work must not:

- Change behavior without approval.
- Remove validation to improve a result.
- Weaken archive containment, module identity checks, persistence atomicity, recovery, or filesystem safety.
- Remove required logging or diagnostics without approval.
- Introduce automatic secret or path filtering as a performance tactic.
- Introduce unsafe or unbounded concurrency.
- Add caching without ownership, invalidation, and stale-state rules.
- Pool objects or buffers without evidence and safe lifetime handling.
- Depend on machine-specific assumptions.
- Treat Debug results as Release evidence.
- Treat one stopwatch result or an incomparable third-party value as proof.
- Optimize third-party-library time as if it were CalradiaForge-owned overhead.
- Keep complex code solely because it appears lower-level or faster.
- Obscure clear code for negligible or unmeasured gain.
- Present theoretical savings as measured results.
- Introduce runtime log-level switching, a second logger pipeline, a second normal close path, shutdown archival, collision suffixes, or configurable retention.

Small optimizations may still be accepted when they are safe, clear, remove repeated work, preserve maintainability, and align with deliberate software craftsmanship. They must still be described honestly and supported by appropriate evidence.

## Audit Report Artifact

Use the dated path pattern:

```text
docs/audits/calradiaforge_performance_audit_YYYY-MM-DD.md
```

Phase 9 selects and records the exact path. Phase 10 and Phase 11 update that same report rather than creating competing reports. Do not create the dated report during documentation planning unless the owner separately requests a blank checked-in template.

### Report Lifecycle

- Phase 9 creates the report and authoritative baseline.
- Developer review records decisions in the same report.
- Phase 10 updates implementation status and before/after outcomes.
- Phase 11 completes final verification and remaining limitations.
- Phase 12 uses the report as a release/documentation source but does not rewrite its evidence.

### Required Report Structure

```markdown
# CalradiaForge Performance Audit

Status: In Review
Audit date:
Audit commit:
Audit branch:
Report owner:

## Audit Scope
## Excluded Scope
## Environment
## Build Configuration
## Benchmark Methodology
## Fixture And Dataset Inventory
## Existing Baselines
## Executive Summary
## Findings Summary
| ID | Title | Classification | Priority | Confidence | Developer decision | Implementation status |
|---|---|---|---|---|---|---|
## Detailed Findings
### PERF-001 - Finding title
- Status:
- Classification:
- Priority:
- Confidence:
- Affected files:
- Affected workflow:
- Current behavior:
- Suspected cost:
- Evidence:
- Baseline result:
- Suspected cause:
- Proposed optimization:
- Expected benefit:
- Implementation complexity:
- Correctness risk:
- Maintainability impact:
- Verification plan:
- Developer decision:
- Developer notes:
- Implementation status:
- Implementation summary:
- Final benchmark result:
- Final disposition:
## SevenZipWrapper Baseline Comparison
### Comparability Assessment
### Total Extraction Duration
### Extraction-Library Baseline
### Estimated CalradiaForge Orchestration Overhead
### Allocation Results
### Setup And Cleanup Costs
### Validation Costs
### Metadata-Processing Costs
### Destination-Preparation Costs
### Post-Extraction Parsing And Scanning Costs
### Limitations
## Startup Performance
## Dependency Injection And Composition Performance
## Filesystem And Path Performance
## Mod Scanning Performance
## Parsing Performance
## Modpack And Load-Order Performance
## Extraction Performance
## Caching And Persistence Performance
## Logging Performance
## Asynchronous And Concurrency Findings
## Allocation And Memory Findings
## Correctness Issues Discovered During Performance Review
## Rejected Optimizations
## Deferred Optimizations
## Needs More Evidence
## Reverted Optimizations
## Remaining Limitations
## Final Verification Results
### Build Results
### Test Results
### Benchmark Results
### Analyzer Results
### Architecture Verification
### Manual Smoke-Test Results
### Practical Stopping-Criteria Assessment
```

## Phase 9 Audit Boundary

Phase 9 is report-only. It may run tests, benchmarks, analyzers, source inspection, and narrowly scoped test/benchmark changes required to collect evidence when explicitly in scope. It must not edit production code, silently approve a finding, weaken safety, or transition into Phase 10. Every proposed production change remains `Pending Developer Review` until the owner decides.

## Phase 10 Optimization Boundary

Phase 10 implements approved finding IDs only, in small batches. It preserves observable behavior unless a separate behavior change is approved, updates affected tests and benchmarks, compares under comparable conditions, and records improvements, no material change, regressions, inconclusive outcomes, reverts, blocked work, and deferred or rejected findings honestly. It stops for manual developer inspection before changelog or migration-map work.

## Phase 11 Verification Boundary

Phase 11 uses a bounded loop:

```text
run build/tests/benchmarks/analyzers/smoke checks
  -> record failures and variance
  -> investigate root causes
  -> make only narrowly scoped justified corrections
  -> rerun affected verification
  -> compare against Phase 9 and Phase 10 evidence
  -> repeat until practical stopping criteria are satisfied
```

Completion does not mean mathematically optimal code. It means required verification passes or accepted exceptions are documented, approved findings have dispositions, regressions are explained, architecture boundaries remain intact, and remaining limitations and theoretical opportunities no longer justify additional scope, risk, or audit time.

## Shared Closeout Boundary

Use the shared workflow in `refactor_master_plan.md`:

1. Complete approved work.
2. Run required build, test, benchmark, analyzer, and smoke verification.
3. Complete reviewer and correction passes.
4. Report changed files, completed/deferred work, risks, and verification results.
5. Stop for manual developer inspection and approval before changelog or migration-map work.
6. After approval, update `docs/CHANGELOG.md` unless the owner instructs otherwise.
7. The owner manually commits and pushes accepted source and changelog changes to `origin`.
8. Update the migration map only when explicitly requested and after its prerequisites are met.
9. Stop on a changelog/Git-diff mismatch.

Documentation-only planning or audit work is excluded from the code-only migration map under the current policy.

## Prompt Template 1: Performance Audit And Report Generation

```text
Perform Phase 9, Performance Audit And Report Generation, from `docs/refactor/refactor_master_plan.md` for CalradiaForge.

This is an audit-and-report task. Do not modify production code and do not transition into optimization implementation.

Before editing:
- Read the Phase 9 section in `docs/refactor/refactor_master_plan.md`.
- Read `docs/refactor/performance_audit_and_optimization_policy.md` and `docs/refactor/testing_strategy.md`.
- Read `docs/refactor/dependency_injection_plan.md` and verify the one-collection, one-provider, Core/UI registration, startup-coordinator/deferred-`MainWindow`, retained-page lifetime, Serilog, and disposal rules.
- For logger-related findings, inspect the current source under `source/CalradiaForge.Core/Infra/Logging/`; distinguish current `Create()`/non-retaining behavior from planned Phase 6.B ownership.
- Read every relevant supporting document in `docs/refactor` and `docs/Architecture`.
- Read the completed test and benchmark infrastructure and the finalized source files in scope.
- Locate the exact dated report path selected for this phase.
- Locate SevenZipWrapper benchmark results and their methodology/environment metadata, or record them as unavailable/incomparable.

Shared workflow:
- Follow the Subagent Workflow, global constraints, verification rules, and Shared Implementation Phase Closeout Workflow in `refactor_master_plan.md`.
- Keep subagent scopes non-overlapping. Suitable scopes include startup/composition, filesystem/scanning/parsing, archive/extraction, persistence/caching, logging/async behavior, allocation analysis, and benchmark methodology.
- After audit subagents finish, use a reviewer subagent to validate findings, classifications, comparability, report completeness, and the no-production-code-edit boundary.

Audit requirements:
- Record environment, build configuration, commit, branch, runtime, benchmark/analyzer versions, fixture identity, and cache/filesystem conditions.
- Build and run correctness tests before drawing performance conclusions.
- Run approved Release benchmarks with warmup and repeated iterations; capture allocations where supported.
- Inspect startup, one-provider DI composition, singleton identity, deferred `MainWindow` and retained-page construction, lifetime/disposal, Serilog construction, filesystem, scanning, parsing, persistence, extraction, logging, async/concurrency, UI-sensitive workflows, and memory behavior where applicable.
- For logging, inspect factory invocation count, active-sink startup, startup-only archival/fixed-retention scaling, neutral formatter cost, async backpressure, provider-owned flush/close, shutdown races, file-size behavior, and allocations. Performance evidence does not define credential ownership or future export policy.
- Classify every finding, assign priority/confidence, and assign a stable `PERF-NNN` ID.
- Preserve theoretical, maintainability-only, rejected, deferred, out-of-scope, and needs-more-evidence findings.

SevenZipWrapper:
- Use a controlled same-harness A/B comparison when possible.
- Require comparable archive, format, sizes, destination, machine, runtime, build, version, warmup, iterations, cache, and instrumentation conditions.
- Do not subtract unrelated historical values.
- Report total duration, library baseline, estimated orchestration overhead, allocations, setup/cleanup, validation, metadata, destination preparation, and post-extraction parsing/scanning separately where measurable.

Production boundary:
- Do not edit production source code.
- Do not weaken validation, archive safety, persistence safety, logging, cancellation, cleanup, or architecture boundaries.
- Add or adjust benchmark/test code only when explicitly needed to gather audit evidence.
- Do not silently transition into Phase 10.

Completion:
- Complete the exact report, raw-result cross-check, reviewer pass, and verification summary.
- Mark proposed production changes `Pending Developer Review` unless explicitly decided by the owner.
- Stop for manual developer review and wait for finding-specific decisions.
- Do not perform changelog or migration-map work for documentation-only audit work unless the owner explicitly instructs otherwise.
```

## Prompt Template 2: Performance Optimization Implementation

```text
Implement Phase 10, Approved Performance Optimization Implementation, from `docs/refactor/refactor_master_plan.md` for CalradiaForge.

Implement only performance findings explicitly approved by the developer.

Before editing:
- Read the Phase 10 section in `docs/refactor/refactor_master_plan.md`.
- Read this policy, `docs/refactor/testing_strategy.md`, and `docs/refactor/dependency_injection_plan.md`.
- For logger-related findings, inspect the current logger source under `source/CalradiaForge.Core/Infra/Logging/`; formatter behavior is a presentation concern and credential ownership remains with callers and Nexus components.
- Read the exact approved dated performance audit report and the developer's finding-specific decisions.
- Read every supporting refactor and architecture document governing the approved source areas.
- Read all required source, test, benchmark, fixture, and configuration files.

Approved scope:
- Create an explicit implementation list of approved `PERF-NNN` IDs before editing.
- Do not implement pending, rejected, deferred, needs-more-evidence, out-of-scope, or theoretical-only findings unless separately approved.
- If approval is modified or ambiguous, preserve the modified scope or leave the finding unimplemented and report the ambiguity.

Shared workflow:
- Follow the Subagent Workflow, global constraints, verification rules, and Shared Implementation Phase Closeout Workflow in `refactor_master_plan.md`.
- Keep assignments small, cohesive, and non-overlapping.
- After implementation subagents finish, use a reviewer subagent to compare changes against approved IDs, decisions, architecture, safety policies, tests, benchmark methodology, and behavior-preservation requirements.
- Repeat reviewer/fix passes until no required work remains or remaining items are explicitly blocked, deferred, reverted, or require owner approval.

Implementation rules:
- Preserve externally observable behavior unless separately approved.
- Preserve UI/Core/Nexus layering, one collection, one validated provider, Core/UI registration ownership, startup-coordinator/deferred-shell behavior, retained pages, provider ownership, and Serilog singleton identity.
- Preserve one global DI-owned logger, provider disposal as the sole normal close, no post-disposal logging, fixed startup-selected level, shutdown leaving `Latest`, next-startup archival with no suffix/no overwrite, and fixed seven-day retention.
- Keep ModInstaller and ModExtractor authoritative.
- Do not weaken archive containment, module validation, persistence safety, logging, cancellation, cleanup, or diagnostics.
- Do not introduce unsafe concurrency, stale caches, undocumented assumptions, or complexity without measured benefit.
- Implement in small batches so attribution remains possible.

Verification:
- Add or update regression tests for affected behavior.
- Run affected tests and Release benchmarks after each batch where practical.
- Compare against Phase 9 under comparable archive, dataset, machine, runtime, build, warmup, iteration, cache, and destination conditions.
- Capture allocations and variance where relevant.
- Record `Verified Improved`, `Verified No Material Change`, `Regressed`, `Inconclusive`, `Reverted`, or `Blocked` honestly.
- Revert or recommend reverting changes that worsen performance, regress behavior, weaken safety, or add unjustified maintenance complexity.
- If a command cannot run, explain why and do not fabricate a result.

Closeout:
- Update the same audit report with implementation summaries, tests, benchmarks, allocations, final statuses, and limitations.
- Stop for manual developer inspection after implementation, verification, report updates, and reviewer/fix passes.
- Do not proceed to changelog or migration-map work until the developer accepts the source changes or explicitly instructs continuation.
- After acceptance, follow the shared changelog, owner commit/push, and explicit migration-map workflow boundaries.
```

## Phase 9 Measurement Constraints and Open Choices

When measuring later work, treat 6.A as coordinator/snapshot/quiescence behavior, 6.B as DI/lifecycle/logger-construction behavior, and 6.C as caller-migration behavior. No performance exercise changes their locked ownership or file policy.

### Confirmed constraints

- xUnit in `source/CalradiaForge.Tests` and BenchmarkDotNet in `source/CalradiaForge.Benchmarks` are the approved Phase 4 frameworks and project paths.
- Local raw benchmark output is stored under the ignored `source/CalradiaForge.Benchmarks/BenchmarkDotNet.Artifacts` path; promotion of reviewed evidence into a durable audit artifact remains a Phase 9 decision.

### Open choices

- Additional analyzer/tool selection remains open and requires approval.
- Existing SevenZipWrapper benchmark source, environment, and methodology must be identified before historical values are used for subtraction.
- Baseline retention and CI blocking policy remain undefined; informational results are the default.
- Exact UI manual responsiveness method remains environment-sensitive.
