# Dependency Direction

The current project references are:

| Project | References | Constraint |
| --- | --- | --- |
| `CalradiaForge.UI` | Core | UI may compose and present Core behavior. |
| `CalradiaForge.Core` | None in the solution | Core remains WPF-free. |
| `CalradiaForge.Nexus` | Core | Nexus networking and transport stay in Nexus. |
| `CalradiaForge.ConsoleUtils` | Core | Utility code may use Core localization/path services. |
| `CalradiaForge.Tests` | Core, UI | Tests may replace dependencies through seams but do not redefine ownership. |
| `CalradiaForge.Benchmarks` | Core | Benchmarks measure Core contracts and must not mutate real installations without explicit consent. |

Do not move Nexus networking into Core, add WPF references to Core, bypass
`ModInstaller` or `ModExtractor`, store credentials in `AppConfig`, or put
application behavior into tests or benchmarks.

For the composition root and lifetime order, see
[Composition and Lifecycle](Composition_and_Lifecycle.md). For detailed
capability ownership, see [Systems](../02_Systems/README.md).
