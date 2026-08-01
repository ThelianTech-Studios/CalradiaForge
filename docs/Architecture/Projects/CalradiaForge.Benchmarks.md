# CalradiaForge.Benchmarks

## Project Overview

`CalradiaForge.Benchmarks` is the owner-approved BenchmarkDotNet performance-infrastructure project. It is developer-only and does not ship with the WPF application.

## Currently Implemented

- `Core.Benchmarks` contains parser, modpack validation, mod-cache persistence, and module-scanner cases.
- `Nexus.Benchmarks` and `UI.Benchmarks` are reserved for their owning projects and later phases.
- Benchmarks use Release short-run jobs, warmup, repeated iterations, generated fixtures, environment reporting, and managed-allocation reporting.
- `run-phase9-benchmarks.ps1` captures the authoritative post-Phase-8 optimization-decision baseline and records repository/source state plus cache limitations. Results remain informational and machine-specific.
- `run-phase9-real-installation-benchmarks.ps1` is a separate consent-gated, owner-approved supplement. It reads local and Workshop module metadata in place, invokes only Core parser/scanner APIs, writes only temporary configuration and ignored artifacts, verifies corpus stability, and redacts literal paths.
- The historical Phase 4 script is a non-runnable marker that prevents current measurements from being mislabeled as provisional Phase 4 evidence; original artifacts remain tied to commit `580f3a4e19e383fd701a86b75953a39b69832aad`.

## Dependencies

- BenchmarkDotNet package set selected in the project file.
- `CalradiaForge.Core`.

## Design Constraints

- Use isolated generated fixtures and keep setup/cleanup outside measured component methods.
- Deterministic benchmarks and CI do not use real user, Steam, Bannerlord, Nexus, credential, or network data. The explicit local supplement is the sole exception and never becomes a CI dependency or portable baseline.
- Standard and supplemental runners select disjoint BenchmarkDotNet categories. The supplemental suite must not invoke detection persistence, `ModPipelineManager`, WPF/application startup, production logging, Nexus, or network workflows.
- Do not copy proprietary installation content. Keep raw output ignored and promote only aggregate, path-redacted evidence.
- Keep results informational; no single-machine threshold is blocking.
- Do not weaken behavior, safety, diagnostics, or ownership boundaries to improve measurements.
