> Historical benchmark evidence migrated from the preserved documentation snapshot on 2026-08-02. This report retains its original scope, baseline, and limitations; it is not authoritative for current implementation behavior.

# CalradiaForge Performance Audit

Status: In Review - generated and owner-approved real-installation evidence complete; developer decisions required before Phase 10

Audit date: 2026-08-01

Audit commit: `ab7d6df8f25c8e953b16547f208d55d638e6b75d`

Audit branch: `dev-V0-14-CodeRefactor`

Report owner: Codex

## Audit Scope

This canonical combined Phase 9 audit captures the authoritative generated-fixture post-Phase-8 baseline and the owner-approved supplemental real-installation evidence, reviews performance-sensitive Core/UI workflows, verifies composition and logger lifecycle boundaries, classifies findings, and stops for developer decisions. Production application code was not changed.

The audited commit differs from the Phase 8 source endpoint only by the owner's region-only `TranslationStrings` commit and combined changelog/migration-map closeout commit. At benchmark capture, production application source was clean; the repository and benchmark harness were modified by this Phase 9 audit work.

## Excluded Scope

- Production optimizations, observable behavior changes, and automatic transition to Phase 10.
- New benchmark/analyzer packages, blocking thresholds, large copied fixtures, Nexus credentials/networking, or UI automation. The separately consented local suite reads only installed module metadata in place.
- A controlled SevenZipWrapper A/B measurement without an owner-approved representative archive fixture.
- Major/minor version changes, changelog work, and migration-map work.
- Claims that static inspection or automated tests prove interactive WPF responsiveness.

## Environment

| Item | Recorded value |
|---|---|
| OS | Windows 11 `10.0.26200.8894`, x64 |
| CPU | AMD Ryzen 7 5800X, 8 physical / 16 logical cores, 3.80 GHz |
| .NET SDK | `10.0.301` |
| Benchmark runtime | .NET `10.0.9`, X64 RyuJIT AVX2 |
| Benchmark framework | BenchmarkDotNet `0.15.2` |
| Power plan | BenchmarkDotNet selected High performance for the run |
| Temporary-fixture drive | `C:` fixed NTFS, approximately 1.86 TB total / 1.34 TB free at capture |
| Real-installation storage | Local Modules, Workshop, and repository on the same fixed NTFS volume; exact volume/path redacted |
| Physical memory | Not recorded: the available system-information query was denied in this environment |
| Antivirus/interference | Not controlled or independently measured |
| Repository state | Modified by Phase 9 benchmark-support/report work; production source clean |

## Build Configuration

- Correctness builds and analyzers used Release or the explicitly required Debug configuration.
- BenchmarkDotNet used `ShortRun-.NET 10.0`: one launch, three warmups, and three measurement iterations.
- The initial normal Debug solution build could not replace a loaded UI output DLL. Verification therefore used isolated artifacts under the operating-system temporary directory, without terminating the owner's process.
- Results are informational and machine-specific. ShortRun confidence intervals are especially wide for filesystem writes.

## Benchmark Methodology

