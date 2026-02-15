namespace CalradiaForge.UI.Pages {
	using System;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows.Media;

	using CalradiaForge.Core.Infra.Modpacks;
	using CalradiaForge.Core.Models;

	using MahApps.Metro.IconPacks;

	using Microsoft.Win32;

	/// <summary>
	/// Modpacks management page. Handles creating, saving, importing modpacks,
	/// viewing and editing their load order entries.
	/// </summary>
	public partial class ModpacksPage : Page, INotifyPropertyChanged {
		#region Fields
		private readonly ModpackService _modpackService;
		private int _selectedModpackIndex = -1;
		private int _selectedEntryIndex = -1;
		private string _selectedCreatedBy = string.Empty;
		private string _selectedLastUpdated = string.Empty;
		private string _inputModpackName = string.Empty;
		private string _inputCreatedBy = string.Empty;
		private string _editModuleId = string.Empty;
		private string _editModuleName = string.Empty;
		private string _editVersion = string.Empty;
		private string _editModuleUrl = string.Empty;
		private string _statusText = string.Empty;
		private bool _hasSelection;
		private Visibility _isEditPanelVisible = Visibility.Collapsed;
		private Visibility _isCreatePanelVisible = Visibility.Collapsed;
		private ModpackModel? _selectedModpack;
		private ModpackEntryModel? _selectedEntry;
		private ModpackTemplate _pendingTemplate = ModpackTemplate.Vanilla;
		#endregion

		#region Observable Collections
		/// <summary>
		/// All available modpacks for the ComboBox.
		/// </summary>
		public ObservableCollection<ModpackModel> ModpackList { get; set; } = [];

		/// <summary>
		/// Editable load order for the currently selected modpack.
		/// This is a working copy — changes are not persisted until Save is clicked.
		/// </summary>
		public ObservableCollection<ModpackEntryModel> EditableLoadOrder { get; set; } = [];
		#endregion

		#region Bound Properties
		public int SelectedModpackIndex {
			get => _selectedModpackIndex;
			set { _selectedModpackIndex = value; OnPropertyChanged(); }
		}

		public int SelectedEntryIndex {
			get => _selectedEntryIndex;
			set { _selectedEntryIndex = value; OnPropertyChanged(); }
		}

		public string SelectedCreatedBy {
			get => _selectedCreatedBy;
			set { _selectedCreatedBy = value; OnPropertyChanged(); }
		}

		public string SelectedLastUpdated {
			get => _selectedLastUpdated;
			set { _selectedLastUpdated = value; OnPropertyChanged(); }
		}

		public string InputModpackName {
			get => _inputModpackName;
			set { _inputModpackName = value; OnPropertyChanged(); }
		}

		public string InputCreatedBy {
			get => _inputCreatedBy;
			set { _inputCreatedBy = value; OnPropertyChanged(); }
		}

		public string EditModuleId {
			get => _editModuleId;
			set { _editModuleId = value; OnPropertyChanged(); }
		}

		public string EditModuleName {
			get => _editModuleName;
			set { _editModuleName = value; OnPropertyChanged(); }
		}

		public string EditVersion {
			get => _editVersion;
			set { _editVersion = value; OnPropertyChanged(); }
		}

		public string EditModuleUrl {
			get => _editModuleUrl;
			set { _editModuleUrl = value; OnPropertyChanged(); }
		}

		public string StatusText {
			get => _statusText;
			set { _statusText = value; OnPropertyChanged(); }
		}

		public bool HasSelection {
			get => _hasSelection;
			set { _hasSelection = value; OnPropertyChanged(); }
		}

		public Visibility IsEditPanelVisible {
			get => _isEditPanelVisible;
			set { _isEditPanelVisible = value; OnPropertyChanged(); }
		}

		public Visibility IsCreatePanelVisible {
			get => _isCreatePanelVisible;
			set { _isCreatePanelVisible = value; OnPropertyChanged(); }
		}
		#endregion

		#region INotifyPropertyChanged
		public event PropertyChangedEventHandler? PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string name = "") {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
		#endregion

		#region Constructor
		public ModpacksPage() {
			InitializeComponent();
			DataContext = this;

			_modpackService = App.ModpackService;
			PopulateModpackList();

			// Wire trash icon clicks via the ListBox's tunneling event
			LoadOrderListBox.PreviewMouseLeftButtonUp += LoadOrderListBox_PreviewMouseLeftButtonUp;
		}
		#endregion

		#region Populate

		/// <summary>
		/// Reloads the modpack list from the service into the ComboBox collection.
		/// Preserves selection if possible.
		/// </summary>
		private void PopulateModpackList() {
			string? previousSelection = _selectedModpack?.ModpackName;

			ModpackComboBox.SelectionChanged -= ModpackComboBox_SelectionChanged;
			ModpackList.Clear();

			foreach (ModpackModel modpack in _modpackService.AllModpacks) {
				ModpackList.Add(modpack);
			}

			// Restore previous selection
			if (previousSelection is not null) {
				int index = ModpackList
					.Select((m, i) => new { m, i })
					.FirstOrDefault(x => string.Equals(x.m.ModpackName, previousSelection, StringComparison.OrdinalIgnoreCase))?.i ?? -1;
				if (index >= 0) {
					SelectedModpackIndex = index;
					ModpackComboBox.SelectionChanged += ModpackComboBox_SelectionChanged;
					LoadSelectedModpackData();
					return;
				}
			}

			// Default to first item
			if (ModpackList.Count > 0) {
				SelectedModpackIndex = 0;
			}

			ModpackComboBox.SelectionChanged += ModpackComboBox_SelectionChanged;
			LoadSelectedModpackData();
		}

		/// <summary>
		/// Loads the selected modpack's data into the load order list and metadata fields.
		/// </summary>
		private void LoadSelectedModpackData() {
			EditableLoadOrder.Clear();
			ClearEditPanel();
			SelectedCreatedBy = string.Empty;
			SelectedLastUpdated = string.Empty;

			if (SelectedModpackIndex < 0 || SelectedModpackIndex >= ModpackList.Count) {
				_selectedModpack = null;
				HasSelection = false;
				return;
			}

			_selectedModpack = ModpackList[SelectedModpackIndex];
			HasSelection = true;
			SelectedCreatedBy = _selectedModpack.CreatedBy;
			SelectedLastUpdated = _selectedModpack.LastUpdated;

			foreach (ModpackEntryModel entry in _selectedModpack.LoadOrder) {
				EditableLoadOrder.Add(entry.Clone());
			}
		}

		#endregion

		#region ComboBox Selection

		private void ModpackComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			LoadSelectedModpackData();
			StatusText = string.Empty;
		}

		#endregion

		#region Load Order Entry Selection + Edit Panel

		private void LoadOrderListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (SelectedEntryIndex < 0 || SelectedEntryIndex >= EditableLoadOrder.Count) {
				_selectedEntry = null;
				ClearEditPanel();
				return;
			}

			_selectedEntry = EditableLoadOrder[SelectedEntryIndex];
			EditModuleId = _selectedEntry.ModuleId;
			EditModuleName = _selectedEntry.ModuleName;
			EditVersion = _selectedEntry.RequiredVersion;
			EditModuleUrl = _selectedEntry.ModuleURL ?? string.Empty;
			IsEditPanelVisible = Visibility.Visible;
		}

		private void ClearEditPanel() {
			_selectedEntry = null;
			EditModuleId = string.Empty;
			EditModuleName = string.Empty;
			EditVersion = string.Empty;
			EditModuleUrl = string.Empty;
			IsEditPanelVisible = Visibility.Collapsed;
		}

		/// <summary>
		/// Commits the edit panel values back into the selected load order entry.
		/// Does NOT save to disk — only updates the in-memory working copy.
		/// </summary>
		private void SaveEntryButton_Click(object sender, RoutedEventArgs e) {
			if (_selectedEntry is null || SelectedEntryIndex < 0 || SelectedEntryIndex >= EditableLoadOrder.Count) {
				StatusText = "No entry selected.";
				return;
			}

			_selectedEntry.ModuleId = EditModuleId?.Trim() ?? string.Empty;
			_selectedEntry.ModuleName = EditModuleName?.Trim() ?? string.Empty;
			_selectedEntry.RequiredVersion = EditVersion?.Trim() ?? string.Empty;
			_selectedEntry.ModuleURL = string.IsNullOrWhiteSpace(EditModuleUrl) ? null : EditModuleUrl.Trim();

			// Replace the item in the collection to trigger UI refresh
			int index = SelectedEntryIndex;
			EditableLoadOrder[index] = _selectedEntry;
			SelectedEntryIndex = index;

			StatusText = $"Updated '{_selectedEntry.ModuleName}' in working copy. Click Save to persist.";
		}

		/// <summary>
		/// Intercepts mouse clicks on the LoadOrderListBox and checks whether
		/// the click target is the trash can icon (PackIconMaterial with Kind == TrashCan).
		/// If so, resolves the bound ModpackEntryModel from the DataContext
		/// and removes it from the editable load order.
		/// This avoids wiring events inside the ResourceDictionary DataTemplate.
		/// </summary>
		private void LoadOrderListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			if (e.OriginalSource is not DependencyObject source) {
				return;
			}

			// Walk up the visual tree looking for the DeleteIcon
			PackIconMaterial? icon = FindAncestor<PackIconMaterial>(source);
			if (icon is null || icon.Kind != PackIconMaterialKind.TrashCan) {
				return;
			}

			// Resolve the data context from the icon's templated parent chain
			if (icon.DataContext is ModpackEntryModel entry) {
				RemoveEntryAtIndex(entry);
				e.Handled = true;
			}
		}

		/// <summary>
		/// Walks up the visual tree from the given element looking for
		/// an ancestor of the specified type.
		/// </summary>
		private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject {
			while (current is not null) {
				if (current is T match) {
					return match;
				}
				current = VisualTreeHelper.GetParent(current);
			}
			return null;
		}

		/// <summary>
		/// Removes a load order entry when the trash can icon is clicked.
		/// </summary>
		internal void RemoveEntryAtIndex(ModpackEntryModel entry) {
			EditableLoadOrder.Remove(entry);
			ClearEditPanel();
			StatusText = $"Removed '{entry.ModuleName}'. Click Save to persist.";
		}

		#endregion

		#region Import

		/// <summary>
		/// Opens a file dialog for importing a CalradiaForge modpack (.json)
		/// or a Novus Launcher preset (.xml). Delegates parsing and saving
		/// to <see cref="ModpackService.Import"/>, then selects the newly
		/// imported modpack in the ComboBox.
		/// </summary>
		private void ImportButton_Click(object sender, RoutedEventArgs e) {
			OpenFileDialog dialog = new() {
				Title = "Import Modpack or Novus Preset",
				Filter = "All Supported|*.json;*.xml|CalradiaForge Modpack (*.json)|*.json|Novus Launcher Preset (*.xml)|*.xml",
				Multiselect = false,
				CheckFileExists = true
			};

			if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FileName)) {
				return;
			}

			var (success, modpack, message) = _modpackService.Import(dialog.FileName);
			StatusText = message;

			if (success && modpack is not null) {
				PopulateModpackList();

				// Select the newly imported modpack
				int newIndex = ModpackList
					.Select((m, i) => new { m, i })
					.FirstOrDefault(x => string.Equals(x.m.ModpackName, modpack.ModpackName, StringComparison.OrdinalIgnoreCase))?.i ?? -1;
				if (newIndex >= 0) {
					SelectedModpackIndex = newIndex;
					LoadSelectedModpackData();
				}
			}
		}

		#endregion

		#region Create New

		private void CreateNewButton_Click(object sender, RoutedEventArgs e) {
			ShowCreatePanel(ModpackTemplate.Vanilla);
		}

		private void CreateTemplateDropdown_Click(object sender, RoutedEventArgs e) {
			if (sender is Button button && button.ContextMenu is not null) {
				button.ContextMenu.PlacementTarget = button;
				button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
				button.ContextMenu.IsOpen = true;
			}
		}

		private void CreateFromVanilla_Click(object sender, RoutedEventArgs e) {
			ShowCreatePanel(ModpackTemplate.Vanilla);
		}

		private void CreateFromButterLib_Click(object sender, RoutedEventArgs e) {
			ShowCreatePanel(ModpackTemplate.ButterLib);
		}

		private void ShowCreatePanel(ModpackTemplate template) {
			_pendingTemplate = template;
			InputModpackName = string.Empty;
			InputCreatedBy = string.Empty;
			IsCreatePanelVisible = Visibility.Visible;
			CreateNameBox.Focus();
		}

		private void HideCreatePanel() {
			IsCreatePanelVisible = Visibility.Collapsed;
			InputModpackName = string.Empty;
			InputCreatedBy = string.Empty;
		}

		private void CreateConfirmButton_Click(object sender, RoutedEventArgs e) {
			string name = InputModpackName?.Trim() ?? string.Empty;
			string createdBy = InputCreatedBy?.Trim() ?? string.Empty;

			if (string.IsNullOrWhiteSpace(name)) {
				StatusText = "Modpack name cannot be empty.";
				return;
			}

			if (string.IsNullOrWhiteSpace(createdBy)) {
				createdBy = "User";
			}

			bool success = _modpackService.CreateNew(name, createdBy, _pendingTemplate);

			if (success) {
				HideCreatePanel();
				PopulateModpackList();

				// Select the newly created modpack
				int newIndex = ModpackList
					.Select((m, i) => new { m, i })
					.FirstOrDefault(x => string.Equals(x.m.ModpackName, name, StringComparison.OrdinalIgnoreCase))?.i ?? -1;
				if (newIndex >= 0) {
					SelectedModpackIndex = newIndex;
					LoadSelectedModpackData();
				}

				StatusText = $"Created '{name}' with {_pendingTemplate} template.";
			} else {
				StatusText = $"Failed to create '{name}'. A modpack with this name may already exist.";
			}
		}

		private void CreateCancelButton_Click(object sender, RoutedEventArgs e) {
			HideCreatePanel();
			StatusText = string.Empty;
		}

		#endregion

		#region Save

		/// <summary>
		/// Saves the current editable load order back to the selected modpack on disk.
		/// </summary>
		private void SaveButton_Click(object sender, RoutedEventArgs e) {
			if (_selectedModpack is null) {
				StatusText = "No modpack selected.";
				return;
			}

			var entries = EditableLoadOrder.Select(entry => entry.Clone()).ToList();
			bool success = _modpackService.Save(_selectedModpack, entries);

			if (success) {
				PopulateModpackList();
				StatusText = $"Saved '{_selectedModpack.ModpackName}' successfully.";
			} else {
				StatusText = $"Failed to save '{_selectedModpack.ModpackName}'.";
			}
		}

		#endregion
	}
}
