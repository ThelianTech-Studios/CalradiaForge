# Localization

## Currently Implemented
- `TranslationService` owns active language state.
- `TranslationManager` reads the language manifest and language JSON files.
- `TranslationStrings` provides bindable UI text defaults.
- Lifecycle confirmation titles, messages, buttons, and authoritative scan/install summaries use strongly typed `TranslationStrings` values. A fresh display model is resolved from the currently active strings whenever a confirmation opens.
- Startup performs first-run language selection before translation initialization and before the EULA gate. The provider-owned `TranslationService` instance is shared by all constructed pages and windows.
- The console utility can regenerate the default English file from the translation defaults.

## Architecture Guidance
- `en-US` is the default language fallback.
- The language manifest is read-only from the runtime's point of view.
- UI binds to `TranslationStrings`; Core performs the loading.
- Language switching persists through configuration.

## Key Files
- `source/CalradiaForge.Core/Infra/Localization/TranslationService.cs`
- `source/CalradiaForge.Core/Infra/Localization/TranslationManager.cs`
- `source/CalradiaForge.Core/Infra/Localization/TranslationStrings.cs`
- `source/CalradiaForge.UI/Lifecycle/ApplicationStartupCoordinator.cs`
- `source/CalradiaForge.UI/Dialogs/ConfirmDialogModelResolver.cs`

## Deferred / Future Work
- Additional languages and UI text are expected to grow over time, but only when backed by source changes.
