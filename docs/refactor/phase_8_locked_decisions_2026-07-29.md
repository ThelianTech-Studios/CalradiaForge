# Phase 8 Locked Decisions Reference

Status: owner-approved future implementation direction; no Phase 8 source implementation is represented by this record.

Date: 2026-07-29

Source ledger: `docs/TEMP_Codex_Instructions/CF_P8/01_DECISIONS.md`.

## Purpose And Boundaries

Phase 8 is a staged presentation-ownership extraction from page-heavy WPF
code-behind to independently testable ViewModels. It is not a full-UI-purity
rewrite, a zero-code-behind requirement, a page renaming exercise, a navigation
framework, a second workflow manager, a Core rewrite, visual redesign, Nexus
work, or performance optimization.

The inherited contracts remain authoritative: `ModPipelineManager` owns
admission, operation identity, cancellation, accepted snapshots, required
reconciliation, terminal classification, and quiescence; `ModInstaller` and
`ModExtractor` retain install/extraction mechanics; and the application-lifetime
presenter owns the correlated progress-to-terminal toast lifecycle. Core remains
WPF-free.

## Locked Phase Structure

| Phase | Approved future scope |
|---|---|
| 8.A - MVVM Foundation | Bounded toolkit primitives, explicit lifecycle, narrow UI dispatcher, DI conventions, and focused tests. |
| 8.B - Unified Stateful Workflow Extraction | One phase containing `LauncherViewModel`, `ModpacksViewModel`, and `SettingsViewModel`, in that internal order only. It has no B subphases. |
| 8.C - Shell, Navigation, and Cleanup | Narrow `MainWindow` lifecycle integration and removal of temporary migration seams and obsolete workflow-owning code-behind. |
| 8.D - Robust Automated and Human-Driven Runtime Verification | Comprehensive deterministic hardening, Debug/Release verification, owner-driven Visual Studio session, evidence-graded report. |
| 8.E - Conditional Defect Correction and Revalidation | Runs only for validated Phase 8.D production defects; it is not automatic cleanup. |

Phase 7 is complete. Interphase maintenance is not a Phase 7 closeout gate and
must not be folded into Phase 8.

## Presentation Model

- Use bounded `CommunityToolkit.Mvvm` primitives: `ObservableObject`, explicit
  `RelayCommand`/`AsyncRelayCommand` instances, and relevant interfaces.
- Do not use generator attributes, generated cancellation commands, Messenger,
  `ObservableRecipient`, toolkit navigation, a service locator, event bus, or a
  generic scheduler/task-runner framework.
- Stateful pages and their ViewModels are DI-owned singletons. Each page receives
  exactly one ViewModel by constructor injection and assigns `DataContext` once
  after `InitializeComponent()`. Navigation neither recreates nor swaps them.
- Initialization is one-time, idempotent, concurrency-safe, and awaitable.
  Activation is repeatable and deterministic; deactivation is UI-local and does
  not cancel Core work or dispose singleton ViewModels.
- Use a narrow UI-layer dispatcher abstraction. ViewModels await state-application
  work; UI-bound collections mutate on the UI thread; Core remains dispatcher-free.
- WPF-aware ViewModels may use `ICollectionView` and approved presentation state,
  but never controls, pages, windows, dialogs, visual-tree types, or Gong drag/drop
  interfaces. View-owned gesture adaptation forwards semantic requests.

## Identity, Shell, And Interaction Boundaries

- Retain `LauncherPage`; add `LauncherViewModel`; keep Launcher-based internal
  identifiers; visible navigation text is **Home**. Do not create `LauncherView`
  or perform a second Phase 8 page rename. `ModsPage` remains reserved.
- `MainWindow` remains a narrow WPF shell: retained-page selection, visual
  navigation, serialized transition/lifecycle ordering, focus/window mechanics,
  and lifecycle failure logging. It is not a ViewModel, coordinator, page catalog,
  route framework, or workflow owner.
- Preserve `IApplicationDialogService`, per-invocation confirmation windows, the
  presenter, and `ToastService`. Add `IFilePicker`, `IFolderPicker`, or narrow
  `IShellLauncher` only for a concrete stateful workflow. They return choices;
  ViewModels retain validation, persistence, sequencing, and result ownership.
- Code-behind retains WPF mechanics such as chrome, focus, menus, visual
  selection/layout/animation, drag/drop gesture adaptation, static links,
  readiness signaling, one-time DataContext wiring, and narrow event forwarding.
  It must not own workflow invocation, persistence, validation, accepted-snapshot
  reconciliation, semantic completion, notification lifecycle, raw toast IDs,
  service resolution, or workflow-owning `async void`.

## Launcher Consumption Ordering

`LauncherViewModel` consumes the existing result/progress boundary; it does not
recreate manager authority or retain raw toast identifiers. For every accepted
operation correlation it: receives the terminal result; applies the accepted
snapshot through the dispatcher; reapplies the selected modpack; recalculates
dependency/load-order/selection/launch readiness; updates presentation state;
reports semantic completion exactly once; then allows the presenter to finalize
its terminal toast; and finally completes local command/busy/cancel state.

The design distinguishes success, warnings, partial failure, validation failure,
busy/admission-stopped rejection, cancellation before or after changes, unblock
warning/failure, refresh/reconciliation failure, presentation reconciliation
failure, unexpected failure, and navigation away/return.

## Verification And Documentation Boundaries

Phase 8.A-C use focused deterministic tests without arbitrary delays or
wall-clock assertions. Phase 8.D owns complete result/lifecycle/architecture
hardening, clean Debug and Release verification, and owner-driven Visual Studio
runtime evidence classified as Directly Observed, Corroborated, Inconclusive,
Blocked, or Fail. Automated tests do not prove interactive WPF behavior.

This preimplementation documentation pass updates refactor planning only. After
accepted Phase 8.D (and any necessary 8.E revalidation), regular/canonical docs,
sitemap, changelog, migration map, and Phase 9 handoff may be reconciled against
verified source. Phase 9 owns the authoritative post-Phase-8 performance
baseline; Phase 12 owns version-source and release-metadata alignment.
