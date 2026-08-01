namespace CalradiaForge.UI.Interactions;

/// <summary>
/// Returns archive choices for one launcher install request.
/// </summary>
public interface IModArchiveFilePicker {
	IReadOnlyList<string> PickArchives();
}
