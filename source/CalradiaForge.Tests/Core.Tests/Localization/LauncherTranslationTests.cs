namespace CalradiaForge.Tests.Core.Localization;

using CalradiaForge.Core.Infra.Localization;

public sealed class LauncherTranslationTests {
	private static readonly string[] RequiredLauncherPageKeys = [
		nameof(TranslationStrings.Launcher_ModpacksLabel),
		nameof(TranslationStrings.Launcher_InstallButton),
		nameof(TranslationStrings.Launcher_LoadOrderHeader),
		nameof(TranslationStrings.Launcher_AvailableModsHeader),
		nameof(TranslationStrings.Launcher_RefreshTooltip),
		nameof(TranslationStrings.Launcher_PlayBannerlord),
		nameof(TranslationStrings.Launcher_PlayWithBLSE),
		nameof(TranslationStrings.Launcher_LaunchTargetTooltip),
		nameof(TranslationStrings.Launcher_LaunchTargetBannerlord),
		nameof(TranslationStrings.Launcher_LaunchTargetBLSE),
		nameof(TranslationStrings.Launcher_SelectModpackPrompt),
		nameof(TranslationStrings.Launcher_InstallInProgress),
		nameof(TranslationStrings.Launcher_Launching),
		nameof(TranslationStrings.Launcher_ScanningForMods),
		nameof(TranslationStrings.Launcher_NoModsFound),
		nameof(TranslationStrings.Launcher_InstallDialogTitle)
	];

	private static readonly string[] LegacyModsPageKeys = [
		"Mods_ModpacksLabel",
		"Mods_InstallButton",
		"Mods_LoadOrderHeader",
		"Mods_AvailableModsHeader",
		"Mods_RefreshTooltip",
		"Mods_PlayBannerlord",
		"Mods_PlayWithBLSE",
		"Mods_LaunchTargetTooltip",
		"Mods_LaunchTargetBannerlord",
		"Mods_LaunchTargetBLSE",
		"Mods_SelectModpackPrompt",
		"Mods_InstallInProgress",
		"Mods_Launching",
		"Mods_ScanningForMods",
		"Mods_NoModsFound",
		"Mods_InstallDialogTitle"
	];

	private static readonly string[] RequiredInstallPresenterKeys = [
		nameof(TranslationStrings.Toast_InstallSucceededWithWarnings),
		nameof(TranslationStrings.Toast_InstallCancelled),
		nameof(TranslationStrings.Toast_InstallRejectedBusy),
		nameof(TranslationStrings.Toast_InstallRejectedAdmissionStopped),
		nameof(TranslationStrings.Toast_InstallValidationFailed),
		nameof(TranslationStrings.Toast_InstallValidationArchiveSelectionEmpty),
		nameof(TranslationStrings.Toast_InstallValidationGameDirectoryNotConfigured),
		nameof(TranslationStrings.Toast_InstallValidationGameDirectoryMissing),
		nameof(TranslationStrings.Toast_InstallValidationModulesDirectoryMissing),
		nameof(TranslationStrings.Toast_InstallDiagnosticGeneric),
		nameof(TranslationStrings.Toast_InstallFailed),
		nameof(TranslationStrings.Toast_InstallProgressFormat),
		nameof(TranslationStrings.Toast_InstallSummaryFormat),
		nameof(TranslationStrings.Toast_InstallCancelledWithChanges),
		nameof(TranslationStrings.Toast_InstallCancelledWithoutChanges),
		nameof(TranslationStrings.Toast_InstallReconciliationFailed),
		nameof(TranslationStrings.Toast_InstallBlseSucceeded),
		nameof(TranslationStrings.Toast_InstallBlseFailed),
		nameof(TranslationStrings.Toast_InstallUnblockWarningFormat),
		nameof(TranslationStrings.Toast_InstallUnblockFailed),
		nameof(TranslationStrings.Toast_InstallReconciliationWarnings)
	];

	[Fact]
	public void Defaults_UseLauncherNavigationIdentityAndContainPresenterKeys() {
		Dictionary<string, string> defaults = TranslationStrings.GetDefaultTranslations();

		Assert.Equal("Launcher", defaults[nameof(TranslationStrings.Nav_LauncherTab)]);
		Assert.DoesNotContain("Nav_ModsTab", defaults.Keys);
		Assert.All(
			RequiredLauncherPageKeys,
			key => Assert.True(defaults.ContainsKey(key), key));
		Assert.All(
			LegacyModsPageKeys,
			key => Assert.DoesNotContain(key, defaults.Keys));
		Assert.All(
			RequiredInstallPresenterKeys,
			key => Assert.True(defaults.ContainsKey(key), key));
	}

	[Fact]
	public void Apply_UsesLauncherPageKeysAndIgnoresLegacyModsPageKeys() {
		TranslationStrings strings = new();

		strings.Apply(new Dictionary<string, string> {
			[nameof(TranslationStrings.Launcher_PlayBannerlord)] = "Localized Bannerlord",
			["Mods_PlayWithBLSE"] = "Legacy BLSE"
		});

		Assert.Equal("Localized Bannerlord", strings.Launcher_PlayBannerlord);
		Assert.Equal("Play with BLSE", strings.Launcher_PlayWithBLSE);
	}

	[Fact]
	public void Apply_UsesLauncherFallbackWhenLegacyNavigationKeyIsSupplied() {
		TranslationStrings strings = new();

		strings.Apply(new Dictionary<string, string> {
			["Nav_ModsTab"] = "Legacy Mods"
		});

		Assert.Equal("Launcher", strings.Nav_LauncherTab);
	}

	[Fact]
	public void Apply_UsesEnglishFallbacksForMissingPresenterDiagnosticKeys() {
		TranslationStrings strings = new();

		strings.Apply(new Dictionary<string, string> {
			[nameof(TranslationStrings.Nav_LauncherTab)] = "Localized Launcher"
		});

		Assert.Equal(
			"Select at least one supported mod archive to install.",
			strings.Toast_InstallValidationArchiveSelectionEmpty);
		Assert.Equal(
			"Installed files could not be unblocked. Use Settings > Tools > Unblock DLLs before launching.",
			strings.Toast_InstallUnblockFailed);
		Assert.Equal(
			"The module list was updated with warnings. Review the log if something looks incorrect.",
			strings.Toast_InstallReconciliationWarnings);
	}
}
