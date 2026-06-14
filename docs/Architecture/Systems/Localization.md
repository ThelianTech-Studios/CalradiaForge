# Localization

## Currently Implemented
- `TranslationService` owns active language state.
- `TranslationManager` reads the language manifest and language JSON files.
- `TranslationStrings` provides bindable UI text defaults.
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

## Deferred / Future Work
- Additional languages and UI text are expected to grow over time, but only when backed by source changes.
