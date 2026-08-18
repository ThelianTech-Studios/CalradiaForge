# Project Status

## Baseline

The documentation baseline is the accepted source at the current branch `HEAD`
(`a8dfbc7c616bdeeb5313ae0addca31b60fa69e32` when this overhaul began). The
branch is `dev-V0-14-CodeRefactor`. The working tree already contained the
owner's removal of the former active documentation tree; this overhaul creates
the replacement `docs/` tree and does not restore those files.

## Current implementation signals

- Shared application version metadata is centralized in
  `source/Directory.Build.props`.
- Core owns configuration recovery, logging construction, platform detection,
  mod scanning/install coordination, modpacks, localization, EULA, and launch
  mechanics.
- UI owns composition, WPF lifecycle, retained pages, ViewModels, dialogs,
  dispatching, and toast presentation.
- Nexus has a project boundary but no current implementation surface to support
  feature claims.
- Tests and benchmark inputs exist, but manual WPF/Steam/release evidence must
  remain separately classified.

See the dated records under [Reviews](../Reviews/README.md) for evidence and
limitations rather than treating this summary as a test result.
