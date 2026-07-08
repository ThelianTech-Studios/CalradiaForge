# Testing Strategy

## Purpose

CalradiaForge should move from manual/runtime verification toward phased automated coverage, starting with Core behavior that is risky, file-heavy, or easy to regress.

## Current Grounding

- There is no test project checked in today.
- `AppConfig` and `AppConfigSettings` own JSON-backed settings and typed config behavior.
- `ModsData` owns mod cache file I/O.
- `ModpackData` owns modpack and last-used file I/O.
- `ModpackService` owns modpack workflow behavior, validation, import/export, and default modpack creation.
- `ModInstaller`, `ModExtractor`, and `BLSEInstaller` own install and extraction behavior that needs coverage before Nexus downloads.
- `Logger` currently writes session logs directly and has no central redaction layer.

## Test Layers

| Layer | Timing | Scope |
|---|---|---|
| Core unit tests | First | Parser behavior, persistence helpers, modpack validation, result mapping, BLSE allowlist rules. |
| Integration-style filesystem tests | Early | Atomic writes, backup recovery, archive preflight, temp extraction containment, install destination checks. |
| ViewModel tests | After MVVM extraction begins | Commands, state transitions, busy/error/status state, operation results. |
| CI validation | After test projects exist | Build, test, and later format/analyzer checks. |
| WPF UI automation | Later | Only high-value user flows that justify maintenance cost. |

## Report-Driven Test Categories

The documentation alignment report calls for a real testing/QA strategy that covers safety-sensitive and release-sensitive systems. Refactor test planning should represent these categories where relevant:

- Scanner/path behavior, including Steam library and Workshop path resolution.
- Installer/archive behavior, including extraction, preflight, overwrite safety, and BLSE validation.
- Persistence behavior, including config, mod cache, modpacks, backups, and corrupt-file handling.
- Modpack workflows, including import, export, validation, save, save-as, and last-used behavior.
- Nexus metadata boundaries, when implemented, without storing credentials in `ModuleModel` or `AppConfig`.
- Logging/redaction behavior, including secret and path redaction expectations.
- UI/ViewModel behavior, including commands, status, warning, error, progress, and cancel states.

These categories are planning coverage targets. They do not make refactor docs canonical architecture until accepted decisions are migrated into stable documentation, ADRs, or release notes.

## Scanner And Path Test Categories

Scanner/path tests must use fake temp directories only. They must not require a real Steam install, real Bannerlord install, real Workshop folder, or real user path.

Required categories include:

- Steam client installed on one fake root while Bannerlord is installed under another fake Steam library root.
- Multiple fake Steam library roots.
- Workshop content located under the Bannerlord library root.
- Workshop content missing.
- Workshop content exists but contains no valid modules.
- Workshop content exists with invalid or incomplete module content.
- Empty Workshop content directories.
- Manual Workshop path override, if supported.
- Invalid manual Workshop path override, if supported.
- No Workshop path candidates discovered.

Tests should verify that the scanner can distinguish local module scan results from Workshop scan results and that missing/invalid Workshop paths produce structured warnings rather than requiring real environment state.

## Phased Implementation

| Phase | Scope | Notes |
|---|---|---|
| 1 | Core unit test project | Add tests for pure or mostly-pure Core behavior first. |
| 2 | Persistence tests | Cover `AppConfig`, `ModsData`, and `ModpackData` save/load/default/corruption paths using temp directories. |
| 3 | Modpack workflow tests | Cover `ModpackService.Save`, `SaveAs`, `CreateNew`, `Import`, `Export`, `SaveLastUsed`, and `ValidateLoadOrder`. |
| 4 | Installer safety tests | Cover archive openability, module identity preflight, reserved module folder blocks, BLSE allowlist validation, and overwrite decisions. |
| 5 | Logging/security tests | Verify redaction and secret-key blocking after those policies are implemented. |
| 6 | Integration-style file tests | Cover realistic file-system workflows that cannot be trusted through unit mocks alone. |
| 7 | Scanner/path tests | Cover fake Steam client roots, multiple fake library roots, Bannerlord library Workshop content, missing/empty/invalid Workshop content, and manual override behavior if supported. |
| 8 | ViewModel tests | Add only after MVVM extraction creates stable ViewModels. |
| 9 | CI | Run `dotnet build` first, then `dotnet test source/CalradiaForge.slnx` once tests exist. |
| 10 | WPF UI automation | Defer until specific high-value UI flows justify the maintenance cost. |

## Verification Expectations

- Tests must use isolated temp folders and must not read or modify a real Bannerlord install, real app config, real modpacks, or real logs.
- Persistence tests should verify missing file behavior, valid JSON round trips, invalid JSON handling, backup recovery once implemented, and atomic write behavior once implemented.
- Modpack tests should verify missing installed mods are reported without mutating saved modpack data.
- Installer tests should verify blocked unsafe inputs do not write outside the intended temp or destination roots.
- Scanner/path tests should verify fake Steam libraries, Workshop candidates, missing paths, empty paths, invalid module content, and manual override behavior without touching real Steam or Bannerlord directories.
- Logging tests should verify debug and normal logs follow the same redaction rules.
- CI should fail on test failures once tests exist.

## Guardrails

- Keep Core tests free of WPF references.
- Do not require Nexus credentials, network access, or real user paths.
- Do not bypass `ModInstaller`, `ModExtractor`, `ModsData`, or `ModpackData` to make tests easier.
- Test observable behavior rather than private implementation details unless no stable boundary exists yet.
- Do not add broad WPF UI automation during the initial testing phase.

## Future Documentation Cross-References

Accepted test strategy decisions should later be migrated into future testing/QA documentation and linked from platform/path-detection, installer/archive, persistence, modpack, Nexus metadata, logging/security, and UI/MVVM docs as those documents are created or expanded.

## Out Of Scope

- Full WPF UI automation in the initial test phase.
- Nexus API integration tests before Nexus auth/download boundaries exist.
- Large MVVM test coverage before ViewModels exist.
- Requiring contributors to have Bannerlord installed to run automated tests.

## Open Questions

- Which test framework should be standard: xUnit, NUnit, or MSTest?
- Should file-system helpers be wrapped in adapters before broad persistence tests?
- What minimum coverage should block CI once the test project exists?
- Which archive fixtures can be checked in without licensing or size concerns?
