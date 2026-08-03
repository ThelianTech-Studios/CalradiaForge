namespace CalradiaForge.UI.Pages;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using CalradiaForge.Core.Models;
using CalradiaForge.UI.ViewModels;

using MahApps.Metro.IconPacks;


/// <summary>
/// Retained WPF view for modpack management. Workflow state and commands are
/// owned by <see cref="ModpacksViewModel"/>.
/// </summary>
public partial class ModpacksPage : Page {
	private readonly ModpacksViewModel _viewModel;

	public ModpacksPage(ModpacksViewModel viewModel) {
		_viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
		InitializeComponent();
		DataContext = _viewModel;

		LoadOrderListBox.PreviewMouseLeftButtonUp +=
			LoadOrderListBox_PreviewMouseLeftButtonUp;
	}

	/// <summary>
	/// Opens the template context menu below its WPF placement target.
	/// </summary>
	private void CreateTemplateDropdown_Click(object sender, RoutedEventArgs e) {
		if (sender is Button button && button.ContextMenu is not null) {
			button.ContextMenu.PlacementTarget = button;
			button.ContextMenu.Placement =
				System.Windows.Controls.Primitives.PlacementMode.Bottom;
			button.ContextMenu.IsOpen = true;
		}
	}

	/// <summary>
	/// Forwards the semantic create request, then applies view-owned focus.
	/// </summary>
	private void CreateNewButton_Click(object sender, RoutedEventArgs e) {
		if (_viewModel.ShowCreatePanelCommand.CanExecute(null)) {
			_viewModel.ShowCreatePanelCommand.Execute(null);
			CreateNameBox.Focus();
		}
	}

	/// <summary>
	/// Adapts the trash-icon gesture into a semantic entry-removal command.
	/// </summary>
	private void LoadOrderListBox_PreviewMouseLeftButtonUp(
		object sender,
		MouseButtonEventArgs e) {
		if (e.OriginalSource is not DependencyObject source) {
			return;
		}

		PackIconMaterial? icon = FindAncestor<PackIconMaterial>(source);
		if (icon is null
			|| icon.Kind != PackIconMaterialKind.TrashCan
			|| icon.DataContext is not ModpackEntryModel entry
			|| !_viewModel.RemoveEntryCommand.CanExecute(entry)) {
			return;
		}

		_viewModel.RemoveEntryCommand.Execute(entry);
		e.Handled = true;
	}

	private static T? FindAncestor<T>(DependencyObject current)
		where T : DependencyObject {
		while (current is not null) {
			if (current is T match) {
				return match;
			}
			current = VisualTreeHelper.GetParent(current);
		}
		return null;
	}
}
