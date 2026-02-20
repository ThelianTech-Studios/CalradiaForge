namespace CalradiaForge.UI.Pages {
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.IO;
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
	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Infra.Modpacks;
	using CalradiaForge.Core.Infra.Mods;
	using CalradiaForge.Core.Models;

	using CalradiaForge.UI.Toasts;

	using GongSolutions.Wpf.DragDrop;

	using Microsoft.Win32;

	/// <summary>
	/// Mods dashboard page. Manages active load order, available mods,
	/// modpack selection, search filtering, and game launch readiness.
	/// Implements <see cref="IDropTarget"/> for GongSolutions drag-and-drop
	/// between the LoadOrder and AvailableMods lists.
	/// </summary>
	public partial class ModsPage : Page, INotifyPropertyChanged, IDropTarget {
		#region Fields
		private readonly ModService _modService;
		private readonly ModInstaller _modInstaller;
		private readonly ModpackService _modpackService;
		private readonly GameLauncher _gameLauncher;
		private readonly Logger _logger = Logger.Instance;
		private bool _canStart;
		private bool _isRefreshing;
		private bool _isInstalling;
		private string _dependencyWarningText = string.Empty;
		private string _searchQuery = string.Empty;
		private int _selectedModpackIndex = -1;
		private ModpackModel? _selectedModpack;
		private LaunchTarget _activeLaunchTarget;
		private Guid _installToastId;
		private DateTime _lastExtractionProgressUpdate = DateTime.MinValue;

		/// <summary>
		/// Tracks whether the initial startup scan from <see cref="StartupRescanAsync"/>
		/// has completed. Pre-scan calls to <see cref="ApplySelectedModpack"/> suppress
		/// toast notifications to prevent duplicate toasts during the startup sequence
		/// (constructor → Loaded → scan). Only the post-scan apply shows the toast.
		/// Also gates <see cref="RefreshAvailableMods"/> and <see cref="RefreshModpackList"/>
		/// to prevent duplicate work while the startup scan is still in progress.
		/// </summary>
		private bool _hasCompletedInitialScan;

		/// <summary>
		/// Minimum interval between extraction progress UI updates.
		/// Prevents dispatcher flooding on fast SSDs where hundreds of
		/// per-file events fire before the UI can render a single frame.
		/// </summary>
		private static readonly TimeSpan _extractionThrottleInterval = TimeSpan.FromMilliseconds(150);

		/// <summary>
		/// Shorthand accessor for the active translation strings.
		/// Avoids repeating <c>App.Translator.Strings</c> throughout the file.
		/// </summary>
		private static TranslationStrings T => App.Translator.Strings;

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
			? T.Mods_PlayWithBLSE
			: T.Mods_PlayBannerlord;
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
		/// Initializes the mods page and wires UI bindings and services.
		/// The constructor populates lists from cached data with toasts suppressed.
		/// The authoritative scan and single toast fire happens later in
		/// <see cref="StartupRescanAsync"/> triggered by the Loaded event.
		/// </summary>
		public ModsPage() {
			InitializeComponent();
			DataContext = this;

			_modService = App.ModService;
			_modInstaller = App.ModInstaller;
			_modpackService = App.ModpackService;
			_gameLauncher = App.GameLauncher;

			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModsPage: Initializing.");
			}
			LoadOrderView = CollectionViewSource.GetDefaultView(CurrentLoadOrder);
			LoadOrderView.Filter = ModSearchFilter;

			AvailableModsView = CollectionViewSource.GetDefaultView(AvailableModsList);
			AvailableModsView.Filter = ModSearchFilter;

			// Restore persisted launch target from config
			_activeLaunchTarget = App.AppConfig.DefaultLaunchTarget;
			UpdateLaunchTargetCheckmarks();

			// Populate from cache with toasts suppressed — the startup scan
			// in Loaded will apply the modpack authoritatively with toasts
			PopulateAvailableModsFromCache();
			PopulateModpackList(suppressToast: true);
			UpdateCanStart();

			// If an install was already running when the user navigated back,
			// re-sync the UI state from the service
			if (_modInstaller.IsInstalling) {
				IsInstalling = true;
				DependencyWarningText = T.Mods_InstallInProgress;
			}

			// Subscribe/unsubscribe in Loaded/Unloaded to survive WPF's
			// extra layout cycles that can fire Unloaded on cold start.
			Loaded += ModsPage_Loaded;
			Unloaded += ModsPage_Unloaded;
		}

		/// <summary>
		/// Handles the Loaded event — subscribes to install service events
		/// and kicks off the startup rescan. Using Loaded ensures handlers
		/// are always wired even after WPF re-layout Unloaded/Loaded cycles.
		/// <see cref="StartupRescanAsync"/> is the single authoritative startup
		/// path — it scans, applies the modpack with toasts enabled, and sets
		/// <see cref="_hasCompletedInitialScan"/>. All earlier calls from the
		/// constructor run with toasts suppressed.
		/// </summary>
		private async void ModsPage_Loaded(object sender, RoutedEventArgs e) {
			// Guard against duplicate subscriptions from repeated Loaded fires
			_modInstaller.InstallProgressChanged -= OnInstallProgressChanged;
			_modInstaller.InstallCompleted -= OnInstallCompleted;
			_modInstaller.ExtractionProgressChanged -= OnExtractionProgressChanged;

			_modInstaller.InstallProgressChanged += OnInstallProgressChanged;
			_modInstaller.InstallCompleted += OnInstallCompleted;
			_modInstaller.ExtractionProgressChanged += OnExtractionProgressChanged;

			// Re-sync install state in case Unloaded fired mid-install
			if (_modInstaller.IsInstalling && _installToastId == Guid.Empty) {
				IsInstalling = true;
				DependencyWarningText = T.Mods_InstallInProgress;
			}

			await StartupRescanAsync();
		}

		/// <summary>
		/// Handles the Unloaded event — unsubscribes from install service events.
		/// </summary>
		private void ModsPage_Unloaded(object sender, RoutedEventArgs e) {
			_modInstaller.InstallProgressChanged -= OnInstallProgressChanged;
			_modInstaller.InstallCompleted -= OnInstallCompleted;
			_modInstaller.ExtractionProgressChanged -= OnExtractionProgressChanged;
		}
		#endregion

		#region Install Event Handlers

		/// <summary>
		/// Handles progress updates from the <see cref="ModInstaller"/> service.
		/// Dispatches to the UI thread to update both the status text and the
		/// persistent progress toast.
		/// </summary>
		private void OnInstallProgressChanged(ModInstallSummary summary) {
			Dispatcher.BeginInvoke(() => {
				string progressMsg = $"Processed {summary.TotalCount} archive(s)... " +
					$"({summary.InstalledCount} installed, {summary.SkippedCount} skipped, {summary.FailedCount} failed)";
				DependencyWarningText = progressMsg;

				if (_installToastId != Guid.Empty) {
					App.Toasts.UpdateProgress(_installToastId, summary.TotalCount, summary.TotalCount + 1, progressMsg);
				}
			});
		}

		/// <summary>
		/// Handles install batch completion from the <see cref="ModInstaller"/> service.
		/// Dispatches to the UI thread to update status, close the progress toast,
		/// show a summary toast, refresh the mod list, run DLL unblocking,
		/// and re-evaluate <see cref="CanStart"/>.
		/// </summary>
		private async void OnInstallCompleted(ModInstallSummary summary) {
			await Dispatcher.InvokeAsync(async () => {
				if (_installToastId != Guid.Empty) {
					App.Toasts.Close(_installToastId);
					_installToastId = Guid.Empty;
				}

				DependencyWarningText = summary.ToSummaryString();

				ToastSeverity severity = summary.FailedCount > 0
					? ToastSeverity.Warning
					: ToastSeverity.Success;

				App.Toasts.Show(new ToastRequest {
					Title = summary.FailedCount > 0 ? T.Toast_InstallCompleteWithErrors : T.Toast_InstallComplete,
					Message = summary.ToSummaryString(),
					Severity = severity
				});

				string modulesPath = App.AppConfig.ModulesDirectoryPath;
				if (!string.IsNullOrWhiteSpace(modulesPath) && Directory.Exists(modulesPath)) {
					UnblockResult unblockResult = await DLLUnblocker.UnblockAllAsync(modulesPath);
					DependencyWarningText += $" | DLLs: {unblockResult.ToSummaryString()}";
				}

				if (summary.InstalledCount > 0 || summary.UpgradedCount > 0) {
					await _modService.RefreshAsync();
					UpdateAvailableModsList();
					ApplySelectedModpack();
				}

				IsInstalling = false;
				UpdateCanStart();
			});
		}

		/// <summary>
		/// Handles per-entry extraction progress from the <see cref="ModInstaller"/> service.
		/// Computes a batch-level ETA based on cumulative files extracted across all
		/// archives, using <see cref="ExtractionProgress.BatchStartUtc"/> as the stable
		/// reference point. The progress bar represents the entire batch, not individual
		/// archives, so it never resets mid-install.
		/// Uses <see cref="ExtractionProgress.TimestampUtc"/> captured on the extraction
		/// thread to avoid dispatcher-queue delay inflating the elapsed time.
		/// Throttled to prevent dispatcher flooding on fast SSDs — only dispatches
		/// a UI update when at least 150 ms have elapsed since the last one.
		/// </summary>
		private void OnExtractionProgressChanged(ExtractionProgress progress) {
			// Throttle: skip this event if we updated the UI too recently.
			// This runs on the extraction thread so use the event's own timestamp
			// to avoid clock skew with dispatcher-queued DateTime.UtcNow calls.
			DateTime now = progress.TimestampUtc;
			bool isFinalUpdate = progress.BatchFilesExtracted >= progress.EstimatedTotalFiles;
			if (!isFinalUpdate && (now - _lastExtractionProgressUpdate) < _extractionThrottleInterval) {
				return;
			}
			_lastExtractionProgressUpdate = now;

			Dispatcher.BeginInvoke(() => {
				// Build the progress message
				string archiveName = Path.GetFileNameWithoutExtension(progress.ArchiveFileName);
				string batchPosition = $"[{progress.ArchiveIndex}/{progress.TotalArchives}]";
				string filesText = $"{progress.BatchFilesExtracted} {T.Common_FilesExtracted}";

				// Calculate batch-level ETA using extraction-thread timestamps
				string eta = string.Empty;
				if (progress.BatchFilesExtracted > 5 && progress.EstimatedTotalFiles > 0) {
					TimeSpan elapsed = progress.TimestampUtc - progress.BatchStartUtc;
					if (elapsed.TotalMilliseconds > 250) {
						double filesPerSec = progress.BatchFilesExtracted / elapsed.TotalSeconds;
						if (filesPerSec > 0) {
							int remaining = progress.EstimatedTotalFiles - progress.BatchFilesExtracted;
							if (remaining > 0) {
								TimeSpan timeLeft = TimeSpan.FromSeconds(remaining / filesPerSec);
								eta = timeLeft.TotalSeconds < 5
									? $" — {T.Common_AlmostDone}"
									: $" — ~{FormatTimeRemaining(timeLeft)} {T.Common_Remaining}";
							} else {
								eta = $" — {T.Common_AlmostDone}";
							}
						}
					}
				}

				string message = $"{batchPosition} {T.Common_Extracting}: {archiveName}\n{filesText}{eta}";
				DependencyWarningText = message;

				// Update the persistent progress toast with batch-level values
				if (_installToastId != Guid.Empty) {
					App.Toasts.UpdateProgress(
						_installToastId,
						progress.BatchFilesExtracted,
						progress.EstimatedTotalFiles,
						message);
				}
			});
		}

		/// <summary>
		/// Formats a <see cref="TimeSpan"/> as a short human-readable ETA string.
		/// </summary>
		private static string FormatTimeRemaining(TimeSpan time) {
			if (time.TotalMinutes >= 1) {
				return $"{(int)time.TotalMinutes}m {time.Seconds}s";
			}
			return $"{time.Seconds}s";
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
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModsPage: Populated cached mods.", new { Count = _modService.CurrentMods.Count });
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
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModsPage: Updated available mods list.", new { Count = _modService.CurrentMods.Count });
			}
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
		/// will not fire a missing-mods toast. Used during the constructor to avoid
		/// duplicate toasts before <see cref="StartupRescanAsync"/> completes.
		/// </param>
		private void PopulateModpackList(bool suppressToast = false) {
			ModPackComboBox.SelectionChanged -= ModPack_SelectionChanged;
			ModpackList.Clear();

			bool isAlwaysAsk = App.AppConfig.ModpackStartupMode == ModpackStartupMode.AlwaysAsk;

			if (isAlwaysAsk) {
				ModpackList.Add(_ghostModpack);
			}

			foreach (ModpackModel modpack in _modpackService.AllModpacks) {
				ModpackList.Add(modpack);
			}

			SelectedModpackIndex = ResolveStartupModpackIndex();

			ModPackComboBox.SelectionChanged += ModPack_SelectionChanged;

			ApplySelectedModpack(showToast: !suppressToast);
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModsPage: Populated modpack list.", new { Count = ModpackList.Count, StartupIndex = SelectedModpackIndex, AlwaysAsk = isAlwaysAsk, SuppressToast = suppressToast });
			}
		}

		/// <summary>
		/// Determines which modpack index to select on startup based on
		/// the <see cref="AppConfigSettings.ModpackStartupMode"/> setting.
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

			ModpackStartupMode mode = App.AppConfig.ModpackStartupMode;

			switch (mode) {
				case ModpackStartupMode.AlwaysDefault:
					// Find the built-in "Vanilla" modpack by name
					int vanillaIndex = FindModpackIndexByName(VanillaModules.DefaultModpackName);
					return vanillaIndex >= 0 ? vanillaIndex : 0;

				case ModpackStartupMode.LastUsed:
					// Restore the previously selected modpack
					string lastSelected = App.AppConfig.LastSelectedModpack;
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
		/// back to ModsPage after creating modpacks on the ModpacksPage.
		/// Reloads modpacks from disk first to pick up external changes
		/// (added, deleted, or modified modpack files).
		/// Preserves the ghost sentinel at index 0 if AlwaysAsk mode is active
		/// and the user hasn't yet picked a real modpack.
		/// Skipped if the initial startup scan has not yet completed,
		/// because <see cref="StartupRescanAsync"/> handles the first apply.
		/// </summary>
		/// <param name="suppressApply">
		/// When <c>true</c>, skips the <see cref="ApplySelectedModpack"/> call
		/// at the end. Used when the caller will apply the modpack separately
		/// (e.g. <see cref="RefreshAvailableMods"/> follows immediately after).
		/// </param>
		public void RefreshModpackList(bool suppressApply = false) {
			if (!_hasCompletedInitialScan) {
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModsPage: Skipping modpack refresh — initial scan not yet complete.");
				}
				return;
			}

			_modpackService.Refresh();

			ModpackModel? previousModpack = _selectedModpack;
			bool wasGhostSelected = IsGhostModpack(previousModpack);
			string? previousName = wasGhostSelected ? null : previousModpack?.ModpackName;

			ModPackComboBox.SelectionChanged -= ModPack_SelectionChanged;
			ModpackList.Clear();

			bool isAlwaysAsk = App.AppConfig.ModpackStartupMode == ModpackStartupMode.AlwaysAsk;

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
		private void ApplySelectedModpack(bool showToast = true) {
			CurrentLoadOrder.Clear();
			AvailableModsList.Clear();
			DependencyWarningText = string.Empty;

			if (SelectedModpackIndex < 0 || SelectedModpackIndex >= ModpackList.Count) {
				_selectedModpack = null;
				// No modpack selected — all mods go to Available
				foreach (ModuleModel mod in _modService.CurrentMods) {
					AvailableModsList.Add(mod);
				}
				LoadOrderView.Refresh();
				AvailableModsView.Refresh();
				UpdateCanStart();
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModsPage: No modpack selected.", new { AvailableCount = AvailableModsList.Count });
				}
				return;
			}

			_selectedModpack = ModpackList[SelectedModpackIndex];

			// Ghost sentinel — empty load order, prompt user to pick
			if (IsGhostModpack(_selectedModpack)) {
				foreach (ModuleModel mod in _modService.CurrentMods) {
					AvailableModsList.Add(mod);
				}
				DependencyWarningText = T.Mods_SelectModpackPrompt;
				LoadOrderView.Refresh();
				AvailableModsView.Refresh();
				UpdateCanStart();
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModsPage: Ghost modpack selected.");
				}
				return;
			}

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

			// Rebuild available mods list — mods not in the load order
			foreach (ModuleModel mod in _modService.CurrentMods) {
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
					App.Toasts.Show(new ToastRequest {
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
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModsPage: Applied modpack.", new { ModpackName = _selectedModpack.ModpackName, LoadOrderCount = CurrentLoadOrder.Count, AvailableCount = AvailableModsList.Count, MissingCount = missingModNames.Count });
			}
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
		/// During startup (before <see cref="_hasCompletedInitialScan"/> is set),
		/// toasts are suppressed because <see cref="StartupRescanAsync"/> will
		/// apply the modpack authoritively with fresh data.
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

			// Suppress toasts during startup — StartupRescanAsync owns the
			// authoritative apply with fresh data and shows the toast once
			ApplySelectedModpack(showToast: _hasCompletedInitialScan);
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModsPage: Modpack selection changed.", new { SelectedIndex = SelectedModpackIndex, SelectedName = _selectedModpack?.ModpackName });
			}
		}
		#endregion

		#region Mod Actions

		/// <summary>
		/// Validates the game directory, opens a file dialog for archive selection,
		/// and delegates the install to <see cref="ModInstaller.StartInstallAsync"/>.
		/// The install runs on the service layer — surviving page navigation.
		/// Progress and completion are observed via service events.
		/// Shows a persistent progress toast for the duration of the install.
		/// </summary>
		private void InstallModsButton_Click(object sender, RoutedEventArgs e) {
			// Guard — service rejects duplicates, but skip the dialog too
			if (_modInstaller.IsInstalling) {
				App.Toasts.Show(new ToastRequest {
					Title = T.Toast_InstallInProgress,
					Message = T.Mods_InstallInProgress,
					Severity = ToastSeverity.Warning
				});
				return;
			}

			// Step 1: Validate game directory
			if (!_modInstaller.ValidateGameDirectory(out string validationError)) {
				App.Toasts.Show(new ToastRequest {
					Title = "Invalid Game Directory",
					Message = validationError,
					Severity = ToastSeverity.Error
				});
				return;
			}

			// Step 2: Open multi-select file dialog
			OpenFileDialog dialog = new() {
				Title = T.Mods_InstallDialogTitle,
				Filter = ModInstaller.FileDialogFilter,
				Multiselect = true,
				CheckFileExists = true
			};

			if (dialog.ShowDialog() != true || dialog.FileNames.Length == 0) {
				return;
			}

			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModsPage: Installing mods.", new { ArchiveCount = dialog.FileNames.Length });
			}
			// Step 3: Show persistent progress toast with progress bar
			_installToastId = App.Toasts.Show(new ToastRequest {
				Title = T.Toast_InstallingMods,
				Message = $"Processing {dialog.FileNames.Length} archive(s)...",
				Severity = ToastSeverity.Info,
				TemplateKey = ToastTemplateKeys.InstallProgress,
				IsPersistent = true,
				AllowClickDismiss = false,
				ShowCloseButton = false,
				ProgressValue = 0,
				ProgressMax = 100
			});

			// Step 4: Kick off the install — service owns the task lifetime
			IsInstalling = true;
			DependencyWarningText = $"Installing {dialog.FileNames.Length} archive(s)...";
			_modInstaller.StartInstallAsync(dialog.FileNames);
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

					App.Toasts.Show(new ToastRequest {
						Title = T.Toast_ModListUpdated,
						Message = $"{_modService.AddedMods.Count} added, {_modService.RemovedMods.Count} removed.",
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
				App.Toasts.Show(new ToastRequest {
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
				App.AppConfig.LastSelectedModpack = _selectedModpack.ModpackName;
			}

			// Launch the game with the active target (auto-starts Steam if needed)
			CanStart = false;
			DependencyWarningText = T.Mods_Launching;

			GameLaunchResult result = await _gameLauncher.LaunchAsync(CurrentLoadOrder.ToList(), _activeLaunchTarget);
			DependencyWarningText = result.Message;

			App.Toasts.Show(new ToastRequest {
				Title = result.Success ? T.Toast_GameLaunched : T.Toast_LaunchFailed,
				Message = result.Message,
				Severity = result.Success ? ToastSeverity.Success : ToastSeverity.Error
			});

			UpdateCanStart();
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModsPage: Launch result.", new { Target = _activeLaunchTarget.ToString(), Success = result.Success, Message = result.Message });
			}
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
			App.AppConfig.DefaultLaunchTarget = target;
			UpdateLaunchTargetCheckmarks();
			OnPropertyChanged(nameof(PlayButtonText));
			UpdateCanStart();
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModsPage: Launch target set.", new { Target = target.ToString() });
			}
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
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModsPage: CanStart evaluated.", new { HasLoadOrder = hasLoadOrder, CanLaunchBase = canLaunchBase, Target = _activeLaunchTarget.ToString(), CanStart });
			}
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
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModsPage: Synced load order.", new { EntryCount = _modpackService.CurrentLoadOrderEntries.Count });
			}
		}
		#endregion

		#region Auto Refresh

		/// <summary>
		/// Performs a background mod rescan on startup to ensure
		/// the UI reflects the actual file system state.
		/// The cache provides instant UI population, but mods may have been
		/// added or removed from the Modules directory between sessions.
		/// This is the single authoritative startup path — the only call
		/// that applies the modpack with toasts enabled during initial load.
		/// Sets <see cref="_hasCompletedInitialScan"/> when finished so
		/// subsequent navigation-triggered refreshes are no longer blocked.
		/// </summary>
		private async Task StartupRescanAsync() {
			if (_hasCompletedInitialScan || _modService.IsRefreshing) {
				return;
			}
			try {
				IsRefreshing = true;
				if (_modService.CurrentMods.Count == 0) {
					DependencyWarningText = T.Mods_ScanningForMods;
				}
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModsPage: Startup rescan starting.", new { ExistingCount = _modService.CurrentMods.Count });
				}
				bool hasChanges = await _modService.RefreshAsync();
				UpdateAvailableModsList();

				// This is the single authoritative apply — toasts enabled
				ApplySelectedModpack(showToast: true);

				if (hasChanges) {
					DependencyWarningText =
						$"{_modService.AddedMods.Count} mod(s) added, " +
						$"{_modService.RemovedMods.Count} mod(s) removed since last session.";
				} else if (_modService.CurrentMods.Count > 0) {
					// Preserve the AlwaysAsk prompt if ghost is still selected
					if (IsGhostModpack(_selectedModpack)) {
						DependencyWarningText = T.Mods_SelectModpackPrompt;
					} else {
						DependencyWarningText = string.Empty;
					}
				} else {
					DependencyWarningText = T.Mods_NoModsFound;
					App.Toasts.Show(new ToastRequest {
						Title = T.Toast_NoModsFound,
						Message = T.Mods_NoModsFound,
						Severity = ToastSeverity.Warning
					});
				}
			} catch (OperationCanceledException) {
				// Scan was cancelled — cache data remains in the UI
			} catch (Exception ex) {
				DependencyWarningText = $"Auto-scan failed: {ex.Message}";
				App.Toasts.Show(new ToastRequest {
					Title = T.Toast_AutoScanFailed,
					Message = ex.Message,
					Severity = ToastSeverity.Error
				});
			} finally {
				_hasCompletedInitialScan = true;
				IsRefreshing = false;
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModsPage: Startup rescan finished.", new { Count = _modService.CurrentMods.Count });
				}
			}
		}

		/// <summary>
		/// Refreshes the available mods list by performing a live directory scan.
		/// Called when navigating back to ModsPage to pick up any changes
		/// from mod installations, deletions, cache clears, or rescans
		/// performed elsewhere (e.g. mods removed from the game directory).
		/// Skipped if the initial startup scan has not yet completed,
		/// because <see cref="StartupRescanAsync"/> already covers the
		/// same work and running both produces duplicate scans.
		/// </summary>
		public async void RefreshAvailableMods() {
			if (!_hasCompletedInitialScan) {
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModsPage: Skipping refresh — initial scan not yet complete.");
				}
				return;
			}
			if (_modService.IsRefreshing) {
				return;
			}
			try {
				IsRefreshing = true;
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModsPage: Refreshing available mods.");
				}
				await _modService.RefreshAsync();
				UpdateAvailableModsList();
				ApplySelectedModpack();
			} catch (OperationCanceledException) {
				// Refresh was cancelled — no action needed
			} catch (Exception ex) {
				DependencyWarningText = $"Refresh failed: {ex.Message}";
				App.Toasts.Show(new ToastRequest {
					Title = T.Toast_RefreshFailed,
					Message = ex.Message,
					Severity = ToastSeverity.Error
				});
			} finally {
				IsRefreshing = false;
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModsPage: Refresh available mods complete.", new { Count = _modService.CurrentMods.Count });
				}
			}
		}
		#endregion
	}
}
