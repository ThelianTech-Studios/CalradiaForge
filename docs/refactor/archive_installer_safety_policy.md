# Archive And Installer Safety Policy

## Purpose

Improve mod archive, overwrite, and BLSE install safety without bypassing the current authoritative install pipeline.

## Current Grounding

- `ModInstaller` owns install orchestration, background task lifetime, progress, cancellation, version checks, and install summaries.
- `ModExtractor` owns archive extraction and mod-root detection.
- `BLSEInstaller` owns BLSE archive detection and game-bin file placement.
- `ModsPage.xaml.cs` decides when installs start and displays status/toasts; it must not take over install mechanics.

## Guardrails

- Do not bypass `ModInstaller`, `ModExtractor`, or `BLSEInstaller`.
- Keep Core free of WPF references.
- Keep install work service-owned; UI observes progress and completion.
- Preserve Bannerlord-specific mod loading compatibility until overwrite rules are reviewed.
- Do not treat BLSE as a normal module.
- Never allow archive paths to escape the extraction or install destination.
- Never silently overwrite official/reserved Bannerlord module folders.
- Do not weaken archive containment, module identity validation, cleanup, or user-visible diagnostics for benchmark output.

## Current Risk Areas

- `ModInstaller` deletes an existing module folder during upgrade with `Directory.Delete(..., recursive: true)`.
- A folder that already exists but lacks `SubModule.xml` is treated as an installable overwrite target.
- Reserved/official module folder protection is not yet enforced.
- `BLSEInstaller` currently copies all files from the detected source bin folder and overwrites existing files.
- Archive extraction does not yet have a documented containment validation phase.
- Module identity preflight happens implicitly after extraction, not as a named validation result.

## Normal Mod Archive Policy

Archives must pass preflight before install:

- Archive can be opened.
- Archive extension is accepted: `.zip`, `.rar`, `.7z`.
- Extraction stays contained inside the app-managed extraction directory.
- Exactly one intended module root is identified, unless future multi-module policy allows more.
- `SubModule.xml` exists and can be parsed.
- Module id, name, and version are captured when available.
- Target folder resolves under `AppConfigSettings.ModulesDirectoryPath`.
- Target folder is not an official/reserved module folder.
- Overwrite/delete behavior is described in a result object before execution.

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

## Phased Implementation

| Phase | Work | Verification |
|---|---|---|
| 1 | Add named archive/module preflight result types around current extraction flow. | Unit tests for valid module, invalid archive, and missing `SubModule.xml`. |
| 2 | Add reserved/official module folder protection. | Tests prove official folders are blocked. |
| 3 | Add overwrite safety checks before recursive delete/copy. | Tests for same module upgrade, unknown folder block, and same/newer skip. |
| 4 | Add BLSE allowlist validation before copy. | Tests for allowed BLSE files, unexpected files, and platform bin selection. |
| 5 | Add integration-style filesystem tests. | Temp directory install simulations with no writes outside destination. |

## Verification Expectations

- Valid module archives still install through `ModInstaller`.
- Invalid archives fail with a clear result and no partial destination writes.
- Reserved/official modules are blocked.
- BLSE only copies allowlisted files.
- Cleanup never deletes outside app-managed temp directories.
- Logs and toasts distinguish accepted, skipped, blocked, overwritten, and failed files.

## Open Questions

- What exact Bannerlord official/reserved module folder list should be locked?
- Should upgrades create a temporary backup before delete/copy?
- How should multi-module archives be handled?
- Should suspicious scripts/executables inside normal modules warn or block?
- What user confirmation level is acceptable for overwrite cases?

## Out Of Scope

- Nexus download implementation.
- Full rollback system until overwrite policy is finalized.
- Cross-platform UI work.
- Replacing `ModInstaller`/`ModExtractor` with a new pipeline.
