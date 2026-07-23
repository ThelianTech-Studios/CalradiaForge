namespace CalradiaForge.Tests.Core.Mods;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Infra.Paths;
using CalradiaForge.Core.Models;
using CalradiaForge.Tests.Core.Support;

public sealed class ModPipelineManagerTests {
	[Fact]
	public async Task CompleteScan_CommitsCacheAndPublishesOneVersionedSnapshot() {
		using TestDirectory temp = new();
		AppConfigSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
		ModsData data = CreateData(temp);
		data.SaveCurrent([TestDirectory.Module("Old")]);
		FakeScanner scanner = new(_ => Task.FromResult(CompleteResult(
			[TestDirectory.Module("Native"), TestDirectory.Module("New")])));
		ModPipelineManager coordinator = CreateManager(settings, data, scanner);
		AcceptedModSnapshot cached = coordinator.LoadAcceptedCache();

		ModPipelineResult result = await coordinator.RefreshAsync();

		Assert.True(result.Success);
		Assert.True(result.IsComplete);
		Assert.True(result.WasCommitted);
		Assert.True(result.HasChanges);
		Assert.Equal(cached.Version + 1, result.AcceptedSnapshot.Version);
		Assert.Equal(["Native", "New"], result.AcceptedSnapshot.Modules.Select(module => module.ModuleId));
		Assert.Equal("New", Assert.Single(result.AcceptedSnapshot.AddedModules, module => module.ModuleId == "New").ModuleId);
		Assert.Equal("Old", Assert.Single(result.AcceptedSnapshot.RemovedModules).ModuleId);
		Assert.Equal(["Native", "New"], data.LoadCurrent().Select(module => module.ModuleId));
		Assert.Equal("Old", Assert.Single(data.LoadBackup()).ModuleId);
	}

	[Fact]
	public async Task IncompleteConfiguredWorkshop_PreservesAcceptedSnapshotAndBothCacheFiles() {
		using TestDirectory temp = new();
		AppConfigSettings settings = CreateValidSettings(temp, GameProvider.Steam);
		settings.SteamWorkshopFolderPath = temp.GetPath("MissingWorkshop");
		ModsData data = CreateData(temp);
		data.SaveCurrent([TestDirectory.Module("Current")]);
		data.SaveBackup([TestDirectory.Module("Backup")]);
		FakeScanner scanner = new(_ => Task.FromResult(new ModScanResult {
			Modules = [TestDirectory.Module("Local.Partial")],
			LocalRoot = Root(ModScanRootStatus.Succeeded, 1),
			WorkshopRoot = Root(ModScanRootStatus.Missing),
			Warnings = ["Configured Workshop root is missing."]
		}));
		ModPipelineManager coordinator = CreateManager(settings, data, scanner);
		AcceptedModSnapshot before = coordinator.LoadAcceptedCache();
		IReadOnlyList<ModuleModel> beforeRemoved = before.RemovedModules;

		ModPipelineResult result = await coordinator.RefreshAsync();

		Assert.Equal(ModPipelineStatus.Incomplete, result.Status);
		Assert.False(result.WasCommitted);
		Assert.Same(before, coordinator.AcceptedSnapshot);
		Assert.Equal("Current", Assert.Single(data.LoadCurrent()).ModuleId);
		Assert.Equal("Backup", Assert.Single(data.LoadBackup()).ModuleId);
		Assert.Same(beforeRemoved, coordinator.AcceptedSnapshot.RemovedModules);
	}

