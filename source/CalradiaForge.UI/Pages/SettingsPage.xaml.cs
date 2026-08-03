namespace CalradiaForge.UI.Pages;

using System.Windows;
using System.Windows.Controls;

using CalradiaForge.Core.Infra.Paths;
using CalradiaForge.UI.ViewModels;


/// <summary>
/// Retained WPF Settings view. Workflow state and commands are owned by
/// <see cref="SettingsViewModel"/>.
/// </summary>
public partial class SettingsPage : Page {
	private readonly SettingsViewModel _viewModel;
	private UIElement[] _panels = [];

	public SettingsPage(SettingsViewModel viewModel) {
		_viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
		InitializeComponent();
		DataContext = _viewModel;
		_panels = [PanelGeneral, PanelGameConfig, PanelTools, PanelWip, PanelAbout];
	}

	/// <summary>Retains view-owned tab selection and panel layout.</summary>
	private void SettingsNav_SelectionChanged(
		object sender,
		SelectionChangedEventArgs e) {
		if (_panels.Length == 0) {
			return;
		}
		int selectedIndex = SettingsNavBar.SelectedIndex;
		if (selectedIndex < 0 || selectedIndex >= _panels.Length) {
			return;
		}
		for (int i = 0; i < _panels.Length; i++) {
			_panels[i].Visibility = i == selectedIndex
				? Visibility.Visible
				: Visibility.Collapsed;
		}
	}

	/// <summary>Retains the static About-page license link mechanic.</summary>
	private void ViewLicense_Click(object sender, RoutedEventArgs e) {
		ExplorerHelper.OpenUrl(
			"https://github.com/ThelianTech/CalradiaForge/blob/master_docs/LICENSE.md");
	}

	/// <summary>Retains the static About-page repository link mechanic.</summary>
	private void ViewGitHub_Click(object sender, RoutedEventArgs e) {
		ExplorerHelper.OpenUrl(
			"https://github.com/ThelianTech-Studios/CalradiaForge");
	}
}
