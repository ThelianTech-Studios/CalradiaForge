# EULA

## Currently Implemented
- `EulaService` loads the embedded EULA text.
- The app shows a first-launch EULA gate before the main window.
- Acceptance is stored in configuration.

## Architecture Guidance
- The EULA is a startup gate, not a UI preference.
- EULA text is embedded in the application payload.
- The runtime should continue to work if the EULA gate is already accepted.

## Key Files
- `source/CalradiaForge.Core/Infra/Eula/EulaService.cs`
- `source/CalradiaForge.UI/App.xaml.cs`
- `source/CalradiaForge.UI/Views/EulaWindow.xaml`

## Deferred / Future Work
- Additional legal or license flows are not currently defined.
