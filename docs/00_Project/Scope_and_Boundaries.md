# Scope and Boundaries

## In scope today

- Portable Windows WPF launcher behavior.
- Game-platform detection and manual configuration paths represented by Core.
- Module scanning, parsing, archive installation, DLL unblocking, and accepted
  module snapshots.
- Modpack persistence, import, templates, and last-used state.
- Configuration, logging, localization, EULA, notifications, and application
  lifecycle services.
- Automated Core/UI tests and Core benchmark projects.

## Explicit boundaries

| Area | Boundary |
| --- | --- |
| UI/Core | UI owns timing, presentation, and user interaction; Core owns runtime mechanics and has no WPF reference. |
| Nexus | The Nexus project is reserved/optional. Do not document REST auth, credentials, downloads, NXM handling, polling, or update checks as shipped unless current source proves them. |
| Persistence | `ConfigFileManager`, `ModsData`, `ModpackData`, and `AtomicFileWriter` own their respective storage concerns. |
| Installation | `ModPipelineManager` coordinates admission, cancellation, reconciliation, and accepted-state publication; `ModInstaller` and `ModExtractor` retain installation mechanics. |
| Evidence | Automated tests do not establish manual WPF, Steam, Bannerlord, packaged-runtime, or public-release acceptance. |
| Agent architecture | Detailed `AGENTS.md` and `.agents/skills/` redesign is deferred and is not implemented by this documentation cycle. |

## Out of scope for this documentation cycle

Product behavior changes, source refactors, test changes, CI documentation
validation, release publication, Git publication, and modification of the
preserved historical documentation snapshot are excluded.
