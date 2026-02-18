namespace CalradiaForge.UI.Pages {
	using System;
	using System.Collections.Generic;
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

	using CalradiaForge.UI.Toasts;

	using MahApps.Metro.IconPacks;

	using Microsoft.Win32;

	/// <summary>
	/// Modpacks management page. Handles creating, saving, importing modpacks,
	/// viewing and editing their load order entries.
	/// Shows a side-by-side comparison of the saved modpack data (right, editable)
	/// and the active load order from the Mods page (left, read-only).
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
		/// Editable load order for the currently selected modpack (saved data on disk).
		/// This is a working copy — changes are not persisted until Save is clicked.
		/// Displayed in the right list.
		/// </summary>
		public ObservableCollection<ModpackEntryModel> EditableLoadOrder { get; set; } = [];

		/// <summary>
		/// Read-only snapshot of the active load order from the Mods page.
		/// Populated from <see cref="ModpackService.CurrentLoadOrderEntries"/>.
		/// Displayed in the left list for visual comparison.
		/// </summary>
		public ObservableCollection<ModpackEntryModel> ActiveLoadOrder { get; set; } = [];
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
			PopulateActiveLoadOrder();

			// Set initial checkmark state for the default template
			UpdateTemplateCheckmarks();

			// Wire trash icon clicks via the ListBox's tunneling event
			LoadOrderListBox.PreviewMouseLeftButtonUp += LoadOrderListBox_PreviewMouseLeftButtonUp;

			// Refresh the active load order whenever the page becomes visible
			IsVisibleChanged += ModpacksPage_IsVisibleChanged;
		}
		#endregion

		#region Page Visibility

		/// <summary>
		/// Refreshes the active load order and modpack list whenever the page becomes visible.
		/// This ensures both lists always reflect the latest state from ModsPage and disk,
		/// even if the user switched modpacks, reordered mods, or created new modpacks
		/// before navigating here.
		/// Triggers a live mod rescan so <see cref="Core.Infra.Mods.ModService.CurrentMods"/>
		/// is up-to-date before template creation or load order comparison.
		/// </summary>
		private async void ModpacksPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			if (e.NewValue is true) {
				Core.Infra.Mods.ModService modService = App.ModService;
				if (!modService.IsRefreshing) {
					try {
						await modService.RefreshAsync();
					} catch (OperationCanceledException) {
						// Refresh was cancelled — proceed with cached data
					} catch (Exception ex) {
						StatusText = $"Mod rescan failed: {ex.Message}";
						App.Toasts.Show(new ToastRequest {
							Title = "Mod Rescan Failed",
							Message = ex.Message,
							Severity = ToastSeverity.Error
						});
					}
				}
				PopulateModpackList();
				PopulateActiveLoadOrder();
			}
		}

		#endregion

		#region Populate

		/// <summary>
		/// Reloads the modpack list from the service into the ComboBox collection.
		/// Reloads modpacks from disk first to pick up external changes
		/// (added, deleted, or modified modpack files).
		/// Preserves selection if possible.
		/// </summary>
		private void PopulateModpackList() {
			// Reload from disk to detect added/deleted modpack files
			_modpackService.Refresh();

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
		/// Loads the selected modpack's data into the right list and metadata fields.
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

		/// <summary>
		/// Populates the left list with the current active load order
		/// from <see cref="ModpackService.CurrentLoadOrderEntries"/>.
		/// This reflects what the Mods page has in memory right now.
		/// </summary>
		private void PopulateActiveLoadOrder() {
			ActiveLoadOrder.Clear();

			foreach (ModpackEntryModel entry in _modpackService.CurrentLoadOrderEntries) {
				ActiveLoadOrder.Add(entry.Clone());
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

				App.Toasts.Show(new ToastRequest {
					Title = "Modpack Imported",
					Message = $"'{modpack.ModpackName}' imported with {modpack.LoadOrder.Count} entries.",
					Severity = ToastSeverity.Success
				});
			} else {
				App.Toasts.Show(new ToastRequest {
					Title = "Import Failed",
					Message = message,
					Severity = ToastSeverity.Error
				});
			}
		}

		#endregion

		#region Create New

		/// <summary>
		/// Opens the create panel using the currently selected template.
		/// The template is controlled by the dropdown — this button
		/// only initiates the naming step.
		/// </summary>
		private void CreateNewButton_Click(object sender, RoutedEventArgs e) {
			ShowCreatePanel(_pendingTemplate);
		}

		/// <summary>
		/// Opens the template selection context menu below the dropdown chevron.
		/// </summary>
		private void CreateTemplateDropdown_Click(object sender, RoutedEventArgs e) {
			if (sender is Button button && button.ContextMenu is not null) {
				button.ContextMenu.PlacementTarget = button;
				button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
				button.ContextMenu.IsOpen = true;
			}
		}

		/// <summary>
		/// Selects the Vanilla template. Does NOT open the create panel.
		/// </summary>
		private void CreateFromVanilla_Click(object sender, RoutedEventArgs e) {
			SetActiveTemplate(ModpackTemplate.Vanilla);
		}

		/// <summary>
		/// Selects the ButterLib template. Does NOT open the create panel.
		/// </summary>
		private void CreateFromButterLib_Click(object sender, RoutedEventArgs e) {
			SetActiveTemplate(ModpackTemplate.ButterLib);
		}

		/// <summary>
		/// Selects the Vanilla + WarSails template. Does NOT open the create panel.
		/// </summary>
		private void CreateFromVanillaWarSails_Click(object sender, RoutedEventArgs e) {
			SetActiveTemplate(ModpackTemplate.VanillaWarSails);
		}

		/// <summary>
		/// Selects the ButterLib + WarSails template. Does NOT open the create panel.
		/// </summary>
		private void CreateFromButterLibWarSails_Click(object sender, RoutedEventArgs e) {
			SetActiveTemplate(ModpackTemplate.ButterLibWarSails);
		}

		/// <summary>
		/// Applies the given template as the active selection for new modpack creation.
		/// Updates <see cref="_pendingTemplate"/> and toggles checkmark visibility
		/// in the dropdown menu. Does not persist — resets to Vanilla on app restart.
		/// </summary>
		private void SetActiveTemplate(ModpackTemplate template) {
			_pendingTemplate = template;
			UpdateTemplateCheckmarks();
			StatusText = $"Template set to {template}. Click Create New to use it.";
		}

		/// <summary>
		/// Toggles the checkmark icon visibility in the template dropdown.
		/// Only the currently selected template shows its checkmark.
		/// Mirrors the pattern used by <c>UpdateLaunchTargetCheckmarks</c> on ModsPage.
		/// </summary>
		private void UpdateTemplateCheckmarks() {
			CheckVanilla.Visibility = _pendingTemplate == ModpackTemplate.Vanilla
				? Visibility.Visible
				: Visibility.Collapsed;
			CheckButterLib.Visibility = _pendingTemplate == ModpackTemplate.ButterLib
				? Visibility.Visible
				: Visibility.Collapsed;
			CheckVanillaWarSails.Visibility = _pendingTemplate == ModpackTemplate.VanillaWarSails
				? Visibility.Visible
				: Visibility.Collapsed;
			CheckButterLibWarSails.Visibility = _pendingTemplate == ModpackTemplate.ButterLibWarSails
				? Visibility.Visible
				: Visibility.Collapsed;
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
				App.Toasts.Show(new ToastRequest {
					Title = "Name Required",
					Message = "Enter a name for the new modpack.",
					Severity = ToastSeverity.Warning
				});
				return;
			}

			if (string.IsNullOrWhiteSpace(createdBy)) {
				createdBy = "User";
			}

			// Pass installed mods so the template resolves live version data
			List<ModuleModel> installedMods = App.ModService.CurrentMods;
			bool success = _modpackService.CreateNew(name, createdBy, _pendingTemplate, installedMods);

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
				App.Toasts.Show(new ToastRequest {
					Title = "Modpack Created",
					Message = $"'{name}' created with {_pendingTemplate} template.",
					Severity = ToastSeverity.Success
				});
			} else {
				StatusText = $"Failed to create '{name}'. A modpack with this name may already exist.";
				App.Toasts.Show(new ToastRequest {
					Title = "Create Failed",
					Message = $"A modpack named '{name}' may already exist.",
					Severity = ToastSeverity.Error
				});
			}
		}

		private void CreateCancelButton_Click(object sender, RoutedEventArgs e) {
			HideCreatePanel();
			StatusText = string.Empty;
		}

		#endregion

		#region Save

		/// <summary>
		/// Overwrites the selected modpack's saved data on disk with the current
		/// active load order from the Mods page. Reads from
		/// <see cref="ModpackService.CurrentLoadOrderEntries"/> which is kept
		/// in sync by ModsPage whenever the user drags or reorders mods.
		/// After saving, refreshes both lists so the right side reflects the new disk state.
		/// </summary>
		private void SaveButton_Click(object sender, RoutedEventArgs e) {
			if (_selectedModpack is null) {
				StatusText = "No modpack selected.";
				return;
			}

			if (_modpackService.CurrentLoadOrderEntries.Count == 0) {
				StatusText = "No active load order found. Arrange mods on the Mods page first.";
				App.Toasts.Show(new ToastRequest {
					Title = "Nothing to Save",
					Message = "Arrange mods on the Mods page first.",
					Severity = ToastSeverity.Warning
				});
				return;
			}

			List<ModpackEntryModel> entries = _modpackService.CurrentLoadOrderEntries
				.Select(entry => entry.Clone()).ToList();

			bool success = _modpackService.Save(_selectedModpack, entries);

			if (success) {
				PopulateModpackList();
				PopulateActiveLoadOrder();
				StatusText = $"Saved active load order to '{_selectedModpack.ModpackName}'.";
				App.Toasts.Show(new ToastRequest {
					Title = "Modpack Saved",
					Message = $"'{_selectedModpack.ModpackName}' updated with {entries.Count} entries.",
					Severity = ToastSeverity.Success
				});
			} else {
				StatusText = $"Failed to save '{_selectedModpack.ModpackName}'.";
				App.Toasts.Show(new ToastRequest {
					Title = "Save Failed",
					Message = $"Could not save '{_selectedModpack.ModpackName}'. Check logs for details.",
					Severity = ToastSeverity.Error
				});
			}
		}

		#endregion
	}
}
