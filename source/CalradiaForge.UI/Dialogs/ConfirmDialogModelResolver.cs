namespace CalradiaForge.UI.Dialogs;

using CalradiaForge.Core.Infra.Localization;
using CalradiaForge.Core.Models.Dialogs;

/// <summary>
/// Resolves current localized strings into a fresh confirmation display model.
/// </summary>
internal static class ConfirmDialogModelResolver {
	internal static ConfirmDialogModel Resolve(
		ConfirmDialogPurpose purpose,
		TranslationStrings strings,
		string? dynamicContent = null) {
		ArgumentNullException.ThrowIfNull(strings);

		bool hasDynamicContent = !string.IsNullOrWhiteSpace(dynamicContent);
		return purpose switch {
			ConfirmDialogPurpose.Shutdown => Create(
				strings.ConfirmDialogShutdownTitleString,
				hasDynamicContent
					? strings.ConfirmDialogShutdownActiveOperationsMessageString
					: strings.ConfirmDialogShutdownMessageString,
				strings.ConfirmDialogShutdownPrimaryButtonString,
				strings.ConfirmDialogSecondaryButtonString,
				dynamicContent),
			ConfirmDialogPurpose.Restart => Create(
				strings.ConfirmDialogRestartTitleString,
				hasDynamicContent
					? strings.ConfirmDialogRestartActiveOperationsMessageString
					: strings.ConfirmDialogRestartMessageString,
				strings.ConfirmDialogRestartPrimaryButtonString,
				strings.ConfirmDialogSecondaryButtonString,
				dynamicContent),
			ConfirmDialogPurpose.DelayedShutdown => Create(
				strings.ConfirmDialogDSTitleString,
				strings.ConfirmDialogDSMessageString,
				strings.ConfirmDialogDSPrimaryButtonString,
				strings.ConfirmDialogDSSecondaryButtonString,
				dynamicContent),
			_ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, "Unsupported confirmation-dialog purpose.")
		};
	}

	private static ConfirmDialogModel Create(
		string title,
		string message,
		string primaryButtonText,
		string secondaryButtonText,
		string? dynamicContent) =>
		new(
			title,
			title,
			message,
			primaryButtonText,
			secondaryButtonText,
			dynamicContent);
}
