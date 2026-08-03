# CalradiaForge Benchmarks

This project contains developer-only performance infrastructure. Phase 4 results are historical infrastructure validation and provisional pre-refactor baselines. Phase 9 captures the authoritative post-Phase-8 optimization-decision baseline; results remain informational and machine-specific.

Core benchmarks belong under `Core.Benchmarks`. The `Nexus.Benchmarks` and `UI.Benchmarks` folders are reserved for their owning projects and later phases.

## Run

From the repository root, run the authoritative Phase 9 baseline in Release configuration:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File source/CalradiaForge.Benchmarks/run-phase9-benchmarks.ps1
```

Run one benchmark group while developing or verifying the harness:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File source/CalradiaForge.Benchmarks/run-phase9-benchmarks.ps1 -Filter '*ModpackValidationBenchmarks*'
```

The process-scoped execution-policy option is useful on Windows machines that block local unsigned scripts; it does not change the machine or user execution policy. The outer benchmark project uses the existing restored package graph, while BenchmarkDotNet may restore its generated harness project. Run `dotnet restore source/CalradiaForge.slnx` explicitly when dependencies are not already restored, and ensure NuGet access is available for a first generated-harness run.

The standard runner selects only deterministic `AuthoritativePostPhase8Baseline` cases. It cannot execute the real-installation suite accidentally.

An owner may deliberately run the local supplemental suite:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File source/CalradiaForge.Benchmarks/run-phase9-real-installation-benchmarks.ps1 -ConsentToReadRealInstallation
```

That runner discovers the local Steam installation without printing or accepting paths on the command line. It reads `SubModule.xml` metadata and directory structure in place, uses temporary configuration, and writes only to BenchmarkDotNet build/temp locations and the ignored `phase9-real-installation` artifact directory. It never copies, deletes, renames, or writes installation content. A descriptor-content fingerprint must remain unchanged, and exact game, Workshop, repository, and user-profile literals are redacted from text artifacts and checked before results are accepted. Do not invoke this runner from CI.

The Phase 9 runner writes BenchmarkDotNet raw results and `phase9-environment.txt` beneath `source/CalradiaForge.Benchmarks/BenchmarkDotNet.Artifacts/phase9-baseline`. That directory is intentionally ignored because raw results are machine- and working-tree-specific. The dated Phase 9 audit report retains reviewed summary evidence, environment, fixture, and comparability metadata. Phase 10 and Phase 11 use the same baseline methodology for approved comparisons.

The historical `run-phase4-benchmarks.ps1` marker refuses to run against current source because doing so would mislabel current measurements as provisional Phase 4 evidence. Original Phase 4 artifacts belong to commit `580f3a4e19e383fd701a86b75953a39b69832aad`; inspect that commit separately rather than treating current output as a reproduction.

BenchmarkDotNet supplies warmup, repeated short-run iterations, distribution statistics, environment/runtime details, and allocation reporting through `MemoryDiagnoser`. Filesystem setup and cleanup use isolated directories under the operating-system temporary path and remain outside the measured methods.

## Current fixture groups

- Parser component cases with 0, 10, and 100 dependencies.
- Modpack validation component cases with 10, 100, and 500 entries.
- Mod-cache persistence cases with 10, 100, and 500 modules.
- End-to-end module scanning cases with 10 and 100 fake module directories.

Deterministic benchmarks never read real Steam, Bannerlord, application configuration, Nexus credentials, or network resources. The owner-approved supplemental suite reads only local game/Workshop metadata and does not load the live application configuration, launch WPF, use the production logger, or contact the network. Real results are warm-cache, machine/install-specific evidence and do not measure live UI responsiveness. No result is a blocking threshold.
