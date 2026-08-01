namespace CalradiaForge.Tests.UI.ViewModels;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Localization;
using CalradiaForge.Core.Infra.Modpacks;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Infra.Paths;
using CalradiaForge.Core.Models;
using CalradiaForge.Tests.Core.Support;
using CalradiaForge.UI.Interactions;
using CalradiaForge.UI.Threading;
using CalradiaForge.UI.Toasts;
using CalradiaForge.UI.ViewModels;

using Newtonsoft.Json;

public sealed class ModpacksViewModelTests {
	[Fact]
	public async Task InitializeAsync_PopulatesStableClonedCollectionsAndSelection() {
		using TestContext context = new();
		context.ModpackService.CurrentLoadOrderEntries = [
			new ModpackEntryModel("Active", "Active Module", "1.0")
		];
		object modpacksIdentity = context.ViewModel.ModpackList;
		object activeIdentity = context.ViewModel.ActiveLoadOrder;

		await context.ViewModel.InitializeAsync();

		Assert.Same(modpacksIdentity, context.ViewModel.ModpackList);
		Assert.Same(activeIdentity, context.ViewModel.ActiveLoadOrder);
		Assert.NotEmpty(context.ViewModel.ModpackList);
		Assert.True(context.ViewModel.HasSelection);
		ModpackEntryModel active = Assert.Single(context.ViewModel.ActiveLoadOrder);
		Assert.Equal("Active", active.ModuleId);
		Assert.NotSame(
			context.ModpackService.CurrentLoadOrderEntries[0],
			active);
	}

	[Fact]
	public async Task EntryCommands_EditAndRemoveOnlyTheWorkingCopy() {
		using TestContext context = new();
		context.ModpackService.CreateNew(
			"Editable",
			"Owner",
			ModpackTemplate.Vanilla);
		await context.ViewModel.InitializeAsync();
		context.SelectModpack("Editable");
		int savedCount = context.SelectedServiceModpack("Editable").LoadOrder.Count;
		context.ViewModel.SelectedEntryIndex = 0;
		context.ViewModel.EditModuleName = "  Edited Name  ";
		context.ViewModel.EditVersion = "  2.0  ";

		context.ViewModel.SaveEntryCommand.Execute(null);

		Assert.Equal("Edited Name", context.ViewModel.EditableLoadOrder[0].ModuleName);
		Assert.Equal("2.0", context.ViewModel.EditableLoadOrder[0].RequiredVersion);
		Assert.NotEqual(
			"Edited Name",
			context.SelectedServiceModpack("Editable").LoadOrder[0].ModuleName);

		ModpackEntryModel entry = context.ViewModel.EditableLoadOrder[0];
		context.ViewModel.RemoveEntryCommand.Execute(entry);

		Assert.Equal(savedCount - 1, context.ViewModel.EditableLoadOrder.Count);
		Assert.Equal(savedCount, context.SelectedServiceModpack("Editable").LoadOrder.Count);
		Assert.False(context.ViewModel.IsEditPanelVisible);
	}

	[Fact]
	public async Task SaveCommand_PreservesActiveLoadOrderSourceSemantics() {
		using TestContext context = new();
		context.ModpackService.CreateNew(
			"Save Target",
			"Owner",
			ModpackTemplate.Vanilla);
		context.ModpackService.CurrentLoadOrderEntries = [
			new ModpackEntryModel("Active", "Active Module", "9.0")
		];
		await context.ViewModel.InitializeAsync();
		context.SelectModpack("Save Target");
		context.ViewModel.SelectedEntryIndex = 0;
		context.ViewModel.EditModuleName = "Working Copy Edit";
		context.ViewModel.SaveEntryCommand.Execute(null);

		context.ViewModel.SaveModpackCommand.Execute(null);

		ModpackEntryModel persisted = Assert.Single(
			context.SelectedServiceModpack("Save Target").LoadOrder);
		Assert.Equal("Active", persisted.ModuleId);
		Assert.Equal("Active Module", persisted.ModuleName);
		Assert.Contains("Saved active load order", context.ViewModel.StatusText);
	}

