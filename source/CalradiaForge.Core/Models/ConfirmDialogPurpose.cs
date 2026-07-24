namespace CalradiaForge.Core.Models.Dialogs;

/// <summary>
/// Identifies the lifecycle decision presented by the confirmation dialog.
/// </summary>
public enum ConfirmDialogPurpose {
	Shutdown = 1,
	Restart = 2,
	DelayedShutdown = 3
}
