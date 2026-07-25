# MVVM Refactor Plan

## Purpose

Move the WPF UI from page-heavy code-behind toward staged MVVM without breaking current workflows.

## Current Source Observations

- `ModsPage.xaml.cs`, `ModpacksPage.xaml.cs`, and `SettingsPage.xaml.cs` implement `INotifyPropertyChanged` directly. The current page is planned to become `LauncherPage` in Phase 7 before extraction.
- Pages currently own observable collections, selected state, status text, command handlers, dialog opening, toast calls, and service event subscriptions.
- `ModsPage.xaml.cs` currently observes `ModInstaller` progress/completion events and updates UI through the dispatcher; Phase 7 plans manager-relayed observation and an application-lifetime presenter first.
- `ModpacksPage.xaml.cs` uses `ModpackService.CurrentLoadOrderEntries` and keeps editable working-copy state in code-behind.
- `SettingsPage.xaml.cs` follows "UI decides when, Core decides how" for path selection, validation, redetect, and unblock actions, but still owns page state directly.

## Deferred Phase 8 Target Direction

- `LauncherPage` becomes `LauncherView` with `LauncherViewModel`; pages become thin views.
- ViewModels own state, commands, validation messages, and workflow status.
- Core services remain UI-independent.
- Dialogs, file pickers, explorer opening, and toasts are accessed through UI/platform abstractions.
- Shared operation state consumes the settled Phase 7 result/progress contract and supports `IsBusy`, `StatusMessage`, `ErrorMessage`, `CanCancel`, `CurrentOperation`, and `LastOperationResult` where useful.
- ViewModel tests are added as ViewModels stabilize.

## Implemented-By-Phase-7 Page Rename Prerequisite

Phase 7 has the owner-locked, behavior-preserving rename: `ModsPage` becomes
`LauncherPage` and the visible label becomes **Launcher**. The complete rename
inventory is in [the Phase 7 checklist](ui_page_rename_review_checklist.md).
`ModsPage` is reserved for future mod management; valid domain types are not
renamed.

If a later owner-approved rename map exists, verification must include:

- XAML file names and `x:Class` declarations.
- Code-behind partial class names and constructors.
- Navigation registration, page construction, and any string/key references.
- Resource dictionaries, styles, bindings, commands, and design-time tooling references.
- Generated files after clean build.
- Documentation, screenshots, and smoke-test references.

The rename must complete before the affected page is extracted into a ViewModel
so bindings, navigation, and generated partials do not churn twice.

## Local Implementation Steps

| Step | Work | Verification |
|---|---|---|
| 1 | Define base ViewModel, command, UI state, and navigation conventions. | Existing pages still build and behave unchanged. |
| 2 | Extract install/refresh state from `LauncherPage` into `LauncherViewModel`. | Manager progress/results, presenter ordering, navigation-away behavior, and completion reporting still work. |
| 3 | Extract modpack management state from `ModpacksPage`. | Create/import/save/edit workflows still match current behavior. |
| 4 | Extract settings state from `SettingsPage`. | Auto-save, validation, redetect, unblock, and version display still work. |
| 5 | Move shell/navigation state into ViewModels. | Navigation refresh behavior remains correct. |
| 6 | Remove obsolete code-behind only after equivalent behavior is verified. | ViewModel tests cover key state transitions. |

## Scan Warning Presentation Planning

Phase 7 leaves `LauncherPage` temporary ownership of accepted-snapshot visible
collection synchronization, selected-modpack reapplication, launch/page-state
recalculation, and semantic workflow-completion reporting. Phase 8 must migrate
those responsibilities, plus commands and busy/cancel state, to
`LauncherViewModel` or an explicitly approved presentation-layer owner. It must
not create a speculative ViewModel manager.

During page and ViewModel extraction, scan/refresh state should be able to surface Steam Workshop scan warnings without burying them only in logs. The relevant page/ViewModel should distinguish concise user-facing states such as local modules scanned, Workshop modules scanned, Workshop path detection skipped, no Workshop candidates found, no Workshop mods found, and Workshop scan failed. Technical path details belong in local logs and structured workflow results. The logger does not automatically redact or sanitize paths; callers must not intentionally pass credentials or authentication material.

## Dependency Rules

- Phase 6.B preserves one singleton `MainWindow` and one retained instance of each primary page. Constructor injection replaces static `App.*` service access while existing code-behind, page-owned state, `DataContext`, bindings, navigation refresh, and `Loaded`/`Unloaded` behavior remain intact.
- Phase 6.B does not add transient navigation pages, navigation scopes, a page catalog, or broad ViewModel extraction. Singleton dialog-service contracts create a fresh WPF window per invocation. The Core-safe startup queue remains separate from the UI `StartupNotificationDrainCoordinator`, and `MainWindow.Loaded` only signals readiness.
- Phase 8 follows implemented Phase 6.A/B/C and completed Phase 7 result/progress/presenter/rename contracts.
- ViewModels consume Core outcomes and do not recreate manager admission, completeness, commit, accepted-snapshot, cancellation, or quiescence decisions.
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
- Whether a reusable presentation workflow service is justified after `LauncherViewModel` is implemented.

## Out Of Scope

- Visual redesign.
- New Nexus UI.
- Cross-platform UI migration.
- Moving business logic from Core into ViewModels.
- Reopening the Phase 7 `LauncherPage` rename or designing the future `ModsPage`.
