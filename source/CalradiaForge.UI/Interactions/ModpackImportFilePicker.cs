namespace CalradiaForge.UI.Interactions;

using Microsoft.Win32;

/// <summary>
/// Adapts the WPF file dialog used by the modpack import workflow.
/// </summary>
public sealed class ModpackImportFilePicker : IModpackImportFilePicker {
	public string? PickImportFile() {
		OpenFileDialog dialog = new() {
			Title = "Import Modpack or Novus Preset",
			Filter = "All Supported|*.json;*.xml|CalradiaForge Modpack (*.json)|*.json|Novus Launcher Preset (*.xml)|*.xml",
			Multiselect = false,
			CheckFileExists = true
		};

		return dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FileName)
			? dialog.FileName
			: null;
	}
}
