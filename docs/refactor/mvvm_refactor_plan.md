# MVVM Refactor Plan

> This is approved future Phase 8 direction, not current source behavior. The
> controlling record is the [Phase 8 locked decisions](phase_8_locked_decisions_2026-07-29.md).

## Purpose

Move selected presentation ownership from page-heavy WPF code-behind into
independently testable ViewModels without breaking current workflows. This is
not a full-UI-purity rewrite, zero-code-behind requirement, page rename,
navigation framework, ViewModel manager, service locator, Core rewrite, visual
redesign, Nexus implementation, or performance optimization.

## Current Source Observations

- `LauncherPage.xaml.cs`, `ModpacksPage.xaml.cs`, and `SettingsPage.xaml.cs`
  implement `INotifyPropertyChanged` directly. The current launcher page was
  renamed behavior-preservingly in Phase 7 before extraction.
- Pages currently own observable collections, selected state, status text, command handlers, dialog opening, toast calls, and service event subscriptions.
- `LauncherPage.xaml.cs` no longer observes `ModInstaller` events.
  Manager-relayed install progress/results are owned by the explicitly activated
  application-lifetime presenter.
- `ModpacksPage.xaml.cs` uses `ModpackService.CurrentLoadOrderEntries` and keeps editable working-copy state in code-behind.
- `SettingsPage.xaml.cs` follows "UI decides when, Core decides how" for path selection, validation, redetect, and unblock actions, but still owns page state directly.

## Approved Future Phase 8 Target Direction

- Retain `LauncherPage`; add `LauncherViewModel`; visible navigation text is **Home** while internal identities remain Launcher-based. Do not create `LauncherView` or perform another page rename.
- Add singleton `LauncherViewModel`, `ModpacksViewModel`, and `SettingsViewModel`; pages remain singleton, constructor-inject one ViewModel, and assign `DataContext` once after `InitializeComponent()`.
- Use explicit bounded `CommunityToolkit.Mvvm` members (`ObservableObject`, `RelayCommand`, and `AsyncRelayCommand`). Do not use generator attributes, Messenger, `ObservableRecipient`, toolkit navigation, or a ViewModel locator.
- Define one-time initialization, repeatable activation, UI-local deactivation, and a narrow awaited UI dispatcher. Core services remain UI-independent and dispatcher-free.
- Use picker/folder/shell interfaces only where a concrete workflow needs them; preserve dialog and presenter/toast ownership. ViewModels may use `ICollectionView` where it materially improves binding or filtering, but do not own controls, pages, windows, dialogs, visual-tree objects, or Gong drag/drop interfaces.

## Implemented Phase 7 Page Rename Prerequisite

Phase 7 completed the owner-locked, behavior-preserving rename: `ModsPage` became
`LauncherPage`; the current visible label is **Home**. The complete rename
inventory is in [the Phase 7 checklist](ui_page_rename_review_checklist.md).
`ModsPage` is reserved for future mod management; valid domain types are not
renamed.

Phase 8 adds `LauncherViewModel` but retains `LauncherPage`; no second rename
or rename-map gate is required. `ModsPage` remains reserved for future dedicated
mod management.

## Approved Implementation Sequence

| Phase | Work | Verification |
|---|---|---|
| 8.A | Add only minimal explicit MVVM, lifecycle, dispatcher, DI, and focused-test foundation. | Existing page behavior remains unchanged. |
| 8.B | One unified phase: extract Launcher, then Modpacks, then Settings. This is internal order, not B subphases. | Focused ownership-transfer tests pass for all three ViewModels. |
| 8.C | Keep `MainWindow` as the narrow shell; await lifecycle navigation and remove temporary seams/obsolete workflow code-behind. | Navigation/lifecycle tests pass without a shell ViewModel or navigation framework. |
| 8.D | Comprehensive deterministic hardening plus owner-driven Visual Studio runtime observation. | Clean Debug/Release checks and evidence-graded runtime report. |
| 8.E | Correct and revalidate only Phase 8.D-validated production defects. | Conditional; no automatic cleanup. |

