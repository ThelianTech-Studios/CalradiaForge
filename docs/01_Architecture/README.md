# Architecture

This domain owns structural facts: solution shape, project responsibilities,
allowed references, composition, lifecycle, and invariants. It does not own
detailed persistence, logging, mod-operation, WPF presentation, or test
procedures; those topics link to their owners.

| Document | Role | Status |
| --- | --- | --- |
| [Solution_Architecture.md](Solution_Architecture.md) | Solution and layer overview | Current |
| [Dependency_Direction.md](Dependency_Direction.md) | Allowed project references and boundary rules | Current |
| [Composition_and_Lifecycle.md](Composition_and_Lifecycle.md) | Root provider, startup, shutdown, restart, disposal | Current |
| [Projects](Projects/) | One structural record per current solution project | Current |

Current source is the authority for implementation status. Historical phase
language is retained only in [Archive](../Archive/README.md), [Plans](../Plans/README.md),
and [Reviews](../Reviews/README.md).
