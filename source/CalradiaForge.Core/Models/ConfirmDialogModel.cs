namespace CalradiaForge.Core.Models.Dialogs;

/// <summary>
/// Immutable, WPF-independent display model for one lifecycle confirmation.
/// </summary>
public sealed record ConfirmDialogModel(
	string WindowTitle,
	string Heading,
	string Message,
	string PrimaryButtonText,
	string SecondaryButtonText,
	string? DynamicContent) {
	public bool HasDynamicContent => !string.IsNullOrWhiteSpace(DynamicContent);
}
