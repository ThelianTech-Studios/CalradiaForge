namespace CalradiaForge.Tests.UI.ViewModels;

using System.Windows.Threading;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Launch;
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

public sealed class LauncherViewModelTests {
	[Fact]
	public async Task InitializeAsync_UsesCurrentTranslationForGhostModpackLabel() {
		using TestContext context = new(
			[TestDirectory.Module("Native")],
			translations: new Dictionary<string, string> {
				[nameof(TranslationStrings.Launcher_GhostModpackLabel)] =
					"Localized modpack choice"
			});

		await context.ViewModel.InitializeAsync();

		Assert.Equal(
			"Localized modpack choice",
			context.ViewModel.ModpackList[0].ModpackName);
	}

	[Fact]
	public async Task ActivateAsync_RefreshesGhostModpackLabelAfterLanguageChange() {
		using TestContext context = new(
			[TestDirectory.Module("Native")],
			translations: new Dictionary<string, string> {
				[nameof(TranslationStrings.Launcher_GhostModpackLabel)] =
					"Initial modpack choice"
			});
		await context.ViewModel.InitializeAsync();
		await context.ViewModel.ActivateAsync();
		context.ViewModel.Translator.Strings.Apply(new Dictionary<string, string> {
			[nameof(TranslationStrings.Launcher_GhostModpackLabel)] =
				"Updated modpack choice"
		});

		await context.ViewModel.DeactivateAsync();
		await context.ViewModel.ActivateAsync();

		Assert.Equal(
			"Updated modpack choice",
			context.ViewModel.ModpackList[0].ModpackName);
	}

	[Fact]
	public async Task LaunchTargetCommand_UsesTypedTargetAndPersistsSelection() {
		using TestContext context = new([TestDirectory.Module("Native")]);
		await context.ViewModel.InitializeAsync();

		context.ViewModel.SetLaunchTargetCommand.Execute(LaunchTarget.BLSE);

		Assert.Equal(LaunchTarget.BLSE, context.ViewModel.ActiveLaunchTarget);
		Assert.Equal(LaunchTarget.BLSE, context.Settings.DefaultLaunchTarget);
		Assert.True(context.ViewModel.IsBlseTarget);
		Assert.False(context.ViewModel.IsBannerlordTarget);

		context.ViewModel.SetLaunchTargetCommand.Execute(LaunchTarget.Bannerlord);
		Assert.Equal(LaunchTarget.Bannerlord, context.Settings.DefaultLaunchTarget);
	}

	[Fact]
	public async Task MoveModule_PreservesCollectionsAndSynchronizesWorkingLoadOrder() {
		using TestContext context = new([TestDirectory.Module("Native")]);
		LauncherViewModel viewModel = context.ViewModel;
		await viewModel.InitializeAsync();
		object loadOrderIdentity = viewModel.CurrentLoadOrder;
		object availableIdentity = viewModel.AvailableModsList;
		ModuleModel module = Assert.Single(viewModel.AvailableModsList);

		viewModel.MoveModule(
			LauncherModuleCollection.Available,
			LauncherModuleCollection.LoadOrder,
			module,
			0);

		Assert.Same(loadOrderIdentity, viewModel.CurrentLoadOrder);
		Assert.Same(availableIdentity, viewModel.AvailableModsList);
		Assert.Same(module, Assert.Single(viewModel.CurrentLoadOrder));
		Assert.Equal("Native", Assert.Single(context.ModpackService.CurrentLoadOrderEntries).ModuleId);
		Assert.True(viewModel.CanStart);
	}

	[Fact]
	public async Task InstallCommand_PickerCancellationInvokesNoPipelineWork() {
		using TestContext context = new(
			[TestDirectory.Module("Native")],
			pickerChoices: []);
		await context.ViewModel.InitializeAsync();

		await context.ViewModel.InstallModsCommand.ExecuteAsync(null);

		Assert.Equal(ModPipelineOperation.None, context.Manager.ActiveOperation);
		Assert.Null(context.ViewModel.LastInstallResult);
		Assert.Empty(context.Presenter.Completions);
		Assert.False(context.ViewModel.IsInstalling);
	}

