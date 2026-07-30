# UI Page Rename Review Checklist

Status: implemented and automatically verified; any owner visual inspection is historical evidence and not a Phase 7 closeout gate.

## Purpose

Phase 7 made the behavior-preserving `ModsPage` -> `LauncherPage` rename
before Phase 8 MVVM extraction. The current visible navigation label is
**Home**. `ModsPage` is reserved for future mod management; this checklist
does not authorize creation of that future page or renaming domain types.

## Implemented Rename Inventory

| Pre-Phase 7 item | Implemented Phase 7 target | Required review/validation |
|---|---|---|
| `Pages/ModsPage.xaml` | `Pages/LauncherPage.xaml` | File name, XAML `x:Class`, generated partials, resource/style references, design-time tooling. |
| `Pages/ModsPage.xaml.cs` | `Pages/LauncherPage.xaml.cs` | CLR type, constructor, retained DI registration, event ownership, comments. |
| `MainWindow` page construction/navigation | `LauncherPage` references and historical Phase 7 label migration | Navigation indices, construction, page cache/retained lifetime, visible selection and test fixtures. |
| `TranslationStrings.Nav_ModsTab` and Launcher-page `Mods_*` text keys | Approved `Nav_LauncherTab` and `Launcher_*` identity/key migration | Strongly typed key usage, fallback behavior, UI assertions, and the separate ConsoleUtils language-file regeneration workflow. |
| Documentation/tests/comments | Launcher terminology where it means the current page | Search stale page-identity references; preserve valid `ModInstaller`, `ModPipelineManager`, `ModScanner`, `ModModel`, and modpack names. |

## Required Checks

- CLR/XAML, DI, navigation, localization, tests, comments, and relevant
  documentation were updated as one behavior-preserving change.
- Automated verification covers `MainWindow` construction/navigation,
  `ToastService` host composition, clean XAML generation, and stale authored
  source/test page-identity references.
- Owner runtime inspection must still confirm the visible Home label,
  navigation away/back, and global toast-host behavior.
- Do not use the rename to relocate the install button, redesign the page,
  implement MVVM, create a future ModsPage, or alter genuine mod-domain naming.
  The owner-approved `Mods_*` -> `Launcher_*` translation-key migration applies
  only to the 16 values owned by the `LauncherPage` sections of
  `TranslationStrings`. Generated language files are not edited during this
  source pass; the owner regenerates them separately with
  `CalradiaForge.ConsoleUtils`.

## Phase 8 Handoff

Historical Phase 7 rename evidence is retained above. The approved Phase 8
amendment retains `LauncherPage`, adds `LauncherViewModel`, keeps Launcher-based
internal identity, and retains **Home** as the visible navigation label. Do not
create `LauncherView` or perform a second Phase 8 rename. The future dedicated `ModsPage` remains a
separate Nexus/mod-management decision. See the
[Phase 7 migration map](phase_7_migration_map.md) and
[future mod-management context](nexus_future_mod_management_context.md).
