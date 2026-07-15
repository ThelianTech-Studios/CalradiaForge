# CalradiaForge Steam Multi-Library Auto-Detection Test Report

> Historical pre-fix snapshot. This report records the diagnostic state before the scoped resolver implementation. It is superseded for current implementation status by `calradiaforge_steam_multilibrary_fix_verification_2026-07-15.md`; the original evidence below is intentionally preserved.

Status: Patch-boundary tests generated and intentionally skipped; production source unchanged  
Report date: 2026-07-15  
Branch: `dev-V0-14-CodeRefactor`  
Base commit: `798fc3d87d4fa2f9588f8d5b8c4cbb17b7161a75` plus current working-tree diagnostics

## Purpose

This report records the `GamePathsHelper` patch-boundary tests created after scanner testing showed that `ModScanner` can merge separate game and Workshop roots when configuration is correct. They model the suspected upstream failure: Steam installed in its main C location while Bannerlord is installed in a Steam library on D. At the owner's direction, no production source change is retained.

## Researched Example Layout

The test uses temporary directories with `C` and `D` path segments so it never reads or writes real drive roots:

```text
C:/Program Files (x86)/Steam/
  steam.exe
  steamapps/
    libraryfolders.vdf

D:/SteamLibrary/
  steamapps/
    appmanifest_261550.acf
    common/
      Mount & Blade II Bannerlord/
        bin/Win64_Shipping_Client/Bannerlord.exe
    workshop/
      content/
        261550/
          1234567890/
```

Officially supported facts:

- Steam documents `C:/Program Files (x86)/Steam/steamapps/common` as its default Windows game location and supports alternate library locations on other drives: [Steam Support](https://help.steampowered.com/en/faqs/view/4578-18A7-C819-8620), [moving games and libraries](https://help.steampowered.com/en/faqs/view/4BD4-4528-6B2E-8327).
- Bannerlord's Steam App ID is `261550`: [official Steam store page](https://store.steampowered.com/app/261550/Mount__Blade_II_Bannerlord/).
- Steamworks exposes application install directories as independent data through `ISteamApps::GetAppInstallDir`: [ISteamApps](https://partner.steamgames.com/doc/api/ISteamApps).
- Steamworks exposes absolute installed Workshop item paths through `ISteamUGC::GetItemInstallInfo`: [ISteamUGC](https://partner.steamgames.com/doc/api/ISteamUGC).

`D:/SteamLibrary`, the numeric Workshop item ID, and the minimal KeyValues contents are representative synthetic fixture choices. Valve does not publish `libraryfolders.vdf` or `appmanifest_*.acf` schemas as stable third-party API contracts, so the test avoids volatile fields, ordering requirements, timestamps, build IDs, ownership data, and depot details.

## Test Boundary

The existing public application entry point remains unchanged:

```csharp
GamePathsHelper.TryAutoDetectGameFolder(AppConfigSettings config)
```

No injectable Steam-path boundary currently exists in production. The generated tests therefore use reflection to describe a future non-public overload accepting `AppConfigSettings` and `Func<string?>`, but both tests are marked skipped. This keeps the desired contract compile-safe without changing `GamePathsHelper`, reading the real registry, or introducing a global mutable test hook.

An earlier diagnostic pass temporarily used such an internal overload to confirm the suspected behavior. That source edit was reverted at the owner's request and is not part of the current working tree. The current executable evidence is the scanner-level split-root test; the direct auto-detection contracts remain pending.

## Tests And Results

Test file: `source/CalradiaForge.Tests/Core.Tests/Paths/GamePathsHelperTests.cs`

### Same-Root Positive Control Contract

`TryAutoDetectGameFolder_WhenBannerlordUsesMainSteamLibrary_DetectsSteamPaths`

Bannerlord and Workshop content are placed beneath the fake C Steam root. Once an owner-approved boundary exists, the test expects:

- `GameProvider.Steam`;
- the exact game root;
- the exact Bannerlord launcher path;
- the exact Workshop root.

This contract protects existing same-root behavior when the future resolver boundary is introduced.

### Alternate-Library Patch Contract

`TryAutoDetectGameFolder_WhenBannerlordExistsOnlyInAlternateSteamLibrary_DetectsSteamPaths`

The test fixture verifies that all relevant D inputs exist, the equivalent C game candidate does not exist, and the C `libraryfolders.vdf` points to the D library with App ID `261550`. The desired post-patch result is:

```text
GameProvider = Steam
GameFolderPath = D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord
GameLauncherFilePath = D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/bin/Win64_Shipping_Client/Bannerlord.exe
SteamWorkshopFolderPath = D:/SteamLibrary/steamapps/workshop/content/261550
```

Source inspection shows that current `GamePathsHelper` builds candidates only beneath the registry Steam root and does not parse alternate-library metadata. The direct test is skipped because safely executing that path against a fake root requires an approved injection boundary.

## Patch Boundary Established

The eventual resolver patch should reuse and activate this fixture with the existing expectations:

```text
GameProvider = Steam
GameFolderPath = D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord
GameLauncherFilePath = D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/bin/Win64_Shipping_Client/Bannerlord.exe
SteamWorkshopFolderPath = an owner-approved valid Workshop candidate
```

The patch must decide and document:

1. How Steam library roots are discovered and parsed defensively.
2. Whether `appmanifest_261550.acf`, the `apps` entry in `libraryfolders.vdf`, direct directory checks, Steamworks APIs, or layered fallbacks are authoritative.
3. Whether Workshop resolution prefers the Bannerlord library, preserves a manual override first, searches every library, or merges valid candidates.
4. How a valid Steam game with no Workshop content remains classified as Steam rather than `StandAlone`.
5. Which structured diagnostic fields report client root, library roots, candidates, selected paths, and rejection reasons.

After the resolver patch, an integration test should pass the resolved configuration to the existing `ModScanner` fixture and assert the exact eight local plus three Workshop module IDs, for a total of 11.

## Verification

- Focused `GamePathsHelperTests` (Debug): 0 failed, 0 passed, 2 skipped as designed.
- Debug test suite: 37 passed, 0 failed, 2 skipped.
- Release test suite: 37 passed, 0 failed, 2 skipped; build emitted existing warnings.
- Real Steam paths or registry values accessed by tests: none.
- Production `GamePathsHelper` source changed: no; its content matches the repository version.
- Bug fixed: no; the executable scanner boundary and pending resolver contract are documented.
- `docs/CHANGELOG.md`: not updated.
- `docs/MIGRATION_MAP.md`: not updated.
