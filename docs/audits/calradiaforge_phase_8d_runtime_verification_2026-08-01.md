# CalradiaForge Phase 8.D Runtime Verification Audit

Status: runtime evidence recorded; deterministic verification is not yet clean

Audit date: 2026-08-01

Scope: Phase 8.D only. This record covers the existing uncommitted Phase 8
MVVM implementation, owner-driven Debug runtime observation, generated-log
review, deterministic build/test verification, and source-diff inspection. It
does not apply production fixes, perform Phase 8.E, update canonical
architecture, or perform release bookkeeping.

## Scope And Test Configuration

The owner performed four Visual Studio 2026 Debug sessions after a clean
rebuild, restarting the debugger where required. The owner intentionally
changed `LoggingSettings` so `DebugMode` defaults to `true` for this exercise
and will restore the production default before accepting a source endpoint.

The test logs are retained outside the source diff at:

`source/CalradiaForge.UI/bin/Debug/net10.0-windows7.0/Logs/`

The reviewed set contains four archived sessions and the final active log:

- `CalradiaForge_2026-08-01_10-41.log`
- `CalradiaForge_2026-08-01_10-47.log`
- `CalradiaForge_2026-08-01_10-59.log`
- `CalradiaForge_2026-08-01_11-04.log`
- `CalradiaForge_Latest.log`

All runtime claims below are classified using the Phase 8.D vocabulary. Codex
did not observe the WPF windows directly; owner observations are corroborated
by the recorded logs where the behavior emits an applicable event.

## Runtime Evidence Matrix

| Scenario | Classification | Evidence and disposition |
|---|---|---|
| Fresh Debug startup followed by EULA decline | Pass - Corroborated | The owner completed the scenario. The 10-41 log records default configuration initialization, `EULA was declined; the application shell will not be created`, and a quiescent user-request shutdown. |
| General navigation and feature exercise with verbose Debug logging | Pass - Corroborated | The owner reported the exercised features as working. The 10-47 log records repeated accepted snapshots, navigation, persisted last-used load order, and quiescent shutdown. It contains one expected warning for an intentionally invalid selected game folder and no Error/Fatal events. |
| Runtime performance | Inconclusive | The owner reported no unacceptable responsiveness issue, but no controlled measurement, baseline, or performance methodology was captured. Phase 8.D does not make a performance claim; Phase 9 owns that work. |
| Non-English localization | Known limitation / excluded | The owner confirmed that only `en-US.json` has the current key/value coverage. This is expected translation-content debt, not evidence of a Phase 8 MVVM regression. No localization-data update was made in this phase. |
| Automatic re-detection and manual Steam folder selection | Inconclusive | The owner observed concerns, but did not provide a reproducible expected/actual sequence. The 10-47 log corroborates rejection of an invalid non-Bannerlord folder and shows a later valid Steam configuration; it does not establish an automatic-detection production failure. Preserve this for a focused repro and separate decision. |
| Mod installer, modpack creation/import, BLSE installation, and BLSE launch | Pass - Corroborated | The owner completed the full workflow. The 10-59 log records a 60-archive batch completion, modpack operations, successful BLSE installation with nine copied files, and a successful Steam/BLSE launch request. |
| Forced shutdown during active mod installation | Pass - Corroborated | The owner observed the confirmation dialog and accepted shutdown. The 10-59 log records a second 60-archive batch, `Install batch was cancelled`, then `Application lifecycle committed` with `Quiescent=True`. |
| Shutdown-cancel choice | Inconclusive | The owner intended to exercise this in the final session, but did not report the result. The 11-04 and final active logs prove clean shutdown/restart sequencing only; they do not prove that a cancel choice preserved the running session. |
| Final Debug restart and normal close | Pass - Corroborated | The 11-04 log records `RestartReason=DebugModeChanged` with `Quiescent=True`; `CalradiaForge_Latest.log` records a displayed main window and a subsequent quiescent user-request shutdown. |
| Toast visibility duration | Inconclusive candidate UX finding | The owner observed some toasts disappearing too quickly. The current defaults are three seconds for Warning, five seconds for Success/Information, and eight seconds for Error. Logs do not capture perceived readability or a missed action, so this requires an owner-approved target duration and focused revalidation rather than an automatic change. |

## Log Review

The five reviewed files contain no runtime Error or Fatal events. The only
Warning event is the expected rejection of an invalid manually selected game
folder during the second session. The successful installation and cancellation
entries retain the expected Core ownership boundary: `ModInstaller` runs the
batch, the manager publishes accepted snapshots, and shutdown commits only
after quiescence.

The logs demonstrate that the temporary Debug configuration was active. This
is useful runtime evidence only; it must not become the shipped default.

## Deterministic Verification

