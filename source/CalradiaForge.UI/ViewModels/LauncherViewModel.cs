namespace CalradiaForge.UI.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Launch;
using CalradiaForge.Core.Infra.Localization;
using CalradiaForge.Core.Infra.Modpacks;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Models;
using CalradiaForge.UI.Interactions;
using CalradiaForge.UI.Threading;
using CalradiaForge.UI.Toasts;

using CommunityToolkit.Mvvm.Input;

using Serilog;

using LaunchTargetRelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand<CalradiaForge.Core.Infra.Launch.LaunchTarget>;
using ToolkitRelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand;

public enum LauncherModuleCollection {
	LoadOrder,
	Available
}

/// <summary>
/// Owns Launcher presentation state and coordinates user intent with the
/// authoritative Core services.
/// </summary>
public sealed class LauncherViewModel : ViewModelBase {
	private readonly AppSettings _appSettings;
	private readonly ModPipelineManager _modPipeline;
	private readonly ModpackService _modpackService;
	private readonly GameLauncher _gameLauncher;
	private readonly IUiDispatcher _uiDispatcher;
	private readonly IModArchiveFilePicker _archivePicker;
	private readonly IToastNotificationSink _notifications;
	private readonly IInstallNotificationPresenter _installNotifications;
	private readonly ModpackModel _ghostModpack;
	private readonly HashSet<Guid> _reportedInstallCompletions = [];
	private readonly object _completionGate = new();
	private bool _canStart;
	private bool _isRefreshing;
	private bool _isInstalling;
	private bool _isLaunching;
	private bool _canCancel;
	private bool _suppressSelectionApply;
	private bool _hasActivated;
	private string _dependencyWarningText = string.Empty;
	private string _searchQuery = string.Empty;
	private int _selectedModpackIndex = -1;
	private ModpackModel? _selectedModpack;
	private LaunchTarget _activeLaunchTarget;
	private ModPipelineOperation _currentOperation;
	private ModInstallOperationResult? _lastInstallResult;
	private ModPipelineResult? _lastRefreshResult;
	private GameLaunchResult? _lastLaunchResult;

	public LauncherViewModel(
		AppSettings appSettings,
		ModPipelineManager modPipeline,
		ModpackService modpackService,
		GameLauncher gameLauncher,
		IUiDispatcher uiDispatcher,
		IModArchiveFilePicker archivePicker,
		IToastNotificationSink notifications,
		IInstallNotificationPresenter installNotifications,
		TranslationService translator) {
		_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
		_modPipeline = modPipeline ?? throw new ArgumentNullException(nameof(modPipeline));
		_modpackService = modpackService ?? throw new ArgumentNullException(nameof(modpackService));
		_gameLauncher = gameLauncher ?? throw new ArgumentNullException(nameof(gameLauncher));
		_uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
		_archivePicker = archivePicker ?? throw new ArgumentNullException(nameof(archivePicker));
		_notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
		_installNotifications = installNotifications
			?? throw new ArgumentNullException(nameof(installNotifications));
		Translator = translator ?? throw new ArgumentNullException(nameof(translator));
		_ghostModpack = new ModpackModel(
			Translator.Strings.Launcher_GhostModpackLabel,
			string.Empty,
			[]);

		LoadOrderView = new ListCollectionView(CurrentLoadOrder);
		LoadOrderView.Filter = ModSearchFilter;
		AvailableModsView = new ListCollectionView(AvailableModsList);
		AvailableModsView.Filter = ModSearchFilter;

		InstallModsCommand = new AsyncRelayCommand(InstallModsAsync, CanInstallMods);
		RefreshModsCommand = new AsyncRelayCommand(RefreshModsAsync, CanRefreshMods);
		LaunchCommand = new AsyncRelayCommand(LaunchAsync, CanLaunch);
		CancelOperationCommand = new ToolkitRelayCommand(RequestCancellation, () => CanCancel);
		SetLaunchTargetCommand = new LaunchTargetRelayCommand(SetActiveLaunchTarget);

	}

	private TranslationStrings T => Translator.Strings;

	public TranslationService Translator { get; }
	public ObservableCollection<ModuleModel> CurrentLoadOrder { get; } = [];
	public ObservableCollection<ModuleModel> AvailableModsList { get; } = [];
	public ObservableCollection<ModpackModel> ModpackList { get; } = [];
	public ICollectionView LoadOrderView { get; }
	public ICollectionView AvailableModsView { get; }

