namespace CalradiaForge.UI.Interactions;

/// <summary>
/// Returns one user-selected modpack or Novus preset import path.
/// </summary>
public interface IModpackImportFilePicker {
	string? PickImportFile();
}
