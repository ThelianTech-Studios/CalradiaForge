# Benchmarking

The benchmark project uses BenchmarkDotNet and references Core. Current
synthetic cases cover parser, scanner, modpack validation, and mods-data work.
The real-installation path is explicit-consent, offline, read-only parser/scanner
measurement with corpus and artifact safeguards.

See [Benchmark Methodology](Benchmark_Methodology.md), [Performance Reviews](../../../Reviews/Benchmarks/),
and [Compatibility limits](../../../04_Application/Compatibility/Limitations_and_Deferred_Features.md).