	[Fact]
	public async Task RefreshCommand_AppliesAcceptedSnapshotToStableCollections() {
		using TestContext context = new(
			[TestDirectory.Module("Prior")],
			scannedModules: [TestDirectory.Module("First"), TestDirectory.Module("Second")]);
		await context.ViewModel.InitializeAsync();
		object loadOrderIdentity = context.ViewModel.CurrentLoadOrder;
		object availableIdentity = context.ViewModel.AvailableModsList;

		await context.ViewModel.RefreshModsCommand.ExecuteAsync(null);

		Assert.Equal(1, context.Scanner.CallCount);
		Assert.Same(loadOrderIdentity, context.ViewModel.CurrentLoadOrder);
		Assert.Same(availableIdentity, context.ViewModel.AvailableModsList);
		Assert.Equal(2, context.ViewModel.AvailableModsList.Count);
		Assert.NotNull(context.ViewModel.LastRefreshResult);
		Assert.False(context.ViewModel.IsRefreshing);
	}

	[Fact]
	public async Task Activation_SkipsDuplicateStartupScanThenRefreshesOnNavigationReturn() {
		using TestContext context = new([TestDirectory.Module("Native")]);

		await context.ViewModel.InitializeAsync();
		await context.ViewModel.ActivateAsync();

		Assert.Equal(0, context.Scanner.CallCount);

		await context.ViewModel.DeactivateAsync();
		await context.ViewModel.ActivateAsync();

		Assert.Equal(1, context.Scanner.CallCount);
	}

	[Fact]
	public async Task InstallCommand_PresentationFailureReportsSemanticCompletionExactlyOnce() {
		using TestDirectory temp = new();
		string archive = temp.CreateZip(
			"Example.zip",
			("Example/SubModule.xml", TestDirectory.ModuleXml("Example.Mod", "Example Mod")));
		using TestContext context = new(
			[TestDirectory.Module("Prior")],
			scannedModules: [TestDirectory.Module("Example.Mod")],
			pickerChoices: [archive],
			externalDirectory: temp);
		await context.ViewModel.InitializeAsync();
		object availableIdentity = context.ViewModel.AvailableModsList;

		await context.ViewModel.InstallModsCommand.ExecuteAsync(null);

		ModInstallOperationResult result =
			Assert.IsType<ModInstallOperationResult>(context.ViewModel.LastInstallResult);
		Assert.True(
			result.WasAdmitted,
			$"{result.Status}: {string.Join("; ", result.TechnicalDiagnostics)}");
		LauncherInstallPresentationCompletion completion =
			Assert.Single(context.Presenter.Completions);
		Assert.Equal(result.OperationId, completion.OperationId);
		Assert.False(completion.Succeeded);
		Assert.False(string.IsNullOrWhiteSpace(completion.ErrorMessage));
		Assert.Same(availableIdentity, context.ViewModel.AvailableModsList);
		Assert.False(context.ViewModel.IsInstalling);
		Assert.Equal(ModPipelineOperation.None, context.ViewModel.CurrentOperation);
	}

	[Fact]
	public async Task InstallCommand_AdmittedSuccessOnWpfDispatcher_ReappliesSelectedModpackAndCompletesOnce() {
		await using DispatcherThreadHost host = await DispatcherThreadHost.StartAsync();
		using TestDirectory temp = new();
		string archive = temp.CreateZip(
			"Example.zip",
			("Example/SubModule.xml", TestDirectory.ModuleXml("Example.Mod", "Example Mod")),
			("Example/payload.txt", "payload"));
		TestContext context = await host.Dispatcher.InvokeAsync(() => new TestContext(
			[TestDirectory.Module("Prior")],
			scannedModules: [TestDirectory.Module("Example.Mod")],
			pickerChoices: [archive],
			externalDirectory: temp,
			uiDispatcher: new WpfUiDispatcher(host.Dispatcher)));
		using (context) {
			context.ModpackService.SaveAs(
				"Installed Selection",
				"Tester",
				[new ModpackEntryModel("Example.Mod", "Example Mod", "v1.0.0")]);
			await context.ViewModel.InitializeAsync();
			await host.Dispatcher.InvokeAsync(() =>
				context.ViewModel.SelectedModpackIndex = context.ViewModel.ModpackList
					.Select((modpack, index) => new { modpack, index })
					.Single(item => item.modpack.ModpackName == "Installed Selection")
					.index);

			await context.ViewModel.InstallModsCommand.ExecuteAsync(null);

			ModInstallOperationResult result =
				Assert.IsType<ModInstallOperationResult>(context.ViewModel.LastInstallResult);
			Assert.True(result.WasAdmitted);
			Assert.True(result.Success, result.Status.ToString());
			Assert.Equal("Example.Mod", Assert.Single(context.ViewModel.CurrentLoadOrder).ModuleId);
			Assert.True(context.ViewModel.CanStart);
			LauncherInstallPresentationCompletion completion =
				Assert.Single(context.Presenter.Completions);
			Assert.Equal(result.OperationId, completion.OperationId);
			Assert.True(completion.Succeeded);
		}
	}

