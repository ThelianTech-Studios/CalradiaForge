# CalradiaForge Technical Onboarding

CalradiaForge is a Windows WPF launcher for Mount & Blade II: Bannerlord. The
current solution combines a UI host, a WPF-free Core library, a reserved Nexus
assembly, a console language-resource utility, automated tests, and Core
benchmarks.

## Current shape

The product is in an open-beta development line. The accepted source baseline
uses `.NET 10`; the UI targets `net10.0-windows7.0`, while Core, Nexus,
ConsoleUtils, and benchmarks target `net10.0`. The shared version source is
`source/Directory.Build.props`, currently producing `0.14.0-beta` informational
metadata. This document does not claim packaged-runtime or manual WPF/Steam
acceptance.

Start with [Solution Architecture](../01_Architecture/Solution_Architecture.md)
and [Dependency Direction](../01_Architecture/Dependency_Direction.md), then
follow the relevant capability and system links:

- Product behavior: [Application](../04_Application/README.md)
- Durable data: [Data](../03_Data/README.md)
- Presentation: [UI](../05_UI/README.md)
- Engineering and evidence: [Development](../06_Development/README.md)
- Security boundaries: [Security](../07_Security/README.md)
- Release records: [Releases](../08_Releases/README.md)

## Reader routing

| Reader | Start here |
| --- | --- |
| Maintainer | [Architecture](../01_Architecture/README.md), [Systems](../02_Systems/README.md), [Engineering](../06_Development/Engineering/README.md) |
| Contributor | [Contribution Workflow](../06_Development/Engineering/Contribution_Workflow/README.md), [Testing](../06_Development/Engineering/Testing/README.md) |
| Reviewer | [Authority](../DOCUMENT_AUTHORITY.md), [Reviews](../Reviews/README.md), then the affected canonical domain |
| Auditor | [Document Map](../DOCUMENT_MAP.md), [Security](../07_Security/README.md), and the applicable review evidence |
| AI agent | [Engineering](../06_Development/Engineering/README.md), [AI Agents boundary](../06_Development/AI_Agents/README.md), and [Authority](../DOCUMENT_AUTHORITY.md) |

## Boundaries

UI decides when and how to present an operation. Core decides how the runtime
operation works and remains free of WPF references. Nexus is optional and
reserved for future Nexus networking; it must not be described as an implemented
download or authentication feature without source evidence. Tests and
benchmarks are development evidence, not runtime systems.

See [Scope and Boundaries](Scope_and_Boundaries.md), [Terminology](Terminology.md),
and [Project Status](Project_Status.md) for the maintained summaries.
