# CalradiaForge Benchmarks

This project contains developer-only performance infrastructure. Phase 4 results are informational infrastructure validation and provisional pre-refactor baselines. They are not comparable to the authoritative post-refactor baseline that Phase 8 captures after Phase 7 settles.

Core benchmarks belong under `Core.Benchmarks`. The `Nexus.Benchmarks` and `UI.Benchmarks` folders are reserved for their owning projects and later phases.

## Run

From the repository root, run all approved Phase 4 benchmarks in Release configuration:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File source/CalradiaForge.Benchmarks/run-phase4-benchmarks.ps1
```

Run one benchmark group while developing or verifying the harness:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File source/CalradiaForge.Benchmarks/run-phase4-benchmarks.ps1 -Filter '*ModpackValidationBenchmarks*'
```

The process-scoped execution-policy option is useful on Windows machines that block local unsigned scripts; it does not change the machine or user execution policy. The outer benchmark project uses the existing restored package graph, while BenchmarkDotNet may restore its generated harness project. Run `dotnet restore source/CalradiaForge.slnx` explicitly when dependencies are not already restored, and ensure NuGet access is available for a first generated-harness run.

The runner writes BenchmarkDotNet raw results and `phase4-environment.txt` to `source/CalradiaForge.Benchmarks/BenchmarkDotNet.Artifacts`. That directory is intentionally ignored because raw results are machine- and working-tree-specific. Copy intentionally retained evidence to the later Phase 8 audit artifact only after reviewing its fixture, environment, and comparability metadata.

BenchmarkDotNet supplies warmup, repeated short-run iterations, distribution statistics, environment/runtime details, and allocation reporting through `MemoryDiagnoser`. Filesystem setup and cleanup use isolated directories under the operating-system temporary path and remain outside the measured methods.

## Current fixture groups

- Parser component cases with 0, 10, and 100 dependencies.
- Modpack validation component cases with 10, 100, and 500 entries.
- Mod-cache persistence cases with 10, 100, and 500 modules.
- End-to-end module scanning cases with 10 and 100 fake module directories.

No benchmark reads real Steam, Bannerlord, application configuration, Nexus credentials, or network resources. No result is a blocking threshold.
