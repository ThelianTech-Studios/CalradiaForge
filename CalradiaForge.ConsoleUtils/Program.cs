using CalradiaForge.Core.Infra.Localization;
using CalradiaForge.Core.Infra.Paths;

var filepath = AppPaths.DefaultLanguageFilePath;
var manager = new TranslationManager(
	AppPaths.LanguagesDirectory,
	AppPaths.LanguagesManifestFilePath,
	AppPaths.DefaultLanguageFilePath);

manager.DefaultEnglishLanguageFile(TranslationStrings.GetDefaultTranslations(), filepath);

Console.WriteLine($"Default English language file created at: {filepath}");
Console.ReadKey();
