# CalradiaForge.Nexus

## Project Overview
CalradiaForge.Nexus is the reserved integration boundary for Nexus Mods functionality.
At the moment, the project exists as a dedicated assembly shell and does not contain implementation code.

## Purpose
Provide a separate place for Nexus networking, authentication, download orchestration, metadata handling, and handler registration when that work is implemented.

## Responsibilities
### Currently Implemented
- The project is present in the solution as a separate assembly.
- The project currently has no source files beyond its project file.

### Planned / Locked Architecture
- Host Nexus networking outside Core.
- Keep Core as the foundation.
- Keep UI as presentation and intent only.
- Keep Nexus-specific auth, downloads, and metadata logic out of `AppConfig`.

## Public Boundaries
- No public API is currently implemented.
- Once implemented, Nexus should expose only what the UI or Core needs for Nexus-specific workflows.

## Dependencies
### Currently Implemented
- `CalradiaForge.Core`

## Major Systems
- None yet in source code.

## Design Constraints
- Nexus integration is optional.
- CalradiaForge must continue to work without Nexus.
- Do not move Core app systems into Nexus.
- Do not move Nexus networking into Core.
- Do not store credentials in `AppConfig`.
- Do not modify `ModuleModel` to store Nexus metadata.

## Known Extension Points
- Authentication and credential storage.
- NXM registration and link handling.
- Manual update checks for Nexus-linked mods.
- Download queue management and retry/resume behavior.
- Nexus metadata storage and cache cleanup.

## Deferred Work
- All Nexus runtime behavior is deferred until implementation exists.
