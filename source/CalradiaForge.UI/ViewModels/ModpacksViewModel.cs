namespace CalradiaForge.UI.ViewModels;

using System.Collections.ObjectModel;

using CalradiaForge.Core.Infra.Localization;
using CalradiaForge.Core.Infra.Modpacks;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Models;
using CalradiaForge.UI.Interactions;
using CalradiaForge.UI.Threading;
using CalradiaForge.UI.Toasts;

using CommunityToolkit.Mvvm.Input;

using Serilog;

using ToolkitRelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand;
using ModpackTemplateRelayCommand =
	CommunityToolkit.Mvvm.Input.RelayCommand<CalradiaForge.Core.Infra.Modpacks.ModpackTemplate>;
using ModpackEntryRelayCommand =
	CommunityToolkit.Mvvm.Input.RelayCommand<CalradiaForge.Core.Models.ModpackEntryModel>;

/// <summary>
/// Owns modpack-management presentation state and translates user intent into
/// calls to the authoritative Core modpack and pipeline services.
/// </summary>
public sealed class ModpacksViewModel : ViewModelBase {
	private readonly ModpackService _modpackService;
	private readonly ModPipelineManager _modPipeline;
	private readonly IModpackImportFilePicker _importFilePicker;
	private readonly IToastNotificationSink _notifications;
	private readonly IUiDispatcher _uiDispatcher;
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
	private bool _isEditPanelVisible;
	private bool _isCreatePanelVisible;
	private bool _suppressSelectionLoad;
	private ModpackModel? _selectedModpack;
	private ModpackEntryModel? _selectedEntry;
	private ModpackTemplate _selectedTemplate = ModpackTemplate.Vanilla;

	public ModpacksViewModel(
		ModpackService modpackService,
		ModPipelineManager modPipeline,
		IModpackImportFilePicker importFilePicker,
		IToastNotificationSink notifications,
		IUiDispatcher uiDispatcher,
		TranslationService translator) {
		_modpackService = modpackService ?? throw new ArgumentNullException(nameof(modpackService));
		_modPipeline = modPipeline ?? throw new ArgumentNullException(nameof(modPipeline));
		_importFilePicker = importFilePicker
			?? throw new ArgumentNullException(nameof(importFilePicker));
		_notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
		_uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
		Translator = translator ?? throw new ArgumentNullException(nameof(translator));

		ImportCommand = new ToolkitRelayCommand(Import);
		ShowCreatePanelCommand = new ToolkitRelayCommand(ShowCreatePanel);
		CancelCreateCommand = new ToolkitRelayCommand(CancelCreate);
		ConfirmCreateCommand = new ToolkitRelayCommand(ConfirmCreate);
		SaveEntryCommand = new ToolkitRelayCommand(SaveEntry, () => _selectedEntry is not null);
		SaveModpackCommand = new ToolkitRelayCommand(SaveModpack, () => HasSelection);
		SelectTemplateCommand = new ModpackTemplateRelayCommand(SelectTemplate);
		RemoveEntryCommand = new ModpackEntryRelayCommand(RemoveEntry);
	}

	public TranslationService Translator { get; }
	public ObservableCollection<ModpackModel> ModpackList { get; } = [];
	public ObservableCollection<ModpackEntryModel> EditableLoadOrder { get; } = [];
	public ObservableCollection<ModpackEntryModel> ActiveLoadOrder { get; } = [];

	public ToolkitRelayCommand ImportCommand { get; }
	public ToolkitRelayCommand ShowCreatePanelCommand { get; }
	public ToolkitRelayCommand CancelCreateCommand { get; }
	public ToolkitRelayCommand ConfirmCreateCommand { get; }
	public ToolkitRelayCommand SaveEntryCommand { get; }
	public ToolkitRelayCommand SaveModpackCommand { get; }
	public ModpackTemplateRelayCommand SelectTemplateCommand { get; }
	public ModpackEntryRelayCommand RemoveEntryCommand { get; }

	public int SelectedModpackIndex {
		get => _selectedModpackIndex;
		set {
			if (SetProperty(ref _selectedModpackIndex, value) && !_suppressSelectionLoad) {
				LoadSelectedModpackData();
				StatusText = string.Empty;
			}
		}
	}

