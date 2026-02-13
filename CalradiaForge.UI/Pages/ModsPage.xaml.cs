namespace CalradiaForge.UI.Pages {
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.IO;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;

	using CalradiaForge.Core.Infra.Modpacks;
	using CalradiaForge.Core.Infra.Mods;
	using CalradiaForge.Core.Models;

	using Microsoft.Win32;

	/// <summary>
	/// Mods dashboard page. Manages active load order, available mods,
	/// modpack selection, search filtering, and game launch readiness.
	/// </summary>
	public partial class ModsPage : Page, INotifyPropertyChanged {
		#region Fields
		private readonly ModService _modService;
		private readonly ModInstaller _modInstaller;
		private readonly ModpackService _modpackService;
		private bool _canStart;
		private bool _isRefreshing;
		private bool _isInstalling;
		private string _dependencyWarningText = string.Empty;
		private string _searchQuery = string.Empty;
		private int _selectedModpackIndex = -1;
		private ModpackModel? _selectedModpack;
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
		/// Available modpacks for selection via the ComboBox.
		/// </summary>
		public ObservableCollection<ModpackModel> ModpackList { get; set; } = [];
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

		/// <summary>
		/// Indicates whether a refresh is in progress.
		/// Bound to the refresh button's IsEnabled (inverted) to prevent double-clicks.
		/// </summary>
		public bool IsRefreshing {
			get => _isRefreshing;
			set { _isRefreshing = value; OnPropertyChanged(); }
		}

		/// <summary>
		/// Indicates whether a mod installation batch is in progress.
		/// Bound to the install button's IsEnabled (inverted) to prevent overlapping installs.
		/// </summary>
		public bool IsInstalling {
			get => _isInstalling;
			set { _isInstalling = value; OnPropertyChanged(); }
		}

		public int SelectedModpackIndex {
			get => _selectedModpackIndex;
			set { _selectedModpackIndex = value; OnPropertyChanged(); }
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

			_modService = App.ModService;
			_modInstaller = App.ModInstaller;
			_modpackService = App.ModpackService;

			LoadOrderView = CollectionViewSource.GetDefaultView(CurrentLoadOrder);
			LoadOrderView.Filter = ModSearchFilter;

			AvailableModsView = CollectionViewSource.GetDefaultView(AvailableModsList);
			AvailableModsView.Filter = ModSearchFilter;

			PopulateAvailableModsFromCache();
			PopulateModpackList();
		}
		#endregion

		#region Populate Mods

		/// <summary>
		/// Populates <see cref="AvailableModsList"/> from the cached mod data
		/// that was loaded during app startup via <see cref="ModService.LoadFromCache"/>.
		/// </summary>
		private void PopulateAvailableModsFromCache() {
			AvailableModsList.Clear();
			foreach (ModuleModel mod in _modService.CurrentMods) {
				AvailableModsList.Add(mod);
			}
		}

		/// <summary>
		/// Replaces the contents of <see cref="AvailableModsList"/> with fresh data
		/// from the <see cref="ModService"/>. Must be called on the UI thread.
		/// </summary>
		private void UpdateAvailableModsList() {
			AvailableModsList.Clear();
			foreach (ModuleModel mod in _modService.CurrentMods) {
				AvailableModsList.Add(mod);
			}
			AvailableModsView.Refresh();
		}

		#endregion

		#region Populate Modpacks

		/// <summary>
		/// Loads all available modpacks from <see cref="ModpackService"/> into the ComboBox.
		/// Selects the first modpack by default.
		/// </summary>
		private void PopulateModpackList() {
			ModPackComboBox.SelectionChanged -= ModPack_SelectionChanged;
			ModpackList.Clear();

			foreach (ModpackModel modpack in _modpackService.AllModpacks) {
				ModpackList.Add(modpack);
			}

			if (ModpackList.Count > 0) {
				SelectedModpackIndex = 0;
			}

			ModPackComboBox.SelectionChanged += ModPack_SelectionChanged;

			// Load the first modpack's load order
			ApplySelectedModpack();
		}

		/// <summary>
		/// Refreshes the modpack ComboBox from the service. Called when navigating
		/// back to ModsPage after creating modpacks on the ModpacksPage.
		/// </summary>
		public void RefreshModpackList() {
			string? previousSelection = _selectedModpack?.ModpackName;

			ModPackComboBox.SelectionChanged -= ModPack_SelectionChanged;
			ModpackList.Clear();

			foreach (ModpackModel modpack in _modpackService.AllModpacks) {
				ModpackList.Add(modpack);
			}

			// Restore previous selection if still available
			if (previousSelection is not null) {
				int index = ModpackList
					.Select((m, i) => new { m, i })
					.FirstOrDefault(x => string.Equals(x.m.ModpackName, previousSelection, StringComparison.OrdinalIgnoreCase))?.i ?? -1;
				if (index >= 0) {
					SelectedModpackIndex = index;
				} else if (ModpackList.Count > 0) {
					SelectedModpackIndex = 0;
				}
			} else if (ModpackList.Count > 0) {
				SelectedModpackIndex = 0;
			}

			ModPackComboBox.SelectionChanged += ModPack_SelectionChanged;
		}

		#endregion

		#region Modpack Selection

		/// <summary>
		/// Applies the currently selected modpack's load order to the dual lists.
		/// Validates entries against installed mods — valid entries go to LoadOrder,
		/// missing entries are reported in the status text.
		/// Mods not in the modpack remain in AvailableModsList.
		/// </summary>
		private void ApplySelectedModpack() {
			CurrentLoadOrder.Clear();
			DependencyWarningText = string.Empty;

			if (SelectedModpackIndex < 0 || SelectedModpackIndex >= ModpackList.Count) {
				_selectedModpack = null;
				return;
			}

			_selectedModpack = ModpackList[SelectedModpackIndex];

			// Validate the modpack against installed mods
			var (validEntries, missingModNames) = ModpackService.ValidateLoadOrder(
				_selectedModpack, _modService.CurrentMods);

			// Build a set of module IDs that are in the load order
			HashSet<string> loadOrderIds = new(
				validEntries.Select(e => e.ModuleId),
				StringComparer.OrdinalIgnoreCase);

			// Move matching installed mods into the load order (preserving modpack order)
			foreach (ModpackEntryModel entry in validEntries) {
				ModuleModel? installedMod = _modService.CurrentMods
					.FirstOrDefault(m => string.Equals(m.ModuleId, entry.ModuleId, StringComparison.OrdinalIgnoreCase));
				if (installedMod is not null) {
					CurrentLoadOrder.Add(installedMod);
				}
			}

			// Rebuild available mods list excluding those now in the load order
			AvailableModsList.Clear();
			foreach (ModuleModel mod in _modService.CurrentMods) {
				if (!loadOrderIds.Contains(mod.ModuleId)) {
					AvailableModsList.Add(mod);
				}
			}

			// Report missing mods in the status text
			if (missingModNames.Count > 0) {
				string names = string.Join(", ", missingModNames);
				DependencyWarningText = $"{missingModNames.Count} mod(s) not found: {names}";
			}

			// Refresh filtered views
			LoadOrderView.Refresh();
			AvailableModsView.Refresh();

			// Update the service's working copy for cross-page access
			_modpackService.CurrentLoadOrderEntries =
				ModpackService.BuildEntryListFromModules(CurrentLoadOrder.ToList());
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
		/// Loads the selected modpack's load order into the dual lists.
		/// </summary>
		private void ModPack_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			ApplySelectedModpack();
		}
		#endregion

		#region Mod Actions

		/// <summary>
		/// Full mod installation pipeline:
		/// 1. Validate game directory is set and exists
		/// 2. Open multi-select file dialog for archive selection
		/// 3. Batch install mods via async queue
		/// 4. Auto-unblock DLLs in the Modules folder
		/// 5. Refresh the mod list to pick up new installations
		/// 6. Report results via <see cref="DependencyWarningText"/>
		/// </summary>
		private async void InstallModsButton_Click(object sender, RoutedEventArgs e) {
			// Step 1: Validate game directory
			if (!_modInstaller.ValidateGameDirectory(out string validationError)) {
				DependencyWarningText = validationError;
				return;
			}

			// Step 2: Open multi-select file dialog
			OpenFileDialog dialog = new() {
				Title = "Select Mod Archives to Install",
				Filter = ModInstaller.FileDialogFilter,
				Multiselect = true,
				CheckFileExists = true
			};

			if (dialog.ShowDialog() != true || dialog.FileNames.Length == 0) {
				return;
			}

			// Step 3: Batch install mods
			try {
				IsInstalling = true;
				DependencyWarningText = $"Installing {dialog.FileNames.Length} mod(s)...";

				ModInstallSummary installSummary = await _modInstaller.InstallModsAsync(dialog.FileNames);

				DependencyWarningText = installSummary.ToSummaryString();

				// Step 4: Auto-unblock DLLs after installation
				string modulesPath = App.AppConfig.ModulesDirectoryPath;
				if (!string.IsNullOrWhiteSpace(modulesPath) && Directory.Exists(modulesPath)) {
					UnblockResult unblockResult = await DLLUnblocker.UnblockAllAsync(modulesPath);
					DependencyWarningText += $" | DLLs: {unblockResult.ToSummaryString()}";
				}

				// Step 5: Refresh mod list to pick up newly installed mods
				if (installSummary.InstalledCount > 0 || installSummary.UpgradedCount > 0) {
					await _modService.RefreshAsync();
					UpdateAvailableModsList();
					ApplySelectedModpack();
				}

			} catch (OperationCanceledException) {
				DependencyWarningText = "Mod installation was cancelled.";
			} catch (Exception ex) {
				DependencyWarningText = $"Installation error: {ex.Message}";
			} finally {
				IsInstalling = false;
			}
		}

		/// <summary>
		/// Manually unblocks all DLL files in the game's Modules directory.
		/// </summary>
		private async void UnblockDllsButton_Click(object sender, RoutedEventArgs e) {
			string modulesPath = App.AppConfig.ModulesDirectoryPath;
			if (string.IsNullOrWhiteSpace(modulesPath) || !Directory.Exists(modulesPath)) {
				DependencyWarningText = "Modules folder not found. Please check your Settings.";
				return;
			}

			try {
				DependencyWarningText = "Unblocking DLL files...";
				UnblockResult result = await DLLUnblocker.UnblockAllAsync(modulesPath);
				DependencyWarningText = result.ToSummaryString();
			} catch (Exception ex) {
				DependencyWarningText = $"Unblock failed: {ex.Message}";
			}
		}

		/// <summary>
		/// Refreshes the mod list by performing a full async directory scan.
		/// </summary>
		private async void RefreshModsButton_Click(object sender, RoutedEventArgs e) {
			if (_modService.IsRefreshing) {
				return;
			}
			try {
				IsRefreshing = true;
				bool hasChanges = await _modService.RefreshAsync();
				UpdateAvailableModsList();

				if (hasChanges) {
					DependencyWarningText =
						$"{_modService.AddedMods.Count} mod(s) added, " +
						$"{_modService.RemovedMods.Count} mod(s) removed since last scan.";
				} else {
					DependencyWarningText = string.Empty;
				}

				ApplySelectedModpack();
			} catch (OperationCanceledException) {
				// Refresh was cancelled
			} catch (Exception ex) {
				DependencyWarningText = $"Refresh failed: {ex.Message}";
			} finally {
				IsRefreshing = false;
			}
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