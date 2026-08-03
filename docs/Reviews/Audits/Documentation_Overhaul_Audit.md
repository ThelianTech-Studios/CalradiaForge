# Documentation Overhaul Manual Audit

Status: Complete

Date: 2026-08-02

## Audit Scope

This audit covers the new curated `docs/` tree, the root public README, the
historical evidence migration, canonical changelog and migration-map placement,
and the requirement that the preserved legacy documentation remain read-only.
It does not authorize or evaluate source changes.

Authority for current claims was checked against the live source tree and the
checked-in test/benchmark projects. Historical reports remain evidence with
their original status and are not treated as current implementation contracts.

## Findings And Dispositions

| ID | Classification | Finding | Disposition |
| --- | --- | --- | --- |
| DOC-001 | Confirmed defect | The former root sitemap and tracked legacy documentation routes no longer describe the intended curated structure. | Resolved by creating the numbered domains, lifecycle directories, and generated `DOCUMENT_MAP.md`. |
| DOC-002 | Confirmed defect | The root agent guidance says no test project is checked in, while the live solution contains `CalradiaForge.Tests`. | Deferred outside docs-overhaul scope; source/instruction governance was not edited. |
| DOC-003 | Confirmed defect | The solution file still contains stale entries for removed legacy documentation paths. | Deferred outside docs-overhaul scope because source/project files are read-only for this task. |
| DOC-004 | Uncertain finding | `AppPaths` exposes a `Resources/Eula.txt` path while `EulaService` currently loads the embedded `CalradiaForge.Resources.EULA.txt` resource. | Documented as an unresolved source-level discrepancy; no intended contract was inferred. |
| DOC-005 | Accepted limitation | Manual WPF, Steam, Bannerlord, packaged-installation, and release acceptance evidence cannot be established by this documentation-only pass. | Recorded as a release/evidence limitation; no runtime claim is made. |
| DOC-006 | Deferred work | Historical performance findings remain pending owner/developer review; Phase 10 optimization is not treated as complete. | Preserved in benchmark evidence and release limitations. |
| DOC-007 | Deferred work | Archive/install safety has no confirmed rollback, entry-count, size, compression-time, or reparse-point budget in the current source evidence. | Documented for future hardening review; no behavior was changed. |
| DOC-008 | Accepted limitation | Nexus remains an optional/reserved integration boundary and is not described as shipped networking behavior. | Canonical Nexus documentation states the current boundary and deferrals. |
| DOC-009 | Accepted limitation | Translation inventories do not establish complete non-English coverage. | Localization docs describe the runtime fallback and avoid a completeness claim. |
| DOC-010 | False positive | Historical planning prompts and old route names are not current architecture just because they remain in the preserved evidence set. | They were archived or summarized and excluded from active canonical links. |

## Structural Checks

- The canonical tree is numbered `00_Project` through `08_Releases`.
- Lifecycle directories exist for decisions, reviews, plans, archive, and
  templates.
- Architecture project pages use the required underscore naming convention.
- Test and benchmark material is under development/review areas rather than
  production system ownership pages.
- Only explicitly public historical releases have release directories.
- The changelog is at `docs/08_Releases/CHANGELOG.md`.
- The migration map is at
  `docs/06_Development/Engineering/Contribution_Workflow/MIGRATION_MAP.md`.
- `DOCUMENT_MAP.md` contains only active curated documentation paths and does
  not register the preserved legacy directory.

## Link And Scope Checks

The final local Markdown-link check covered the root README and all 184 active
Markdown documents. Every relative Markdown target resolved. External URLs and
fragment-only references are excluded from that filesystem check. A
legacy-route search found no active Markdown link targeting the former
`docs/Architecture`, `docs/audits`, `docs/refactor`, root changelog, root
migration map, or old sitemap locations.

The working-tree review confirms that implementation edits are confined to the
root README and the new `docs/` tree. The supplied overhaul-instruction file
and preserved legacy directory remain present as untracked owner-provided
inputs. No file under `source/` was edited. The preserved legacy directory was
not written, renamed, deleted, or rewritten.

## Verification Results

The exact final commands and results are recorded below after execution:

```text
Markdown link check: PASS
Document-map structure check: PASS
Legacy active-link check: PASS
Source-scope check: PASS
Debug build: PASS, 0 errors, 6 MSB3270 SevenZip MSIL/AMD64 warnings
Debug tests: PASS, 300 passed, 0 failed, 0 skipped
Release build: PASS, 0 errors, 5 MSB3270 SevenZip MSIL/AMD64 warnings
Release tests: PASS, 300 passed, 0 failed, 0 skipped
```

The build/test lines are deliberately not presented as manual WPF, Steam, or
release acceptance. Those require separate runtime evidence. The SevenZip
architecture warnings remain an open verification warning and were not changed
because source/project edits are outside this task.

## Conclusion

The documentation overhaul is complete within the authorized scope. The new
tree is curated and source-backed, historical material is separated from
canonical documentation, unresolved discrepancies are visible, and excluded
source/runtime work is explicitly classified rather than silently changed.
