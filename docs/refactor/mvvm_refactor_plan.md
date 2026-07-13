# MVVM Refactor Plan

## Purpose

Move the WPF UI from page-heavy code-behind toward staged MVVM without breaking current workflows.

## Current Source Observations

- `ModsPage.xaml.cs`, `ModpacksPage.xaml.cs`, and `SettingsPage.xaml.cs` implement `INotifyPropertyChanged` directly.
- Pages currently own observable collections, selected state, status text, command handlers, dialog opening, toast calls, and service event subscriptions.
- `ModsPage.xaml.cs` observes `ModInstaller` progress/completion events and updates UI through the dispatcher.
- `ModpacksPage.xaml.cs` uses `ModpackService.CurrentLoadOrderEntries` and keeps editable working-copy state in code-behind.
- `SettingsPage.xaml.cs` follows "UI decides when, Core decides how" for path selection, validation, redetect, and unblock actions, but still owns page state directly.

## Target Direction

- Pages become thin views.
- ViewModels own state, commands, validation messages, and workflow status.
- Core services remain UI-independent.
- Dialogs, file pickers, explorer opening, and toasts are accessed through UI/platform abstractions.
- Shared operation state supports `IsBusy`, `StatusMessage`, `ErrorMessage`, `CanCancel`, `CurrentOperation`, and `LastOperationResult` where useful.
- ViewModel tests are added as ViewModels stabilize.

## Deferred Owner Checkpoint - UI Page Rename Review

Some current WPF page `.xaml` names may need owner-approved rename/refactor planning before broad page ViewModel extraction or future Nexus-facing UI work. The purpose is to reduce confusing page naming before new Nexus UI concepts are added.

This is a deferred decision checkpoint, not an implementation task:

- Do not rename UI pages in this refactor plan.
- Do not invent a rename map.
- Before page ViewModel extraction begins, create a short owner-review document that inventories current UI page files, code-behind partial classes, `x:Class` values, navigation references, proposed rename candidates, reasons, and risks.
- No rename may proceed until the owner approves an explicit rename map.

If a later owner-approved rename map exists, verification must include:

- XAML file names and `x:Class` declarations.
- Code-behind partial class names and constructors.
- Navigation registration, page construction, and any string/key references.
- Resource dictionaries, styles, bindings, commands, and design-time tooling references.
- Generated files after clean build.
- Documentation, screenshots, and smoke-test references.

Renames should happen before the affected page is extracted into a ViewModel whenever the rename would otherwise churn bindings, navigation, or generated partial classes twice.

## Local Implementation Steps

| Step | Work | Verification |
|---|---|---|
| 1 | Define base ViewModel, command, UI state, and navigation conventions. | Existing pages still build and behave unchanged. |
| 2 | Extract install/refresh state from `ModsPage` into a ViewModel. | Install progress, completion, toasts, refresh, and navigation-away behavior still work. |
| 3 | Extract modpack management state from `ModpacksPage`. | Create/import/save/edit workflows still match current behavior. |
| 4 | Extract settings state from `SettingsPage`. | Auto-save, validation, redetect, unblock, and version display still work. |
| 5 | Move shell/navigation state into ViewModels. | Navigation refresh behavior remains correct. |
| 6 | Remove obsolete code-behind only after equivalent behavior is verified. | ViewModel tests cover key state transitions. |

## Scan Warning Presentation Planning

During page and ViewModel extraction, scan/refresh state should be able to surface Steam Workshop scan warnings without burying them only in logs. The relevant page/ViewModel should distinguish concise user-facing states such as local modules scanned, Workshop modules scanned, Workshop path detection skipped, no Workshop candidates found, no Workshop mods found, and Workshop scan failed. Technical path details belong in local logs and structured workflow results. The logger does not automatically redact or sanitize paths; callers must not intentionally pass credentials or authentication material.

## Dependency Rules

- DI and platform adapter foundations should exist before broad ViewModel construction changes.
- Result/workflow coordination should be introduced before moving install completion logic deeply into ViewModels.
- Core workflow tests should exist where practical before high-risk page extractions.
- ViewModel tests should grow with each stable ViewModel.

## Performance And Responsiveness Verification

The final performance audit should inspect UI-thread blocking, startup and navigation construction, repeated transient UI work, scan/install/settings workflows, and service calls that can delay interaction. Use component benchmarks where they provide a stable boundary and manual responsiveness smoke checks for interactive WPF behavior.

Do not encode fragile wall-clock expectations in ViewModel unit tests. Preserve current UX behavior unless a separate owner-approved change exists.

## Guardrails

- Do not rewrite the whole UI in one pass.
- Do not move file I/O or install logic into ViewModels.
- Do not introduce WPF references into Core.
- Preserve the Toast System as the user-visible notification surface.
- Keep current UX behavior unless a later task explicitly changes it.
- Keep visual redesign out of this architecture phase.
- Do not improve benchmark output by moving work off the UI thread without verifying behavior, cancellation, state ownership, and user-visible ordering.

## Verification Expectations

- `dotnet build source/CalradiaForge.slnx` succeeds.
- ViewModel tests cover start/progress/success/failure/cancel states as workflows are extracted.
- Manual smoke tests cover navigation, mod scan, install, BLSE, modpack save/load/import/export, settings, language selection, debug-mode toggle, and toasts.
- No new WPF references appear in Core.
- Manual smoke checks cover UI responsiveness and UI-thread-sensitive workflows where CLI execution cannot provide evidence.

## Future Documentation Cross-References

Accepted MVVM decisions should later be migrated into future UI/UX, navigation/page model, MVVM command policy, status/error/progress presentation, and accessibility documentation. This refactor plan remains staging material until stable decisions are moved into canonical docs or ADRs.

## Open Questions

- Which MVVM helper pattern should be used: hand-written base classes or a toolkit?
- Should file dialogs be abstracted immediately or during each page extraction?
- Should navigation be refactored before or after the install workflow ViewModel?
- Which UI page `.xaml` files should be considered for owner-approved rename review later?
- Should the rename review happen during Phase 1 cleanup or immediately before Phase 7 MVVM extraction?

## Out Of Scope

- Visual redesign.
- New Nexus UI.
- Cross-platform UI migration.
- Moving business logic from Core into ViewModels.
- UI page renames without an explicit owner-approved rename map.