	[Fact]
	public async Task InvalidConfiguration_DoesNotInvokeScannerOrCommit() {
		using TestDirectory temp = new();
		AppConfigSettings settings = new(new AppConfig(temp.GetPath("config.json"))) {
			GameProvider = GameProvider.ManualConfiguration,
			GameFolderPath = temp.GetPath("MissingGame")
		};
		ModsData data = CreateData(temp);
		data.SaveCurrent([TestDirectory.Module("Cached")]);
		FakeScanner scanner = new(_ => throw new InvalidOperationException("Scanner must not run."));
		ModPipelineManager coordinator = CreateManager(settings, data, scanner);
		AcceptedModSnapshot before = coordinator.LoadAcceptedCache();

		ModPipelineResult result = await coordinator.InitializeForStartupAsync();

		Assert.Equal(ModPipelineStatus.InvalidConfiguration, result.Status);
		Assert.False(result.WasCommitted);
		Assert.Equal(0, scanner.CallCount);
		Assert.Same(before, coordinator.AcceptedSnapshot);
		Assert.Equal("Cached", Assert.Single(data.LoadCurrent()).ModuleId);
	}

	[Fact]
	public async Task UnexpectedScannerFailure_PreservesAcceptedSnapshot() {
		using TestDirectory temp = new();
		AppConfigSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
		ModsData data = CreateData(temp);
		data.SaveCurrent([TestDirectory.Module("Cached")]);
		FakeScanner scanner = new(_ => throw new IOException("Injected scanner failure."));
		ModPipelineManager coordinator = CreateManager(settings, data, scanner);
		AcceptedModSnapshot before = coordinator.LoadAcceptedCache();

		ModPipelineResult result = await coordinator.RefreshAsync();

		Assert.Equal(ModPipelineStatus.Failed, result.Status);
		Assert.False(result.WasCommitted);
		Assert.Same(before, coordinator.AcceptedSnapshot);
		Assert.Equal("Cached", Assert.Single(data.LoadCurrent()).ModuleId);
	}

	[Fact]
	public async Task StartupAndRefresh_UseTheSameCommitBoundary() {
		using TestDirectory temp = new();
		AppConfigSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
		ModsData data = CreateData(temp);
		FakeScanner scanner = new(_ => Task.FromResult(CompleteResult([TestDirectory.Module("Native")])));
		ModPipelineManager coordinator = CreateManager(settings, data, scanner);
		coordinator.LoadAcceptedCache();

		ModPipelineResult startup = await coordinator.InitializeForStartupAsync();
		ModPipelineResult refresh = await coordinator.RefreshAsync();

		Assert.Equal(ModPipelineOperation.StartupScan, startup.Operation);
		Assert.Equal(ModPipelineOperation.RefreshScan, refresh.Operation);
		Assert.True(startup.WasCommitted);
		Assert.True(refresh.WasCommitted);
		Assert.Equal(startup.AcceptedSnapshot.Version + 1, refresh.AcceptedSnapshot.Version);
		Assert.Equal(2, scanner.CallCount);
	}

	[Fact]
	public async Task LoadAcceptedCache_AfterCommitAdvancesVersionInsteadOfRegressingIt() {
		using TestDirectory temp = new();
		AppConfigSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
		ModsData data = CreateData(temp);
		FakeScanner scanner = new(_ => Task.FromResult(CompleteResult([TestDirectory.Module("Native")])));
		ModPipelineManager coordinator = CreateManager(settings, data, scanner);
		AcceptedModSnapshot initial = coordinator.LoadAcceptedCache();
		AcceptedModSnapshot committed = (await coordinator.RefreshAsync()).AcceptedSnapshot;

		AcceptedModSnapshot reloaded = coordinator.LoadAcceptedCache();

		Assert.True(committed.Version > initial.Version);
		Assert.Equal(committed.Version + 1, reloaded.Version);
		Assert.Same(reloaded, coordinator.AcceptedSnapshot);
	}

