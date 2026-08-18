# Documentation Authority

This document defines where a claim belongs and how contradictions are
resolved. It is the governance authority for the active `docs/` tree.

## Authority order for implemented behavior

1. Current accepted source code.
2. Current automated tests.
3. Current verified runtime or review evidence.
4. Existing documentation, only after verification against the first three.

When sources disagree, stop the affected documentation claim, identify the
conflict and baseline, and record it as uncertain or deferred. Do not use a
planning document or an old report to override source behavior.

## Documentation classes

| Class | Location | Role | Authority for current behavior |
| --- | --- | --- | --- |
| Canonical | `00_Project/` through `08_Releases/` | Current product, architecture, system, data, application, UI, development, security, and release documentation | Yes, subject to source and test verification |
| Decision | `Decisions/ADR/`, `Decisions/Policies/` | Durable rationale and cross-cutting rules | ADRs explain accepted direction; policies govern current practice; neither overrides source |
| Review evidence | `Reviews/` | Audits, implementation records, verification, benchmarks, and diagnostics | Evidence for a stated scope and baseline only |
| Plan | `Plans/` | Active, queued, and completed work planning | No authority for unimplemented behavior |
| Archive | `Archive/` | Condensed history and cycle context | Explicitly non-authoritative |
| Template | `Templates/` | Reusable document profiles | No product or implementation authority |

## One owner per concept

Each concept has one authoritative specification. Related documents provide a
short relationship summary, name the owner, and link to it; they do not redefine
the same behavior.

| Concept | Canonical owner |
| --- | --- |
| Product purpose and boundaries | `00_Project/` |
| Project responsibilities and dependency direction | `01_Architecture/` |
| Technical mechanisms | `02_Systems/` |
| Durable storage and contracts | `03_Data/` |
| User-visible capability behavior | `04_Application/` |
| Presentation and interaction behavior | `05_UI/` |
| Engineering and verification procedure | `06_Development/` |
| Trust boundaries and security invariants | `07_Security/` |
| Release process and public-release records | `08_Releases/` |
| Rationale and current cross-cutting rules | `Decisions/` |
| Evidence and findings | `Reviews/` |
| Work sequencing | `Plans/` |
| Historical context | `Archive/` |

The boundary is intentionally explicit: `04_Application` says what the user
can do, `05_UI` says how it is presented, `02_Systems` says how it works,
`01_Architecture` says who owns it structurally, `03_Data` says what persists,
and `07_Security` says which invariants apply across the system.

## Current-state rules

- Label implemented, planned, deferred, blocked, uncertain, and historical
  material distinctly.
- Keep future Nexus behavior subordinate to current source and clearly
  unimplemented.
- Do not turn automated build or test results into manual WPF, Steam, packaged
  runtime, or release acceptance claims.
- Keep detailed source inventories in the owning technical document only when
  they explain a boundary; do not create a document for every transient type.
- Keep public-release folders limited to versions explicitly classified as public
  in the active changelog.

## Lifecycle limitations

ADRs consolidate durable decisions for a cycle or substantial initiative; they
are not a log of every implementation choice. Policies contain current,
cross-cutting rules and must be justified by accepted decisions. Reviews retain
evidence for a stated scope and baseline. Plans describe work that may not exist.
Completed plans retain only a short index and closeout. Archives summarize
history and are never a current implementation source.

## Cross-linking and registration

Every meaningful domain or lifecycle folder has an index when it needs an
authority boundary, reading order, register, maintenance rule, or relationship
explanation. Registers should identify path, title, authority class, status,
ownership, applicability, supersession, related decisions, related evidence,
and retention where those fields matter. Metadata does not need to be repeated
in every document.

The canonical entry registry is maintained by the domain indexes below and
represented structurally by [DOCUMENT_MAP.md](DOCUMENT_MAP.md):

| Domain | Register |
| --- | --- |
| Project | [00_Project/README.md](00_Project/README.md) |
| Architecture | [01_Architecture/README.md](01_Architecture/README.md) |
| Systems | [02_Systems/README.md](02_Systems/README.md) |
| Data | [03_Data/README.md](03_Data/README.md) |
| Application | [04_Application/README.md](04_Application/README.md) |
| UI | [05_UI/README.md](05_UI/README.md) |
| Development | [06_Development/README.md](06_Development/README.md) |
| Security | [07_Security/README.md](07_Security/README.md) |
| Releases | [08_Releases/README.md](08_Releases/README.md) |
| Decisions | [Decisions/README.md](Decisions/README.md) |
| Reviews | [Reviews/README.md](Reviews/README.md) |
| Plans | [Plans/README.md](Plans/README.md) |
| Archive | [Archive/README.md](Archive/README.md) |
| Templates | [Templates/README.md](Templates/README.md) |

## Update timing and closeout

During implementation, update directly affected canonical architecture,
system, data, application, UI, development, and security documents with the
verified source and tests. After owner acceptance and an accepted source
endpoint, update the changelog, migration map, public-release material, and
version-specific evidence. A documentation-only change does not create a
source migration entry unless the owner explicitly changes that policy.

Before closeout, reconcile links, registers, map entries, path lengths,
supersession, archive notices, unresolved placeholders, and preservation
boundaries. Record accepted limitations and deferred work in the appropriate
review, plan, or policy record rather than silently weakening the canonical
claim.
