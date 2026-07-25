# Archive And Installer Safety Policy

## Purpose

Improve mod archive, overwrite, and BLSE install safety without bypassing the current authoritative install pipeline.

## Current Grounding

- `ModInstaller` owns install orchestration, background task lifetime, progress, cancellation, version checks, and install summaries.
- `ModExtractor` owns archive opening, pre-extraction lexical entry-containment validation, extraction, managed-temp cleanup, and mod-root detection.
- `BLSEInstaller` owns BLSE archive detection and game-bin file placement.
- `ModsPage.xaml.cs` currently starts installs and displays status/toasts; it must not take over install mechanics. Phase 7 plans its behavior-preserving rename to `LauncherPage` and removes page-owned application notification observation.
- `ModPipelineManager` already owns awaitable application admission/cancellation/quiescence. Phase 7 must build on that boundary rather than recreate it.
- Normal module installs now require exactly one `SubModule.xml`, a parseable module id, a contained target path, and a matching parseable identity when the target folder already exists.
- Existing target folders with unknown or mismatched identities are blocked, and an upgrade copy does not proceed when recursive deletion fails.

## Guardrails

- Do not bypass `ModInstaller`, `ModExtractor`, or `BLSEInstaller`.
- `ModPipelineManager` coordinates admission, cancellation, completion observation, and quiescence around those owners, but it must not duplicate or replace their install, extraction, rollback, overwrite, or BLSE mechanics.
- Keep Core free of WPF references.
- Keep install work service-owned; UI observes progress and completion.
- Preserve Bannerlord-specific mod loading compatibility until overwrite rules are reviewed.
- Do not treat BLSE as a normal module.
- Never allow archive paths to escape the extraction or install destination.
- Never silently overwrite official/reserved Bannerlord module folders.
- Do not weaken archive containment, module identity validation, cleanup, or user-visible diagnostics for benchmark output.

## Current Risk Areas

- `ModInstaller` deletes an existing module folder during upgrade with `Directory.Delete(..., recursive: true)`.
- Reserved/official module folder protection is not yet enforced.
- `BLSEInstaller` currently copies all files from the detected source bin folder and overwrites existing files.
- Normal-module backup, rollback, and user-confirmation behavior is not yet locked.

## Planned Phase 7 Result And Reconciliation Boundary

Phase 7 plans one manager-owned admitted sequence: validate/admit, run
`ModInstaller`, retain its archive-level summary, run required Modules-directory
DLL unblocking, reuse a private authoritative scan/commit path, construct the
terminal Core result, then release/quiesce. Public `RefreshAsync` must not be
recursively admitted from an install. Cancellation may retain a partial summary,
requires bounded consistency finalization after partial filesystem changes, and
does not imply rollback. BLSE participates in terminal classification. These
plans do not mark deferred installer safety decisions as solved.

## Normal Mod Archive Policy

Implemented normal archives pass these preflight checks before destination writes:

- Archive can be opened.
- Archive extension is accepted: `.zip`, `.rar`, `.7z`.
- Extraction stays contained inside the app-managed extraction directory.
- Exactly one intended module root is identified through one `SubModule.xml`; multi-module archives are currently blocked.
- `SubModule.xml` exists and can be parsed.
- Module id, name, and version are captured when available.
- Target folder resolves under `AppConfigSettings.ModulesDirectoryPath`.
- Target path is contained under `AppConfigSettings.ModulesDirectoryPath`.
- An existing target must contain a parseable matching module id before version/delete/copy work continues.

Official/reserved folder enforcement and a fully locked overwrite/backup/confirmation contract remain deferred owner decisions.

Flat archives with `SubModule.xml` directly at the extraction root still inherit the generated extraction-directory name as their target folder. A deterministic target rule (for example, module id versus another archive identity) or an explicit block requires owner approval because either choice can change compatibility. This layout remains a known Phase 3 limitation and must not be described as fully stabilized.

## Overwrite And Delete Rules

Initial safe direction:

- Same or newer installed version may be skipped as today.
- Upgrade may replace an existing matching module only after identity validation confirms the target is the same module.
- Unknown existing folders must not be deleted automatically.
- Reserved/official modules must be blocked.
- Backup or rollback rules are deferred until Bannerlord-specific constraints are reviewed.

## BLSE Rules

BLSE remains a special-case install path:

- BLSE detection stays behind `BLSEInstaller`.
- Direct overwrite of approved BLSE files is allowed.
- Files copied to the game bin folder must be allowlisted.
- Allowed files and folder structure must be explicit for `Win64_Shipping_Client` and `Gaming.Desktop.x64_Shipping_Client`.
- Unexpected files must be blocked and reported.
- Logs should record accepted, skipped, blocked, and overwritten BLSE files.
- User-facing status/toast messages should explain blocked unsafe files.

## Performance And Benchmark Verification

Later performance work may measure the archive workflow, but safety remains part of the required end-to-end pipeline. Do not remove validation, containment, module identity checks, cancellation, cleanup, or result reporting to improve benchmark results.

Where applicable, distinguish archive open and inspection, extraction-library execution, temporary destination preparation, containment and module identity validation, metadata processing, destination copy/move work, post-extraction parsing/scanning, cleanup, and progress/cancellation integration.

SevenZipWrapper values are comparison evidence only when archive content, format, sizes, destination conditions, machine, runtime, build configuration, warmup, iterations, cache state, antivirus interference, and setup/cleanup placement are comparable. Otherwise mark the values as historical or not directly comparable and do not subtract them from CalradiaForge results.

## Local Implementation Steps

| Step | Work | Verification |
|---|---|---|
| 1 | Partially implemented: named archive extraction and module preflight results, archive openability, lexical entry containment, exactly-one-module validation, and managed-temp cleanup; deterministic naming or blocking for flat-root modules remains owner-gated. | Automated coverage remains Phase 4; targeted smoke verification is required in the Phase 3 closeout. |
| 2 | Deferred: add reserved/official module folder protection after the exact owner-approved list exists. | Tests must prove every approved official folder is blocked. |
| 3 | Partially implemented: unknown/unparsable/mismatched targets are blocked and failed deletion aborts copy; final backup/confirmation rules remain owner-gated. | Same-module upgrade, unknown folder block, same/newer skip, and delete-failure tests remain Phase 4 work. |
| 4 | Deferred: add BLSE allowlist validation after the exact owner-approved manifest and folder structure exist. | Tests must cover allowed BLSE files, unexpected files, and platform bin selection. |
| 5 | Add integration-style filesystem tests. | Temp directory install simulations with no writes outside destination. |

## Acceptance And Verification Expectations

- Valid module archives still install through `ModInstaller`.
- Invalid archives fail with a clear result and no partial destination writes.
- Future reserved/official module protection is verified against the owner-approved list; this is currently deferred and unverified.
- Future BLSE allowlist enforcement proves only approved files are copied; this is currently deferred and unverified.
- Cleanup never deletes outside app-managed temp directories.
- Logs and toasts distinguish accepted, skipped, blocked, overwritten, and failed files.

## Open Questions

- What exact Bannerlord official/reserved module folder list should be locked?
- Should upgrades create a temporary backup before delete/copy?
- How should multi-module archives be handled?
- Should a flat archive target a validated module id-derived folder or be blocked until it provides an explicit module folder?
- Should suspicious scripts/executables inside normal modules warn or block?
- What user confirmation level is acceptable for overwrite cases?

## Out Of Scope

- Nexus download implementation.
- Full rollback system until overwrite policy is finalized.
- Cross-platform UI work.
- Replacing `ModInstaller`/`ModExtractor` with a new pipeline.