	[Fact]
	public async Task InstallCommand_SucceededWithWarnings_RemainsAdmittedAndCompletesPresentation() {
		using TestDirectory temp = new();
		string archive = temp.CreateZip(
			"native.zip",
			("Native/SubModule.xml", TestDirectory.ModuleXml("Native", "Native")));

		await AssertAdmittedTerminalStatusAsync(
			temp,
			[archive],
			[TestDirectory.Module("Native")],
			ModInstallOperationStatus.SucceededWithWarnings,
			expectReconciliation: false);
	}

	[Fact]
	public async Task InstallCommand_PartiallyFailed_ReconcilesAndCompletesPresentation() {
		using TestDirectory temp = new();
		string validArchive = temp.CreateZip(
			"valid.zip",
			("ValidMod/SubModule.xml", TestDirectory.ModuleXml("Valid.Mod", "Valid Mod")));
		string unsupportedArchive = temp.GetPath("unsupported.txt");
		File.WriteAllText(unsupportedArchive, "not an archive");

		await AssertAdmittedTerminalStatusAsync(
			temp,
			[validArchive, unsupportedArchive],
			[TestDirectory.Module("Valid.Mod")],
			ModInstallOperationStatus.PartiallyFailed,
			expectReconciliation: true);
	}

	[Fact]
	public async Task InstallCommand_Failed_RemainsAdmittedAndCompletesPresentation() {
		using TestDirectory temp = new();
		string unsupportedArchive = temp.GetPath("unsupported.txt");
		File.WriteAllText(unsupportedArchive, "not an archive");

		await AssertAdmittedTerminalStatusAsync(
			temp,
			[unsupportedArchive],
			[TestDirectory.Module("Native")],
			ModInstallOperationStatus.Failed,
			expectReconciliation: false);
	}

	[Fact]
	public async Task InstallCommand_MapsBusyAndAdmissionStoppedSeparately() {
		using TestContext context = new(
			[TestDirectory.Module("Native")],
			pickerChoices: ["selected.zip"],
			blockScanner: true);
		await context.ViewModel.InitializeAsync();
		Task<ModPipelineResult> activeRefresh = context.Manager.RefreshAsync();
		await context.Scanner.Entered;

		await context.ViewModel.InstallModsCommand.ExecuteAsync(null);

		Assert.Equal(
			ModInstallOperationStatus.RejectedBusy,
			Assert.IsType<ModInstallOperationResult>(
				context.ViewModel.LastInstallResult).Status);
		string busyText = context.ViewModel.DependencyWarningText;
		context.Scanner.Release();
		await activeRefresh;

		context.Manager.StopAcceptingNewWork();
		await context.ViewModel.InstallModsCommand.ExecuteAsync(null);

		Assert.Equal(
			ModInstallOperationStatus.RejectedAdmissionStopped,
			Assert.IsType<ModInstallOperationResult>(
				context.ViewModel.LastInstallResult).Status);
		Assert.NotEqual(busyText, context.ViewModel.DependencyWarningText);
		Assert.Empty(context.Presenter.Completions);
	}

	[Fact]
	public async Task CancelCommand_RequestsManagerCancellationAndReturnsToIdle() {
		using TestContext context = new(
			[TestDirectory.Module("Native")],
			blockScanner: true);
		await context.ViewModel.InitializeAsync();

		Task refresh = context.ViewModel.RefreshModsCommand.ExecuteAsync(null);
		await context.Scanner.Entered;
		Assert.True(context.ViewModel.CanCancel);

		context.ViewModel.CancelOperationCommand.Execute(null);
		await refresh;

		Assert.True(context.Scanner.CancellationObserved);
		Assert.False(context.Manager.HasActiveWork);
		Assert.False(context.ViewModel.CanCancel);
		Assert.False(context.ViewModel.IsRefreshing);
		Assert.Equal(ModPipelineOperation.None, context.ViewModel.CurrentOperation);
	}

