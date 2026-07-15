# Platform and Path Detection

## Currently Implemented

- `WindowsSteamClientRootProvider` reads the Steam client root from the existing Windows registry location.
- `SteamInstallationResolver` treats the client root as one library candidate, parses `steamapps/libraryfolders.vdf`, normalizes and case-insensitively deduplicates registered libraries, and inspects `appmanifest_261550.acf` for Bannerlord.
- Manifest App ID and `installdir` are validated. Rooted or traversal-based install directories that escape `steamapps/common` are rejected, and the derived game folder must pass `GamePathValidator`.
- Workshop selection uses this precedence: a valid configured path during normal detection; the Bannerlord library; then one deterministic alternate library, preferring valid `appworkshop_261550.acf` evidence. Explicit re-detection ignores the configured override.
- A resolved Steam game remains `GameProvider.Steam` when no Workshop directory exists. Local modules still scan, Settings keeps the Workshop selector visible, and a localized warning explains manual recovery.
- Selecting a valid game folder manually reverse-matches it against the manifest-backed Steam result. A match repairs Steam provider and Workshop state; an unmatched folder is classified as `StandAlone` and stale Workshop state is cleared.
- Resolver statuses and diagnostics record candidates, selections, malformed or inaccessible metadata, rejection reasons, and the Steam-without-Workshop state.

## Boundaries

- `ISteamClientRootProvider` isolates the Windows registry lookup.
- `ISteamInstallationResolver` owns Steam metadata parsing and deterministic candidate selection.
- `GamePathsHelper` applies successful results to `AppConfigSettings` and preserves the existing startup API.
- `ModScanner` consumes the resolved configuration; it does not discover Steam libraries.
- Core remains free of WPF references. Settings decides when to re-detect or request manual selection and uses the Toast/localization surfaces for user feedback.

## Key Files

- `source/CalradiaForge.Core/Infra/Paths/ISteamClientRootProvider.cs`
- `source/CalradiaForge.Core/Infra/Paths/SteamInstallationResolver.cs`
- `source/CalradiaForge.Core/Infra/Paths/SteamResolutionResult.cs`
- `source/CalradiaForge.Core/Infra/Paths/ValveKeyValuesParser.cs`
- `source/CalradiaForge.Core/Infra/Paths/GamePathsHelper.cs`
- `source/CalradiaForge.Core/Infra/Paths/GamePathValidator.cs`
- `source/CalradiaForge.UI/Pages/SettingsPage.xaml.cs`

## Verification Boundary

Automated tests use temporary fake roots and injected client-root providers; they do not read the real registry or user Steam data. Coverage includes same-root and split-root installs, malformed metadata, duplicate/missing libraries, Workshop precedence, missing Workshop content, manual provider repair, an exact 11-module scan, and a Novus preset regression. Real Windows/Steam installations still require owner smoke testing before release.

## Deferred / Future Work

- Register the existing path interfaces in the Phase 5.A application composition root instead of creating alternate implementations.
- Add the broader Phase 6 structured scanner-result contract without duplicating resolver diagnostics.
- Epic and Game Pass expansion remains outside this Steam-specific implementation.
