namespace CalradiaForge.UI.Dialogs;

using CalradiaForge.Core.Models;
using CalradiaForge.UI.Views;

/// <summary>
/// Default language-selection dialog service.
/// </summary>
public sealed class LanguageSelectionDialogService : ILanguageSelectionDialogService {
	public bool TrySelectLanguage(
		IReadOnlyList<LanguageOption> languages,
		string currentLanguageCode,
		out string selectedLanguageCode) {
		LanguageSelectWindow window = new(languages, currentLanguageCode);
		DialogOwner.Assign(window);
		bool accepted = window.ShowDialog() == true;
		selectedLanguageCode = accepted ? window.SelectedLanguageCode : currentLanguageCode;
		return accepted;
	}
}
