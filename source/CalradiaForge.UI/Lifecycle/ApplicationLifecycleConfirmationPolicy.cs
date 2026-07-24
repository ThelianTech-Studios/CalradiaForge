namespace CalradiaForge.UI.Lifecycle;

using CalradiaForge.Core.Infra.Localization;
using CalradiaForge.Core.Models;
using CalradiaForge.UI.Dialogs;

/// <summary>
/// Applies pre-commit lifecycle confirmation policy to one immutable work snapshot.
/// </summary>
internal static class ApplicationLifecycleConfirmationPolicy {
	internal static bool ConfirmShutdown(
		IApplicationWorkController workController,
		IApplicationDialogService dialogs,
		TranslationStrings strings) {
		ArgumentNullException.ThrowIfNull(workController);
		ArgumentNullException.ThrowIfNull(dialogs);
		ArgumentNullException.ThrowIfNull(strings);

		ModPipelineOperation operation = workController.CaptureActiveOperation();
		if (operation == ModPipelineOperation.None) {
			return true;
		}

		return dialogs.ConfirmShutdown(FormatActiveOperation(operation, strings));
	}

	internal static bool ConfirmRestart(
		RestartReason reason,
		IApplicationWorkController workController,
		IApplicationDialogService dialogs,
		TranslationStrings strings) {
		ArgumentNullException.ThrowIfNull(workController);
		ArgumentNullException.ThrowIfNull(dialogs);
		ArgumentNullException.ThrowIfNull(strings);

		if (!dialogs.ConfirmRestart(reason)) {
			return false;
		}

		ModPipelineOperation operation = workController.CaptureActiveOperation();
		if (operation == ModPipelineOperation.None) {
			return true;
		}

		return dialogs.ConfirmRestart(reason, FormatActiveOperation(operation, strings));
	}

	internal static string? FormatActiveOperation(
		ModPipelineOperation operation,
		TranslationStrings strings) {
		ArgumentNullException.ThrowIfNull(strings);

		return operation switch {
			ModPipelineOperation.None => null,
			ModPipelineOperation.StartupScan or ModPipelineOperation.RefreshScan =>
				strings.ConfirmDialogActiveScanOperationString,
			ModPipelineOperation.Install =>
				strings.ConfirmDialogActiveInstallOperationString,
			_ => throw new ArgumentOutOfRangeException(
				nameof(operation),
				operation,
				"Unsupported active mod-pipeline operation.")
		};
	}
}
