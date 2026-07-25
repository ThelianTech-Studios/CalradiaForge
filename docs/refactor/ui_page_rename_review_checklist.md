# UI Page Rename Review Checklist

Status: owner-locked Phase 7 implementation checklist; not yet implemented.

## Purpose

Phase 7 must make the behavior-preserving `ModsPage` -> `LauncherPage` rename
before Phase 8 MVVM extraction. The visible navigation label becomes
**Launcher**. `ModsPage` is reserved for future mod management; this checklist
does not authorize creation of that future page or renaming domain types.

## Locked Rename Inventory

| Current item | Phase 7 target | Required review/validation |
|---|---|---|
| `Pages/ModsPage.xaml` | `Pages/LauncherPage.xaml` | File name, XAML `x:Class`, generated partials, resource/style references, design-time tooling. |
| `Pages/ModsPage.xaml.cs` | `Pages/LauncherPage.xaml.cs` | CLR type, constructor, retained DI registration, event ownership, comments. |
| `MainWindow` page construction/navigation | `LauncherPage` references and **Launcher** label | Navigation indices, construction, page cache/retained lifetime, visible selection and test fixtures. |
| `TranslationStrings.Nav_ModsTab` and manifest value | Approved Launcher identity/key migration | Localization key/value usage, fallback behavior, English manifest, UI assertions. |
| Documentation/tests/comments | Launcher terminology where it means the current page | Search stale page-identity references; preserve valid `ModInstaller`, `ModPipelineManager`, `ModScanner`, `ModModel`, and modpack names. |

## Required Checks

- Update CLR/XAML, DI, navigation, localization, tests, comments, and relevant
  documentation as one behavior-preserving change.
- Verify `MainWindow` navigation and global `ToastService` host behavior after
  the rename.
- Verify a clean build does not leave stale generated partial or resource links.
- Verify stale `ModsPage` references are either corrected, deliberately
  historical, or valid future-mod-management context.
- Do not use the rename to relocate the install button, redesign the page,
  implement MVVM, create a future ModsPage, or alter mod-domain naming.

## Phase 8 Handoff

Phase 8 begins with stable `LauncherPage`, `LauncherView`, and
`LauncherViewModel` terminology. The future dedicated `ModsPage` remains a
separate Nexus/mod-management decision. See the
[Phase 7 migration map](phase_7_migration_map.md) and
[future mod-management context](nexus_future_mod_management_context.md).
