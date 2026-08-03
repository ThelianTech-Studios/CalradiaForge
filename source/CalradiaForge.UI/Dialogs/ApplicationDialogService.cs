namespace CalradiaForge.UI.Dialogs;

using System.Windows;

using CalradiaForge.Core.Infra.Localization;
using CalradiaForge.Core.Models.Dialogs;
using CalradiaForge.UI.Lifecycle;
using CalradiaForge.UI.Views;

/// <summary>
/// Creates fresh modal windows for application-level lifecycle decisions.
/// </summary>
public sealed class ApplicationDialogService : IApplicationDialogService {
	private readonly TranslationService _translationService;

	public ApplicationDialogService(TranslationService translationService) {
		_translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
	}

	public bool ConfirmShutdown(string? activeOperationDetails = null) =>
		ShowConfirmation(ConfirmDialogPurpose.Shutdown, activeOperationDetails);

	public bool ConfirmRestart(RestartReason reason, string? activeOperationDetails = null) =>
		ShowConfirmation(ConfirmDialogPurpose.Restart, activeOperationDetails);

	public DelayedShutdownChoice ChooseDelayedShutdown() =>
		MapDelayedShutdownChoice(ShowConfirmation(ConfirmDialogPurpose.DelayedShutdown));

	internal static DelayedShutdownChoice MapDelayedShutdownChoice(bool? dialogResult) =>
		dialogResult == true
			? DelayedShutdownChoice.ExitAnyway
			: DelayedShutdownChoice.ContinueWaiting;

	private bool ShowConfirmation(
		ConfirmDialogPurpose purpose,
		string? dynamicContent = null) {
		ConfirmDialogModel model = ConfirmDialogModelResolver.Resolve(
			purpose,
			_translationService.Strings,
			dynamicContent);
		ConfirmDialogWindow window = new(model);
		DialogOwner.Assign(window);
		return window.ShowDialog() == true;
	}
}

internal static class DialogOwner {
	public static void Assign(Window window) {
		ArgumentNullException.ThrowIfNull(window);

		if (Application.Current?.MainWindow is { IsLoaded: true } owner
			&& !ReferenceEquals(owner, window)) {
			window.Owner = owner;
			window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			return;
		}

		window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
	}
}