	[Fact]
	public async Task ActivationDuringManagerWork_DoesNotStartCompetingRefresh() {
		using TestContext context = new(
			[TestDirectory.Module("Native")],
			blockScanner: true);
		await context.ViewModel.InitializeAsync();
		await context.ViewModel.ActivateAsync();
		await context.ViewModel.DeactivateAsync();
		Task<ModPipelineResult> activeRefresh = context.Manager.RefreshAsync();
		await context.Scanner.Entered;

		await context.ViewModel.ActivateAsync();

		Assert.Equal(1, context.Scanner.CallCount);
		Assert.True(context.ViewModel.IsActive);
		context.Scanner.Release();
		await activeRefresh;
	}

	private static async Task AssertAdmittedTerminalStatusAsync(
		TestDirectory temp,
		IReadOnlyList<string> archives,
		IReadOnlyList<ModuleModel> scannedModules,
		ModInstallOperationStatus expectedStatus,
		bool expectReconciliation) {
		await using DispatcherThreadHost host = await DispatcherThreadHost.StartAsync();
		TestContext context = await host.Dispatcher.InvokeAsync(() => new TestContext(
			[TestDirectory.Module("Native")],
			scannedModules: scannedModules,
			pickerChoices: archives,
			externalDirectory: temp,
			uiDispatcher: new WpfUiDispatcher(host.Dispatcher)));
		using (context) {
			await context.ViewModel.InitializeAsync();

			await context.ViewModel.InstallModsCommand.ExecuteAsync(null);

			ModInstallOperationResult result =
				Assert.IsType<ModInstallOperationResult>(context.ViewModel.LastInstallResult);
			Assert.Equal(expectedStatus, result.Status);
			Assert.True(result.WasAdmitted);
			if (expectReconciliation) {
				Assert.True(
					Assert.IsType<ModPipelineResult>(result.ReconciliationResult).Success);
			} else {
				Assert.Null(result.ReconciliationResult);
			}
			Assert.DoesNotContain(
				result.Status,
				new[] {
					ModInstallOperationStatus.RejectedBusy,
					ModInstallOperationStatus.RejectedAdmissionStopped,
					ModInstallOperationStatus.ValidationFailed
				});
			LauncherInstallPresentationCompletion completion =
				Assert.Single(context.Presenter.Completions);
			Assert.Equal(result.OperationId, completion.OperationId);
			Assert.True(completion.Succeeded);
			Assert.Null(completion.ErrorMessage);
			Assert.False(context.ViewModel.IsBusy);
			Assert.False(context.ViewModel.CanCancel);
			Assert.Equal(ModPipelineOperation.None, context.ViewModel.CurrentOperation);
			Assert.True(context.ViewModel.InstallModsCommand.CanExecute(null));
		}
	}

	private sealed class TestContext : IDisposable {
		private readonly TestDirectory? _ownedDirectory;

		public TestContext(
			IReadOnlyList<ModuleModel> initialModules,
			IReadOnlyList<ModuleModel>? scannedModules = null,
			IReadOnlyList<string>? pickerChoices = null,
			TestDirectory? externalDirectory = null,
			IUiDispatcher? uiDispatcher = null,
			bool blockScanner = false,
			Dictionary<string, string>? translations = null) {
			Directory = externalDirectory ?? new TestDirectory();
			if (externalDirectory is null) {
				_ownedDirectory = Directory;
			}

			string gameRoot = Directory.CreateDirectory("Game");
			string modulesRoot = Directory.CreateDirectory("Game", "Modules");
			Directory.WriteModule(modulesRoot, "Native", "Native");
			Directory.CreateDirectory("Game", "bin");
			string executable = Directory.GetPath("Game", "Bannerlord.exe");
			File.WriteAllText(executable, string.Empty);
			Settings = new AppSettings(
				new ConfigFileManager(Directory.GetPath($"config-{Guid.NewGuid():N}.json"))) {
				GameProvider = GameProvider.Steam,
				GameFolderPath = gameRoot,
				GameLauncherFilePath = executable,
				ModpackStartupMode = ModpackStartupMode.AlwaysAsk
			};

			ModsData modsData = new(
				Directory.GetPath("mods-current.data"),
				Directory.GetPath("mods-backup.data"));
			modsData.SaveCurrent(initialModules.ToList());
			Scanner = new FakeScanner(scannedModules ?? initialModules, blockScanner);
			Manager = new ModPipelineManager(
				Settings,
				modsData,
				new ModInstaller(Settings),
				Scanner,
				new SuccessfulUnblocker());
			Manager.LoadAcceptedCache();

			ModpackService = new ModpackService(new ModpackData(
				Directory.CreateDirectory("Modpacks"),
				Directory.GetPath("last-used.data")));
			ModpackService.LoadAll();
			TranslationService translator = new(
				new TranslationManager(
					Directory.CreateDirectory("Languages"),
					Directory.GetPath("Languages", "languages.json"),
					Directory.GetPath("Languages", "en-US.json")),
				Settings);
			if (translations is not null) {
				translator.Strings.Apply(translations);
			}
			Presenter = new RecordingPresenter();
			ViewModel = new LauncherViewModel(
				Settings,
				Manager,
				ModpackService,
				new GameLauncher(Settings),
				uiDispatcher ?? new ImmediateUiDispatcher(),
				new StubArchivePicker(pickerChoices ?? []),
				new RecordingNotificationSink(),
				Presenter,
				translator);
		}