| Command | Outcome |
|---|---|
| `dotnet restore source/CalradiaForge.slnx` | Passed after approved network-enabled retry. The sandboxed attempt was blocked by NuGet TLS/authentication failure. |
| `dotnet build source/CalradiaForge.slnx -c Debug --no-restore` | Passed: 0 errors, 14 existing warnings. |
| `dotnet test source/CalradiaForge.slnx -c Debug --no-restore --no-build` | Failed: 258 passed, 5 failed, 0 skipped (263 total). |
| `dotnet build source/CalradiaForge.slnx -c Release --no-restore` | Passed: 0 errors, 13 existing warnings. |
| `dotnet test source/CalradiaForge.slnx -c Release --no-restore --no-build` | Failed: 258 passed, 5 failed, 0 skipped (263 total). |
| `git diff --check` | Passed: no whitespace errors. |
| Core WPF boundary inspection | Passed: no `System.Windows`, WPF framework, or Gong references found under `source/CalradiaForge.Core`. |
| UI workflow-owning `async void` inspection | Passed: the only occurrences in the affected page/view boundary are `MainWindow` WPF event handlers. |

The existing Windows-only reachability warnings and the MSIL/AMD64
`SevenZipWrapper` reference warning remain. They predate this report and are
not changed or resolved by Phase 8.D.

### Failed-Test Analysis

Four failures are explained by the temporary owner test configuration in
`LoggingSettings`:

- `Settings_WhenFileIsMissing_CreatesOneCompleteDefaultFile`
- `Settings_WhenConfigIsCorrupt_SeedTypedDefaultsWithoutCrashing`
- `LoggingSettings_RemovesObsoleteConfigurableRetentionKey`
- `DebugCommand_PersistsAndAwaitsOneRestartRequest`

The configuration change sets the default/getter fallback to `true` and also
removes the existing `LogFileDaysToKeep` cleanup call. The first, second, and
fourth tests therefore expect the previous `false` state but receive `true`;
the third correctly detects that the cleanup call is absent. This is not
evidence that the Phase 8 ViewModels regressed. The owner must restore the
production `false` default, getter fallback, and obsolete-key removal before
the acceptance test run.

The fifth failure is an independent deterministic-test defect:

- `LogFileLifecycleTests.PrepareForStartup_ArchivesLatestFromLastWriteTimeAtMinutePrecision`

The test uses a fixed 2026-07-23 archive timestamp while leaving
`LogFileLifecycle`'s retention clock on the real current date. On 2026-08-01,
the seven-day cleanup correctly deletes that fixture before the assertion. The
production file-lifecycle behavior is not implicated. The test must inject a
same-period `utcNow` value before Phase 8.D can claim clean deterministic
verification.

## Source Scope Reviewed

The working tree is intentionally uncommitted. Phase 8.D made no source edits;
it reviewed the existing implementation and added this audit record only.

Tracked Phase 8 implementation files with textual changes reviewed:

- `source/CalradiaForge.Core/Infra/Config/LoggingSettings.cs`
- `source/CalradiaForge.Core/Infra/Localization/TranslationStrings.cs`
- `source/CalradiaForge.Tests/Core.Tests/Localization/LauncherTranslationTests.cs`
- `source/CalradiaForge.Tests/UI.Tests/Composition/UiServiceCollectionExtensionsTests.cs`
- `source/CalradiaForge.Tests/UI.Tests/Lifecycle/ApplicationStartupCoordinatorTests.cs`
- `source/CalradiaForge.UI/Composition/ServiceCollectionExtensions.cs`
- `source/CalradiaForge.UI/Pages/LauncherPage.xaml`
- `source/CalradiaForge.UI/Pages/LauncherPage.xaml.cs`
- `source/CalradiaForge.UI/Pages/ModpacksPage.xaml`
- `source/CalradiaForge.UI/Pages/ModpacksPage.xaml.cs`
- `source/CalradiaForge.UI/Pages/SettingsPage.xaml`
- `source/CalradiaForge.UI/Pages/SettingsPage.xaml.cs`
- `source/CalradiaForge.UI/Views/MainWindow.xaml.cs`

New Phase 8 implementation files reviewed:

- `source/CalradiaForge.UI/ViewModels/LauncherViewModel.cs`
- `source/CalradiaForge.UI/ViewModels/ModpacksViewModel.cs`
- `source/CalradiaForge.UI/ViewModels/SettingsViewModel.cs`
- `source/CalradiaForge.UI/ViewModels/ViewModelBase.cs`
- `source/CalradiaForge.UI/Lifecycle/IViewModelLifecycle.cs`
- `source/CalradiaForge.UI/Threading/IUiDispatcher.cs`
- `source/CalradiaForge.UI/Threading/WpfUiDispatcher.cs`
- `source/CalradiaForge.UI/Interactions/IModArchiveFilePicker.cs`
- `source/CalradiaForge.UI/Interactions/IModpackImportFilePicker.cs`
- `source/CalradiaForge.UI/Interactions/ISettingsDllUnblocker.cs`
- `source/CalradiaForge.UI/Interactions/ISettingsFilePicker.cs`
- `source/CalradiaForge.UI/Interactions/ISettingsFolderPicker.cs`
- `source/CalradiaForge.UI/Interactions/ISettingsShellLauncher.cs`
- `source/CalradiaForge.UI/Interactions/ModArchiveFilePicker.cs`
- `source/CalradiaForge.UI/Interactions/ModpackImportFilePicker.cs`
- `source/CalradiaForge.UI/Interactions/SettingsDllUnblocker.cs`
- `source/CalradiaForge.UI/Interactions/SettingsFilePicker.cs`
- `source/CalradiaForge.UI/Interactions/SettingsFolderPicker.cs`
- `source/CalradiaForge.UI/Interactions/SettingsShellLauncher.cs`
- `source/CalradiaForge.Tests/UI.Tests/Support/GlobalSerilogCollection.cs`
- `source/CalradiaForge.Tests/UI.Tests/Threading/WpfUiDispatcherTests.cs`
- `source/CalradiaForge.Tests/UI.Tests/ViewModels/LauncherViewModelTests.cs`
- `source/CalradiaForge.Tests/UI.Tests/ViewModels/ModpacksViewModelTests.cs`
- `source/CalradiaForge.Tests/UI.Tests/ViewModels/SettingsViewModelTests.cs`
- `source/CalradiaForge.Tests/UI.Tests/ViewModels/ViewModelFoundationTests.cs`
- `source/CalradiaForge.Tests/UI.Tests/Views/MainWindowLifecycleTests.cs`