	[Fact]
	public async Task CancellationBeforeCommitLinearization_PreservesCacheAndSnapshot() {
		using TestDirectory temp = new();
		AppConfigSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
		ModsData data = CreateData(temp);
		data.SaveCurrent([TestDirectory.Module("Cached")]);
		TaskCompletionSource<bool> commitBarrierEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
		TaskCompletionSource<bool> releaseCommitBarrier = new(TaskCreationOptions.RunContinuationsAsynchronously);
		FakeScanner scanner = new(_ => Task.FromResult(CompleteResult([TestDirectory.Module("Replacement")])));
		ModPipelineManager coordinator = new(
			settings,
			data,
			new ModInstaller(settings),
			scanner,
			_ => {
				commitBarrierEntered.TrySetResult(true);
				return releaseCommitBarrier.Task;
			});
		AcceptedModSnapshot before = coordinator.LoadAcceptedCache();
		Task<ModPipelineResult> active = coordinator.RefreshAsync();
		await commitBarrierEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

		coordinator.RequestCancellation();
		releaseCommitBarrier.TrySetResult(true);
		ModPipelineResult result = await active.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.Equal(ModPipelineStatus.Cancelled, result.Status);
		Assert.False(result.WasCommitted);
		Assert.Same(before, coordinator.AcceptedSnapshot);
		Assert.Equal("Cached", Assert.Single(data.LoadCurrent()).ModuleId);
		Assert.Empty(data.LoadBackup());
	}

	[Fact]
	public async Task ConcurrentRequest_IsRejectedBusyUntilFirstOperationCompletes() {
		using TestDirectory temp = new();
		AppConfigSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
		ModsData data = CreateData(temp);
		TaskCompletionSource<bool> entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
		TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
		FakeScanner scanner = new(async token => {
			entered.TrySetResult(true);
			await release.Task.WaitAsync(token);
			return CompleteResult([TestDirectory.Module("Native")]);
		});
		ModPipelineManager coordinator = CreateManager(settings, data, scanner);
		coordinator.LoadAcceptedCache();

		Task<ModPipelineResult> first = coordinator.RefreshAsync();
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		ModPipelineResult second = await coordinator.RefreshAsync();
		ModInstallSummary? install = await coordinator.InstallAsync([]);

		Assert.Equal(ModPipelineStatus.Busy, second.Status);
		Assert.False(second.WasCommitted);
		Assert.Null(install);
		release.TrySetResult(true);
		Assert.True((await first).Success);
	}

	[Fact]
	public async Task ActiveInstall_SharesAdmissionCancellationAndQuiescenceBoundary() {
		using TestDirectory temp = new();
		AppConfigSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
		ModsData data = CreateData(temp);
		string archivePath = temp.CreateZip(
			"lifecycle.zip",
			("Lifecycle/SubModule.xml", TestDirectory.ModuleXml("Lifecycle.Mod", "Lifecycle Mod")),
			("Lifecycle/bin/payload.txt", "payload"));
		ModInstaller installer = new(settings);
		TaskCompletionSource<bool> callbackEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
		TaskCompletionSource<bool> releaseCallback = new(TaskCreationOptions.RunContinuationsAsynchronously);
		int callbackCount = 0;
		installer.ExtractionProgressChanged += _ => {
			Interlocked.Increment(ref callbackCount);
			callbackEntered.TrySetResult(true);
			releaseCallback.Task.GetAwaiter().GetResult();
		};
		ModPipelineManager coordinator = new(
			settings,
			data,
			installer,
			new FakeScanner(_ => Task.FromResult(CompleteResult([]))));
		coordinator.LoadAcceptedCache();

		Task<ModInstallSummary?> activeInstall = coordinator.InstallAsync([archivePath]);
		await callbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Task quiescence = coordinator.WaitForQuiescenceAsync();

		Assert.True(coordinator.IsInstalling);
		Assert.True(coordinator.HasActiveWork);
		Assert.False(quiescence.IsCompleted);
		Assert.Null(await coordinator.InstallAsync([]));
		Assert.Equal(ModPipelineStatus.Busy, (await coordinator.RefreshAsync()).Status);

		coordinator.StopAcceptingNewWork();
		coordinator.StopAcceptingNewWork();
		coordinator.RequestCancellation();
		coordinator.RequestCancellation();
		releaseCallback.TrySetResult(true);

		Assert.NotNull(await activeInstall.WaitAsync(TimeSpan.FromSeconds(5)));
		await quiescence.WaitAsync(TimeSpan.FromSeconds(5));
		int callbacksAtQuiescence = Volatile.Read(ref callbackCount);
		await Task.Delay(50);

		Assert.Equal(callbacksAtQuiescence, Volatile.Read(ref callbackCount));
		Assert.False(coordinator.HasActiveWork);
		Assert.False(coordinator.IsInstalling);
		Assert.Null(await coordinator.InstallAsync([]));
		Assert.Equal(ModPipelineStatus.AdmissionStopped, (await coordinator.RefreshAsync()).Status);
		await coordinator.WaitForQuiescenceAsync();
	}