	public AsyncRelayCommand InstallModsCommand { get; }
	public AsyncRelayCommand RefreshModsCommand { get; }
	public AsyncRelayCommand LaunchCommand { get; }
	public ToolkitRelayCommand CancelOperationCommand { get; }
	public LaunchTargetRelayCommand SetLaunchTargetCommand { get; }

	public bool CanStart {
		get => _canStart;
		private set {
			if (SetProperty(ref _canStart, value)) {
				LaunchCommand.NotifyCanExecuteChanged();
			}
		}
	}

	public bool IsRefreshing {
		get => _isRefreshing;
		private set {
			if (SetProperty(ref _isRefreshing, value)) {
				OnPropertyChanged(nameof(IsBusy));
				NotifyCommandStateChanged();
			}
		}
	}

	public bool IsInstalling {
		get => _isInstalling;
		private set {
			if (SetProperty(ref _isInstalling, value)) {
				OnPropertyChanged(nameof(IsBusy));
				NotifyCommandStateChanged();
			}
		}
	}

	public bool IsLaunching {
		get => _isLaunching;
		private set {
			if (SetProperty(ref _isLaunching, value)) {
				OnPropertyChanged(nameof(IsBusy));
				NotifyCommandStateChanged();
			}
		}
	}

	public bool IsBusy => IsRefreshing || IsInstalling || IsLaunching;

	public bool CanCancel {
		get => _canCancel;
		private set {
			if (SetProperty(ref _canCancel, value)) {
				CancelOperationCommand.NotifyCanExecuteChanged();
			}
		}
	}

	public string DependencyWarningText {
		get => _dependencyWarningText;
		private set => SetProperty(ref _dependencyWarningText, value);
	}

	public string SearchQuery {
		get => _searchQuery;
		set {
			if (SetProperty(ref _searchQuery, value?.Trim() ?? string.Empty)) {
				LoadOrderView.Refresh();
				AvailableModsView.Refresh();
			}
		}
	}

	public int SelectedModpackIndex {
		get => _selectedModpackIndex;
		set {
			if (!SetProperty(ref _selectedModpackIndex, value) || _suppressSelectionApply) {
				return;
			}
			NormalizeGhostSelection();
			ApplySelectedModpack();
		}
	}

	public LaunchTarget ActiveLaunchTarget {
		get => _activeLaunchTarget;
		private set {
			if (!SetProperty(ref _activeLaunchTarget, value)) {
				return;
			}
			OnPropertyChanged(nameof(PlayButtonText));
			OnPropertyChanged(nameof(IsBannerlordTarget));
			OnPropertyChanged(nameof(IsBlseTarget));
		}
	}

	public bool IsBannerlordTarget => ActiveLaunchTarget == LaunchTarget.Bannerlord;
	public bool IsBlseTarget => ActiveLaunchTarget == LaunchTarget.BLSE;
	public string PlayButtonText => ActiveLaunchTarget == LaunchTarget.BLSE
		? T.Launcher_PlayWithBLSE
		: T.Launcher_PlayBannerlord;

	public ModPipelineOperation CurrentOperation {
		get => _currentOperation;
		private set => SetProperty(ref _currentOperation, value);
	}

	public ModInstallOperationResult? LastInstallResult {
		get => _lastInstallResult;
		private set => SetProperty(ref _lastInstallResult, value);
	}

	public ModPipelineResult? LastRefreshResult {
		get => _lastRefreshResult;
		private set => SetProperty(ref _lastRefreshResult, value);
	}

	public GameLaunchResult? LastLaunchResult {
		get => _lastLaunchResult;
		private set => SetProperty(ref _lastLaunchResult, value);
	}

	protected override Task OnInitializeAsync(CancellationToken cancellationToken) =>
		_uiDispatcher.InvokeAsync(() => {
			cancellationToken.ThrowIfCancellationRequested();
			ActiveLaunchTarget = _appSettings.DefaultLaunchTarget;
			ApplySnapshot(_modPipeline.AcceptedSnapshot);
			PopulateModpackList(suppressToast: true);
			UpdateCanStart();
			if (_modPipeline.IsInstalling) {
				IsInstalling = true;
				CanCancel = true;
				CurrentOperation = ModPipelineOperation.Install;
				DependencyWarningText = T.Launcher_InstallInProgress;
			}
		}, cancellationToken);