	[Fact]
	public async Task ImportCommand_PickerCancellationIsANoOp() {
		using TestContext context = new(importPath: null);
		await context.ViewModel.InitializeAsync();
		int initialCount = context.ViewModel.ModpackList.Count;

		context.ViewModel.ImportCommand.Execute(null);

		Assert.Equal(initialCount, context.ViewModel.ModpackList.Count);
		Assert.Empty(context.Notifications.Requests);
		Assert.Equal(1, context.Picker.CallCount);
	}

	[Fact]
	public async Task CreateCommand_DefaultsCreatorAndSelectsCreatedModpack() {
		using TestContext context = new();
		await context.ViewModel.InitializeAsync();
		context.ViewModel.ShowCreatePanelCommand.Execute(null);
		context.ViewModel.InputModpackName = "Created Pack";
		context.ViewModel.InputCreatedBy = " ";

		context.ViewModel.ConfirmCreateCommand.Execute(null);

		ModpackModel created = context.SelectedServiceModpack("Created Pack");
		Assert.Equal("User", created.CreatedBy);
		Assert.Equal("Created Pack", context.ViewModel.ModpackList[
			context.ViewModel.SelectedModpackIndex].ModpackName);
		Assert.False(context.ViewModel.IsCreatePanelVisible);
		Assert.Equal("Modpack Created", Assert.Single(context.Notifications.Requests).Title);
	}

	[Fact]
	public async Task ActivateAsync_RefreshesPipelineAndResynchronizesActivePreview() {
		using TestContext context = new();
		await context.ViewModel.InitializeAsync();
		context.ModpackService.CurrentLoadOrderEntries = [
			new ModpackEntryModel("Later", "Later Module", "1.0")
		];

		await context.ViewModel.ActivateAsync();

		Assert.Equal(1, context.Scanner.CallCount);
		Assert.Equal("Later", Assert.Single(context.ViewModel.ActiveLoadOrder).ModuleId);
		Assert.True(context.ViewModel.IsActive);
	}

	[Fact]
	public async Task RepeatedActivation_PreservesCollectionIdentityWithoutDuplicates() {
		using TestContext context = new();
		context.ModpackService.CreateNew(
			"Stable",
			"Owner",
			ModpackTemplate.Vanilla);
		context.ModpackService.CurrentLoadOrderEntries = [
			new ModpackEntryModel("Active", "Active Module", "1.0")
		];
		await context.ViewModel.InitializeAsync();
		context.SelectModpack("Stable");
		object editableIdentity = context.ViewModel.EditableLoadOrder;
		int editableCount = context.ViewModel.EditableLoadOrder.Count;

		await context.ViewModel.ActivateAsync();
		await context.ViewModel.DeactivateAsync();
		await context.ViewModel.ActivateAsync();

		Assert.Same(editableIdentity, context.ViewModel.EditableLoadOrder);
		Assert.Equal(editableCount, context.ViewModel.EditableLoadOrder.Count);
		Assert.Single(context.ViewModel.ActiveLoadOrder);
		Assert.Equal(2, context.Scanner.CallCount);
	}

	[Fact]
	public async Task CreateCommand_RejectsEmptyNameWithoutWritingModpack() {
		using TestContext context = new();
		await context.ViewModel.InitializeAsync();
		int initialCount = context.ModpackService.AllModpacks.Count;
		context.ViewModel.ShowCreatePanelCommand.Execute(null);
		context.ViewModel.InputModpackName = "   ";

		context.ViewModel.ConfirmCreateCommand.Execute(null);

		Assert.Equal(initialCount, context.ModpackService.AllModpacks.Count);
		Assert.True(context.ViewModel.IsCreatePanelVisible);
		Assert.Equal("Modpack name cannot be empty.", context.ViewModel.StatusText);
		Assert.Equal("Name Required", Assert.Single(context.Notifications.Requests).Title);
	}

	[Fact]
	public async Task CreateCommand_DuplicateNameReportsFailureAndPreservesExistingPack() {
		using TestContext context = new();
		context.ModpackService.CreateNew(
			"Duplicate",
			"Original Owner",
			ModpackTemplate.Vanilla);
		await context.ViewModel.InitializeAsync();
		context.ViewModel.ShowCreatePanelCommand.Execute(null);
		context.ViewModel.InputModpackName = "Duplicate";
		context.ViewModel.InputCreatedBy = "Replacement Owner";

		context.ViewModel.ConfirmCreateCommand.Execute(null);

		Assert.Equal(
			"Original Owner",
			context.SelectedServiceModpack("Duplicate").CreatedBy);
		Assert.True(context.ViewModel.IsCreatePanelVisible);
		Assert.Equal("Create Failed", Assert.Single(context.Notifications.Requests).Title);
	}

