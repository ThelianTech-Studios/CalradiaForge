# EULA

## Currently Implemented
- `EulaService` loads the embedded EULA text.
- The startup coordinator shows a fresh first-launch EULA dialog before resolving the main window.
- Acceptance is stored in configuration.

## Architecture Guidance
- The EULA is a startup gate, not a UI preference.
- EULA text is embedded in the application payload.
- The runtime should continue to work if the EULA gate is already accepted.

## Key Files
- `source/CalradiaForge.Core/Infra/Eula/EulaService.cs`
- `source/CalradiaForge.UI/Lifecycle/ApplicationStartupCoordinator.cs`
- `source/CalradiaForge.UI/Dialogs/EulaDialogService.cs`
- `source/CalradiaForge.UI/Views/EulaWindow.xaml`

## Deferred / Future Work
- Additional legal or license flows are not currently defined.