	protected override async Task OnActivateAsync(CancellationToken cancellationToken) {
		if (!_hasActivated) {
			_hasActivated = true;
			return;
		}
		await _uiDispatcher.InvokeAsync(
			() => RefreshModpackList(suppressApply: true),
			cancellationToken);
		if (IsBusy || _modPipeline.IsInstalling || _modPipeline.IsRefreshing) {
			return;
		}
		await RefreshModsCoreAsync(cancellationToken);
	}

	public void MoveModule(
		LauncherModuleCollection source,
		LauncherModuleCollection target,
		ModuleModel module,
		int insertIndex) {
		ArgumentNullException.ThrowIfNull(module);
		ObservableCollection<ModuleModel> sourceCollection = CollectionFor(source);
		ObservableCollection<ModuleModel> targetCollection = CollectionFor(target);
		int removeIndex = sourceCollection.IndexOf(module);
		if (removeIndex < 0) {
			return;
		}

		sourceCollection.RemoveAt(removeIndex);
		if (ReferenceEquals(sourceCollection, targetCollection) && insertIndex > removeIndex) {
			insertIndex--;
		}
		insertIndex = Math.Clamp(insertIndex, 0, targetCollection.Count);
		targetCollection.Insert(insertIndex, module);
		LoadOrderView.Refresh();
		AvailableModsView.Refresh();
		SyncLoadOrderToService();
		UpdateCanStart();
	}

	private async Task InstallModsAsync() {
		IReadOnlyList<string> choices = _archivePicker.PickArchives();
		if (choices.Count == 0) {
			return;
		}

		await _uiDispatcher.InvokeAsync(() => {
			IsInstalling = true;
			CanCancel = true;
			CurrentOperation = ModPipelineOperation.Install;
			DependencyWarningText = T.Launcher_InstallInProgress;
		});

		ModInstallOperationResult? result = null;
		try {
			result = await _modPipeline.InstallAsync(choices.ToArray());
			if (!result.WasAdmitted) {
				await _uiDispatcher.InvokeAsync(() => {
					LastInstallResult = result;
					DependencyWarningText = result.Status switch {
						ModInstallOperationStatus.RejectedBusy => T.Toast_InstallRejectedBusy,
						ModInstallOperationStatus.RejectedAdmissionStopped =>
							T.Toast_InstallRejectedAdmissionStopped,
						_ => T.Toast_InstallValidationFailed
					};
				});
				return;
			}

			try {
				await _uiDispatcher.InvokeAsync(() => {
					ApplySnapshot(result.AcceptedSnapshot);
					ApplySelectedModpack(acceptedSnapshot: result.AcceptedSnapshot);
					UpdateCanStart();
					LastInstallResult = result;
					string installStatus = string.Format(
						T.Toast_InstallSummaryFormat,
						result.Summary.InstalledCount,
						result.Summary.UpgradedCount,
						result.Summary.SkippedCount,
						result.Summary.FailedCount);
					DependencyWarningText = string.IsNullOrWhiteSpace(DependencyWarningText)
						? installStatus
						: $"{DependencyWarningText} | {installStatus}";
				});
				ReportInstallCompletionOnce(result.OperationId, succeeded: true);
			} catch (Exception ex) {
				Log.Error(
					ex,
					"LauncherViewModel: Presentation reconciliation failed for install {OperationId}.",
					result.OperationId);
				ReportInstallCompletionOnce(result.OperationId, succeeded: false, ex.ToString());
				try {
					await _uiDispatcher.InvokeAsync(() => {
						LastInstallResult = result;
						DependencyWarningText = T.Toast_InstallReconciliationFailed;
					});
				} catch (Exception recoveryException) {
					Log.Error(
						recoveryException,
						"LauncherViewModel: Failed to publish reconciliation failure state for install {OperationId}.",
						result.OperationId);
				}
			}
		} catch (Exception ex) {
			Log.Error(ex, "LauncherViewModel: Install command failed unexpectedly.");
			await _uiDispatcher.InvokeAsync(() =>
				DependencyWarningText = T.Toast_InstallFailed);
		} finally {
			await _uiDispatcher.InvokeAsync(() => {
				IsInstalling = false;
				CanCancel = false;
				CurrentOperation = ModPipelineOperation.None;
			});
		}
	}

