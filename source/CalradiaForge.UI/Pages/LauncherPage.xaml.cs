namespace CalradiaForge.UI.Pages {
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Controls.Primitives;
	using System.Windows.Data;

	using CalradiaForge.Core.Infra.Config;
	using CalradiaForge.Core.Infra.Launch;
	using CalradiaForge.Core.Infra.Localization;
	using CalradiaForge.Core.Infra.Modpacks;
	using CalradiaForge.Core.Infra.Mods;
	using CalradiaForge.Core.Models;

	using CalradiaForge.UI.Toasts;

	using GongSolutions.Wpf.DragDrop;

	using Microsoft.Win32;
	using Serilog;

	/// <summary>
	/// Launcher dashboard page. Manages active load order, available mods,
	/// modpack selection, search filtering, and game launch readiness.
	/// Implements <see cref="IDropTarget"/> for GongSolutions drag-and-drop
	/// between the LoadOrder and AvailableMods lists.
	/// </summary>
	public partial class LauncherPage : Page, INotifyPropertyChanged, IDropTarget {
		#region Fields
		private readonly AppSettings _appSettings;
		private readonly ModPipelineManager _modPipeline;
		private readonly ModpackService _modpackService;
		private readonly GameLauncher _gameLauncher;
		private readonly ToastService _toasts;
		private readonly IInstallNotificationPresenter _installNotifications;
		private bool _canStart;
		private bool _isRefreshing;
		private bool _isInstalling;
		private string _dependencyWarningText = string.Empty;
		private string _searchQuery = string.Empty;
		private int _selectedModpackIndex = -1;
		private ModpackModel? _selectedModpack;
		private LaunchTarget _activeLaunchTarget;
		/// <summary>
		/// Shorthand accessor for the active translation strings.
		/// Avoids repeating <c>Translator.Strings</c> throughout the file.
		/// </summary>
		private TranslationStrings T => Translator.Strings;

		/// <summary>Gets the translation service used by page bindings.</summary>
		public TranslationService Translator { get; }

		/// <summary>
		/// Sentinel "ghost" modpack inserted at index 0 when
		/// <see cref="ModpackStartupMode.AlwaysAsk"/> is active.
		/// Has an empty load order so selecting it results in no mods loaded
		/// and <see cref="CanStart"/> evaluating to <c>false</c>.
		/// Identified exclusively via reference equality — never saved to disk.
		/// </summary>
		private readonly ModpackModel _ghostModpack = new("— Select a modpack —", string.Empty, []);
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
		/// May contain <see cref="_ghostModpack"/> at index 0 when in AlwaysAsk mode.
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

		/// <summary>
		/// Text shown on the left Play button face.
		/// Changes based on the currently selected launch target.
		/// Uses translated strings from <see cref="TranslationStrings"/>.
		/// </summary>
		public string PlayButtonText => _activeLaunchTarget == LaunchTarget.BLSE
			? T.Launcher_PlayWithBLSE
			: T.Launcher_PlayBannerlord;
		#endregion

		#region INotifyPropertyChanged
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Raises a <see cref="PropertyChanged"/> notification for the specified property.
		/// </summary>
		protected void OnPropertyChanged([CallerMemberName] string name = "") {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
		#endregion

		#region Constructor
		/// <summary>
		/// Initializes the Launcher page and wires UI bindings and services.
		/// The application startup coordinator completes the authoritative startup
		/// scan before constructing this retained page.
		/// </summary>
		public LauncherPage(
			AppSettings appSettings,
			ModPipelineManager modPipeline,
			ModpackService modpackService,
			GameLauncher gameLauncher,
			ToastService toasts,
			IInstallNotificationPresenter installNotifications,
			TranslationService translator) {
			_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
			_modPipeline = modPipeline ?? throw new ArgumentNullException(nameof(modPipeline));
			_modpackService = modpackService ?? throw new ArgumentNullException(nameof(modpackService));
			_gameLauncher = gameLauncher ?? throw new ArgumentNullException(nameof(gameLauncher));
			_toasts = toasts ?? throw new ArgumentNullException(nameof(toasts));
			_installNotifications = installNotifications ?? throw new ArgumentNullException(nameof(installNotifications));
			Translator = translator ?? throw new ArgumentNullException(nameof(translator));
			InitializeComponent();
			DataContext = this;

			Log.Debug("LauncherPage: Initializing.");
			LoadOrderView = CollectionViewSource.GetDefaultView(CurrentLoadOrder);
			LoadOrderView.Filter = ModSearchFilter;

			AvailableModsView = CollectionViewSource.GetDefaultView(AvailableModsList);
			AvailableModsView.Filter = ModSearchFilter;

			// Restore persisted launch target from config
			_activeLaunchTarget = _appSettings.DefaultLaunchTarget;
			UpdateLaunchTargetCheckmarks();

			// The startup coordinator already published the accepted snapshot and
			// queued any startup validation notification for the readiness drain.
			PopulateAvailableModsFromCache();
			PopulateModpackList(suppressToast: true);
			UpdateCanStart();

			// Re-sync page-local state if the application-owned manager is still
			// completing an install when this retained page is constructed.
			if (_modPipeline.IsInstalling) {
				IsInstalling = true;
				DependencyWarningText = T.Launcher_InstallInProgress;
			}
		}
		#endregion

		#region Populate Mods

		/// <summary>
		/// Populates <see cref="AvailableModsList"/> from the cached mod data
		/// that was loaded into the accepted pipeline snapshot during app startup.
		/// </summary>
		private void PopulateAvailableModsFromCache() {
			AvailableModsList.Clear();
			AcceptedModSnapshot snapshot = _modPipeline.AcceptedSnapshot;
			foreach (ModuleModel mod in snapshot.Modules) {
				AvailableModsList.Add(mod);
			}
			Log.Debug(
				"LauncherPage: Populated {Count} cached mods from snapshot version {SnapshotVersion}.",
				snapshot.Modules.Count,
				snapshot.Version);
		}

		/// <summary>
		/// Replaces the contents of <see cref="AvailableModsList"/> with fresh data
		/// from the accepted pipeline snapshot. Must be called on the UI thread.
		/// </summary>
		private void UpdateAvailableModsList(AcceptedModSnapshot? acceptedSnapshot = null) {
			AvailableModsList.Clear();
			AcceptedModSnapshot snapshot = acceptedSnapshot ?? _modPipeline.AcceptedSnapshot;
			foreach (ModuleModel mod in snapshot.Modules) {
				AvailableModsList.Add(mod);
			}
			AvailableModsView.Refresh();
			Log.Debug(
				"LauncherPage: Updated available mods list with {Count} mods from snapshot version {SnapshotVersion}.",
				snapshot.Modules.Count,
				snapshot.Version);
		}

		#endregion

		#region Populate Modpacks

		/// <summary>
		/// Loads all available modpacks from <see cref="ModpackService"/> into the ComboBox.
		/// Selects the initial modpack based on <see cref="ModpackStartupMode"/>:
		/// <list type="bullet">
		///   <item><see cref="ModpackStartupMode.LastUsed"/> — restores the previously selected modpack by name.</item>
		///   <item><see cref="ModpackStartupMode.AlwaysDefault"/> — selects the built-in "Vanilla" modpack.</item>
		///   <item><see cref="ModpackStartupMode.AlwaysAsk"/> — inserts a ghost sentinel at index 0 and selects it.</item>
		/// </list>
		/// </summary>
		/// <param name="suppressToast">
		/// When <c>true</c>, the subsequent <see cref="ApplySelectedModpack"/> call
		/// will not fire a missing-mods toast.
		/// </param>
		private void PopulateModpackList(bool suppressToast = false) {
			ModPackComboBox.SelectionChanged -= ModPack_SelectionChanged;
			ModpackList.Clear();

			bool isAlwaysAsk = _appSettings.ModpackStartupMode == ModpackStartupMode.AlwaysAsk;

			if (isAlwaysAsk) {
				ModpackList.Add(_ghostModpack);
			}

			foreach (ModpackModel modpack in _modpackService.AllModpacks) {
				ModpackList.Add(modpack);
			}

			SelectedModpackIndex = ResolveStartupModpackIndex();

			ModPackComboBox.SelectionChanged += ModPack_SelectionChanged;

			ApplySelectedModpack(showToast: !suppressToast);
			Log.Debug(
				"LauncherPage: Populated {ModpackCount} modpacks with startup index {StartupIndex}; always ask: {AlwaysAsk}; suppress toast: {SuppressToast}.",
				ModpackList.Count,
				SelectedModpackIndex,
				isAlwaysAsk,
				suppressToast);
		}

		/// <summary>
		/// Determines which modpack index to select on startup based on
		/// the <see cref="AppSettings.ModpackStartupMode"/> setting.
		/// </summary>
		/// <returns>
		/// The resolved index into <see cref="ModpackList"/>.
		/// For AlwaysAsk, returns 0 (the ghost sentinel).
		/// Falls back to 0 (first real modpack) if the target modpack is not found.
		/// </returns>
		private int ResolveStartupModpackIndex() {
			if (ModpackList.Count == 0) {
				return -1;
			}

			ModpackStartupMode mode = _appSettings.ModpackStartupMode;

			switch (mode) {
				case ModpackStartupMode.AlwaysDefault:
					// Find the built-in "Vanilla" modpack by name
					int vanillaIndex = FindModpackIndexByName(VanillaModules.DefaultModpackName);
					return vanillaIndex >= 0 ? vanillaIndex : 0;

				case ModpackStartupMode.LastUsed:
					// Restore the previously selected modpack
					string lastSelected = _appSettings.LastSelectedModpack;
					if (!string.IsNullOrWhiteSpace(lastSelected)) {
						int lastIndex = FindModpackIndexByName(lastSelected);
						if (lastIndex >= 0) {
							return lastIndex;
						}
					}
					return 0;

				case ModpackStartupMode.AlwaysAsk:
					// Ghost sentinel is at index 0
					return 0;

				default:
					return 0;
			}
		}

		/// <summary>
		/// Checks whether the given modpack is the ghost sentinel
		/// used by the AlwaysAsk startup mode.
		/// Uses reference equality — the ghost is a single instance.
		/// </summary>
		private bool IsGhostModpack(ModpackModel? modpack) {
			return ReferenceEquals(modpack, _ghostModpack);
		}

		/// <summary>
		/// Finds the index of a modpack in <see cref="ModpackList"/> by name (case-insensitive).
		/// Skips the ghost sentinel to avoid false matches.
		/// </summary>
		/// <param name="modpackName">The modpack name to search for.</param>
		/// <returns>The index if found; otherwise -1.</returns>
		private int FindModpackIndexByName(string modpackName) {
			return ModpackList
				.Select((m, i) => new { m, i })
				.Where(x => !IsGhostModpack(x.m))
				.FirstOrDefault(x => string.Equals(x.m.ModpackName, modpackName, StringComparison.OrdinalIgnoreCase))?.i ?? -1;
		}

		/// <summary>
		/// Refreshes the modpack ComboBox from the service. Called when navigating
		/// back to LauncherPage after creating modpacks on the ModpacksPage.
		/// Reloads modpacks from disk first to pick up external changes
		/// (added, deleted, or modified modpack files).
		/// Preserves the ghost sentinel at index 0 if AlwaysAsk mode is active
		/// and the user hasn't yet picked a real modpack.
		/// </summary>
		/// <param name="suppressApply">
		/// When <c>true</c>, skips the <see cref="ApplySelectedModpack"/> call
		/// at the end. Used when the caller will apply the modpack separately
		/// (e.g. <see cref="RefreshAvailableMods"/> follows immediately after).
		/// </param>
		public void RefreshModpackList(bool suppressApply = false) {
			_modpackService.Refresh();

			ModpackModel? previousModpack = _selectedModpack;
			bool wasGhostSelected = IsGhostModpack(previousModpack);
			string? previousName = wasGhostSelected ? null : previousModpack?.ModpackName;

			ModPackComboBox.SelectionChanged -= ModPack_SelectionChanged;
			ModpackList.Clear();

			bool isAlwaysAsk = _appSettings.ModpackStartupMode == ModpackStartupMode.AlwaysAsk;

			// Re-inject ghost if AlwaysAsk is active and user was still on it
			if (isAlwaysAsk && wasGhostSelected) {
				ModpackList.Add(_ghostModpack);
			}

			foreach (ModpackModel modpack in _modpackService.AllModpacks) {
				ModpackList.Add(modpack);
			}

			// Restore previous selection
			if (wasGhostSelected) {
				// User hadn't picked yet — keep ghost selected at 0
				SelectedModpackIndex = 0;
			} else if (previousName is not null) {
				int index = FindModpackIndexByName(previousName);
				SelectedModpackIndex = index >= 0 ? index : 0;
			} else if (ModpackList.Count > 0) {
				SelectedModpackIndex = 0;
			}

			ModPackComboBox.SelectionChanged += ModPack_SelectionChanged;

			// Re-apply the selected modpack to update both lists,
			// unless the caller will handle it (e.g. RefreshAvailableMods follows)
			if (!suppressApply) {
				ApplySelectedModpack();
			}
		}

		#endregion

		#region Modpack Selection

		/// <summary>
		/// Applies the currently selected modpack's load order to the dual lists.
		/// If the ghost sentinel is selected, shows all mods as Available with
		/// an empty load order and a prompt in the status text.
		/// For real modpacks, validates entries against installed mods —
		/// valid entries go to LoadOrder, missing entries are reported.
		/// Mods not in the modpack remain in AvailableModsList.
		/// </summary>
		/// <param name="showToast">
		/// When <c>false</c>, missing-mod toast notifications are suppressed.
		/// The status text (<see cref="DependencyWarningText"/>) is always updated
		/// regardless of this flag. Defaults to <c>true</c> so post-startup callers
		/// (user-driven ComboBox changes, manual refresh, install completion) always
		/// show the toast without needing to pass a flag.
		/// </param>
		private void ApplySelectedModpack(
			bool showToast = true,
			AcceptedModSnapshot? acceptedSnapshot = null) {
			AcceptedModSnapshot snapshot = acceptedSnapshot ?? _modPipeline.AcceptedSnapshot;
			CurrentLoadOrder.Clear();
			AvailableModsList.Clear();
			DependencyWarningText = string.Empty;

			if (SelectedModpackIndex < 0 || SelectedModpackIndex >= ModpackList.Count) {
				_selectedModpack = null;
				// No modpack selected — all mods go to Available
				foreach (ModuleModel mod in snapshot.Modules) {
					AvailableModsList.Add(mod);
				}
				LoadOrderView.Refresh();
				AvailableModsView.Refresh();
				UpdateCanStart();
				Log.Debug(
					"LauncherPage: No modpack selected; {AvailableCount} mods are available.",
					AvailableModsList.Count);
				return;
			}

			_selectedModpack = ModpackList[SelectedModpackIndex];

			// Ghost sentinel — empty load order, prompt user to pick
			if (IsGhostModpack(_selectedModpack)) {
				foreach (ModuleModel mod in snapshot.Modules) {
					AvailableModsList.Add(mod);
				}
				DependencyWarningText = T.Launcher_SelectModpackPrompt;
				LoadOrderView.Refresh();
				AvailableModsView.Refresh();
				UpdateCanStart();
				Log.Debug("LauncherPage: Ghost modpack selected.");
				return;
			}

			// Validate the modpack against installed mods
			var (validEntries, missingModNames) = ModpackService.ValidateLoadOrder(
				_selectedModpack, snapshot.Modules);

			// Build a set of module IDs that are in the load order
			HashSet<string> loadOrderIds = new(
				validEntries.Select(e => e.ModuleId),
				StringComparer.OrdinalIgnoreCase);

			// Move matching installed mods into the load order (preserving modpack order)
			foreach (ModpackEntryModel entry in validEntries) {
				ModuleModel? installedMod = snapshot.Modules
					.FirstOrDefault(m => string.Equals(m.ModuleId, entry.ModuleId, StringComparison.OrdinalIgnoreCase));
				if (installedMod is not null) {
					CurrentLoadOrder.Add(installedMod);
				}
			}

			// Rebuild available mods list — mods not in the load order
			foreach (ModuleModel mod in snapshot.Modules) {
				if (string.IsNullOrEmpty(mod.ModuleId) || !loadOrderIds.Contains(mod.ModuleId)) {
					AvailableModsList.Add(mod);
				}
			}

			// Report missing mods — status text is always updated,
			// toast is gated by the showToast parameter
			if (missingModNames.Count > 0) {
				string names = string.Join(", ", missingModNames);
				DependencyWarningText = $"{missingModNames.Count} mod(s) not found: {names}";

				if (showToast) {
					// Build a line-per-mod message for the toast
					string toastBody = string.Join("\n", missingModNames.Select(n => $"• {n}"));
					_toasts.Show(new ToastRequest {
						Title = $"{missingModNames.Count} {T.Toast_MissingMods}",
						Message = toastBody,
						Severity = ToastSeverity.Warning,
						TemplateKey = ToastTemplateKeys.MissingMods,
						Duration = TimeSpan.FromSeconds(10),
						AllowClickDismiss = false
					});
				}
			}

			// Refresh filtered views
			LoadOrderView.Refresh();
			AvailableModsView.Refresh();

			// Update the service's working copy for cross-page access
			SyncLoadOrderToService();
			UpdateCanStart();
			Log.Debug(
				"LauncherPage: Applied modpack {ModpackName} with {LoadOrderCount} load-order entries, {AvailableCount} available mods, and {MissingCount} missing mods.",
				_selectedModpack.ModpackName,
				CurrentLoadOrder.Count,
				AvailableModsList.Count,
				missingModNames.Count);
		}

		#endregion

		#region Drag and Drop — IDropTarget

		/// <summary>
		/// Called by GongSolutions during a drag hover to decide whether the drop is valid.
		/// Allows reordering within the same list and moving items between the two lists.
		/// </summary>
		void IDropTarget.DragOver(IDropInfo dropInfo) {
			if (dropInfo.Data is not ModuleModel) {
				return;
			}

			dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
			dropInfo.Effects = DragDropEffects.Move;
		}

		/// <summary>
		/// Called by GongSolutions when the user releases a dragged item.
		/// Handles reordering within a list and moving items between lists.
		/// After any change, syncs the working load order to the service.
		/// </summary>
		void IDropTarget.Drop(IDropInfo dropInfo) {
			if (dropInfo.Data is not ModuleModel mod) {
				return;
			}

			ObservableCollection<ModuleModel>? sourceCollection = GetOwningCollection(dropInfo.DragInfo.SourceCollection);
			ObservableCollection<ModuleModel>? targetCollection = GetOwningCollection(dropInfo.TargetCollection);

			if (sourceCollection is null || targetCollection is null) {
				return;
			}

			int removeIndex = sourceCollection.IndexOf(mod);
			if (removeIndex < 0) {
				return;
			}

			sourceCollection.RemoveAt(removeIndex);

			int insertIndex = dropInfo.InsertIndex;

			// Clamp insert index when moving within the same collection
			// because the removal shifted indices
			if (ReferenceEquals(sourceCollection, targetCollection) && insertIndex > removeIndex) {
				insertIndex--;
			}

			if (insertIndex < 0) {
				insertIndex = 0;
			}
			if (insertIndex > targetCollection.Count) {
				insertIndex = targetCollection.Count;
			}

			targetCollection.Insert(insertIndex, mod);

			// Refresh filtered views so search still works
			LoadOrderView.Refresh();
			AvailableModsView.Refresh();

			// Keep the service in sync after every drag operation
			SyncLoadOrderToService();
			UpdateCanStart();
		}

		/// <summary>
		/// Resolves the underlying <see cref="ObservableCollection{ModuleModel}"/>
		/// from a GongSolutions collection reference (which may be a CollectionView).
		/// </summary>
		private ObservableCollection<ModuleModel>? GetOwningCollection(System.Collections.IEnumerable? collection) {
			if (collection == LoadOrderView || collection == CurrentLoadOrder) {
				return CurrentLoadOrder;
			}
			if (collection == AvailableModsView || collection == AvailableModsList) {
				return AvailableModsList;
			}
			return null;
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
		/// When the user picks a real modpack after the ghost sentinel,
		/// removes the ghost from the list so it can't be re-selected.
		/// </summary>
		private void ModPack_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			// If user picked a real modpack, remove the ghost sentinel
			if (SelectedModpackIndex >= 0
				&& SelectedModpackIndex < ModpackList.Count
				&& !IsGhostModpack(ModpackList[SelectedModpackIndex])
				&& ModpackList.Count > 0
				&& IsGhostModpack(ModpackList[0])) {

				ModPackComboBox.SelectionChanged -= ModPack_SelectionChanged;

				// Capture the real modpack name before removal shifts indices
				string selectedName = ModpackList[SelectedModpackIndex].ModpackName;
				ModpackList.RemoveAt(0);

				// Re-find the index after ghost removal
				int newIndex = FindModpackIndexByName(selectedName);
				SelectedModpackIndex = newIndex >= 0 ? newIndex : 0;

				ModPackComboBox.SelectionChanged += ModPack_SelectionChanged;
			}

			ApplySelectedModpack();
			Log.Debug(
				"LauncherPage: Modpack selection changed to index {SelectedIndex} ({SelectedName}).",
				SelectedModpackIndex,
				_selectedModpack?.ModpackName);
		}
		#endregion

		#region Mod Actions

		/// <summary>
		/// Opens a file dialog for archive selection and delegates the authoritative
		/// install, validation, and reconciliation pipeline to
		/// <see cref="ModPipelineManager"/>. Application-wide progress and terminal
		/// presentation remain owned by <see cref="IInstallNotificationPresenter"/>.
		/// </summary>
		private async void InstallModsButton_Click(object sender, RoutedEventArgs e) {
			OpenFileDialog dialog = new() {
				Title = T.Launcher_InstallDialogTitle,
				Filter = ModInstaller.FileDialogFilter,
				Multiselect = true,
				CheckFileExists = true
			};

			if (dialog.ShowDialog() != true || dialog.FileNames.Length == 0) {
				return;
			}

			Log.Debug("LauncherPage: Installing mods from {ArchiveCount} archives.", dialog.FileNames.Length);
			IsInstalling = true;
			DependencyWarningText = T.Launcher_InstallInProgress;
			ModInstallOperationResult result = await _modPipeline.InstallAsync(dialog.FileNames);
			if (!result.WasAdmitted) {
				IsInstalling = false;
				DependencyWarningText = result.Status switch {
					ModInstallOperationStatus.RejectedBusy => T.Toast_InstallRejectedBusy,
					ModInstallOperationStatus.RejectedAdmissionStopped => T.Toast_InstallRejectedAdmissionStopped,
					_ => T.Toast_InstallValidationFailed
				};
				return;
			}

			try {
				UpdateAvailableModsList(result.AcceptedSnapshot);
				ApplySelectedModpack(acceptedSnapshot: result.AcceptedSnapshot);
				UpdateCanStart();
				string installStatus = string.Format(
					T.Toast_InstallSummaryFormat,
					result.Summary.InstalledCount,
					result.Summary.UpgradedCount,
					result.Summary.SkippedCount,
					result.Summary.FailedCount);
				DependencyWarningText = string.IsNullOrWhiteSpace(DependencyWarningText)
					? installStatus
					: $"{DependencyWarningText} | {installStatus}";
				_installNotifications.ReportLauncherCompletion(
					new LauncherInstallPresentationCompletion(result.OperationId, true));
			} catch (Exception ex) {
				Log.Error(
					ex,
					"LauncherPage: Presentation reconciliation failed for install {OperationId}.",
					result.OperationId);
				DependencyWarningText = T.Toast_InstallReconciliationFailed;
				_installNotifications.ReportLauncherCompletion(
					new LauncherInstallPresentationCompletion(
						result.OperationId,
						false,
						ex.ToString()));
			} finally {
				IsInstalling = false;
			}
		}

		/// <summary>
		/// Refreshes the mod list by performing a full async directory scan.
		/// </summary>
		private async void RefreshModsButton_Click(object sender, RoutedEventArgs e) {
			if (_modPipeline.IsRefreshing) {
				return;
			}
			try {
				IsRefreshing = true;
				ModPipelineResult result = await _modPipeline.RefreshAsync();
				UpdateAvailableModsList();

				if (!result.Success) {
					DependencyWarningText = result.UserSummary;
					ShowPipelineWarning(result);
				} else if (result.HasChanges) {
					AcceptedModSnapshot snapshot = result.AcceptedSnapshot;
					DependencyWarningText =
						$"{snapshot.AddedModules.Count} mod(s) added, " +
						$"{snapshot.RemovedModules.Count} mod(s) removed since last scan.";

					_toasts.Show(new ToastRequest {
						Title = T.Toast_ModListUpdated,
						Message = $"{snapshot.AddedModules.Count} added, {snapshot.RemovedModules.Count} removed.",
						Severity = ToastSeverity.Info
					});
				} else {
					DependencyWarningText = string.Empty;
				}

				ApplySelectedModpack();
			} catch (OperationCanceledException) {
				// Refresh was cancelled
			} catch (Exception ex) {
				DependencyWarningText = $"Refresh failed: {ex.Message}";
				_toasts.Show(new ToastRequest {
					Title = T.Toast_RefreshFailed,
					Message = ex.Message,
					Severity = ToastSeverity.Error
				});
			} finally {
				IsRefreshing = false;
			}
		}
		#endregion

		#region Game Launch

		/// <summary>
		/// Saves the current load order, persists the selected modpack,
		/// and launches Bannerlord via the <see cref="GameLauncher"/> service
		/// using the active <see cref="_activeLaunchTarget"/>.
		/// For Steam installs, auto-starts Steam if it's not running and waits
		/// for initialization before launching the game.
		/// Skips persist and launch if the ghost sentinel is selected.
		/// </summary>
		private async void PlayButton_Click(object sender, RoutedEventArgs e) {
			// Defensive — ghost should never reach here (CanStart is false)
			if (IsGhostModpack(_selectedModpack)) {
				return;
			}

			// Save last-used load order
			List<ModpackEntryModel> currentEntries =
				ModpackService.BuildEntryListFromModules(CurrentLoadOrder.ToList());
			_modpackService.SaveLastUsed(currentEntries);

			// Persist which modpack was selected
			if (_selectedModpack is not null) {
				_appSettings.LastSelectedModpack = _selectedModpack.ModpackName;
			}

			// Launch the game with the active target (auto-starts Steam if needed)
			CanStart = false;
			DependencyWarningText = T.Launcher_Launching;

			GameLaunchResult result = await _gameLauncher.LaunchAsync(CurrentLoadOrder.ToList(), _activeLaunchTarget);
			DependencyWarningText = result.Message;

			_toasts.Show(new ToastRequest {
				Title = result.Success ? T.Toast_GameLaunched : T.Toast_LaunchFailed,
				Message = result.Message,
				Severity = result.Success ? ToastSeverity.Success : ToastSeverity.Error
			});

			UpdateCanStart();
			Log.Debug(
				"LauncherPage: Launch target {LaunchTarget} completed with success {Success}: {ResultMessage}",
				_activeLaunchTarget,
				result.Success,
				result.Message);
		}

		/// <summary>
		/// Opens the launch target context menu when the dropdown chevron is clicked.
		/// </summary>
		private void PlayTargetDropdown_Click(object sender, RoutedEventArgs e) {
			if (sender is Button button && button.ContextMenu is not null) {
				button.ContextMenu.PlacementTarget = button;
				button.ContextMenu.Placement = PlacementMode.Bottom;
				button.ContextMenu.IsOpen = true;
			}
		}

		/// <summary>
		/// Selects Bannerlord as the active launch target.
		/// Persists the choice and updates checkmarks. Does NOT launch.
		/// </summary>
		private void LaunchTarget_Bannerlord_Click(object sender, RoutedEventArgs e) {
			SetActiveLaunchTarget(LaunchTarget.Bannerlord);
		}

		/// <summary>
		/// Selects BLSE as the active launch target.
		/// Persists the choice and updates checkmarks. Does NOT launch.
		/// If BLSE is not configured, the Play button disables and a warning appears.
		/// </summary>
		private void LaunchTarget_BLSE_Click(object sender, RoutedEventArgs e) {
			SetActiveLaunchTarget(LaunchTarget.BLSE);
		}

		/// <summary>
		/// Applies the given launch target as the active selection.
		/// Persists to config, updates checkmarks, updates button label text,
		/// and re-evaluates <see cref="CanStart"/> to account for BLSE validity.
		/// </summary>
		private void SetActiveLaunchTarget(LaunchTarget target) {
			_activeLaunchTarget = target;
			_appSettings.DefaultLaunchTarget = target;
			UpdateLaunchTargetCheckmarks();
			OnPropertyChanged(nameof(PlayButtonText));
			UpdateCanStart();
			Log.Debug("LauncherPage: Launch target set to {LaunchTarget}.", target);
		}

		/// <summary>
		/// Toggles the checkmark icon visibility in the launch target dropdown.
		/// Only the currently persisted target shows its checkmark.
		/// </summary>
		private void UpdateLaunchTargetCheckmarks() {
			CheckBannerlord.Visibility = _activeLaunchTarget == LaunchTarget.Bannerlord
				? Visibility.Visible
				: Visibility.Collapsed;
			CheckBLSE.Visibility = _activeLaunchTarget == LaunchTarget.BLSE
				? Visibility.Visible
				: Visibility.Collapsed;
		}

		/// <summary>
		/// Updates <see cref="CanStart"/> based on whether the load order has mods,
		/// the base game config is valid, and (when BLSE is selected) the BLSE
		/// executable is configured and exists. When BLSE validation fails,
		/// disables the Play button and sets a warning in <see cref="DependencyWarningText"/>.
		/// The selection stays on BLSE so the user can fix it in Settings.
		/// </summary>
		private void UpdateCanStart() {
			bool hasLoadOrder = CurrentLoadOrder.Count > 0;
			bool canLaunchBase = _gameLauncher.CanLaunch(out _);

			if (_activeLaunchTarget == LaunchTarget.BLSE) {
				bool canLaunchBLSE = _gameLauncher.CanLaunchBLSE(out string blseError);
				CanStart = hasLoadOrder && canLaunchBase && canLaunchBLSE;

				// Show BLSE warning when invalid and no higher-priority message is displayed
				if (!canLaunchBLSE && hasLoadOrder && canLaunchBase
					&& string.IsNullOrEmpty(DependencyWarningText)) {
					DependencyWarningText = blseError;
				}
			} else {
				CanStart = hasLoadOrder && canLaunchBase;
			}
			Log.Debug(
				"LauncherPage: CanStart evaluated to {CanStart}; has load order: {HasLoadOrder}; can launch base game: {CanLaunchBase}; target: {LaunchTarget}.",
				CanStart,
				hasLoadOrder,
				canLaunchBase,
				_activeLaunchTarget);
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Pushes the current <see cref="CurrentLoadOrder"/> into the
		/// <see cref="ModpackService.CurrentLoadOrderEntries"/> for cross-page access.
		/// </summary>
		private void SyncLoadOrderToService() {
			_modpackService.CurrentLoadOrderEntries =
				ModpackService.BuildEntryListFromModules(CurrentLoadOrder.ToList());
			Log.Debug(
				"LauncherPage: Synced {EntryCount} load-order entries.",
				_modpackService.CurrentLoadOrderEntries.Count);
		}
		#endregion

		#region Auto Refresh

		/// <summary>
		/// Refreshes the available mods list by performing a live directory scan.
		/// Called when navigating back to LauncherPage to pick up any changes
		/// from mod installations, deletions, cache clears, or rescans
		/// performed elsewhere (e.g. mods removed from the game directory).
		/// </summary>
		public async void RefreshAvailableMods() {
			if (_modPipeline.IsRefreshing) {
				return;
			}
			try {
				IsRefreshing = true;
				Log.Debug("LauncherPage: Refreshing available mods.");
				ModPipelineResult result = await _modPipeline.RefreshAsync();
				UpdateAvailableModsList();
				ApplySelectedModpack();
				if (!result.Success) {
					DependencyWarningText = result.UserSummary;
					ShowPipelineWarning(result);
				}
			} catch (OperationCanceledException) {
				// Refresh was cancelled — no action needed
			} catch (Exception ex) {
				DependencyWarningText = $"Refresh failed: {ex.Message}";
				_toasts.Show(new ToastRequest {
					Title = T.Toast_RefreshFailed,
					Message = ex.Message,
					Severity = ToastSeverity.Error
				});
			} finally {
				IsRefreshing = false;
				Log.Debug(
					"LauncherPage: Available-mod refresh completed with {ModCount} accepted mods.",
					_modPipeline.AcceptedSnapshot.Modules.Count);
			}
		}

		private void ShowPipelineWarning(ModPipelineResult result) {
			if (result.Status is ModPipelineStatus.Busy or ModPipelineStatus.Cancelled) {
				return;
			}
			_toasts.Show(new ToastRequest {
				Title = T.Toast_RefreshFailed,
				Message = result.UserSummary,
				Severity = ToastSeverity.Warning
			});
		}
		#endregion
	}
}