The working-tree status also identifies owner changes in
`AppSettings.cs` and `InstallNotificationPresenter.cs`; the Phase 8.D source
review found no required production correction in those files.

## Pending Source Closeout Summary

No source change is accepted for changelog or migration-map closeout until the
owner restores the temporary logging configuration and cleanly revalidates the
endpoint. If accepted, the combined later closeout must describe this Phase 8
source set as one presentation-ownership migration:

- add retained DI singleton `LauncherViewModel`, `ModpacksViewModel`, and
  `SettingsViewModel` instances with explicit lifecycle and awaited UI
  dispatcher support;
- move the three stateful workflow surfaces from page code-behind to those
  ViewModels, leaving pages as one-time `DataContext` wiring, bindings, and
  WPF gesture adapters;
- add narrow UI interaction interfaces/adapters for archive, modpack, folder,
  executable, unblock, and shell actions;
- register the ViewModels, dispatcher, and interaction adapters in UI
  composition, and make `MainWindow` serialize retained-page lifecycle
  navigation instead of owning workflows;
- add focused ViewModel, dispatcher, lifecycle/navigation, composition, and
  localization coverage; and
- add the localized Launcher ghost-modpack label.

The temporary `LoggingSettings` change is test instrumentation and must not be
included as accepted production behavior. The audit/report and sitemap entries
are supporting evidence, not application release content.

## Documentation And Reviewer Record

Reviewed documentation:

- `docs/refactor/refactor_master_plan.md` (Phase 8 and verification boundary)
- `docs/refactor/phase_8_locked_decisions_2026-07-29.md`
- `docs/refactor/mvvm_refactor_plan.md`
- `docs/refactor/testing_strategy.md`
- `docs/refactor/task_execution_optimization_policy.md`
- `docs/refactor/documentation_alignment_handoff_checklist.md`
- `docs/Architecture/Solution/ARCHITECTURE.md`
- `docs/Architecture/Projects/CalradiaForge.Core.md`
- `docs/Architecture/Projects/CalradiaForge.UI.md`
- `docs/Architecture/Systems/ApplicationLifecycle.md`
- `docs/Architecture/Systems/Launcher.md`
- `docs/Architecture/Systems/Logging.md`
- `docs/Architecture/Systems/PlatformAndPathDetection.md`

No subagent was used. The task is a sequential runtime/log correlation and
the task-execution policy does not require delegation when it would add no
independent review value. The primary-agent review found no Core WPF boundary
violation, no diff whitespace issue, and no evidence supporting a Phase 8.E
production fix. It did find the two deterministic-verification blockers and
the two owner discussion items listed above.

## Phase 8.E Disposition And Required Owner Actions

Phase 8.E is **not required at this time**: no production defect has been
validated by Phase 8.D. The test fixture needs deterministic correction, but
that is Phase 8.D verification hardening rather than a production-defect
correction.

Before accepting or publishing a source endpoint, the owner must:

1. Restore the temporary logging default and obsolete-key cleanup in
   `LoggingSettings`.
2. Correct the dated log-archive test to control its retention clock, then rerun
   the complete Debug and Release test suites.
3. Provide a concise real-Steam reproduction for the automatic re-detection or
   manual-folder concern before authorizing any fix.
4. Decide whether the observed toast duration warrants a UX change, including
   the desired durations and affected toast types, before authorizing a scoped
   implementation.
5. Manually inspect and approve the existing Phase 8 source diff.

After owner approval and clean verification, the owner commits and pushes the
accepted source endpoint. Only then may the combined changelog and migration-map
closeout prepare both documentation files for later owner review, commit, and
push.

## Explicit Exclusions

- No production source correction was made.
- No Visual Studio UI automation or performance benchmark was claimed.
- No canonical architecture or localization-content update was made.
- `docs/CHANGELOG.md` was not updated.
- `docs/MIGRATION_MAP.md` was not updated.
