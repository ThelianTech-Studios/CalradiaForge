namespace CalradiaForge.UI.Pages;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using CalradiaForge.Core.Models;
using CalradiaForge.UI.ViewModels;

using GongSolutions.Wpf.DragDrop;

/// <summary>
/// Retained Launcher view. Workflow state belongs to <see cref="LauncherViewModel"/>;
/// this class adapts WPF-only gestures.
/// </summary>
public partial class LauncherPage : Page, IDropTarget {
	private readonly LauncherViewModel _viewModel;

	public LauncherPage(LauncherViewModel viewModel) {
		_viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
		InitializeComponent();
		DataContext = _viewModel;
	}

	void IDropTarget.DragOver(IDropInfo dropInfo) {
		if (dropInfo.Data is not ModuleModel) {
			return;
		}
		dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
		dropInfo.Effects = DragDropEffects.Move;
	}

	void IDropTarget.Drop(IDropInfo dropInfo) {
		if (dropInfo.Data is not ModuleModel module
			|| !TryResolveCollection(dropInfo.DragInfo.SourceCollection, out LauncherModuleCollection source)
			|| !TryResolveCollection(dropInfo.TargetCollection, out LauncherModuleCollection target)) {
			return;
		}
		_viewModel.MoveModule(source, target, module, dropInfo.InsertIndex);
	}

	private bool TryResolveCollection(
		System.Collections.IEnumerable? collection,
		out LauncherModuleCollection owner) {
		if (collection == _viewModel.LoadOrderView
			|| collection == _viewModel.CurrentLoadOrder) {
			owner = LauncherModuleCollection.LoadOrder;
			return true;
		}
		if (collection == _viewModel.AvailableModsView
			|| collection == _viewModel.AvailableModsList) {
			owner = LauncherModuleCollection.Available;
			return true;
		}
		owner = default;
		return false;
	}

	private void SearchBox_GotFocus(object sender, RoutedEventArgs e) {
		if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text)) {
			textBox.Clear();
		}
	}

	private void PlayTargetDropdown_Click(object sender, RoutedEventArgs e) {
		if (sender is Button button && button.ContextMenu is not null) {
			button.ContextMenu.PlacementTarget = button;
			button.ContextMenu.Placement = PlacementMode.Bottom;
			button.ContextMenu.IsOpen = true;
		}
	}

}