	public int SelectedEntryIndex {
		get => _selectedEntryIndex;
		set {
			if (SetProperty(ref _selectedEntryIndex, value)) {
				LoadSelectedEntry();
			}
		}
	}

	public string SelectedCreatedBy {
		get => _selectedCreatedBy;
		private set => SetProperty(ref _selectedCreatedBy, value);
	}

	public string SelectedLastUpdated {
		get => _selectedLastUpdated;
		private set => SetProperty(ref _selectedLastUpdated, value);
	}

	public string InputModpackName {
		get => _inputModpackName;
		set => SetProperty(ref _inputModpackName, value ?? string.Empty);
	}

	public string InputCreatedBy {
		get => _inputCreatedBy;
		set => SetProperty(ref _inputCreatedBy, value ?? string.Empty);
	}

	public string EditModuleId {
		get => _editModuleId;
		set => SetProperty(ref _editModuleId, value ?? string.Empty);
	}

	public string EditModuleName {
		get => _editModuleName;
		set => SetProperty(ref _editModuleName, value ?? string.Empty);
	}

	public string EditVersion {
		get => _editVersion;
		set => SetProperty(ref _editVersion, value ?? string.Empty);
	}

	public string EditModuleUrl {
		get => _editModuleUrl;
		set => SetProperty(ref _editModuleUrl, value ?? string.Empty);
	}

	public string StatusText {
		get => _statusText;
		private set => SetProperty(ref _statusText, value);
	}

	public bool HasSelection {
		get => _hasSelection;
		private set {
			if (SetProperty(ref _hasSelection, value)) {
				SaveModpackCommand.NotifyCanExecuteChanged();
			}
		}
	}

	public bool IsEditPanelVisible {
		get => _isEditPanelVisible;
		private set => SetProperty(ref _isEditPanelVisible, value);
	}

	public bool IsCreatePanelVisible {
		get => _isCreatePanelVisible;
		private set => SetProperty(ref _isCreatePanelVisible, value);
	}

	public ModpackTemplate SelectedTemplate {
		get => _selectedTemplate;
		private set {
			if (!SetProperty(ref _selectedTemplate, value)) {
				return;
			}
			OnPropertyChanged(nameof(IsVanillaTemplate));
			OnPropertyChanged(nameof(IsButterLibTemplate));
			OnPropertyChanged(nameof(IsVanillaWarSailsTemplate));
			OnPropertyChanged(nameof(IsButterLibWarSailsTemplate));
		}
	}

	public bool IsVanillaTemplate => SelectedTemplate == ModpackTemplate.Vanilla;
	public bool IsButterLibTemplate => SelectedTemplate == ModpackTemplate.ButterLib;
	public bool IsVanillaWarSailsTemplate =>
		SelectedTemplate == ModpackTemplate.VanillaWarSails;
	public bool IsButterLibWarSailsTemplate =>
		SelectedTemplate == ModpackTemplate.ButterLibWarSails;

	protected override async Task OnInitializeAsync(CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		_modpackService.Refresh();
		await _uiDispatcher.InvokeAsync(() => {
			PopulateModpackList();
			PopulateActiveLoadOrder();
		}, cancellationToken);
	}

	protected override async Task OnActivateAsync(CancellationToken cancellationToken) {
		try {
			if (!_modPipeline.IsRefreshing) {
				ModPipelineResult result = await _modPipeline.RefreshAsync(cancellationToken);
				if (!result.Success
					&& result.Status is not ModPipelineStatus.Busy
					&& result.Status is not ModPipelineStatus.Cancelled) {
					await _uiDispatcher.InvokeAsync(
						() => StatusText = result.UserSummary,
						cancellationToken);
				}
			}
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			throw;
		} catch (OperationCanceledException) {
			// Core-owned cancellation preserves the accepted snapshot and cached presentation.
		} catch (Exception ex) {
			Log.Error(ex, "ModpacksViewModel: Mod rescan failed.");
			await _uiDispatcher.InvokeAsync(() => {
				StatusText = $"Mod rescan failed: {ex.Message}";
				ShowToast(new ToastRequest {
					Title = "Mod Rescan Failed",
					Message = ex.Message,
					Severity = ToastSeverity.Error
				});
			});
		}

		_modpackService.Refresh();
		await _uiDispatcher.InvokeAsync(() => {
			PopulateModpackList();
			PopulateActiveLoadOrder();
		}, cancellationToken);
	}

