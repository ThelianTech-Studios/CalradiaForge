# ADR-0001 — Documentation Architecture for the v0.14 Refactor Closeout

## Status

Accepted for the documentation overhaul on the current `dev-V0-14-CodeRefactor`
baseline. This ADR governs the active documentation tree; it does not modify
the application architecture.

## Context

The former documentation tree mixed current architecture, phase plans, audits,
temporary prompts, release records, and source migration history. The preserved
snapshot contains useful rationale and evidence but also stale implementation
claims, missing registrations, and historical path references. Current source
and tests show that the v0.14 refactor has implemented the Core workflow
coordinator, provider-owned lifecycle/logging, retained ViewModels, typed
configuration recovery, fail-closed Steam launch readiness, and Phase 9
benchmark infrastructure.

## Consolidated decisions

1. Use numbered canonical domains for current product and implementation
   documentation and unnumbered lifecycle domains for decisions, evidence,
   plans, archives, and templates.
2. Give each concept one authoritative owner and replace duplicated prose with
   direct cross-links.
3. Use current accepted source, tests, and bounded evidence as the authority
   order; plans and archives cannot override them.
4. Keep the changelog at `08_Releases/CHANGELOG.md` and the source migration map
   at `06_Development/Engineering/Contribution_Workflow/MIGRATION_MAP.md`.
5. Create per-release folders only for versions explicitly marked public in the
   changelog.
6. Keep completed plans minimal, archive major cycles in condensed summaries,
   and classify reports by evidence type.
7. Keep the detailed `AGENTS.md` and `.agents/skills/` overhaul deferred to a
   separate phase.

## Alternatives considered

- Copying the former tree into new folders was rejected because it would retain
  conflicting authority and stale future-state claims.
- One large architecture document was rejected because capability, data, UI,
  evidence, and release ownership would remain ambiguous.
- One ADR per phase or implementation choice was rejected in favor of a cycle-
  level record with direct canonical destinations.
- A documentation generator or CI validator was deferred; this task creates a
  deterministic map and a manual refresh/audit procedure only.

## Consequences

Readers have a smaller route to current behavior, but historical context must be
read through reviews and archives. Technical documents need direct cross-links
and index updates when ownership changes. Unverified runtime/release claims stay
visible as limitations rather than being silently removed.

## Canonical destinations

- Current architecture: [01_Architecture](../../01_Architecture/README.md)
- Runtime mechanisms: [02_Systems](../../02_Systems/README.md)
- Product capabilities: [04_Application](../../04_Application/README.md)
- Evidence: [Reviews](../../Reviews/README.md)
- Development and source history: [06_Development](../../06_Development/README.md)
- Release records: [08_Releases](../../08_Releases/README.md)

## Supporting history and supersession

The condensed [v0.14 refactor archive](../../Archive/Development_Cycles/V0_14_Refactor/README.md)
and [completed refactor closeout](../../Plans/Completed/Refactor/CLOSEOUT.md)
retain historical rationale. They supersede neither current source nor this
ADR. The previous sitemap's routing and retention concepts are incorporated
into [Documentation Authority](../../DOCUMENT_AUTHORITY.md) and the domain
indexes.
