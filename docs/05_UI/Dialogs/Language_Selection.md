# Language Selection

`LanguageSelectWindow` and `LanguageSelectionDialogService` present the
available language options. The selected language is persisted through
`AppSettings`, the translation service reloads the active dictionary, and
retained ViewModels refresh their localized properties through the UI lifecycle.

The resource contract is owned by [Localization Data](../../03_Data/Application_Resources/Localization.md).
