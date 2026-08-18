# Core Localization

`TranslationManager` loads the language manifest and JSON resources,
`TranslationService` exposes the active translation, and `TranslationStrings`
provides the default English fallback strings and translated application
properties. The current language resources are under `source/Languages`.

The ConsoleUtils workflow generates the default English file; runtime UI binding
and refresh behavior belong in [Localization Bindings](../../../05_UI/Styling_and_Resources/Localization_Bindings.md).
