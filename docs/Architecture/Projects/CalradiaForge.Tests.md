# CalradiaForge.Tests

## Project Overview

`CalradiaForge.Tests` is the xUnit correctness-test project. Its project-level folders mirror the application projects so coverage remains owned by the layer it verifies.

## Currently Implemented

- `Core.Tests` contains deterministic Core unit, regression, result, and integration-style filesystem tests.
- Fixtures use isolated operating-system temporary directories and generated XML/zip/JSON data.
- Archive and installer tests continue through `ModExtractor`, `ModInstaller`, `ModsData`, and `ModpackData` rather than replacing authoritative owners.
- `Nexus.Tests` and `UI.Tests` are reserved for later work and do not imply current Nexus or WPF automation coverage.

## Dependencies

- xUnit and the .NET test SDK package set selected in the project file.
- `CalradiaForge.Core`.

## Design Constraints

- Do not reference WPF from Core tests.
- Do not use real Steam libraries, Bannerlord installations, app configuration, Nexus credentials, or network access.
- Do not add timing assertions to correctness tests.
- Keep each test inside the folder for its owning application project.