	private Task RefreshModsAsync() => RefreshModsCoreAsync(CancellationToken.None);

	private async Task RefreshModsCoreAsync(CancellationToken cancellationToken) {
		await _uiDispatcher.InvokeAsync(() => {
			IsRefreshing = true;
			CanCancel = true;
			CurrentOperation = ModPipelineOperation.RefreshScan;
		}, cancellationToken);
		try {
			ModPipelineResult result = await _modPipeline.RefreshAsync(cancellationToken);
			await _uiDispatcher.InvokeAsync(() => {
				LastRefreshResult = result;
				ApplySnapshot(result.AcceptedSnapshot);
				if (!result.Success) {
					DependencyWarningText = result.UserSummary;
					ShowPipelineWarning(result);
				} else if (result.HasChanges) {
					DependencyWarningText =
						$"{result.AcceptedSnapshot.AddedModules.Count} mod(s) added, " +
						$"{result.AcceptedSnapshot.RemovedModules.Count} mod(s) removed since last scan.";
					ShowToast(new ToastRequest {
						Title = T.Toast_ModListUpdated,
						Message =
							$"{result.AcceptedSnapshot.AddedModules.Count} added, " +
							$"{result.AcceptedSnapshot.RemovedModules.Count} removed.",
						Severity = ToastSeverity.Info
					});
				} else {
					DependencyWarningText = string.Empty;
				}
				ApplySelectedModpack(acceptedSnapshot: result.AcceptedSnapshot);
			}, cancellationToken);
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			throw;
		} catch (OperationCanceledException) {
			// Core-owned cancellation is a normal terminal path.
		} catch (Exception ex) {
			Log.Error(ex, "LauncherViewModel: Refresh command failed.");
			await _uiDispatcher.InvokeAsync(() => {
				DependencyWarningText = $"Refresh failed: {ex.Message}";
				ShowToast(new ToastRequest {
					Title = T.Toast_RefreshFailed,
					Message = ex.Message,
					Severity = ToastSeverity.Error
				});
			});
		} finally {
			await _uiDispatcher.InvokeAsync(() => {
				IsRefreshing = false;
				CanCancel = false;
				CurrentOperation = ModPipelineOperation.None;
			});
		}
	}

	private async Task LaunchAsync() {
		if (IsGhostModpack(_selectedModpack)) {
			return;
		}

		List<ModpackEntryModel> currentEntries =
			ModpackService.BuildEntryListFromModules(CurrentLoadOrder.ToList());
		_modpackService.SaveLastUsed(currentEntries);
		if (_selectedModpack is not null) {
			_appSettings.LastSelectedModpack = _selectedModpack.ModpackName;
		}

		IsLaunching = true;
		DependencyWarningText = T.Launcher_Launching;
		try {
			GameLaunchResult result =
				await _gameLauncher.LaunchAsync(CurrentLoadOrder.ToList(), ActiveLaunchTarget);
			await _uiDispatcher.InvokeAsync(() => {
				LastLaunchResult = result;
				DependencyWarningText = result.Message;
				ShowToast(new ToastRequest {
					Title = result.Success ? T.Toast_GameLaunched : T.Toast_LaunchFailed,
					Message = result.Message,
					Severity = result.Success ? ToastSeverity.Success : ToastSeverity.Error
				});
			});
		} finally {
			await _uiDispatcher.InvokeAsync(() => {
				IsLaunching = false;
				UpdateCanStart();
			});
		}
	}

	private void RequestCancellation() {
		if (!CanCancel) {
			return;
		}
		_modPipeline.RequestCancellation();
		CanCancel = false;
	}

	private void SetActiveLaunchTarget(LaunchTarget target) {
		ActiveLaunchTarget = target;
		_appSettings.DefaultLaunchTarget = target;
		UpdateCanStart();
	}

	private void PopulateModpackList(bool suppressToast) {
		_ghostModpack.ModpackName = T.Launcher_GhostModpackLabel;
		_suppressSelectionApply = true;
		try {
			ModpackList.Clear();
			if (_appSettings.ModpackStartupMode == ModpackStartupMode.AlwaysAsk) {
				ModpackList.Add(_ghostModpack);
			}
			foreach (ModpackModel modpack in _modpackService.AllModpacks) {
				ModpackList.Add(modpack);
			}
			SelectedModpackIndex = ResolveStartupModpackIndex();
		} finally {
			_suppressSelectionApply = false;
		}
		ApplySelectedModpack(showToast: !suppressToast);
	}