	[Fact]
	public async Task SaveCommand_RejectsEmptyActiveLoadOrderWithoutChangingSavedPack() {
		using TestContext context = new();
		context.ModpackService.CreateNew(
			"Unchanged",
			"Owner",
			ModpackTemplate.Vanilla);
		await context.ViewModel.InitializeAsync();
		context.SelectModpack("Unchanged");
		List<string> originalIds = context.SelectedServiceModpack("Unchanged")
			.LoadOrder.Select(entry => entry.ModuleId).ToList();

		context.ViewModel.SaveModpackCommand.Execute(null);

		Assert.Equal(
			originalIds,
			context.SelectedServiceModpack("Unchanged")
				.LoadOrder.Select(entry => entry.ModuleId));
		Assert.Equal("Nothing to Save", Assert.Single(context.Notifications.Requests).Title);
	}

	[Fact]
	public async Task SaveCommand_WhenPersistenceFails_DoesNotReportSuccess() {
		using TestContext context = new();
		context.ModpackService.CreateNew(
			"Persistence Failure",
			"Owner",
			ModpackTemplate.Vanilla);
		context.ModpackService.CurrentLoadOrderEntries = [
			new ModpackEntryModel("Active", "Active Module", "1.0")
		];
		await context.ViewModel.InitializeAsync();
		context.SelectModpack("Persistence Failure");
		string targetPath = ModpackFileHelper.GetModpackFilePath(
			context.ModpacksDirectory,
			"Persistence Failure");
		File.Delete(targetPath);
		Directory.CreateDirectory(targetPath);

		context.ViewModel.SaveModpackCommand.Execute(null);

		ToastRequest notification = Assert.Single(context.Notifications.Requests);
		Assert.Equal("Save Failed", notification.Title);
		Assert.DoesNotContain(
			context.Notifications.Requests,
			request => request.Title == "Modpack Saved");
		Assert.StartsWith("Failed to save", context.ViewModel.StatusText);
	}

	[Fact]
	public async Task ImportCommand_ImportsAndSelectsValidJsonModpack() {
		using TestContext context = new();
		ModpackModel import = new(
			"Imported Pack",
			"Fixture",
			[new ModpackEntryModel("Example", "Example", "v1")]);
		context.SetImportFile(
			"import.json",
			JsonConvert.SerializeObject(import));
		await context.ViewModel.InitializeAsync();

		context.ViewModel.ImportCommand.Execute(null);

		Assert.NotNull(context.ModpackService.FindByName("Imported Pack"));
		Assert.Equal(
			"Imported Pack",
			context.ViewModel.ModpackList[
				context.ViewModel.SelectedModpackIndex].ModpackName);
		Assert.Equal("Modpack Imported", Assert.Single(context.Notifications.Requests).Title);
	}

	[Fact]
	public async Task ImportCommand_InvalidJsonReportsFailureWithoutChangingList() {
		using TestContext context = new();
		context.SetImportFile("invalid.json", "{ invalid json");
		await context.ViewModel.InitializeAsync();
		int initialCount = context.ViewModel.ModpackList.Count;

		context.ViewModel.ImportCommand.Execute(null);

		Assert.Equal(initialCount, context.ViewModel.ModpackList.Count);
		Assert.Equal("Import Failed", Assert.Single(context.Notifications.Requests).Title);
		Assert.Contains("Failed to read", context.ViewModel.StatusText);
	}

	private sealed class TestContext : IDisposable {
		private readonly TestDirectory _directory = new();

