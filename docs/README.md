# CalradiaForge Documentation

This is the active documentation system for CalradiaForge. It is organized by
authority and reader intent rather than by the historical folder layout.

## How to use this tree

The numbered domains describe the current product and its implementation:

1. [Project](00_Project/README.md) — technical orientation, scope, status, and terminology.
2. [Architecture](01_Architecture/README.md) — solution boundaries, composition, lifecycle, and project ownership.
3. [Systems](02_Systems/README.md) — mechanisms owned by Core, UI, Nexus, and ConsoleUtils.
4. [Data](03_Data/README.md) — durable storage, resources, serialization, and compatibility.
5. [Application](04_Application/README.md) — user-visible capabilities, workflows, and compatibility.
6. [UI](05_UI/README.md) — WPF presentation areas, pages, ViewModels, interaction, and state.
7. [Development](06_Development/README.md) — engineering, testing, benchmarks, documentation, contribution, and the deferred agent boundary.
8. [Security](07_Security/README.md) — trust boundaries, secrets, privacy, verification, and deferred hardening.
9. [Releases](08_Releases/README.md) — versioning, release process, changelog, evidence routing, and confirmed public-release records.

Lifecycle material is separate from current-state documentation:

- [Decisions](Decisions/README.md) contains durable ADRs and current policies.
- [Reviews](Reviews/README.md) contains audits, implementation reports, verification, benchmarks, and diagnostics.
- [Plans](Plans/README.md) contains active, queued, and minimal completed-cycle records.
- [Archive](Archive/README.md) contains condensed, non-authoritative historical summaries.
- [Templates](Templates/README.md) contains the small reusable document profiles.

Read [DOCUMENT_AUTHORITY.md](DOCUMENT_AUTHORITY.md) before deciding where a
claim belongs. Use [DOCUMENT_MAP.md](DOCUMENT_MAP.md) for structural navigation;
the map does not define authority.

## Source of truth

For implemented behavior, current accepted source code is authoritative,
followed by current tests, verified runtime or review evidence, and existing
documentation only after it has been checked against those sources. This tree
must describe uncertainty and manual-verification limits instead of converting
them into implementation claims.

## Recommended paths

- Advanced users and maintainers: [Project](00_Project/README.md) → [Architecture](01_Architecture/README.md) → the relevant [System](02_Systems/README.md).
- Prospective contributors: [Project](00_Project/README.md) → [Engineering](06_Development/Engineering/README.md) → [Contribution Workflow](06_Development/Engineering/Contribution_Workflow/README.md).
- Reviewers and auditors: [Authority](DOCUMENT_AUTHORITY.md) → [Document Map](DOCUMENT_MAP.md) → [Reviews](Reviews/README.md) and the relevant canonical domain.
- AI agents: [Authority](DOCUMENT_AUTHORITY.md) → [Engineering](06_Development/Engineering/README.md) → [AI Agents](06_Development/AI_Agents/README.md). The detailed agent architecture remains a separate, deferred implementation task.
- Release maintainers: [Releases](08_Releases/README.md) → [Release Verification](08_Releases/Release_Verification.md) → the applicable evidence under [Reviews](Reviews/README.md).

## Maintenance rule

Update the authoritative owner of a concept and its direct indexes together.
Use lifecycle records for rationale, evidence, plans, and history; do not copy a
full canonical specification into those records. Refresh the structural map
after tree or registration changes and run the manual documentation audit before
closeout.