	[Fact]
	public async Task StopCancelAndQuiescence_AreIdempotentAndBlockFutureAdmission() {
		using TestDirectory temp = new();
		AppConfigSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
		ModsData data = CreateData(temp);
		TaskCompletionSource<bool> entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
		FakeScanner scanner = new(async token => {
			entered.TrySetResult(true);
			await Task.Delay(Timeout.InfiniteTimeSpan, token);
			return CompleteResult([]);
		});
		ModPipelineManager coordinator = CreateManager(settings, data, scanner);
		AcceptedModSnapshot before = coordinator.LoadAcceptedCache();
		Task<ModPipelineResult> active = coordinator.RefreshAsync();
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));

		coordinator.StopAcceptingNewWork();
		coordinator.StopAcceptingNewWork();
		coordinator.RequestCancellation();
		coordinator.RequestCancellation();
		await coordinator.WaitForQuiescenceAsync().WaitAsync(TimeSpan.FromSeconds(5));

		Assert.Equal(ModPipelineStatus.Cancelled, (await active).Status);
		Assert.Same(before, coordinator.AcceptedSnapshot);
		Assert.False(coordinator.HasActiveWork);
		Assert.False(coordinator.IsAcceptingNewWork);
		Assert.Equal(ModPipelineStatus.AdmissionStopped, (await coordinator.RefreshAsync()).Status);
		await coordinator.WaitForQuiescenceAsync();
	}

	private static AppConfigSettings CreateValidSettings(TestDirectory temp, GameProvider provider) {
		string gameRoot = temp.CreateDirectory("Game");
		string modulesRoot = temp.CreateDirectory("Game", "Modules");
		temp.WriteModule(modulesRoot, "Native", "Native");
		temp.CreateDirectory("Game", "bin");
		return new AppConfigSettings(new AppConfig(temp.GetPath($"config-{Guid.NewGuid():N}.json"))) {
			GameProvider = provider,
			GameFolderPath = gameRoot
		};
	}

	private static ModsData CreateData(TestDirectory temp) =>
		new(temp.GetPath("mods_current.data"), temp.GetPath("mods_backup.data"));

	private static ModPipelineManager CreateManager(
		AppConfigSettings settings,
		ModsData data,
		IModScanner scanner) => new(settings, data, new ModInstaller(settings), scanner);

	private static ModScanResult CompleteResult(IReadOnlyList<ModuleModel> modules) => new() {
		Modules = modules,
		LocalRoot = Root(ModScanRootStatus.Succeeded, modules.Count),
		WorkshopRoot = Root(ModScanRootStatus.NotApplicable)
	};

	private static ModScanRootResult Root(ModScanRootStatus status, int count = 0) => new() {
		Status = status,
		ModuleCount = count
	};

	private sealed class FakeScanner(Func<CancellationToken, Task<ModScanResult>> scan) : IModScanner {
		private int _callCount;
		public int CallCount => Volatile.Read(ref _callCount);
		public Task<ModScanResult> ScanAsync(AppConfigSettings config, CancellationToken token = default) {
			Interlocked.Increment(ref _callCount);
			return scan(token);
		}
	}
}