	private void RefreshModpackList(bool suppressApply) {
		_modpackService.Refresh();
		_ghostModpack.ModpackName = T.Launcher_GhostModpackLabel;
		ModpackModel? previousModpack = _selectedModpack;
		bool wasGhostSelected = IsGhostModpack(previousModpack);
		string? previousName = wasGhostSelected ? null : previousModpack?.ModpackName;

		_suppressSelectionApply = true;
		try {
			ModpackList.Clear();
			if (_appSettings.ModpackStartupMode == ModpackStartupMode.AlwaysAsk && wasGhostSelected) {
				ModpackList.Add(_ghostModpack);
			}
			foreach (ModpackModel modpack in _modpackService.AllModpacks) {
				ModpackList.Add(modpack);
			}
			SelectedModpackIndex = wasGhostSelected
				? 0
				: previousName is not null
					? Math.Max(0, FindModpackIndexByName(previousName))
					: ModpackList.Count > 0 ? 0 : -1;
		} finally {
			_suppressSelectionApply = false;
		}
		if (!suppressApply) {
			ApplySelectedModpack();
		}
	}

	private int ResolveStartupModpackIndex() {
		if (ModpackList.Count == 0) {
			return -1;
		}
		return _appSettings.ModpackStartupMode switch {
			ModpackStartupMode.AlwaysDefault =>
				Math.Max(0, FindModpackIndexByName(VanillaModules.DefaultModpackName)),
			ModpackStartupMode.LastUsed when
				!string.IsNullOrWhiteSpace(_appSettings.LastSelectedModpack) =>
				Math.Max(0, FindModpackIndexByName(_appSettings.LastSelectedModpack)),
			_ => 0
		};
	}

	private void NormalizeGhostSelection() {
		if (SelectedModpackIndex < 0
			|| SelectedModpackIndex >= ModpackList.Count
			|| IsGhostModpack(ModpackList[SelectedModpackIndex])
			|| ModpackList.Count == 0
			|| !IsGhostModpack(ModpackList[0])) {
			return;
		}
		string selectedName = ModpackList[SelectedModpackIndex].ModpackName;
		_suppressSelectionApply = true;
		try {
			ModpackList.RemoveAt(0);
			SelectedModpackIndex = Math.Max(0, FindModpackIndexByName(selectedName));
		} finally {
			_suppressSelectionApply = false;
		}
	}

	private void ApplySelectedModpack(
		bool showToast = true,
		AcceptedModSnapshot? acceptedSnapshot = null) {
		AcceptedModSnapshot snapshot = acceptedSnapshot ?? _modPipeline.AcceptedSnapshot;
		CurrentLoadOrder.Clear();
		AvailableModsList.Clear();
		DependencyWarningText = string.Empty;

		if (SelectedModpackIndex < 0 || SelectedModpackIndex >= ModpackList.Count) {
			_selectedModpack = null;
			AddAvailableModules(snapshot);
			RefreshViewsAndReadiness();
			return;
		}

		_selectedModpack = ModpackList[SelectedModpackIndex];
		if (IsGhostModpack(_selectedModpack)) {
			AddAvailableModules(snapshot);
			DependencyWarningText = T.Launcher_SelectModpackPrompt;
			RefreshViewsAndReadiness();
			return;
		}

		(List<ModpackEntryModel> validEntries, List<string> missingModNames) =
			ModpackService.ValidateLoadOrder(_selectedModpack, snapshot.Modules);
		HashSet<string> loadOrderIds = new(
			validEntries.Select(entry => entry.ModuleId),
			StringComparer.OrdinalIgnoreCase);
		foreach (ModpackEntryModel entry in validEntries) {
			ModuleModel? installed = snapshot.Modules.FirstOrDefault(module =>
				string.Equals(module.ModuleId, entry.ModuleId, StringComparison.OrdinalIgnoreCase));
			if (installed is not null) {
				CurrentLoadOrder.Add(installed);
			}
		}
		foreach (ModuleModel module in snapshot.Modules) {
			if (string.IsNullOrEmpty(module.ModuleId) || !loadOrderIds.Contains(module.ModuleId)) {
				AvailableModsList.Add(module);
			}
		}

		if (missingModNames.Count > 0) {
			string names = string.Join(", ", missingModNames);
			DependencyWarningText = $"{missingModNames.Count} mod(s) not found: {names}";
			if (showToast) {
				ShowToast(new ToastRequest {
					Title = $"{missingModNames.Count} {T.Toast_MissingMods}",
					Message = string.Join("\n", missingModNames.Select(name => $"• {name}")),
					Severity = ToastSeverity.Warning,
					TemplateKey = ToastTemplateKeys.MissingMods,
					AllowClickDismiss = false
				});
			}
		}

		RefreshViewsAndReadiness();
		SyncLoadOrderToService();
	}

