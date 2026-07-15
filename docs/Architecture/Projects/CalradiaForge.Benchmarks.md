# CalradiaForge.Benchmarks

## Project Overview

`CalradiaForge.Benchmarks` is the owner-approved BenchmarkDotNet performance-infrastructure project. It is developer-only and does not ship with the WPF application.

## Currently Implemented

- `Core.Benchmarks` contains parser, modpack validation, mod-cache persistence, and module-scanner cases.
- `Nexus.Benchmarks` and `UI.Benchmarks` are reserved for their owning projects and later phases.
- Benchmarks use Release short-run jobs, warmup, repeated iterations, generated fixtures, environment reporting, and managed-allocation reporting.
- The runner labels Phase 4 output as infrastructure validation or a provisional pre-refactor baseline and records that it is not comparable to the final post-refactor baseline.

## Dependencies

- BenchmarkDotNet package set selected in the project file.
- `CalradiaForge.Core`.

## Design Constraints

- Use isolated generated fixtures and keep setup/cleanup outside measured component methods.
- Do not use real user, Steam, Bannerlord, Nexus, credential, or network data.
- Keep results informational; no single-machine threshold is blocking.
- Do not weaken behavior, safety, diagnostics, or ownership boundaries to improve measurements.
