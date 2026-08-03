# CalradiaForge.Benchmarks

`CalradiaForge.Benchmarks` is a .NET 10 BenchmarkDotNet executable referencing
Core. It contains synthetic Core cases for parsing, scanning, modpack
validation, and mod data, plus an explicit-consent read-only real-installation
parser/scanner path. Benchmark evidence is environment-specific and must not be
presented as UI responsiveness or a universal baseline.

See [Benchmarking](../../06_Development/Engineering/Benchmarking/README.md)
and retained [benchmark reviews](../../Reviews/Benchmarks/).
