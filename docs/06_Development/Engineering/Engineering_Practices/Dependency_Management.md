# Dependency Management

Project references are documented in [Dependency Direction](../../../01_Architecture/Dependency_Direction.md).
Package ownership follows the project that uses it: Core owns runtime
infrastructure packages, UI owns WPF/presentation packages, tests own test
packages, and benchmarks own BenchmarkDotNet packages.

Keep package and target-framework changes separate from documentation-only work.
Security/dependency scans that cannot reach their advisory source are gaps, not
clean results.
