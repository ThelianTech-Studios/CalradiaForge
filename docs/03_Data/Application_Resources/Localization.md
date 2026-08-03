# Localization Data

`source/Languages/languages.json` identifies available language resources and
the language JSON files supply translation keys. `TranslationStrings` supplies
English fallback values for the runtime contract. The ConsoleUtils generator
rebuilds the default English resource when the developer workflow requests it.

The runtime loading and fallback mechanism belongs to [Core Localization](../../02_Systems/CalradiaForge_Core/Localization/README.md);
the binding/presentation rules belong to [Localization Bindings](../../05_UI/Styling_and_Resources/Localization_Bindings.md).