		public TestContext(string? importPath = null) {
			string gameRoot = _directory.CreateDirectory("Game");
			string modulesRoot = _directory.CreateDirectory("Game", "Modules");
			_directory.WriteModule(modulesRoot, "Native", "Native");
			_directory.CreateDirectory("Game", "bin");
			string executable = _directory.GetPath("Game", "Bannerlord.exe");
			File.WriteAllText(executable, string.Empty);
			AppSettings settings = new(
				new ConfigFileManager(_directory.GetPath("config.json"))) {
				GameProvider = GameProvider.Steam,
				GameFolderPath = gameRoot,
				GameLauncherFilePath = executable
			};

			ModsData modsData = new(
				_directory.GetPath("mods-current.data"),
				_directory.GetPath("mods-backup.data"));
			modsData.SaveCurrent([TestDirectory.Module("Native")]);
			Scanner = new FakeScanner([TestDirectory.Module("Native")]);
			ModPipelineManager manager = new(
				settings,
				modsData,
				new ModInstaller(settings),
				Scanner,
				new SuccessfulUnblocker());
			manager.LoadAcceptedCache();

			ModpacksDirectory = _directory.CreateDirectory("Modpacks");
			ModpackService = new ModpackService(new ModpackData(
				ModpacksDirectory,
				_directory.GetPath("last-used.data")));
			ModpackService.LoadAll();
			TranslationService translator = new(
				new TranslationManager(
					_directory.CreateDirectory("Languages"),
					_directory.GetPath("Languages", "languages.json"),
					_directory.GetPath("Languages", "en-US.json")),
				settings);
			Picker = new StubImportPicker(importPath);
			Notifications = new RecordingNotificationSink();
			ViewModel = new ModpacksViewModel(
				ModpackService,
				manager,
				Picker,
				Notifications,
				new ImmediateUiDispatcher(),
				translator);
		}

		public ModpackService ModpackService { get; }
		public string ModpacksDirectory { get; }
		public StubImportPicker Picker { get; }
		public RecordingNotificationSink Notifications { get; }
		public FakeScanner Scanner { get; }
		public ModpacksViewModel ViewModel { get; }

		public void SelectModpack(string name) {
			int index = ViewModel.ModpackList
				.Select((modpack, itemIndex) => new { modpack, itemIndex })
				.Single(item => item.modpack.ModpackName == name)
				.itemIndex;
			ViewModel.SelectedModpackIndex = index;
		}

		public ModpackModel SelectedServiceModpack(string name) =>
			Assert.IsType<ModpackModel>(ModpackService.FindByName(name));

		public void SetImportFile(string fileName, string contents) {
			string path = _directory.GetPath(fileName);
			File.WriteAllText(path, contents);
			Picker.Path = path;
		}

		public void Dispose() => _directory.Dispose();
	}

	private sealed class StubImportPicker(string? path) : IModpackImportFilePicker {
		public int CallCount { get; private set; }
		public string? Path { get; set; } = path;

		public string? PickImportFile() {
			CallCount++;
			return Path;
		}
	}

	private sealed class RecordingNotificationSink : IToastNotificationSink {
		public List<ToastRequest> Requests { get; } = [];

		public IToastNotificationHandle Open(ToastRequest request) {
			Requests.Add(request);
			return new NoOpToastHandle();
		}
	}

	private sealed class NoOpToastHandle : IToastNotificationHandle {
		public void UpdateProgress(double value, double maximum, string? message = null) { }
		public void Transition(ToastRequest request) { }
		public void Close() { }
		public void Dispose() { }
	}

	private sealed class ImmediateUiDispatcher : IUiDispatcher {
		public bool CheckAccess() => true;

		public Task InvokeAsync(
			Action action,
			CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			action();
			return Task.CompletedTask;
		}
	}

	private sealed class FakeScanner(IReadOnlyList<ModuleModel> modules) : IModScanner {
		private int _callCount;
		public int CallCount => Volatile.Read(ref _callCount);

		public Task<ModScanResult> ScanAsync(
			AppSettings config,
			CancellationToken token = default) {
			Interlocked.Increment(ref _callCount);
			return Task.FromResult(new ModScanResult {
				Modules = modules,
				LocalRoot = new ModScanRootResult {
					Status = ModScanRootStatus.Succeeded,
					ModuleCount = modules.Count
				},
				WorkshopRoot = new ModScanRootResult {
					Status = ModScanRootStatus.NotApplicable
				}
			});
		}
	}

	private sealed class SuccessfulUnblocker : IModuleUnblocker {
		public Task<UnblockResult> UnblockModulesAsync(
			string modulesDirectoryPath,
			CancellationToken token = default) =>
			Task.FromResult(new UnblockResult { Succeeded = true });
	}
}
