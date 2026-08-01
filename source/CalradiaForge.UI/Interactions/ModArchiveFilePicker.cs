namespace CalradiaForge.UI.Interactions;

using CalradiaForge.Core.Infra.Localization;
using CalradiaForge.Core.Infra.Mods;

using Microsoft.Win32;

/// <summary>
/// WPF archive-picker adapter. It returns choices only and owns no install logic.
/// </summary>
public sealed class ModArchiveFilePicker(TranslationService translator) : IModArchiveFilePicker {
	private readonly TranslationService _translator = translator
		?? throw new ArgumentNullException(nameof(translator));

	public IReadOnlyList<string> PickArchives() {
		OpenFileDialog dialog = new() {
			Title = _translator.Strings.Launcher_InstallDialogTitle,
			Filter = ModInstaller.FileDialogFilter,
			Multiselect = true,
			CheckFileExists = true
		};
		return dialog.ShowDialog() == true
			? dialog.FileNames
			: [];
	}
}
