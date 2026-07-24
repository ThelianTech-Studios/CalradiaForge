namespace CalradiaForge.Tests.Core.Localization;

using CalradiaForge.Core.Infra.Localization;

public sealed class ConfirmDialogTranslationTests {
	private static readonly string[] RequiredKeys = [
		nameof(TranslationStrings.ConfirmDialogShutdownPrimaryButtonString),
		nameof(TranslationStrings.ConfirmDialogRestartPrimaryButtonString),
		nameof(TranslationStrings.ConfirmDialogSecondaryButtonString),
		nameof(TranslationStrings.ConfirmDialogDSPrimaryButtonString),
		nameof(TranslationStrings.ConfirmDialogDSSecondaryButtonString),
		nameof(TranslationStrings.ConfirmDialogShutdownTitleString),
		nameof(TranslationStrings.ConfirmDialogRestartTitleString),
		nameof(TranslationStrings.ConfirmDialogDSTitleString),
		nameof(TranslationStrings.ConfirmDialogShutdownMessageString),
		nameof(TranslationStrings.ConfirmDialogRestartMessageString),
		nameof(TranslationStrings.ConfirmDialogDSMessageString),
		nameof(TranslationStrings.ConfirmDialogShutdownActiveOperationsMessageString),
		nameof(TranslationStrings.ConfirmDialogRestartActiveOperationsMessageString),
		nameof(TranslationStrings.ConfirmDialogActiveScanOperationString),
		nameof(TranslationStrings.ConfirmDialogActiveInstallOperationString)
	];

	[Fact]
	public void Defaults_ContainEveryConfirmationKey() {
		Dictionary<string, string> defaults = TranslationStrings.GetDefaultTranslations();

		Assert.All(RequiredKeys, key => Assert.True(defaults.ContainsKey(key), key));
	}

	[Fact]
	public void Apply_UsesSuppliedValuesAndEnglishFallbacksForMissingOrBlankKeys() {
		TranslationStrings strings = new();

		strings.Apply(new Dictionary<string, string> {
			[nameof(TranslationStrings.ConfirmDialogShutdownTitleString)] = "Localized shutdown",
			[nameof(TranslationStrings.ConfirmDialogRestartTitleString)] = "   "
		});

		Assert.Equal("Localized shutdown", strings.ConfirmDialogShutdownTitleString);
		Assert.Equal("Restart CalradiaForge", strings.ConfirmDialogRestartTitleString);
		Assert.Equal("Keep Waiting", strings.ConfirmDialogDSSecondaryButtonString);
	}
}