The finalized command was:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File source/CalradiaForge.Benchmarks/run-phase9-benchmarks.ps1 -ArtifactsPath source/CalradiaForge.Benchmarks/BenchmarkDotNet.Artifacts/phase9-2026-08-01
```

The generated harness required a network-enabled restore after the sandbox reproduced the known NuGet TLS/authentication failure. Fixture construction and cleanup occur outside measured methods. BenchmarkDotNet performs repeated warm measurements; no controlled filesystem-cache eviction was performed. Managed allocations come from `MemoryDiagnoser`. The real-installation suite used one launch, three warmups, and seven measurement iterations.

Raw artifacts remain ignored under `source/CalradiaForge.Benchmarks/BenchmarkDotNet.Artifacts/phase9-2026-08-01`. This report retains the reviewed results required for later comparison.

## Fixture And Dataset Inventory

| Benchmark | Parameters | Fixture |
|---|---|---|
| Module XML parsing | 0, 10, 100 dependencies | Generated `SubModule.xml` in an isolated temporary module directory |
| Modpack validation | 10, 100, 500 entries | In-memory modpack; half of module IDs installed |
| Module scanning | 10, 100 modules | Generated standalone game/Modules tree with one XML file per module |
| Mod-cache persistence | 10, 100, 500 modules | Generated module list and isolated current/backup cache files |

The authoritative fixtures do not read user configuration, Steam, Bannerlord, Nexus, credentials, or network data. The separately invoked owner-approved supplement reads only local and Workshop module metadata under the controls recorded below.

## Existing Baselines

Artifacts from commit `580f3a4e19e383fd701a86b75953a39b69832aad` are labeled `Infrastructure validation / Provisional pre-refactor baseline`, were captured from a modified working tree, and explicitly state they are not comparable to the final baseline. They are historical context only and are not used to claim an improvement or regression.

## Executive Summary

All 14 approved benchmark cases completed. Parser, validation, and unique-module scanner cases scale approximately with their input sizes in the tested range. Mod-cache reads also scale with serialized module count. Atomic mod-cache writes show high filesystem variance under the three-iteration ShortRun job, so their means are not treated as universal latency targets.

No result demonstrates a Critical or High performance defect. The supplemental warm real-installation scan completed in approximately 18 ms for the installed local-plus-Workshop corpus, but this is not a live WPF/navigation measurement and does not change any finding classification. Six Medium-priority findings remain strongly supported by source/workflow evidence, and four lower-confidence candidates require measurement before any production change. Composition, singleton identity, logger construction, and provider-owned disposal match accepted architecture and produced no performance finding.

## Authoritative Phase 9 Baseline

### Modpack validation

| Entries | Mean | StdDev | Allocated |
|---:|---:|---:|---:|
| 10 | 569.4 ns | 3.16 ns | 872 B |
| 100 | 4.652 us | 0.112 us | 5.69 KB |
| 500 | 21.900 us | 1.010 us | 24.15 KB |

### Module XML parsing

| Dependencies | Mean | StdDev | Allocated |
|---:|---:|---:|---:|
| 0 | 66.99 us | 3.204 us | 5.59 KB |
| 10 | 74.99 us | 5.001 us | 13.92 KB |
| 100 | 126.20 us | 7.360 us | 66.39 KB |

### Module scanning

| Modules | Mean | StdDev | Allocated |
|---:|---:|---:|---:|
| 10 | 1.255 ms | 0.0349 ms | 70.39 KB |
| 100 | 13.453 ms | 0.9127 ms | 677.83 KB |

### Mod-cache persistence

| Method | Modules | Mean | Median | StdDev | Allocated |
|---|---:|---:|---:|---:|---:|
| Load | 10 | 124.3 us | 122.8 us | 2.89 us | 29.18 KB |
| Save | 10 | 6.688 ms | 6.625 ms | 0.111 ms | 24.89 KB |
| Load | 100 | 228.4 us | 228.5 us | 5.44 us | 161.22 KB |
| Save | 100 | 9.535 ms | 7.880 ms | 3.215 ms | 126.99 KB |
| Load | 500 | 923.5 us | 902.5 us | 45.61 us | 689.25 KB |
| Save | 500 | 13.636 ms | 10.707 ms | 5.101 ms | 523.96 KB |

## Owner-Approved Real-Installation Supplemental Evidence

Detailed methodology, privacy checks, dataset inventory, and interpretation are retained in the [real-installation benchmark supplement](Real_Installation_Benchmark_2026-08-01.md). It is subordinate to this canonical combined audit.

The sanitized dataset contained 134 local top-level module directories and 6 Workshop items, with 141 `SubModule.xml` files totaling 268,343 bytes. The full local tree contained 19,618 files/103.56 GB; the Workshop tree contained 128 files/56.37 MB. Exact roots and identifying module information are not retained.

### Real parser results

| Workload | Mean | StdDev | Allocated |
|---|---:|---:|---:|
| Local corpus | 10.772 ms | 0.306 ms | 2,479.28 KB |
| Workshop corpus | 0.448 ms | 0.012 ms | 105.25 KB |
| Combined corpus | 11.187 ms | 0.632 ms | 2,584.76 KB |

### Real scanner results

| Workload | Mean | StdDev | Allocated |
|---|---:|---:|---:|
| Local Modules | 16.643 ms | 1.271 ms | 2,600.45 KB |
| Workshop plus empty synthetic local root | 0.911 ms | 0.113 ms | 112.73 KB |
| Local Modules plus Workshop | 17.951 ms | 1.592 ms | 2,710.78 KB |

One high Workshop-parser value (0.478 ms) was removed by BenchmarkDotNet's outlier analysis. Scanner cases were measured independently and their wide confidence intervals overlap, so subtracting their means is not a reliable isolated Workshop cost. These offline warm-cache measurements exclude WPF, navigation, UI-thread work, production logging, platform detection, pipeline cache commit, accepted-snapshot publication, and rendering. They are informational evidence for this owner installation, not a universal baseline or live responsiveness result.

## Findings Summary

| ID | Title | Classification | Priority | Confidence | Developer decision | Implementation status |
|---|---|---|---|---|---|---|
| PERF-001 | Navigation activation triggers full module scans | Strongly supported likely inefficiency | Medium | Medium | Pending Developer Review | Not Started |
| PERF-002 | Full modpack reloads occur on UI navigation and mutations | Strongly supported likely inefficiency | Medium | Medium | Pending Developer Review | Not Started |
| PERF-003 | Launcher reconciliation uses nested lookup and notification-heavy rebuilding | Strongly supported likely inefficiency | Medium | Medium | Pending Developer Review | Not Started |
| PERF-004 | One detection result performs repeated complete configuration writes | Strongly supported likely inefficiency | Medium | Medium | Pending Developer Review | Not Started |
| PERF-005 | Accepted scan commits deserialize and reserialize the old cache | Strongly supported likely inefficiency | Medium | Medium | Pending Developer Review | Not Started |
| PERF-006 | Archive installation performs duplicate metadata passes | Strongly supported likely inefficiency | Medium | Medium | Pending Developer Review | Not Started |
| PERF-007 | Duplicate scanner reporting performs a linear key search | Low-confidence concern requiring measurement | Low | Low | Pending Developer Review | Not Started |
| PERF-008 | Per-file extraction progress may add callback/allocation pressure | Low-confidence concern requiring measurement | Medium | Low | Pending Developer Review | Not Started |
| PERF-009 | Log retention materializes the complete archive enumeration | Low-confidence concern requiring measurement | Low | Low | Pending Developer Review | Not Started |
| PERF-010 | Neutral log formatting allocates a builder and final string per event | Low-confidence concern requiring measurement | Low | Low | Pending Developer Review | Not Started |

## Detailed Findings

### PERF-001 - Navigation activation triggers full module scans

- Status: Proposed.
- Classification: Strongly supported likely inefficiency.
- Priority: Medium.
- Confidence: Medium.
- Affected files: `MainWindow.xaml.cs`, `LauncherViewModel.cs`, `ModpacksViewModel.cs`.
- Affected workflow: Retained-page navigation and reactivation.
- Current behavior: Same-target selection performs a full lifecycle cycle. Launcher scans on each return after its first activation; Mod Packs scans on every activation, including its first visit.
- Suspected cost: Repeated filesystem enumeration, XML parsing, cache reconciliation, and queued lifecycle work during ordinary navigation.
- Evidence: Deterministic tests prove the scan call counts and same-target reactivation policy. The generated 100-module scan is 13.453 ms/677.83 KB; the owner-installation warm combined scan is 17.951 ms/2,710.78 KB. Both exclude pipeline/UI work.
- Baseline result: Generated and supplemental real-installation scanner components above; no live UI-navigation trace exists.
- Suspected cause: Activation currently treats navigation freshness as requiring a complete refresh.
- Proposed optimization: Pending review; evaluate explicit freshness/version rules, same-target suppression, or latest-navigation coalescing without weakening explicit refresh semantics.
- Expected benefit: Fewer redundant scans and less navigation latency.
- Implementation complexity: Medium.
- Correctness risk: Medium; accepted snapshots, external changes, and activation ordering must remain correct.
- Maintainability impact: Positive only if freshness ownership remains explicit.
- Verification plan: Measure initial, return, same-target, and rapid navigation with 10/100/500 modules; capture scans, latency, allocations, queueing, and cold/warm conditions.
- Developer decision: Pending Developer Review.
- Developer notes: None recorded.
- Implementation status: Not Started.
- Implementation summary: None.
- Final benchmark result: Not available.
- Final disposition: Pending.

### PERF-002 - Full modpack reloads occur on UI navigation and mutations

- Status: Proposed.
- Classification: Strongly supported likely inefficiency.
- Priority: Medium.
- Confidence: Medium.
- Affected files: `LauncherViewModel.cs`, `ModpacksViewModel.cs`, `ModpackService.cs`, `ModpackData.cs`.
- Affected workflow: Launcher/Mod Packs activation and modpack save/create/import.
- Current behavior: Navigation refreshes synchronously enumerate, read, and deserialize every named modpack; successful single-file mutations also reload the entire directory.
- Suspected cost: UI-thread stalls and filesystem/allocation growth with stored modpack count and size.
- Evidence: Startup already loads modpacks, while first and later navigation plus mutations invoke `Refresh`/`LoadAllModpacks` again.
- Baseline result: No approved modpack CRUD/navigation benchmark exists.
- Suspected cause: Disk-authoritative full refresh is reused for both explicit external refresh and owner-controlled mutations.
- Proposed optimization: Pending review; measure an async/off-dispatch load or affected-entry update with explicit freshness rules.
- Expected benefit: Reduced navigation and mutation latency for large modpack directories.
- Implementation complexity: Medium.
- Correctness risk: Medium; external file changes, normalization, selection, and list identity must remain correct.
- Maintainability impact: Neutral to positive if freshness rules remain narrow.
- Verification plan: Benchmark 1/10/100/500 modpack files under cold/warm conditions; count reads and measure UI-blocked time, elapsed time, and allocations.
- Developer decision: Pending Developer Review.
- Developer notes: None recorded.
- Implementation status: Not Started.
- Implementation summary: None.
- Final benchmark result: Not available.
- Final disposition: Pending.

### PERF-003 - Launcher reconciliation uses nested lookup and notification-heavy rebuilding

- Status: Proposed.
- Classification: Strongly supported likely inefficiency.
- Priority: Medium.
- Confidence: Medium.
- Affected file: `LauncherViewModel.cs`.
- Affected workflow: Accepted-snapshot and selected-modpack presentation reconciliation.
- Current behavior: Collections are cleared/repopulated item-by-item, each valid load-order entry performs `FirstOrDefault` over installed modules, and views are explicitly refreshed after individual notifications.
- Suspected cost: `O(valid entries x modules)` lookup plus repeated WPF collection/filter notifications and allocations.
- Evidence: Source inspection establishes the algorithm and notification sequence; no UI-aware measurement exists.
- Baseline result: The Core validation baseline is linear and fast, but it does not include presentation reconciliation.
- Suspected cause: Stable collection identity is preserved using incremental operations without a lookup index or deferred refresh.
- Proposed optimization: Pending review; benchmark a case-insensitive module index and deferred/batched view refresh while preserving stable bound collections.
- Expected benefit: Lower reconciliation time and UI notification pressure for large lists.
- Implementation complexity: Medium.
- Correctness risk: Medium; selection, filtering, ordering, and collection identity must remain unchanged.
- Maintainability impact: Potentially positive if the index is operation-local and clear.
- Verification plan: Measure 10/100/500/1,000 modules and proportional load orders; count notifications/filter calls and allocations.
- Developer decision: Pending Developer Review.
- Developer notes: None recorded.
- Implementation status: Not Started.
- Implementation summary: None.
- Final benchmark result: Not available.
- Final disposition: Pending.

### PERF-004 - One detection result performs repeated complete configuration writes

- Status: Proposed.
- Classification: Strongly supported likely inefficiency.
- Priority: Medium.
- Confidence: Medium.
- Affected files: `ConfigFileManager.cs`, `AppSettings.cs`, `GamePlatformDetectionResolver.cs`, `GameDetectionService.cs`.
- Affected workflow: Automatic detection, re-detection, and manual configuration commit.
- Current behavior: Each changed typed property serializes and atomically persists the complete configuration dictionary. One logical result may update game, launcher, Workshop, BLSE, and provider values separately.
- Suspected cost: Multiple serialization, flush, temporary-write, and replace cycles for one logical commit.
- Evidence: Source-level call and persistence ownership are deterministic; physical write count/latency is not yet instrumented.
- Baseline result: No configuration-commit benchmark exists.
- Suspected cause: Immediate per-property persistence is used for multi-property domain commits.
- Proposed optimization: Pending review; consider one narrow detection-result commit that persists once and emits required notifications. Do not introduce a generic transaction/deferred-save framework.
- Expected benefit: Fewer durable writes during detection/configuration.
- Implementation complexity: Medium.
- Correctness risk: Medium; all-or-nothing accepted state, immediate persistence, and notifications must remain correct.
- Maintainability impact: Positive only with domain-specific ownership.
- Verification plan: Instrument physical save counts and benchmark automatic/manual success and failure fallback on identical storage; rerun all detection/config tests.
- Developer decision: Pending Developer Review.
- Developer notes: None recorded.
- Implementation status: Not Started.
- Implementation summary: None.
- Final benchmark result: Not available.
- Final disposition: Pending.

### PERF-005 - Accepted scan commits deserialize and reserialize the old cache

- Status: Proposed.
- Classification: Strongly supported likely inefficiency.
- Priority: Medium.
- Confidence: Medium.
- Affected files: `ModPipelineManager.cs`, `ModsData.cs`.
- Affected workflow: Complete scan commit and current/backup cache rotation.
- Current behavior: Rotation reads/deserializes/validates and reserializes the complete current cache to backup; saving the accepted result separately serializes and atomically writes the new current cache.
- Suspected cost: Two full JSON object-graph passes plus two durable writes per accepted commit.
- Evidence: Source inspection plus baseline cache load/save scaling. The benchmark does not yet measure complete rotate-plus-save.
- Baseline result: At 500 modules, load is 923.5 us/689.25 KB and save median is 10.707 ms/523.96 KB on this machine.
- Suspected cause: Rotation operates on reconstructed domain objects instead of a safely validated transferable representation.
- Proposed optimization: Pending review; compare safe validated transfer or a domain-owned combined commit while preserving exact rotation and publication order.
- Expected benefit: Reduced accepted-refresh I/O and allocations.
- Implementation complexity: Medium to high.
- Correctness risk: High; corrupt current data must never replace the good backup, and incomplete scans must not mutate either file.
- Maintainability impact: Neutral only if safety remains explicit.
- Verification plan: Add a 10/100/500 rotate-plus-save benchmark separating validation, backup persistence, new serialization, and flush; rerun corruption/atomicity/commit-gating tests.
- Developer decision: Pending Developer Review.
- Developer notes: None recorded.
- Implementation status: Not Started.
- Implementation summary: None.
- Final benchmark result: Not available.
- Final disposition: Pending.

### PERF-006 - Archive installation performs duplicate metadata passes

- Status: Proposed.
- Classification: Strongly supported likely inefficiency.
- Priority: Medium.
- Confidence: Medium.
- Affected files: `ModInstaller.cs`, `ModExtractor.cs`.
- Affected workflow: Archive batch estimation, validation, and extraction.
- Current behavior: File-count estimation opens and enumerates every archive before processing; extraction later reopens and enumerates entries for mandatory containment validation.
- Suspected cost: Two archive opens/entry enumerations per valid archive and delayed first extraction for large batches.
- Evidence: Deterministic source call sequence. No approved comparable archive baseline exists.
- Baseline result: Blocked.
- Suspected cause: Progress estimation and extraction safety validation use separate archive lifetimes.
- Proposed optimization: Pending review; measure whether safely reusing metadata or lazily estimating is worthwhile. Containment validation may not be removed.
- Expected benefit: Lower archive-batch startup overhead.
- Implementation complexity: Medium.
- Correctness risk: High; archive lifetime, validation, cancellation, progress, and cleanup must remain correct.
- Maintainability impact: Potentially negative without a clear ownership design.
- Verification plan: Same-archive measurement for open/count, containment enumeration, extraction, and full orchestration using an owner-approved fixture.
- Developer decision: Pending Developer Review.
- Developer notes: Representative fixture approval required.
- Implementation status: Not Started.
- Implementation summary: None.
- Final benchmark result: Not available.
- Final disposition: Pending.

### PERF-007 - Duplicate scanner reporting performs a linear key search

- Status: Proposed measurement candidate.
- Classification: Low-confidence concern requiring measurement.
- Priority: Low.
- Confidence: Low.
- Affected file: `ModScanner.cs`.
- Affected workflow: Local/Workshop duplicate-ID reporting.
- Current behavior: A failed dictionary insertion is followed by `accepted.Keys.First(...)` to recover accepted key casing.
- Suspected cost: Worst-case `O(modules x duplicates)` comparisons.
- Evidence: Source algorithm only; current scanner baseline contains unique standalone modules.
- Baseline result: Not available for duplicates.
- Suspected cause: Canonical key recovery enumerates the dictionary.
- Proposed optimization: Pending review; use the already accepted dictionary value/key without full enumeration if measurement warrants it.
- Expected benefit: Lower duplicate-heavy scan cost.
- Implementation complexity: Low.
- Correctness risk: Low to medium; local-wins and deterministic diagnostics must remain exact.
- Maintainability impact: Positive if simpler.
- Verification plan: Benchmark 0%, 50%, and 100% duplicate local/Workshop fixtures at representative scales.
- Developer decision: Pending Developer Review.
- Developer notes: None recorded.
- Implementation status: Not Started.
- Implementation summary: None.
- Final benchmark result: Not available.
- Final disposition: Pending.

### PERF-008 - Per-file extraction progress may add callback/allocation pressure

- Status: Proposed measurement candidate.
- Classification: Low-confidence concern requiring measurement.
- Priority: Medium.
- Confidence: Low.
- Affected file: `ModInstaller.cs`.
- Affected workflow: Per-entry extraction progress.
- Current behavior: Each callback captures current UTC time, creates an `ExtractionProgress`, materializes the event invocation list, and calls subscribers individually.
- Suspected cost: Allocation and dispatch pressure for archives with many small files.
- Evidence: Source inspection only.
- Baseline result: Blocked by missing approved archive fixture.
- Suspected cause: Required progress is published at native entry frequency.
- Proposed optimization: Pending review; do not throttle, batch, skip, or reorder progress without separate behavior approval.
- Expected benefit: Unknown until measured.
- Implementation complexity: Medium.
- Correctness risk: Medium; progress/cancellation and UI ordering are observable.
- Maintainability impact: Unknown.
- Verification plan: Compare no subscriber, one subscriber, and representative presenter subscription using identical archive bytes; capture callback count, allocation, duration, and update cadence.
- Developer decision: Pending Developer Review.
- Developer notes: None recorded.
- Implementation status: Not Started.
- Implementation summary: None.
- Final benchmark result: Not available.
- Final disposition: Pending.

### PERF-009 - Log retention materializes the complete archive enumeration

- Status: Proposed measurement candidate.
- Classification: Low-confidence concern requiring measurement.
- Priority: Low.
- Confidence: Low.
- Affected file: `LogFileLifecycle.cs`.
- Affected workflow: Startup archive cleanup.
- Current behavior: `Directory.EnumerateFiles(...).ToArray()` materializes all matching paths before processing retention.
- Suspected cost: One array proportional to archive count.
- Evidence: Source inspection only; fixed seven-day retention normally bounds the directory.
- Baseline result: No retention-scaling benchmark exists.
- Suspected cause: Eager materialization simplifies exception containment before per-file processing.
- Proposed optimization: Pending review; compare streaming only if large archive counts demonstrate material startup impact.
- Expected benefit: Lower peak allocation in abnormal large directories.
- Implementation complexity: Low.
- Correctness risk: Medium; enumeration and individual-file failures must remain contained.
- Maintainability impact: Neutral.
- Verification plan: Benchmark 0/10/100/1,000/10,000 mixed recent/expired files, cold/warm, with allocation reporting.
- Developer decision: Pending Developer Review.
- Developer notes: None recorded.
- Implementation status: Not Started.
- Implementation summary: None.
- Final benchmark result: Not available.
- Final disposition: Pending.

### PERF-010 - Neutral log formatting allocates a builder and final string per event

- Status: Proposed measurement candidate.
- Classification: Low-confidence concern requiring measurement.
- Priority: Low.
- Confidence: Low.
- Affected file: `SerilogTextFormatter.cs`.
- Affected workflow: Active file/debug sink event formatting.
- Current behavior: Every event creates a `StringBuilder`, renders message/properties/exception, converts to a final string, and writes the line.
- Suspected cost: Per-event managed allocation at high active logging rates.
- Evidence: Source inspection only; no logging benchmark exists.
- Baseline result: Not available.
- Suspected cause: Straightforward readable formatter implementation.
- Proposed optimization: Pending review; measure before considering direct writer formatting or safe builder reuse.
- Expected benefit: Unknown under normal CalradiaForge event rates.
- Implementation complexity: Medium.
- Correctness risk: Medium; exact tested output and structured diagnostic content must remain.
- Maintainability impact: Potentially negative if formatting becomes obscure.
- Verification plan: Benchmark ordinary, structured, enriched, and exception events with allocations; preserve formatter tests byte-for-byte where applicable.
- Developer decision: Pending Developer Review.
- Developer notes: No logging removal, redaction, or filtering is authorized.
- Implementation status: Not Started.
- Implementation summary: None.
- Final benchmark result: Not available.
- Final disposition: Pending.

## SevenZipWrapper Baseline Comparison

### Comparability Assessment

Blocked / Not directly comparable. Historical â€œapproximately 25 minutes to secondsâ€ wording has no retained archive identity, bytes, sizes, file count, machine/runtime/build, cache/antivirus state, warmup, iterations, raw output, or setup/cleanup boundaries. No legal representative archive fixture has been approved.

### Total Extraction Duration

Not measured.

### Extraction-Library Baseline

Not measured.

### Estimated CalradiaForge Orchestration Overhead

Not calculated. Subtracting historical or different-fixture values is prohibited.

### Allocation Results

Unavailable for extraction.

### Setup And Cleanup Costs

Unavailable for extraction.

### Validation Costs

Not isolated; containment and identity validation remain mandatory.

### Metadata-Processing Costs

Not isolated; PERF-006 records the duplicate-pass hypothesis.

### Destination-Preparation Costs

Not isolated.

### Post-Extraction Parsing And Scanning Costs

Not isolated.

### Limitations

A later controlled A/B requires explicit owner fixture approval, identical archive bytes/destination state, independent output validation, Release execution, repeated iterations, allocation data, and documented cache/antivirus conditions.

## Startup Performance

No end-to-end startup timing was captured because no approved repeatable WPF startup measurement method exists. Static inspection confirms the required sequential gates and deferred shell construction. Do not remove validation, detection, pipeline initialization, localization, EULA, or notification readiness merely to reduce startup time.

## Dependency Injection And Composition Performance

Static and automated inspection confirms one production `BuildServiceProvider`, one shared collection, validated scopes/build, deferred `MainWindow`, retained singleton pages/ViewModels, one logger factory invocation, and app-owned once-guarded provider disposal. No duplicate provider, lifetime leak, or performance finding was found.

## Filesystem And Path Performance

The generated scanner/cache cases and supplemental real-installation parser/scanner cases provide the evidence above. Local Steam registry/metadata discovery was used only by the consented benchmark runner; platform-detection execution and configuration writes were not measured. No cold-cache measurement was performed.

## Mod Scanning Performance

Generated standalone scanning measured 1.255 ms/70.39 KB at 10 modules and 13.453 ms/677.83 KB at 100. The installed warm corpus measured 16.643 ms/2,600.45 KB for local scanning, 0.911 ms/112.73 KB for Workshop with an empty synthetic local root, and 17.951 ms/2,710.78 KB for combined scanning. Variance prevents subtracting those independent means. Neither suite measures pipeline commit, inaccessible roots, WPF, or live UI navigation.

## Parsing Performance

Generated single-file XML parsing measured 66.99-126.20 us and 5.59-66.39 KB across 0-100 dependencies. The installed local corpus measured 10.772 ms/2,479.28 KB and the combined corpus measured 11.187 ms/2,584.76 KB. No material parser defect is claimed.

## Modpack And Load-Order Performance

Core validation measured 0.569-21.900 us across 10-500 entries and shows approximately linear growth in this fixture. Disk reload/navigation and mutation costs are not included; see PERF-002 and PERF-003.

## Extraction Performance

No comparable baseline exists. PERF-006 and PERF-008 remain evidence-gathering proposals. `ModInstaller` and `ModExtractor` remain authoritative and all containment, identity, cancellation, progress, and cleanup behavior is preserved.

## Caching And Persistence Performance

Current cache load/save baselines are recorded above. Filesystem save variance is high, so medians and distributions are required in later comparisons. PERF-005 targets the unmeasured complete rotate-plus-save workflow, not the safety mechanisms in isolation.

## Logging Performance

No logging throughput/allocation benchmark exists. The async sink uses its package defaults; no backlog saturation was reproduced. The active fileâ€™s infinite rolling configuration is accepted architecture and its future size/rolling policy remains an owner decision, not a Phase 9 defect. Provider disposal remains the sole normal flush/close path.

## Asynchronous And Concurrency Findings

No unsafe unbounded concurrency or shutdown race was found. Navigation is deliberately serialized and pipeline work remains manager-owned. PERF-001 captures possible redundant queued work; it does not authorize changing cancellation or lifecycle ordering.

## Allocation And Memory Findings

Managed allocations are included for all approved benchmarks. The largest measured case is cache load at 500 modules (689.25 KB) followed by the 100-module scan (677.83 KB) and 500-module cache save (523.96 KB). These are baselines, not independently proven problems. Physical memory and retained-heap profiling were unavailable.

## Correctness Issues Discovered During Performance Review

No new correctness defect was validated. Existing CA1416 Windows reachability warnings and the MSIL/AMD64 SevenZipWrapper reference warning remain packaging/platform evidence rather than performance findings.

## Rejected Optimizations

The audit rejects as unsupported: removing validation or atomic durability, weakening cache commit rules, suppressing diagnostics, removing progress/cancellation, bypassing `ModInstaller`/`ModExtractor`, changing logger ownership, adding unsafe concurrency, or treating the historical SevenZip claim as a subtraction baseline.

## Deferred Optimizations

No production optimization is approved or implemented in Phase 9. All ten finding IDs await an explicit developer status.

## Needs More Evidence

- Representative UI navigation and responsiveness measurements.
- Modpack directory scaling and UI-thread blocked-time evidence.
- Configuration write counts/latency and complete cache rotate-plus-save measurements.
- Duplicate-heavy local/Workshop scanning.
- Approved archive fixture and controlled SevenZipWrapper/full-pipeline A/B.
- Logging formatter, retention scaling, async backlog, active-file growth, and disposal drain measurements.
- Cold-cache protocol and antivirus/storage interference characterization.
- Live WPF navigation measurement to separate the approximately 18 ms warm scanner component from dispatcher, ViewModel, reconciliation, binding, rendering, and logging costs.

## Reverted Optimizations

None; Phase 9 performed no production optimization.

## Remaining Limitations

- BenchmarkDotNet ShortRun uses only three measurement iterations and produces wide confidence intervals for noisy filesystem writes.
- Only warm/repeated cache behavior was captured; the real corpus was warmed by inventory, fingerprinting, and scanner preflight.
- Existing approved benchmarks cover four Core areas, not startup, DI timing, UI responsiveness, logging, extraction, or full install/pipeline orchestration.
- No new packages, profiler, ETW trace, retained-heap tool, or approved DiagnosticsHub configuration was added.
- Manual WPF responsiveness and real game execution were not performed. The local Steam/Bannerlord directories were read only by the offline supplemental parser/scanner benchmark.
- Raw results are ignored and machine-specific; this report is the durable reviewed summary.

## Final Verification Results

### Build Results

- `dotnet restore source/CalradiaForge.slnx`: sandbox attempt failed with NuGet TLS/authentication errors; approved network-enabled retry passed.
- Normal Debug build: blocked by a loaded `CalradiaForge.Core.dll` in the UI output directory; no process was terminated.
- Isolated Debug solution build: passed, 0 errors and 8 existing CA1416 warnings.
- Isolated Release solution build: passed, 0 errors and 8 existing CA1416 warnings.
- A fresh Release-configured restore/build confirmed that the Debug-only `Serilog.Sinks.Debug` assembly and dependency entry are absent from Release output. Reusing assets restored under Debug can retain that conditional package, so configuration-specific restore state matters when comparing outputs.

### Test Results

- Debug: 272 passed, 0 failed, 0 skipped.
- Release: 272 passed, 0 failed, 0 skipped.
- Independent focused logging/extractor/installer/composition/shutdown slice: 35 passed, 0 failed, 0 skipped.

### Benchmark Results

- Final Phase 9 runner: passed; 14/14 cases produced measurements and no `NA` report values.
- Results and allocations are recorded in the authoritative baseline tables above.
- Consent-gated real-installation runner: passed; 6/6 parser/scanner cases produced measurements with no `NA` values.
- The real descriptor corpus was unchanged after capture, and exact input/repository/user-profile literal verification passed after local artifact redaction.
- Consent guard: the real runner exited nonzero when its explicit switch was omitted.
- Category-boundary validation: the standard runner selected only the three requested generated modpack-validation cases and did not discover or execute a real-installation case.

### Analyzer Results

- Clean non-incremental Release build with built-in analyzers: passed with 0 errors and 8 CA1416 Windows-platform reachability warnings.
- No additional analyzer package was approved or added.

### Architecture Verification

- Core WPF boundary preserved.
- UI/Core/Nexus dependency ownership preserved.
- One provider, one logger factory/shared logger, and one provider-owned normal disposal path confirmed.
- No production `Log.CloseAndFlush`, second provider, service locator, startup polling, Nexus polling, or production optimization added.

### Manual Smoke-Test Results

Not performed. This audit makes no direct WPF responsiveness claim. Owner review may supply observations or request a controlled method before assigning finding decisions.

### Practical Stopping-Criteria Assessment

Phase 9 stops here because the generated baseline, owner-approved real-installation supplement, source/architecture inspection, classified findings, limitations, and verification record are complete enough for developer decisions. Additional live-app, cold-cache, archive, or finding-specific measurement requires an approved method or later scoped work. No production change is authorized while findings remain pending.

## Developer Decision Gate

Before Phase 10, assign every `PERF-NNN` one of: `Approved`, `Approved With Modification`, `Rejected`, `Deferred`, `Needs More Evidence`, or `Out Of Scope`. Leaving any item `Pending Developer Review` does not authorize implementation.

