namespace CalradiaForge.UI.Interactions;

using Microsoft.Win32;

/// <summary>Adapts WPF folder dialogs for Settings workflows.</summary>
public sealed class SettingsFolderPicker : ISettingsFolderPicker {
	public string? PickGameFolder() =>
		PickFolder("Select Bannerlord Game Folder");

	public string? PickWorkshopFolder() =>
		PickFolder("Select Steam Workshop Folder");

	private static string? PickFolder(string title) {
		OpenFolderDialog dialog = new() { Title = title };
		return dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName)
			? dialog.FolderName
			: null;
	}
}
