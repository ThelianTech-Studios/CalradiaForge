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
		AppSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
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
		AppSettings settings = CreateValidSettings(temp, GameProvider.Steam);
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
		AppSettings settings = new(new ConfigFileManager(temp.GetPath("config.json"))) {
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
		AppSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
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
		AppSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
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
		AppSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
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
		AppSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
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
		AppSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
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
		ModInstallOperationResult install = await coordinator.InstallAsync([]);

		Assert.Equal(ModPipelineStatus.Busy, second.Status);
		Assert.False(second.WasCommitted);
		Assert.Equal(ModInstallOperationStatus.RejectedBusy, install.Status);
		release.TrySetResult(true);
		Assert.True((await first).Success);
	}

	[Fact]
	public async Task ActiveInstall_SharesAdmissionCancellationAndQuiescenceBoundary() {
		using TestDirectory temp = new();
		AppSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
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

		Task<ModInstallOperationResult> activeInstall = coordinator.InstallAsync([archivePath]);
		await callbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Task quiescence = coordinator.WaitForQuiescenceAsync();

		Assert.True(coordinator.IsInstalling);
		Assert.True(coordinator.HasActiveWork);
		Assert.False(quiescence.IsCompleted);
		Assert.Equal(
			ModInstallOperationStatus.RejectedBusy,
			(await coordinator.InstallAsync([])).Status);
		Assert.Equal(ModPipelineStatus.Busy, (await coordinator.RefreshAsync()).Status);

		coordinator.StopAcceptingNewWork();
		coordinator.StopAcceptingNewWork();
		coordinator.RequestCancellation();
		coordinator.RequestCancellation();
		releaseCallback.TrySetResult(true);

		Assert.Equal(
			ModInstallOperationStatus.Cancelled,
			(await activeInstall.WaitAsync(TimeSpan.FromSeconds(5))).Status);
		await quiescence.WaitAsync(TimeSpan.FromSeconds(5));
		int callbacksAtQuiescence = Volatile.Read(ref callbackCount);
		await Task.Delay(50);

		Assert.Equal(callbacksAtQuiescence, Volatile.Read(ref callbackCount));
		Assert.False(coordinator.HasActiveWork);
		Assert.False(coordinator.IsInstalling);
		Assert.Equal(
			ModInstallOperationStatus.RejectedAdmissionStopped,
			(await coordinator.InstallAsync([])).Status);
		Assert.Equal(ModPipelineStatus.AdmissionStopped, (await coordinator.RefreshAsync()).Status);
		await coordinator.WaitForQuiescenceAsync();
	}

	[Fact]
	public async Task StopCancelAndQuiescence_AreIdempotentAndBlockFutureAdmission() {
		using TestDirectory temp = new();
		AppSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
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

	[Fact]
	public async Task InstallValidationAndRejections_AreNonNullAndTerminalPublished() {
		using TestDirectory temp = new();
		AppSettings settings = new(new ConfigFileManager(temp.GetPath("config.json")));
		ModsData data = CreateData(temp);
		FakeScanner scanner = new(_ => throw new InvalidOperationException("Scanner must not run."));
		ModPipelineManager manager = CreateManager(settings, data, scanner);
		List<ModInstallOperationResult> terminalResults = [];
		manager.InstallOperationCompleted += terminalResults.Add;

		ModInstallOperationResult validation = await manager.InstallAsync([]);
		manager.StopAcceptingNewWork();
		ModInstallOperationResult stopped = await manager.InstallAsync([]);

		Assert.Equal(ModInstallOperationStatus.ValidationFailed, validation.Status);
		Assert.Contains(ModInstallDiagnosticCode.ArchiveSelectionEmpty, validation.DiagnosticCodes);
		Assert.NotEqual(Guid.Empty, validation.OperationId);
		Assert.False(validation.WasAdmitted);
		Assert.Equal(ModInstallOperationStatus.RejectedAdmissionStopped, stopped.Status);
		Assert.Contains(ModInstallDiagnosticCode.AdmissionStopped, stopped.DiagnosticCodes);
		Assert.Equal([validation.OperationId, stopped.OperationId], terminalResults.Select(result => result.OperationId));
		Assert.Equal(0, scanner.CallCount);
	}

	[Fact]
	public async Task SuccessfulInstall_RelaysCorrelatedProgressAndReconcilesBeforeTerminalRelease() {
		using TestDirectory temp = new();
		AppSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
		ModsData data = CreateData(temp);
		string archivePath = temp.CreateZip(
			"progress.zip",
			("ProgressMod/SubModule.xml", TestDirectory.ModuleXml("Progress.Mod", "Progress Mod")),
			("ProgressMod/bin/payload.txt", "payload"));
		FakeScanner scanner = new(_ => Task.FromResult(CompleteResult([TestDirectory.Module("Progress.Mod")])));
		FakeUnblocker unblocker = new(_ => Task.FromResult(new UnblockResult()));
		ModPipelineManager manager = new(settings, data, new ModInstaller(settings), scanner, unblocker);
		manager.LoadAcceptedCache();
		List<ModInstallProgress> progress = [];
		int terminalCount = 0;
		manager.InstallProgressChanged += progress.Add;
		manager.InstallProgressChanged += _ => throw new InvalidOperationException("Injected progress subscriber failure.");
		manager.InstallOperationCompleted += _ => terminalCount++;
		manager.InstallOperationCompleted += _ => throw new InvalidOperationException("Injected terminal subscriber failure.");

		ModInstallOperationResult result = await manager.InstallAsync([archivePath]);

		Assert.Equal(ModInstallOperationStatus.Succeeded, result.Status);
		Assert.True(result.Success);
		Assert.NotNull(result.UnblockResult);
		Assert.True(Assert.IsType<ModPipelineResult>(result.ReconciliationResult).Success);
		Assert.Equal(result.AcceptedSnapshot, manager.AcceptedSnapshot);
		Assert.Equal(1, unblocker.CallCount);
		Assert.Equal(1, scanner.CallCount);
		Assert.Equal(1, terminalCount);
		Assert.False(manager.HasActiveWork);
		Assert.Equal(ModInstallProgressStage.OperationStarted, progress[0].Stage);
		Assert.Contains(progress, item => item.Stage == ModInstallProgressStage.Extracting);
		Assert.Contains(progress, item => item.Stage == ModInstallProgressStage.ArchiveCompleted);
		Assert.Contains(progress, item => item.Stage == ModInstallProgressStage.Unblocking);
		Assert.Contains(progress, item => item.Stage == ModInstallProgressStage.Reconciling);
		Assert.All(progress, item => Assert.Equal(result.OperationId, item.OperationId));
		Guid archiveId = Assert.Single(
			progress,
			item => item.Stage == ModInstallProgressStage.ArchiveCompleted).ArchiveId;
		Assert.NotEqual(Guid.Empty, archiveId);
		Assert.All(
			progress.Where(item => item.Stage is ModInstallProgressStage.Extracting or ModInstallProgressStage.ArchiveCompleted),
			item => Assert.Equal(archiveId, item.ArchiveId));
	}

	[Fact]
	public async Task InstalledAndFailedArchives_ClassifyPartiallyFailed() {
		using TestDirectory temp = new();
		AppSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
		ModsData data = CreateData(temp);
		string validArchive = temp.CreateZip(
			"valid.zip",
			("ValidMod/SubModule.xml", TestDirectory.ModuleXml("Valid.Mod", "Valid Mod")));
		string unsupportedArchive = temp.GetPath("unsupported.txt");
		File.WriteAllText(unsupportedArchive, "not an archive");
		FakeScanner scanner = new(_ => Task.FromResult(CompleteResult([TestDirectory.Module("Valid.Mod")])));
		FakeUnblocker unblocker = new(_ => Task.FromResult(new UnblockResult()));
		ModPipelineManager manager = new(settings, data, new ModInstaller(settings), scanner, unblocker);

		ModInstallOperationResult result = await manager.InstallAsync([validArchive, unsupportedArchive]);

		Assert.Equal(ModInstallOperationStatus.PartiallyFailed, result.Status);
		Assert.Equal(1, result.Summary.InstalledCount);
		Assert.Equal(1, result.Summary.FailedCount);
		Assert.Equal(1, unblocker.CallCount);
		Assert.Equal(1, scanner.CallCount);
	}

	[Fact]
	public async Task SkippedOnlyInstall_SucceedsWithWarningsAndSkipsModuleFinalization() {
		using TestDirectory temp = new();
		AppSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
		ModsData data = CreateData(temp);
		string archivePath = temp.CreateZip(
			"native.zip",
			("Native/SubModule.xml", TestDirectory.ModuleXml("Native", "Native")));
		FakeScanner scanner = new(_ => throw new InvalidOperationException("Scanner must not run for skipped-only work."));
		FakeUnblocker unblocker = new(_ => throw new InvalidOperationException("Unblock must not run for skipped-only work."));
		ModPipelineManager manager = new(settings, data, new ModInstaller(settings), scanner, unblocker);

		ModInstallOperationResult result = await manager.InstallAsync([archivePath]);

		Assert.Equal(ModInstallOperationStatus.SucceededWithWarnings, result.Status);
		Assert.Equal(1, result.Summary.SkippedCount);
		Assert.Equal(0, scanner.CallCount);
		Assert.Equal(0, unblocker.CallCount);
	}

	[Fact]
	public async Task RequiredFinalization_RemainsActiveAndBlocksNestedRefreshUntilReleased() {
		using TestDirectory temp = new();
		AppSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
		ModsData data = CreateData(temp);
		string archivePath = temp.CreateZip(
			"barrier.zip",
			("BarrierMod/SubModule.xml", TestDirectory.ModuleXml("Barrier.Mod", "Barrier Mod")));
		TaskCompletionSource<bool> unblockEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
		TaskCompletionSource<bool> releaseUnblock = new(TaskCreationOptions.RunContinuationsAsynchronously);
		FakeUnblocker unblocker = new(async token => {
			unblockEntered.TrySetResult(true);
			await releaseUnblock.Task.WaitAsync(token);
			return new UnblockResult();
		});
		FakeScanner scanner = new(_ => Task.FromResult(CompleteResult([TestDirectory.Module("Barrier.Mod")])));
		ModPipelineManager manager = new(settings, data, new ModInstaller(settings), scanner, unblocker);

		Task<ModInstallOperationResult> active = manager.InstallAsync([archivePath]);
		await unblockEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
		Task quiescence = manager.WaitForQuiescenceAsync();

		Assert.True(manager.IsInstalling);
		Assert.True(manager.HasActiveWork);
		Assert.False(quiescence.IsCompleted);
		Assert.Equal(ModPipelineStatus.Busy, (await manager.RefreshAsync()).Status);

		releaseUnblock.TrySetResult(true);
		Assert.Equal(
			ModInstallOperationStatus.Succeeded,
			(await active.WaitAsync(TimeSpan.FromSeconds(10))).Status);
		await quiescence.WaitAsync(TimeSpan.FromSeconds(10));
		Assert.False(manager.HasActiveWork);
	}

	[Fact]
	public async Task InstallerDefensiveGateAfterManagerAdmission_IsInvariantFailure() {
		using TestDirectory temp = new();
		AppSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
		ModsData data = CreateData(temp);
		string priorArchivePath = temp.CreateZip(
			"prior-batch.zip",
			("PriorMod/SubModule.xml", TestDirectory.ModuleXml("Prior.Mod", "Prior Mod")));
		string archivePath = temp.CreateZip(
			"defensive-gate.zip",
			("GateMod/SubModule.xml", TestDirectory.ModuleXml("Gate.Mod", "Gate Mod")),
			("GateMod/payload.txt", "payload"));
		ModInstaller installer = new(settings);
		ModInstallSummary priorSummary = Assert.IsType<ModInstallSummary>(
			await installer.InstallAsync([priorArchivePath]));
		Assert.Equal("Prior.Mod", Assert.Single(priorSummary.Results).ModuleId);
		TaskCompletionSource<bool> extractionEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
		TaskCompletionSource<bool> releaseExtraction = new(TaskCreationOptions.RunContinuationsAsynchronously);
		installer.ExtractionProgressChanged += _ => {
			extractionEntered.TrySetResult(true);
			releaseExtraction.Task.GetAwaiter().GetResult();
		};
		Task<ModInstallSummary?> defensiveOperation = installer.InstallAsync([archivePath]);
		await extractionEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
		ModPipelineManager manager = new(
			settings,
			data,
			installer,
			new FakeScanner(_ => throw new InvalidOperationException("Scanner must not run.")),
			new FakeUnblocker(_ => throw new InvalidOperationException("Unblock must not run.")));

		ModInstallOperationResult result = await manager.InstallAsync([archivePath]);
		releaseExtraction.TrySetResult(true);
		await defensiveOperation.WaitAsync(TimeSpan.FromSeconds(10));

		Assert.Equal(ModInstallOperationStatus.Failed, result.Status);
		Assert.Contains(ModInstallDiagnosticCode.InstallerAdmissionInvariantViolation, result.DiagnosticCodes);
		Assert.Empty(result.Summary.Results);
		Assert.False(manager.HasActiveWork);
	}

	[Fact]
	public async Task PreStartCancellation_DoesNotRetainPriorInstallerSummary() {
		using TestDirectory temp = new();
		AppSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
		ModsData data = CreateData(temp);
		string priorArchivePath = temp.CreateZip(
			"prior-cancellation.zip",
			("PriorCancelMod/SubModule.xml", TestDirectory.ModuleXml("Prior.Cancel.Mod", "Prior Cancel Mod")));
		string cancelledArchivePath = temp.CreateZip(
			"cancelled-before-start.zip",
			("CancelledMod/SubModule.xml", TestDirectory.ModuleXml("Cancelled.Mod", "Cancelled Mod")));
		ModInstaller installer = new(settings);
		Assert.NotNull(await installer.InstallAsync([priorArchivePath]));
		FakeScanner scanner = new(_ => throw new InvalidOperationException("Scanner must not run."));
		FakeUnblocker unblocker = new(_ => throw new InvalidOperationException("Unblock must not run."));
		ModPipelineManager manager = new(settings, data, installer, scanner, unblocker);
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();

		ModInstallOperationResult result =
			await manager.InstallAsync([cancelledArchivePath], cancellation.Token);

		Assert.Equal(ModInstallOperationStatus.Cancelled, result.Status);
		Assert.Empty(result.Summary.Results);
		Assert.Equal(0, scanner.CallCount);
		Assert.Equal(0, unblocker.CallCount);
		Assert.False(manager.HasActiveWork);
	}

	[Fact]
	public async Task ReconciliationFailure_AfterInstalledArchiveIsPartiallyFailedAndPreservesSnapshot() {
		using TestDirectory temp = new();
		AppSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
		ModsData data = CreateData(temp);
		data.SaveCurrent([TestDirectory.Module("Cached")]);
		string archivePath = temp.CreateZip(
			"incomplete.zip",
			("IncompleteMod/SubModule.xml", TestDirectory.ModuleXml("Incomplete.Mod", "Incomplete Mod")));
		FakeScanner scanner = new(_ => Task.FromResult(new ModScanResult {
			Modules = [TestDirectory.Module("Incomplete.Mod")],
			LocalRoot = Root(ModScanRootStatus.Partial, 1),
			WorkshopRoot = Root(ModScanRootStatus.NotApplicable),
			Warnings = ["Injected incomplete scan."]
		}));
		FakeUnblocker unblocker = new(_ => Task.FromResult(new UnblockResult()));
		ModPipelineManager manager = new(settings, data, new ModInstaller(settings), scanner, unblocker);
		AcceptedModSnapshot before = manager.LoadAcceptedCache();

		ModInstallOperationResult result = await manager.InstallAsync([archivePath]);

		Assert.Equal(ModInstallOperationStatus.PartiallyFailed, result.Status);
		Assert.Equal(ModPipelineStatus.Incomplete, Assert.IsType<ModPipelineResult>(result.ReconciliationResult).Status);
		Assert.Contains(ModInstallDiagnosticCode.ReconciliationIncomplete, result.DiagnosticCodes);
		Assert.Same(before, manager.AcceptedSnapshot);
		Assert.Same(before, result.AcceptedSnapshot);
	}

	[Fact]
	public async Task ThrowingUnblocker_PreservesFailedChildAndStillReconciles() {
		using TestDirectory temp = new();
		AppSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
		ModsData data = CreateData(temp);
		string archivePath = temp.CreateZip(
			"unblock-throws.zip",
			("UnblockMod/SubModule.xml", TestDirectory.ModuleXml("Unblock.Mod", "Unblock Mod")));
		FakeScanner scanner = new(_ => Task.FromResult(CompleteResult([TestDirectory.Module("Unblock.Mod")])));
		FakeUnblocker unblocker = new(_ => throw new InvalidOperationException("Injected unblock failure."));
		ModPipelineManager manager = new(settings, data, new ModInstaller(settings), scanner, unblocker);

		ModInstallOperationResult result = await manager.InstallAsync([archivePath]);

		Assert.Equal(ModInstallOperationStatus.PartiallyFailed, result.Status);
		Assert.Contains(ModInstallDiagnosticCode.UnblockFailed, result.DiagnosticCodes);
		Assert.DoesNotContain(ModInstallDiagnosticCode.ReconciliationFailed, result.DiagnosticCodes);
		UnblockResult unblockResult = Assert.IsType<UnblockResult>(result.UnblockResult);
		Assert.False(unblockResult.Succeeded);
		Assert.Contains("Injected unblock failure", unblockResult.TechnicalDiagnostic);
		Assert.Equal(1, unblocker.CallCount);
		Assert.Equal(1, scanner.CallCount);
		Assert.True(Assert.IsType<ModPipelineResult>(result.ReconciliationResult).Success);
		Assert.Equal("Unblock.Mod", Assert.Single(result.AcceptedSnapshot.Modules).ModuleId);
	}

	[Fact]
	public async Task SuccessfulReconciliationWarnings_ElevateSuccessfulInstallToWarnings() {
		using TestDirectory temp = new();
		AppSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
		ModsData data = CreateData(temp);
		string archivePath = temp.CreateZip(
			"reconciliation-warning.zip",
			("WarningMod/SubModule.xml", TestDirectory.ModuleXml("Warning.Mod", "Warning Mod")));
		FakeScanner scanner = new(_ => Task.FromResult(new ModScanResult {
			Modules = [TestDirectory.Module("Warning.Mod")],
			LocalRoot = Root(ModScanRootStatus.Succeeded, 1),
			WorkshopRoot = Root(ModScanRootStatus.NotApplicable),
			Warnings = ["Injected successful reconciliation warning."],
			DuplicateModuleIds = ["Warning.Mod"]
		}));
		FakeUnblocker unblocker = new(_ => Task.FromResult(new UnblockResult()));
		ModPipelineManager manager = new(settings, data, new ModInstaller(settings), scanner, unblocker);

		ModInstallOperationResult result = await manager.InstallAsync([archivePath]);

		Assert.Equal(ModInstallOperationStatus.SucceededWithWarnings, result.Status);
		Assert.Contains(ModInstallDiagnosticCode.ReconciliationCompletedWithWarnings, result.DiagnosticCodes);
		ModPipelineResult reconciliation = Assert.IsType<ModPipelineResult>(result.ReconciliationResult);
		Assert.True(reconciliation.Success);
		Assert.True(reconciliation.WasCommitted);
		Assert.Contains("Injected successful reconciliation warning.", reconciliation.Warnings);
		Assert.Contains("Warning.Mod", reconciliation.DuplicateModuleIds);
	}

	[Fact]
	public async Task CancellationAfterCompletedArchive_RetainsSummaryAndFinalizesBeforeQuiescence() {
		using TestDirectory temp = new();
		AppSettings settings = CreateValidSettings(temp, GameProvider.StandAlone);
		ModsData data = CreateData(temp);
		string firstArchive = temp.CreateZip(
			"first.zip",
			("FirstMod/SubModule.xml", TestDirectory.ModuleXml("First.Mod", "First Mod")),
			("FirstMod/payload.txt", "payload"));
		string secondArchive = temp.CreateZip(
			"second.zip",
			("SecondMod/SubModule.xml", TestDirectory.ModuleXml("Second.Mod", "Second Mod")),
			("SecondMod/payload.txt", "payload"));
		ModInstaller installer = new(settings);
		TaskCompletionSource<bool> secondEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
		TaskCompletionSource<bool> releaseSecond = new(TaskCreationOptions.RunContinuationsAsynchronously);
		installer.ExtractionProgressChanged += progress => {
			if (!string.Equals(progress.ArchiveFileName, "second.zip", StringComparison.OrdinalIgnoreCase)) {
				return;
			}
			secondEntered.TrySetResult(true);
			releaseSecond.Task.GetAwaiter().GetResult();
		};
		FakeScanner scanner = new(_ => Task.FromResult(CompleteResult([TestDirectory.Module("First.Mod")])));
		FakeUnblocker unblocker = new(_ => Task.FromResult(new UnblockResult()));
		ModPipelineManager manager = new(settings, data, installer, scanner, unblocker);

		Task<ModInstallOperationResult> active = manager.InstallAsync([firstArchive, secondArchive]);
		await secondEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
		Task quiescence = manager.WaitForQuiescenceAsync();
		manager.RequestCancellation();
		releaseSecond.TrySetResult(true);

		ModInstallOperationResult result = await active.WaitAsync(TimeSpan.FromSeconds(10));
		await quiescence.WaitAsync(TimeSpan.FromSeconds(10));

		Assert.Equal(ModInstallOperationStatus.Cancelled, result.Status);
		Assert.Equal("First.Mod", Assert.Single(result.Summary.Results).ModuleId);
		Assert.Equal(1, unblocker.CallCount);
		Assert.Equal(1, scanner.CallCount);
		Assert.NotNull(result.ReconciliationResult);
		Assert.False(manager.HasActiveWork);
	}

	[Fact]
	public async Task BlseOnlyInstall_ParticipatesInSuccessWithoutModuleReconciliation() {
		using TestDirectory temp = new();
		AppSettings settings = CreateValidSettings(temp, GameProvider.Steam);
		string targetBin = temp.CreateDirectory("Game", "bin", "Win64_Shipping_Client");
		ModsData data = CreateData(temp);
		string archivePath = temp.CreateZip(
			"blse.zip",
			("bin/Win64_Shipping_Client/Bannerlord.BLSE.Standalone.exe", "fixture-exe"),
			("bin/Win64_Shipping_Client/Bannerlord.BLSE.dll", "fixture-dll"));
		FakeScanner scanner = new(_ => throw new InvalidOperationException("Module scanner must not run for BLSE-only work."));
		FakeUnblocker unblocker = new(_ => throw new InvalidOperationException("Modules unblock must not run for BLSE-only work."));
		ModPipelineManager manager = new(settings, data, new ModInstaller(settings), scanner, unblocker);

		ModInstallOperationResult result = await manager.InstallAsync([archivePath]);

		Assert.Equal(ModInstallOperationStatus.Succeeded, result.Status);
		Assert.NotNull(result.Summary.BLSEResult);
		Assert.Equal(0, scanner.CallCount);
		Assert.Equal(0, unblocker.CallCount);
		Assert.True(File.Exists(Path.Combine(targetBin, "Bannerlord.BLSE.Standalone.exe")));
	}

	private static AppSettings CreateValidSettings(TestDirectory temp, GameProvider provider) {
		string gameRoot = temp.CreateDirectory("Game");
		string modulesRoot = temp.CreateDirectory("Game", "Modules");
		temp.WriteModule(modulesRoot, "Native", "Native");
		temp.CreateDirectory("Game", "bin");
		return new AppSettings(new ConfigFileManager(temp.GetPath($"config-{Guid.NewGuid():N}.json"))) {
			GameProvider = provider,
			GameFolderPath = gameRoot
		};
	}

	private static ModsData CreateData(TestDirectory temp) =>
		new(temp.GetPath("mods_current.data"), temp.GetPath("mods_backup.data"));

	private static ModPipelineManager CreateManager(
		AppSettings settings,
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
		public Task<ModScanResult> ScanAsync(AppSettings config, CancellationToken token = default) {
			Interlocked.Increment(ref _callCount);
			return scan(token);
		}
	}

	private sealed class FakeUnblocker(Func<CancellationToken, Task<UnblockResult>> unblock) : IModuleUnblocker {
		private int _callCount;
		public int CallCount => Volatile.Read(ref _callCount);
		public Task<UnblockResult> UnblockModulesAsync(
			string modulesDirectoryPath,
			CancellationToken token = default) {
			Interlocked.Increment(ref _callCount);
			return unblock(token);
		}
	}
}