	private void PopulateModpackList() {
		string? previousSelection = _selectedModpack?.ModpackName;
		_suppressSelectionLoad = true;
		try {
			ModpackList.Clear();
			foreach (ModpackModel modpack in _modpackService.AllModpacks) {
				ModpackList.Add(modpack);
			}

			int selectedIndex = previousSelection is null
				? (ModpackList.Count > 0 ? 0 : -1)
				: FindModpackIndex(previousSelection);
			if (selectedIndex < 0 && ModpackList.Count > 0) {
				selectedIndex = 0;
			}
			SetProperty(ref _selectedModpackIndex, selectedIndex, nameof(SelectedModpackIndex));
		} finally {
			_suppressSelectionLoad = false;
		}
		LoadSelectedModpackData();
	}

	private void PopulateActiveLoadOrder() {
		ActiveLoadOrder.Clear();
		foreach (ModpackEntryModel entry in _modpackService.CurrentLoadOrderEntries) {
			ActiveLoadOrder.Add(entry.Clone());
		}
	}

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

	private void LoadSelectedEntry() {
		if (SelectedEntryIndex < 0 || SelectedEntryIndex >= EditableLoadOrder.Count) {
			ClearEditPanel();
			return;
		}

		_selectedEntry = EditableLoadOrder[SelectedEntryIndex];
		EditModuleId = _selectedEntry.ModuleId;
		EditModuleName = _selectedEntry.ModuleName;
		EditVersion = _selectedEntry.RequiredVersion;
		EditModuleUrl = _selectedEntry.ModuleURL ?? string.Empty;
		IsEditPanelVisible = true;
		SaveEntryCommand.NotifyCanExecuteChanged();
	}

	private void ClearEditPanel() {
		_selectedEntry = null;
		if (_selectedEntryIndex != -1) {
			SetProperty(ref _selectedEntryIndex, -1, nameof(SelectedEntryIndex));
		}
		EditModuleId = string.Empty;
		EditModuleName = string.Empty;
		EditVersion = string.Empty;
		EditModuleUrl = string.Empty;
		IsEditPanelVisible = false;
		SaveEntryCommand.NotifyCanExecuteChanged();
	}

	private void SaveEntry() {
		if (_selectedEntry is null
			|| SelectedEntryIndex < 0
			|| SelectedEntryIndex >= EditableLoadOrder.Count) {
			StatusText = "No entry selected.";
			return;
		}

		_selectedEntry.ModuleId = EditModuleId.Trim();
		_selectedEntry.ModuleName = EditModuleName.Trim();
		_selectedEntry.RequiredVersion = EditVersion.Trim();
		_selectedEntry.ModuleURL = string.IsNullOrWhiteSpace(EditModuleUrl)
			? null
			: EditModuleUrl.Trim();

		int index = SelectedEntryIndex;
		EditableLoadOrder[index] = _selectedEntry;
		SelectedEntryIndex = index;
		StatusText =
			$"Updated '{_selectedEntry.ModuleName}' in working copy. Click Save to persist.";
	}

	private void RemoveEntry(ModpackEntryModel? entry) {
		if (entry is null || !EditableLoadOrder.Remove(entry)) {
			return;
		}
		ClearEditPanel();
		StatusText = $"Removed '{entry.ModuleName}'. Click Save to persist.";
	}

	private void Import() {
		string? importFilePath = _importFilePicker.PickImportFile();
		if (string.IsNullOrWhiteSpace(importFilePath)) {
			return;
		}

		var (success, modpack, message) = _modpackService.Import(importFilePath);
		StatusText = message;
		if (success && modpack is not null) {
			PopulateModpackList();
			SelectModpackByName(modpack.ModpackName);
			ShowToast(new ToastRequest {
				Title = "Modpack Imported",
				Message = $"'{modpack.ModpackName}' imported with {modpack.LoadOrder.Count} entries.",
				Severity = ToastSeverity.Success
			});
			return;
		}

		ShowToast(new ToastRequest {
			Title = "Import Failed",
			Message = message,
			Severity = ToastSeverity.Error
		});
	}

