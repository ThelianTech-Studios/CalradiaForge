> Historical benchmark evidence migrated from the preserved documentation snapshot on 2026-08-02. This report retains its original scope, baseline, and limitations; it is not authoritative for current implementation behavior.

# CalradiaForge Real-Installation Benchmark Supplement

Status: Complete - owner-approved supplemental evidence

Capture date: 2026-08-01

Source commit: `ab7d6df8f25c8e953b16547f208d55d638e6b75d`

Branch: `dev-V0-14-CodeRefactor`

Canonical combined report: [CalradiaForge Performance Audit](Performance_Audit_2026-08-01.md)

## Consent And Boundary

The owner explicitly authorized a second Phase 9 suite to read the current Steam Bannerlord and Workshop module directories. The run was local, Release-only, offline, and read-only at the application API boundary. It called `ModParser.Parse` and `ModScanner.ScanAsync`; it did not start WPF or Bannerlord, load or mutate the live CalradiaForge configuration, invoke `ModPipelineManager`, persist a cache, use the production logger, access Nexus, or contact the network.

The deterministic generated-fixture results remain the authoritative reusable baseline. This supplement is owner-installation-specific evidence and is not a CI input, portable comparison, or blocking threshold.

## Sanitized Dataset Inventory

| Dataset | Top-level directories | Recursive directories | Files | `SubModule.xml` files | Aggregate size |
|---|---:|---:|---:|---:|---:|
| Local Modules | 134 | 3,063 | 19,618 | 135 | 103.56 GB |
| Steam Workshop | 6 | 40 | 128 | 6 | 56.37 MB |

The two measured roots were on the same fixed NTFS volume as the repository. No path, username, library name, module name, module ID, or filename from the installation is retained here. The measured XML corpus contained 141 descriptors totaling 268,343 bytes.

## Methodology

- BenchmarkDotNet `0.15.2`, .NET `10.0.9`, Release, X64 RyuJIT AVX2.
- One process launch, three warmup iterations, and seven measurement iterations.
- Managed allocations captured with `MemoryDiagnoser`.
- Paths were delivered to child processes through process-local environment variables, never BenchmarkDotNet parameters.
- Parser cases used the same direct-or-one-level-nested descriptor discovery shape as the production scanner.
- Scanner cases used temporary `AppSettings` configuration. Workshop-only timing used a benchmark-owned empty local Modules root plus the real Workshop root.
- Inventory, preflight, pilot, and warmup work warmed filesystem caches. No cold-cache result is claimed.
- A private fingerprint of descriptor-relative paths/content and recursive directory topology, plus descriptor/directory counts and byte totals, matched before and after capture. No fingerprint value or path is retained in this report.
- Text artifacts were redacted and checked for exact game, Workshop, repository, and user-profile literals. Verification passed.

## Parser Results

| Workload | Mean | StdDev | Allocated |
|---|---:|---:|---:|
| Parse local module corpus | 10.772 ms | 0.306 ms | 2,479.28 KB |
| Parse Workshop corpus | 0.448 ms | 0.012 ms | 105.25 KB |
| Parse combined corpus | 11.187 ms | 0.632 ms | 2,584.76 KB |

One high Workshop-parser value (0.478 ms) was removed by BenchmarkDotNet's configured outlier analysis. The combined mean should not be interpreted as the arithmetic sum of separately measured runs because each case has independent warmup, cache history, and sampling.

## Scanner Results

| Workload | Mean | StdDev | Allocated |
|---|---:|---:|---:|
| Scan local Modules | 16.643 ms | 1.271 ms | 2,600.45 KB |
| Scan Workshop with empty synthetic local root | 0.911 ms | 0.113 ms | 112.73 KB |
| Scan local Modules and Workshop | 17.951 ms | 1.592 ms | 2,710.78 KB |

The cases were measured independently with their own warmup, cache history, and sampling. Their wide 99.9% confidence intervals overlap, so subtracting the local-only mean from the combined mean would not provide a reliable isolated Workshop cost.

## Interpretation And Potential Skew

These numbers are closer to the owner's installed data shape than the generated fixtures, but they are still offline microbenchmarks rather than live application measurements.

Potential skews include:

- Filesystem caches were deliberately warm after inventory and preflight.
- Antivirus, indexing, Steam synchronization, mod-manager activity, and unrelated disk load were not controlled.
- BenchmarkDotNet used isolated Release child processes with a silent logger; the live application uses WPF, navigation coordination, ViewModels, DI services, and an asynchronous file logger.
- Scanner timing excludes startup gates, platform detection, cache rotation/save, accepted-snapshot publication, Launcher reconciliation, binding notifications, rendering, and toast work.
- `MemoryDiagnoser` reports managed allocations but not native filesystem, OS cache, antivirus, or storage-controller work.
- The installed module mix, dependency shape, XML sizes, nesting, game version, Workshop contents, storage, fragmentation, and background state will change over time.
- The generated-fixture scan used the operating-system temporary volume, while the real roots and repository were on another volume; synthetic and real filesystem timings are not storage-controlled comparisons.
- BenchmarkDotNet selected multiple invocations during pilot calibration. Reported means remain per operation, but repeated invocation further warms caches.

Therefore these results support a practical statement that the isolated parser/scanner operations complete in tens of milliseconds on this warm local dataset. They do not establish Launcher navigation latency, startup latency, perceived responsiveness, or live UI-thread cost.

## Privacy And Mutation Verification

- Production source state was clean at capture; only benchmark/report support was modified.
- No proprietary file was copied into the repository.
- The descriptor corpus remained stable before and after execution.
- Exact installation, Workshop, repository, and user-profile literals were absent after artifact redaction.
- This document contains aggregate counts and measurements only.

## Finding Impact

The results add real-data evidence to `PERF-001` but do not change its classification. Navigation may trigger a roughly 18 ms warm combined scanner operation on this machine, but the benchmark does not measure the live navigation queue, dispatcher, UI work, cache commit, or contention. `PERF-001` therefore remains `Strongly supported likely inefficiency`, `Medium` priority/confidence, `Pending Developer Review`, and `Not Started`.

