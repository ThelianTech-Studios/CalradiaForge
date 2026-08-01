namespace CalradiaForge.UI.Interactions;

using Microsoft.Win32;

/// <summary>Adapts WPF executable dialogs for Settings workflows.</summary>
public sealed class SettingsFilePicker : ISettingsFilePicker {
	public string? PickGameExecutable() =>
		PickExecutable("Select Bannerlord Executable");

	public string? PickBlseExecutable() =>
		PickExecutable("Select BLSE Standalone Executable");

	private static string? PickExecutable(string title) {
		OpenFileDialog dialog = new() {
			Title = title,
			Filter = "Executable Files (*.exe)|*.exe",
			CheckFileExists = true
		};
		return dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FileName)
			? dialog.FileName
			: null;
	}
}