	private void SelectTemplate(ModpackTemplate template) {
		SelectedTemplate = template;
		StatusText = $"Template set to {template}. Click Create New to use it.";
	}

	private void ShowCreatePanel() {
		InputModpackName = string.Empty;
		InputCreatedBy = string.Empty;
		IsCreatePanelVisible = true;
	}

	private void CancelCreate() {
		HideCreatePanel();
		StatusText = string.Empty;
	}

	private void HideCreatePanel() {
		IsCreatePanelVisible = false;
		InputModpackName = string.Empty;
		InputCreatedBy = string.Empty;
	}

	private void ConfirmCreate() {
		string name = InputModpackName.Trim();
		string createdBy = InputCreatedBy.Trim();
		if (string.IsNullOrWhiteSpace(name)) {
			StatusText = "Modpack name cannot be empty.";
			ShowToast(new ToastRequest {
				Title = "Name Required",
				Message = "Enter a name for the new modpack.",
				Severity = ToastSeverity.Warning
			});
			return;
		}
		if (string.IsNullOrWhiteSpace(createdBy)) {
			createdBy = "User";
		}

		List<ModuleModel> installedMods = _modPipeline.AcceptedSnapshot.Modules.ToList();
		bool success = _modpackService.CreateNew(
			name,
			createdBy,
			SelectedTemplate,
			installedMods);
		if (success) {
			HideCreatePanel();
			PopulateModpackList();
			SelectModpackByName(name);
			StatusText = $"Created '{name}' with {SelectedTemplate} template.";
			ShowToast(new ToastRequest {
				Title = "Modpack Created",
				Message = $"'{name}' created with {SelectedTemplate} template.",
				Severity = ToastSeverity.Success
			});
			return;
		}

		StatusText =
			$"Failed to create '{name}'. A modpack with this name may already exist.";
		ShowToast(new ToastRequest {
			Title = "Create Failed",
			Message = $"A modpack named '{name}' may already exist.",
			Severity = ToastSeverity.Error
		});
	}

	private void SaveModpack() {
		if (_selectedModpack is null) {
			StatusText = "No modpack selected.";
			return;
		}
		if (_modpackService.CurrentLoadOrderEntries.Count == 0) {
			StatusText = "No active load order found. Arrange mods on the Launcher page first.";
			ShowToast(new ToastRequest {
				Title = "Nothing to Save",
				Message = "Arrange mods on the Launcher page first.",
				Severity = ToastSeverity.Warning
			});
			return;
		}

		List<ModpackEntryModel> entries = _modpackService.CurrentLoadOrderEntries
			.Select(entry => entry.Clone())
			.ToList();
		string selectedName = _selectedModpack.ModpackName;
		bool success = _modpackService.Save(_selectedModpack, entries);
		if (success) {
			PopulateModpackList();
			SelectModpackByName(selectedName);
			PopulateActiveLoadOrder();
			StatusText = $"Saved active load order to '{selectedName}'.";
			ShowToast(new ToastRequest {
				Title = "Modpack Saved",
				Message = $"'{selectedName}' updated with {entries.Count} entries.",
				Severity = ToastSeverity.Success
			});
			return;
		}

		StatusText = $"Failed to save '{selectedName}'.";
		ShowToast(new ToastRequest {
			Title = "Save Failed",
			Message = $"Could not save '{selectedName}'. Check logs for details.",
			Severity = ToastSeverity.Error
		});
	}

	private void SelectModpackByName(string modpackName) {
		int index = FindModpackIndex(modpackName);
		if (index < 0) {
			return;
		}
		SelectedModpackIndex = index;
		if (_selectedModpack?.ModpackName != modpackName) {
			LoadSelectedModpackData();
		}
	}

	private int FindModpackIndex(string modpackName) => ModpackList
		.Select((modpack, index) => new { modpack, index })
		.FirstOrDefault(item => string.Equals(
			item.modpack.ModpackName,
			modpackName,
			StringComparison.OrdinalIgnoreCase))?.index ?? -1;

	private void ShowToast(ToastRequest request) => _notifications.Open(request);
}
