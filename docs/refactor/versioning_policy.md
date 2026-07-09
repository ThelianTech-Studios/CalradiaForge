# Versioning Policy

## Purpose

Make one public app version authoritative across project files, UI display, changelog entries, release titles, and future release artifacts.

## Current Source Observations

- `CalradiaForge.UI.csproj` has `<Version>0.12.15</Version>` and package release notes.
- `CalradiaForge.Core.csproj` also has `<Version>0.12.15</Version>`.
- `CalradiaForge.Nexus.csproj` and `CalradiaForge.ConsoleUtils.csproj` do not currently declare versions.
- The audit reports `docs/CHANGELOG.md` contains newer entries than the project metadata.
- `SettingsPage.xaml.cs` reads `Assembly.GetExecutingAssembly().GetName().Version`, but displays only major/minor/build and does not show prerelease labels.
- A future `Directory.Build.props` should become the shared source of truth.

## Version Format

CalradiaForge uses:

```text
MAJOR.MINOR.PATCH[-prerelease]
```

No build metadata is used for now.

## Before v1.0

Before v1.0, versions use:

```text
0.MILESTONE.PATCH[-prerelease]
```

- `MAJOR` is always `0` while CalradiaForge is still pre-1.0/open beta.
- `MINOR` represents a milestone version.
- `PATCH` represents an intentional released revision inside the current milestone.
- Patch numbers are not raw commit counters.

Example:

```text
v0.12.15-beta
```

## After v1.0

- `MAJOR` is for app-wide architecture, compatibility, visual shell, or platform foundation breaks.
- `MINOR` is for new user-facing features, major modules, workflows, or integrations.
- `PATCH` is for bug fixes, compatibility fixes, stabilization, and small improvements.

## Source Of Truth

`Directory.Build.props` should own shared version properties.

Project files should inherit shared values unless there is a documented technical reason not to.

The public app version must be used for:

- GitHub release titles.
- Nexus Mods release titles.
- Changelog entries.
- App UI display.
- Release artifact names.
- Installer/package names if applicable.

## Changelog Heading Format

When Codex updates `docs/CHANGELOG.md`, preserve the existing heading form:

```text
## VERSION - TAG | YYYY-MM-DD
```

- `VERSION` is the approved app version, prerelease version, or internal placeholder version already consistent with this policy.
- `TAG` is the release classification, such as `Internal` or `Public Release`.
- Default new Codex-created sections to `Internal` unless the owner explicitly instructs Codex to use `Public Release`.
- Follow user changelog instructions first when they conflict with the default flow.

This heading format applies to both newly created sections and updates to the latest existing section.

## Experimental Nexus Builds

Experimental Nexus builds use prerelease labels:

```text
v[MAJOR.MINOR.PATCH]-experimental.nexus.1
```

Example release title pattern for Github Releases:

```text
CalradiaForge v[MAJOR.MINOR.PATCH]-experimental.nexus.1 - Nexus API Experimental Release
```

Acceptable example progression:

- `v[MAJOR.MINOR.PATCH]-experimental.nexus.1`
- `v[MAJOR.MINOR.PATCH]-experimental.nexus.2`
- `v[MAJOR.MINOR.PATCH]-beta.1`
- `v[MAJOR.MINOR.PATCH]-beta.2`
- `v1.0`

## Changelog Mapping

| Change type | Before v1.0 | After v1.0 |
|---|---|---|
| New milestone/module | MINOR | MINOR |
| App-wide rewrite/foundation change | Usually MINOR | MAJOR + MINOR |
| WPF to AvaloniaUI or major shell redesign | MINOR | MAJOR |
| Bug fixes | PATCH | PATCH |
| Small UI polish | PATCH | PATCH |
| Docs/internal only | Usually no app version bump | Usually no app version bump |
| Nexus SSO/API experimental work | Prerelease label | MINOR |

## Phased Implementation

| Phase | Work | Verification |
|---|---|---|
| 1 | Inventory existing version drift. | Confirm `.csproj`, changelog, UI display, and release-note values are known. |
| 2 | Move shared version properties into `Directory.Build.props`. | `dotnet build source/CalradiaForge.slnx` succeeds. |
| 3 | Remove conflicting duplicated project versions. | Inspect all `.csproj` files for hardcoded version drift. |
| 4 | Update UI version display to read informational version where possible. | About/settings page shows prerelease labels correctly. |
| 5 | Align changelog and release title drafts. | Release title, changelog heading, and artifact name use the same version. |

## Guardrails

- Codex must not casually change major/minor version numbers.
- Do not rewrite old changelog history unless explicitly approved.
- Docs-only changes usually do not require an app version bump.
- Assembly-specific versioning needs a documented technical reason.
- Do not describe experimental Nexus features as stable.

## Open Questions

- What exact next beta version should be used after current changelog/project metadata drift?
- Should `AssemblyInformationalVersion` include the leading `v` or should UI add it?
- Should console utility artifacts share the public app version even if developer-only?

## Out Of Scope

- Release automation implementation.
- NuGet/package publishing policy.
- Post-v1.0 release governance beyond the rules above.