		public TestDirectory Directory { get; }
		public AppSettings Settings { get; }
		public ModPipelineManager Manager { get; }
		public ModpackService ModpackService { get; }
		public FakeScanner Scanner { get; }
		public RecordingPresenter Presenter { get; }
		public LauncherViewModel ViewModel { get; }

		public void Dispose() => _ownedDirectory?.Dispose();
	}

	private sealed class ImmediateUiDispatcher : IUiDispatcher {
		public bool CheckAccess() => true;

		public Task InvokeAsync(Action action, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			action();
			return Task.CompletedTask;
		}
	}

	private sealed class StubArchivePicker(IReadOnlyList<string> choices)
		: IModArchiveFilePicker {
		public IReadOnlyList<string> PickArchives() => choices;
	}

	private sealed class RecordingPresenter : IInstallNotificationPresenter {
		public List<LauncherInstallPresentationCompletion> Completions { get; } = [];

		public void Activate() { }

		public void ReportLauncherCompletion(LauncherInstallPresentationCompletion completion) =>
			Completions.Add(completion);
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

	private sealed class FakeScanner(
		IReadOnlyList<ModuleModel> modules,
		bool block = false) : IModScanner {
		private int _callCount;
		private readonly TaskCompletionSource<bool> _entered =
			new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource<bool> _release =
			new(TaskCreationOptions.RunContinuationsAsynchronously);
		public int CallCount => Volatile.Read(ref _callCount);
		public Task Entered => _entered.Task;
		public bool CancellationObserved { get; private set; }
		public void Release() => _release.TrySetResult(true);

		public async Task<ModScanResult> ScanAsync(
			AppSettings config,
			CancellationToken token = default) {
			Interlocked.Increment(ref _callCount);
			_entered.TrySetResult(true);
			if (block) {
				try {
					await _release.Task.WaitAsync(token);
				} catch (OperationCanceledException) {
					CancellationObserved = true;
					throw;
				}
			}
			return new ModScanResult {
				Modules = modules,
				LocalRoot = new ModScanRootResult {
					Status = ModScanRootStatus.Succeeded,
					ModuleCount = modules.Count
				},
				WorkshopRoot = new ModScanRootResult {
					Status = ModScanRootStatus.NotApplicable
				}
			};
		}
	}

	private sealed class DispatcherThreadHost : IAsyncDisposable {
		private readonly Thread _thread;

		private DispatcherThreadHost(Thread thread, Dispatcher dispatcher) {
			_thread = thread;
			Dispatcher = dispatcher;
		}

		public Dispatcher Dispatcher { get; }

		public static async Task<DispatcherThreadHost> StartAsync() {
			TaskCompletionSource<Dispatcher> ready =
				new(TaskCreationOptions.RunContinuationsAsynchronously);
			Thread thread = new(() => {
				Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
				ready.TrySetResult(dispatcher);
				Dispatcher.Run();
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			return new DispatcherThreadHost(thread, await ready.Task);
		}

		public async ValueTask DisposeAsync() {
			await Dispatcher.InvokeAsync(Dispatcher.InvokeShutdown);
			_thread.Join();
		}
	}

	private sealed class SuccessfulUnblocker : IModuleUnblocker {
		public Task<UnblockResult> UnblockModulesAsync(
			string modulesDirectoryPath,
			CancellationToken token = default) =>
			Task.FromResult(new UnblockResult { Succeeded = true });
	}
}
