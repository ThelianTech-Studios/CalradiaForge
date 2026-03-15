namespace CalradiaForge.UI.Views;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using CalradiaForge.Core.Infra.Localization;

/// <summary>
/// Modal language selection window shown on first launch before EULA.
/// </summary>
public partial class LanguageSelectWindow : Window {
	/// <summary>
	/// Gets the selected language code when the dialog is accepted.
	/// </summary>
	public string SelectedLanguageCode { get; private set; } = "en-US";

	/// <summary>
	/// Initializes the language selection window.
	/// </summary>
	public LanguageSelectWindow(IReadOnlyList<LanguageOption> availableLanguages, string currentLanguageCode) {
		InitializeComponent();

		List<LanguageOption> languageOptions = (availableLanguages ?? [])
			.Where(l => !string.IsNullOrWhiteSpace(l.Code))
			.ToList();

		if (languageOptions.Count == 0) {
			languageOptions.Add(new LanguageOption {
				Code = "en-US",
				DisplayName = "English (United States)"
			});
		}

		LanguageComboBox.ItemsSource = languageOptions;

		int selectedIndex = languageOptions
			.Select((lang, i) => new { lang, i })
			.FirstOrDefault(x => string.Equals(x.lang.Code, currentLanguageCode, StringComparison.OrdinalIgnoreCase))?.i ?? 0;

		LanguageComboBox.SelectedIndex = selectedIndex;
		if (LanguageComboBox.SelectedItem is LanguageOption selected) {
			SelectedLanguageCode = selected.Code;
		}
	}

	/// <summary>
	/// Enables dragging the window by the title bar.
	/// </summary>
	private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
		try {
			DragMove();
		} catch (InvalidOperationException) {
			// Mouse was released outside the window bounds during dragging.
		}
	}

	/// <summary>
	/// Closes the window without explicit confirmation.
	/// </summary>
	private void CloseButton_Click(object sender, RoutedEventArgs e) {
		DialogResult = false;
		Close();
	}

	/// <summary>
	/// Updates in-memory selected language state when the user changes selection.
	/// </summary>
	private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (LanguageComboBox.SelectedItem is LanguageOption selected) {
			SelectedLanguageCode = selected.Code;
		}
	}

	/// <summary>
	/// Confirms selected language and closes the window.
	/// </summary>
	private void ContinueButton_Click(object sender, RoutedEventArgs e) {
		if (LanguageComboBox.SelectedItem is LanguageOption selected) {
			SelectedLanguageCode = selected.Code;
		}
		DialogResult = true;
		Close();
	}
}
