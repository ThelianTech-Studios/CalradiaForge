namespace CalradiaForge.Tests.Core.Localization;

using CalradiaForge.Core.Infra.Localization;

public sealed class SettingsTranslationTests {
	[Fact]
	public void Defaults_ContainWorkshopValidationKeys() {
		Dictionary<string, string> defaults = TranslationStrings.GetDefaultTranslations();

		Assert.Equal(
			"✓ Valid Steam Workshop folder found.",
			defaults[nameof(TranslationStrings.Settings_WorkshopFolderValid)]);
		Assert.Equal(
			"✗ Steam Workshop folder not found.",
			defaults[nameof(TranslationStrings.Settings_WorkshopFolderInvalid)]);
	}

	[Fact]
	public void Apply_UsesLocalizedWorkshopValidationValues() {
		TranslationStrings strings = new();

		strings.Apply(new Dictionary<string, string> {
			[nameof(TranslationStrings.Settings_WorkshopFolderValid)] =
				"Localized valid Workshop folder",
			[nameof(TranslationStrings.Settings_WorkshopFolderInvalid)] =
				"Localized missing Workshop folder"
		});

		Assert.Equal(
			"Localized valid Workshop folder",
			strings.Settings_WorkshopFolderValid);
		Assert.Equal(
			"Localized missing Workshop folder",
			strings.Settings_WorkshopFolderInvalid);
	}
}