## Scan Warning Presentation Planning

Current Phase 7 source leaves `LauncherPage` temporary ownership of accepted-snapshot visible
collection synchronization, selected-modpack reapplication, launch/page-state
recalculation, and semantic workflow-completion reporting. Phase 8 must migrate
those responsibilities, plus commands and busy/cancel state, to
`LauncherViewModel`. It must not create a speculative ViewModel manager.

During page and ViewModel extraction, scan/refresh state should be able to surface Steam Workshop scan warnings without burying them only in logs. The relevant page/ViewModel should distinguish concise user-facing states such as local modules scanned, Workshop modules scanned, Workshop path detection skipped, no Workshop candidates found, no Workshop mods found, and Workshop scan failed. Technical path details belong in local logs and structured workflow results. The logger does not automatically redact or sanitize paths; callers must not intentionally pass credentials or authentication material.

## Lifecycle, Ownership, And Interaction Rules

- Phase 6.B preserves one singleton `MainWindow` and one retained instance of each primary page. Constructor injection replaces static `App.*` service access while existing code-behind, page-owned state, `DataContext`, bindings, navigation refresh, and `Loaded`/`Unloaded` behavior remain intact.
- Phase 6.B does not add transient navigation pages, navigation scopes, a page catalog, or broad ViewModel extraction. Singleton dialog-service contracts create a fresh WPF window per invocation. The Core-safe startup queue remains separate from the UI `StartupNotificationDrainCoordinator`, and `MainWindow.Loaded` only signals readiness.
- Phase 8 follows implemented Phase 6.A/B/C and completed Phase 7 result/progress/presenter/rename contracts.
- ViewModels consume Core outcomes and do not recreate manager admission, identity, cancellation, reconciliation, terminal classification, quiescence, installer/extractor mechanics, or the presenter toast lifecycle.
- For Launcher, the terminal result is followed by dispatcher-applied accepted-snapshot reconciliation, selected-modpack reapplication, derived launch-state recalculation, state updates, exactly-once semantic completion, presenter terminal notification, and local command completion.
- `MainWindow` selects retained pages and serializes initialization/deactivation/display/activation; it does not become a workflow coordinator or call page-specific workflow refreshes after 8.C.
- Code-behind retains only WPF mechanics: chrome, focus, menus, layout, animation, visual selection, drag/drop gesture adaptation, static links, readiness signaling, one-time DataContext wiring, and narrow event forwarding. Workflow invocation, validation, persistence, authoritative workflow collections, reconciliation, semantic completion, raw toast IDs, service resolution, and workflow-owning `async void` are prohibited.

## Performance And Responsiveness Verification

Phase 8 observations may inform the Phase 9 audit, but Phase 8 neither creates `PERF-NNN` findings nor makes speculative optimizations. The final performance audit owns measurements of UI-thread blocking, startup/navigation construction, and interactive responsiveness.

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

- 8.A-C use focused deterministic task-driven tests; no arbitrary delays, wall-clock assertions, or live-window unit tests.
- 8.D owns comprehensive result/lifecycle/architecture hardening, clean Debug/Release verification, and the owner-driven Visual Studio runtime matrix.
- Runtime evidence is Directly Observed, Corroborated, Inconclusive, Blocked, or Fail; automated tests do not prove interactive WPF behavior.
- No new WPF references appear in Core.

## Future Documentation Cross-References

After verified 8.D and any required 8.E revalidation, accepted implementation may be migrated into canonical UI/UX, navigation/page model, testing, and accessibility documentation. Sitemap/changelog/migration closeout is postverification only. Phase 9 owns performance baselines and Phase 12 owns version/release alignment.

## Out Of Scope

- Visual redesign.
- New Nexus UI.
- Cross-platform UI migration.
- Moving business logic from Core into ViewModels.
- Reopening the Phase 7 `LauncherPage` rename or designing the future `ModsPage`.
