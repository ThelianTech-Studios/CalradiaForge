namespace CalradiaForge.UI.pages {
	using System;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Runtime.CompilerServices;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;

	using CalradiaForge.Core.Models;

	/// <summary>
	/// Mods dashboard page. Manages active load order, available mods,
	/// modpack selection, search filtering, and game launch readiness.
	/// </summary>
	public partial class ModsPage : Page, INotifyPropertyChanged {
		#region Fields
		private bool _canStart;
		private string _dependencyWarningText = string.Empty;
		private string _searchQuery = string.Empty;
		private int _currentSelectedModListIndex;
		#endregion

		#region Observable Collections
		/// <summary>
		/// Modules in the active load order (left list).
		/// </summary>
		public ObservableCollection<ModuleModel> CurrentLoadOrder { get; set; } = [];

		/// <summary>
		/// Modules available but not in the active load order (right list).
		/// </summary>
		public ObservableCollection<ModuleModel> AvailableModsList { get; set; } = [];

		/// <summary>
		/// Available modpacks for selection.
		/// </summary>
		public ObservableCollection<string> ModListOptions { get; set; } = [];

		/// <summary>
		/// Missing mods referenced by the selected modpack but not installed.
		/// </summary>
		public ObservableCollection<string> MissingModList { get; set; } = [];
		#endregion

		#region Collection Views
		/// <summary>
		/// Filtered view over <see cref="CurrentLoadOrder"/>.
		/// Items remain in the collection; only visibility is toggled by the search filter.
		/// </summary>
		public ICollectionView LoadOrderView { get; private set; }

		/// <summary>
		/// Filtered view over <see cref="AvailableModsList"/>.
		/// Items remain in the collection; only visibility is toggled by the search filter.
		/// </summary>
		public ICollectionView AvailableModsView { get; private set; }
		#endregion

		#region Bound Properties
		public bool CanStart {
			get => _canStart;
			set { _canStart = value; OnPropertyChanged(); }
		}

		public int CurrentSelectedModListIndex {
			get => _currentSelectedModListIndex;
			set { _currentSelectedModListIndex = value; OnPropertyChanged(); }
		}

		public string DependencyWarningText {
			get => _dependencyWarningText;
			set { _dependencyWarningText = value; OnPropertyChanged(); }
		}
		#endregion

		#region INotifyPropertyChanged
		public event PropertyChangedEventHandler? PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string name = "") {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
		#endregion

		#region Constructor
		public ModsPage() {
			InitializeComponent();
			DataContext = this;

			LoadOrderView = CollectionViewSource.GetDefaultView(CurrentLoadOrder);
			LoadOrderView.Filter = ModSearchFilter;

			AvailableModsView = CollectionViewSource.GetDefaultView(AvailableModsList);
			AvailableModsView.Filter = ModSearchFilter;
		}
		#endregion

		#region Search Filter
		/// <summary>
		/// Filter predicate applied to both collection views.
		/// Returns true if the item's ModuleName contains the current search query.
		/// When the search box is empty, all items are visible.
		/// </summary>
		private bool ModSearchFilter(object item) {
			if (string.IsNullOrWhiteSpace(_searchQuery)) {
				return true;
			}
			if (item is ModuleModel mod) {
				string modName = mod.ModuleName ?? string.Empty;
				return modName.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}

		/// <summary>
		/// Filters both the load order and available mods lists based on search input.
		/// Uses <see cref="ICollectionView.Filter"/> — items stay in their collections,
		/// only visibility changes. No items are moved or removed.
		/// </summary>
		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) {
			if (sender is not TextBox textBox) {
				return;
			}
			_searchQuery = textBox.Text.Trim();
			LoadOrderView.Refresh();
			AvailableModsView.Refresh();
		}

		/// <summary>
		/// Clears the search box text when it regains focus so the user
		/// starts with a fresh query each time.
		/// </summary>
		private void SearchBox_GotFocus(object sender, RoutedEventArgs e) {
			if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text)) {
				textBox.Clear();
			}
		}
		#endregion

		#region ModPack Actions
		/// <summary>
		/// Handles modpack ComboBox selection changes.
		/// </summary>
		private void ModPack_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			// TODO: Load selected modpack into CurrentLoadOrder
		}
		#endregion

		#region Mod Actions
		/// <summary>
		/// Opens a file dialog to install a mod from an archive or folder.
		/// </summary>
		private void InstallModsButton_Click(object sender, RoutedEventArgs e) {
			// TODO: Show OpenFileDialog for archive/folder selection and install mod
		}

		/// <summary>
		/// Unblocks all DLL files in the game's modules directory.
		/// </summary>
		private void UnblockDllsButton_Click(object sender, RoutedEventArgs e) {
			// TODO: Call Core helper to unblock DLLs in modules folder
		}

		/// <summary>
		/// Refreshes the mod list from the modules folder.
		/// </summary>
		private void RefreshModsButton_Click(object sender, RoutedEventArgs e) {
			// TODO: Re-scan modules directory and repopulate lists
		}

		/// <summary>
		/// Handles file/folder drag-drop onto the page for mod installation.
		/// </summary>
		private void Page_Drop(object sender, DragEventArgs e) {
			// TODO: Extract dropped archive/folder and install mod
		}
		#endregion

		#region Game Launch
		/// <summary>
		/// Launches the game with the current load order.
		/// </summary>
		private void PlayButton_Click(object sender, RoutedEventArgs e) {
			// TODO: Save last-used modpack and start game with CurrentLoadOrder
		}
		#endregion
	}
}