	private void ApplySnapshot(AcceptedModSnapshot snapshot) {
		AvailableModsList.Clear();
		foreach (ModuleModel module in snapshot.Modules) {
			AvailableModsList.Add(module);
		}
		AvailableModsView.Refresh();
	}

	private void AddAvailableModules(AcceptedModSnapshot snapshot) {
		foreach (ModuleModel module in snapshot.Modules) {
			AvailableModsList.Add(module);
		}
	}

	private void RefreshViewsAndReadiness() {
		LoadOrderView.Refresh();
		AvailableModsView.Refresh();
		UpdateCanStart();
	}

	private void UpdateCanStart() {
		bool hasLoadOrder = CurrentLoadOrder.Count > 0;
		bool canLaunchBase = _gameLauncher.CanLaunch(out _);
		if (ActiveLaunchTarget == LaunchTarget.BLSE) {
			bool canLaunchBlse = _gameLauncher.CanLaunchBLSE(out string blseError);
			CanStart = hasLoadOrder && canLaunchBase && canLaunchBlse && !IsBusy;
			if (!canLaunchBlse && hasLoadOrder && canLaunchBase
				&& string.IsNullOrEmpty(DependencyWarningText)) {
				DependencyWarningText = blseError;
			}
		} else {
			CanStart = hasLoadOrder && canLaunchBase && !IsBusy;
		}
	}

	private void SyncLoadOrderToService() {
		_modpackService.CurrentLoadOrderEntries =
			ModpackService.BuildEntryListFromModules(CurrentLoadOrder.ToList());
	}

	private void ReportInstallCompletionOnce(
		Guid operationId,
		bool succeeded,
		string? errorMessage = null) {
		lock (_completionGate) {
			if (!_reportedInstallCompletions.Add(operationId)) {
				return;
			}
		}
		_installNotifications.ReportLauncherCompletion(
			new LauncherInstallPresentationCompletion(operationId, succeeded, errorMessage));
	}

	private void ShowPipelineWarning(ModPipelineResult result) {
		if (result.Status is ModPipelineStatus.Busy or ModPipelineStatus.Cancelled) {
			return;
		}
		ShowToast(new ToastRequest {
			Title = T.Toast_RefreshFailed,
			Message = result.UserSummary,
			Severity = ToastSeverity.Warning
		});
	}

	private void ShowToast(ToastRequest request) => _notifications.Open(request);

	private bool ModSearchFilter(object item) {
		if (string.IsNullOrWhiteSpace(SearchQuery)) {
			return true;
		}
		return item is not ModuleModel module
			|| (module.ModuleName ?? string.Empty)
				.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase);
	}

	private bool IsGhostModpack(ModpackModel? modpack) =>
		ReferenceEquals(modpack, _ghostModpack);

	private int FindModpackIndexByName(string modpackName) => ModpackList
		.Select((modpack, index) => new { modpack, index })
		.Where(item => !IsGhostModpack(item.modpack))
		.FirstOrDefault(item => string.Equals(
			item.modpack.ModpackName,
			modpackName,
			StringComparison.OrdinalIgnoreCase))?.index ?? -1;

	private ObservableCollection<ModuleModel> CollectionFor(LauncherModuleCollection collection) =>
		collection == LauncherModuleCollection.LoadOrder
			? CurrentLoadOrder
			: AvailableModsList;

	private bool CanInstallMods() => !IsBusy;
	private bool CanRefreshMods() => !IsBusy;
	private bool CanLaunch() => CanStart && !IsBusy;

	private void NotifyCommandStateChanged() {
		InstallModsCommand.NotifyCanExecuteChanged();
		RefreshModsCommand.NotifyCanExecuteChanged();
		LaunchCommand.NotifyCanExecuteChanged();
		UpdateCanStart();
	}
}
