namespace CalradiaForge.Tests.UI.Toasts;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Localization;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Models;
using CalradiaForge.Tests.Core.Support;
using CalradiaForge.UI.Toasts;

using Serilog;
using Serilog.Core;

public sealed class InstallNotificationPresenterTests {
	[Fact]
	public void Activate_IsIdempotent_AndDisposeUnsubscribesAndClosesActiveHandle() {
		using TestContext context = new();

		context.Presenter.Activate();
		context.Presenter.Activate();
		Assert.Equal(1, context.Source.ProgressAddCount);
		Assert.Equal(1, context.Source.TerminalAddCount);

		context.Source.RaiseProgress(Started(Guid.NewGuid()));
		FakeToastHandle handle = Assert.Single(context.Sink.Handles);

		context.Presenter.Dispose();
		Assert.Equal(1, context.Source.ProgressRemoveCount);
		Assert.Equal(1, context.Source.TerminalRemoveCount);
		Assert.True(handle.Closed);
	}

	[Fact]
	public void Progress_CoalescesLatestExtraction_RejectsStaleIds_AndPreservesStageTransitions() {
		using TestContext context = new();
		context.Presenter.Activate();
		Guid operationId = Guid.NewGuid();
		DateTime started = DateTime.UtcNow;
		context.Source.RaiseProgress(Started(operationId, started));
		FakeToastHandle handle = Assert.Single(context.Sink.Handles);

		context.Source.RaiseProgress(Extracting(operationId, "first.zip", 10, started.AddMilliseconds(10)));
		context.Source.RaiseProgress(Extracting(operationId, "latest.zip", 20, started.AddMilliseconds(20)));
		context.Source.RaiseProgress(new ModInstallProgress {
			OperationId = Guid.NewGuid(),
			Stage = ModInstallProgressStage.ArchiveCompleted,
			TimestampUtc = started.AddMilliseconds(21)
		});
		Assert.Empty(handle.Updates);

		context.Scheduler.RunScheduled();
		ToastProgressUpdate latest = Assert.Single(handle.Updates);
		Assert.Equal(20, latest.Value);
		Assert.Contains("latest", latest.Message, StringComparison.OrdinalIgnoreCase);

		context.Source.RaiseProgress(new ModInstallProgress {
			OperationId = operationId,
			Stage = ModInstallProgressStage.ArchiveCompleted,
			ArchiveFileName = "latest.zip",
			ArchiveIndex = 1,
			TotalArchives = 2,
			ProcessedArchiveCount = 1,
			InstalledCount = 1,
			TimestampUtc = started.AddMilliseconds(25)
		});
		Assert.Equal(2, handle.Updates.Count);
		Assert.Equal(1, handle.Updates[1].Value);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void TerminalNotification_WaitsForCoreResultAndLauncherCompletion_InEitherOrder(
		bool terminalFirst) {
		using TestContext context = new();
		context.Presenter.Activate();
		Guid operationId = Guid.NewGuid();
		context.Source.RaiseProgress(Started(operationId));
		FakeToastHandle handle = Assert.Single(context.Sink.Handles);
		ModInstallOperationResult result = Result(operationId, ModInstallOperationStatus.Succeeded);
		LauncherInstallPresentationCompletion completion = new(operationId, Succeeded: true);

		if (terminalFirst) {
			context.Source.RaiseTerminal(result);
			Assert.Empty(handle.Transitions);
			context.Presenter.ReportLauncherCompletion(completion);
		} else {
			context.Presenter.ReportLauncherCompletion(completion);
			Assert.Empty(handle.Transitions);
			context.Source.RaiseTerminal(result);
		}

		ToastRequest terminal = Assert.Single(handle.Transitions);
		Assert.Equal(ToastSeverity.Success, terminal.Severity);
		Assert.Equal("Install Complete", terminal.Title);

		context.Source.RaiseTerminal(result);
		context.Presenter.ReportLauncherCompletion(completion);
		Assert.Single(handle.Transitions);
	}

	[Theory]
	[InlineData(ModInstallOperationStatus.SucceededWithWarnings, ToastSeverity.Warning)]
	[InlineData(ModInstallOperationStatus.PartiallyFailed, ToastSeverity.Error)]
	[InlineData(ModInstallOperationStatus.Failed, ToastSeverity.Error)]
	public void AdmittedTerminalStatus_MapsToDeterministicSeverity(
		ModInstallOperationStatus status,
		ToastSeverity expectedSeverity) {
		using TestContext context = new();
		context.Presenter.Activate();
		Guid operationId = Guid.NewGuid();
		context.Source.RaiseProgress(Started(operationId));

		context.Source.RaiseTerminal(Result(operationId, status));
		Assert.Empty(context.Sink.Handles[0].Transitions);
		context.Presenter.ReportLauncherCompletion(new(operationId, Succeeded: true));

		ToastRequest terminal = Assert.Single(context.Sink.Handles[0].Transitions);
		Assert.Equal(expectedSeverity, terminal.Severity);
	}

	[Theory]
	[InlineData(ModInstallOperationStatus.RejectedBusy, ToastSeverity.Warning)]
	[InlineData(ModInstallOperationStatus.RejectedAdmissionStopped, ToastSeverity.Info)]
	[InlineData(ModInstallOperationStatus.ValidationFailed, ToastSeverity.Warning)]
	public void RejectedRequests_UseStandaloneNotifications(
		ModInstallOperationStatus status,
		ToastSeverity severity) {
		using TestContext context = new();
		context.Presenter.Activate();

		context.Source.RaiseTerminal(Result(Guid.NewGuid(), status));

		Assert.Single(context.Sink.Requests);
		Assert.Equal(severity, context.Sink.Requests[0].Severity);
		Assert.Empty(context.Sink.Handles[0].Transitions);
	}

	[Theory]
	[InlineData(
		ModInstallDiagnosticCode.ArchiveSelectionEmpty,
		"Select at least one supported mod archive")]
	[InlineData(
		ModInstallDiagnosticCode.GameDirectoryNotConfigured,
		"Set the Bannerlord game folder in Settings")]
	[InlineData(
		ModInstallDiagnosticCode.GameDirectoryMissing,
		"configured Bannerlord game folder could not be found")]
	[InlineData(
		ModInstallDiagnosticCode.ModulesDirectoryMissing,
		"Bannerlord Modules folder could not be found")]
	[InlineData(
		ModInstallDiagnosticCode.InstallerAdmissionInvariantViolation,
		"installation could not complete normally")]
	public void ValidationDiagnostics_MapToLocalizedActionableTextWithoutTechnicalLeakage(
		ModInstallDiagnosticCode code,
		string expectedMessage) {
		using TestContext context = new();
		context.Presenter.Activate();
		Guid operationId = Guid.NewGuid();
		context.Source.RaiseTerminal(new ModInstallOperationResult {
			OperationId = operationId,
			Status = ModInstallOperationStatus.ValidationFailed,
			DiagnosticCodes = [code],
			TechnicalDiagnostics = [@"C:\Users\owner\private\archive.zip"]
		});

		ToastRequest terminal = Assert.Single(context.Sink.Requests);
		Assert.Contains(expectedMessage, terminal.Message, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain(@"C:\Users\owner", terminal.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Theory]
	[InlineData(
		ModInstallDiagnosticCode.UnblockFailed,
		"Settings > Tools > Unblock DLLs")]
	[InlineData(
		ModInstallDiagnosticCode.ReconciliationCompletedWithWarnings,
		"module list was updated with warnings")]
	public void AdmittedFinalizationDiagnostics_MapToLocalizedNonTechnicalText(
		ModInstallDiagnosticCode code,
		string expectedMessage) {
		using TestContext context = new();
		context.Presenter.Activate();
		Guid operationId = Guid.NewGuid();
		context.Source.RaiseProgress(Started(operationId));
		context.Source.RaiseTerminal(new ModInstallOperationResult {
			OperationId = operationId,
			Status = ModInstallOperationStatus.SucceededWithWarnings,
			DiagnosticCodes = [code],
			TechnicalDiagnostics = ["Duplicate.Module.Id"]
		});
		context.Presenter.ReportLauncherCompletion(new(operationId, Succeeded: true));

		ToastRequest terminal = Assert.Single(context.Sink.Handles[0].Transitions);
		Assert.Contains(expectedMessage, terminal.Message, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("Duplicate.Module.Id", terminal.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void BusyRejection_DoesNotCancelActiveOperationProgress() {
		using TestContext context = new();
		context.Presenter.Activate();
		Guid operationId = Guid.NewGuid();
		DateTime started = DateTime.UtcNow;
		context.Source.RaiseProgress(Started(operationId, started));
		context.Source.RaiseProgress(Extracting(
			operationId,
			"active.zip",
			15,
			started.AddMilliseconds(10)));

		context.Source.RaiseTerminal(Result(
			Guid.NewGuid(),
			ModInstallOperationStatus.RejectedBusy));
		context.Scheduler.RunScheduled();

		ToastProgressUpdate update = Assert.Single(context.Sink.Handles[0].Updates);
		Assert.Equal(15, update.Value);
	}

	[Fact]
	public void CancelledWithChanges_UsesWarningAndLocalizedChildOutcomeFormatting() {
		using TestContext context = new();
		context.Presenter.Activate();
		Guid operationId = Guid.NewGuid();
		context.Source.RaiseProgress(Started(operationId));
		ModInstallSummary summary = new() {
			Results = [
				new ModInstallResult {
					ModuleId = "Example",
					Status = ModInstallStatus.Installed
				},
				new ModInstallResult {
					ModuleId = "BLSE",
					Status = ModInstallStatus.Failed
				}
			]
		};
		ModInstallOperationResult result = new() {
			OperationId = operationId,
			Status = ModInstallOperationStatus.Cancelled,
			Summary = summary,
			UnblockResult = new UnblockResult { FailedCount = 2 }
		};

		context.Source.RaiseTerminal(result);
		context.Presenter.ReportLauncherCompletion(new(operationId, Succeeded: true));

		ToastRequest terminal = Assert.Single(context.Sink.Handles[0].Transitions);
		Assert.Equal(ToastSeverity.Warning, terminal.Severity);
		Assert.Contains("after some changes", terminal.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("1 installed", terminal.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("BLSE installation failed", terminal.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("2 file(s)", terminal.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void CancelledWithChangesAndIncompleteReconciliation_DoesNotClaimReconciliationSucceeded() {
		using TestContext context = new();
		context.Presenter.Activate();
		Guid operationId = Guid.NewGuid();
		context.Source.RaiseProgress(Started(operationId));
		context.Source.RaiseTerminal(new ModInstallOperationResult {
			OperationId = operationId,
			Status = ModInstallOperationStatus.Cancelled,
			Summary = new ModInstallSummary {
				Results = [
					new ModInstallResult {
						ModuleId = "Example",
						Status = ModInstallStatus.Installed
					}
				]
			},
			ReconciliationResult = new ModPipelineResult {
				Operation = ModPipelineOperation.Install,
				Status = ModPipelineStatus.Incomplete,
				IsComplete = false,
				WasCommitted = false,
				UserSummary = "technical reconciliation detail",
				TechnicalDiagnostic = @"C:\Users\owner\private\Modules"
			},
			DiagnosticCodes = [ModInstallDiagnosticCode.ReconciliationIncomplete],
			TechnicalDiagnostics = [@"C:\Users\owner\private\Modules"]
		});
		context.Presenter.ReportLauncherCompletion(new(operationId, Succeeded: true));

		ToastRequest terminal = Assert.Single(context.Sink.Handles[0].Transitions);
		Assert.Equal(ToastSeverity.Warning, terminal.Severity);
		Assert.Contains(
			"cancelled after some changes completed",
			terminal.Message,
			StringComparison.OrdinalIgnoreCase);
		Assert.Contains(
			"could not be fully reconciled",
			terminal.Message,
			StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain(
			"module list was reconciled",
			terminal.Message,
			StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain(
			@"C:\Users\owner",
			terminal.Message,
			StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain(
			"technical reconciliation detail",
			terminal.Message,
			StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void LauncherReconciliationFailure_UsesLocalizedErrorWithoutSurfacingTechnicalText() {
		using TestContext context = new();
		context.Presenter.Activate();
		Guid operationId = Guid.NewGuid();
		context.Source.RaiseProgress(Started(operationId));
		context.Source.RaiseTerminal(Result(operationId, ModInstallOperationStatus.Succeeded));

		context.Presenter.ReportLauncherCompletion(new(
			operationId,
			Succeeded: false,
			ErrorMessage: "sensitive technical detail"));

		ToastRequest terminal = Assert.Single(context.Sink.Handles[0].Transitions);
		Assert.Equal(ToastSeverity.Error, terminal.Severity);
		Assert.Equal("Installation Failed", terminal.Title);
		Assert.DoesNotContain("sensitive technical detail", terminal.Message, StringComparison.Ordinal);
		Assert.Contains("could not be fully reconciled", terminal.Message, StringComparison.OrdinalIgnoreCase);
	}

	private static ModInstallProgress Started(Guid operationId, DateTime? timestamp = null) => new() {
		OperationId = operationId,
		Stage = ModInstallProgressStage.OperationStarted,
		TotalArchives = 2,
		TimestampUtc = timestamp ?? DateTime.UtcNow
	};

	private static ModInstallProgress Extracting(
		Guid operationId,
		string archiveName,
		int batchFilesProcessed,
		DateTime timestamp) => new() {
			OperationId = operationId,
			ArchiveId = Guid.NewGuid(),
			Stage = ModInstallProgressStage.Extracting,
			ArchiveFileName = archiveName,
			ArchiveIndex = 1,
			TotalArchives = 2,
			BatchFilesProcessed = batchFilesProcessed,
			EstimatedTotalFiles = 100,
			TimestampUtc = timestamp
		};

	private static ModInstallOperationResult Result(
		Guid operationId,
		ModInstallOperationStatus status) => new() {
			OperationId = operationId,
			Status = status
		};

	private sealed class TestContext : IDisposable {
		private readonly TestDirectory _directory = new();
		private readonly Logger _logger;

		public TestContext() {
			string configPath = _directory.GetPath("config.json");
			AppSettings settings = new(new ConfigFileManager(configPath));
			TranslationManager manager = new(
				_directory.RootPath,
				_directory.GetPath("languages.json"),
				_directory.GetPath("en-US.json"));
			TranslationService translator = new(manager, settings);
			_logger = new LoggerConfiguration().CreateLogger();
			Presenter = new InstallNotificationPresenter(
				Source,
				Sink,
				translator,
				_logger,
				Scheduler);
		}

		public FakeNotificationSource Source { get; } = new();
		public FakeToastSink Sink { get; } = new();
		public FakeScheduler Scheduler { get; } = new();
		public InstallNotificationPresenter Presenter { get; }

		public void Dispose() {
			Presenter.Dispose();
			_logger.Dispose();
			_directory.Dispose();
		}
	}

	private sealed class FakeNotificationSource : IModInstallOperationNotificationSource {
		private Action<ModInstallProgress>? _progress;
		private Action<ModInstallOperationResult>? _terminal;

		public int ProgressAddCount { get; private set; }
		public int ProgressRemoveCount { get; private set; }
		public int TerminalAddCount { get; private set; }
		public int TerminalRemoveCount { get; private set; }

		public event Action<ModInstallProgress>? InstallProgressChanged {
			add { ProgressAddCount++; _progress += value; }
			remove { ProgressRemoveCount++; _progress -= value; }
		}

		public event Action<ModInstallOperationResult>? InstallOperationCompleted {
			add { TerminalAddCount++; _terminal += value; }
			remove { TerminalRemoveCount++; _terminal -= value; }
		}

		public void RaiseProgress(ModInstallProgress progress) => _progress?.Invoke(progress);
		public void RaiseTerminal(ModInstallOperationResult result) => _terminal?.Invoke(result);
	}

	private sealed class FakeToastSink : IToastNotificationSink {
		public List<ToastRequest> Requests { get; } = [];
		public List<FakeToastHandle> Handles { get; } = [];

		public IToastNotificationHandle Open(ToastRequest request) {
			Requests.Add(request);
			FakeToastHandle handle = new();
			Handles.Add(handle);
			return handle;
		}
	}

	private sealed class FakeToastHandle : IToastNotificationHandle {
		public List<ToastProgressUpdate> Updates { get; } = [];
		public List<ToastRequest> Transitions { get; } = [];
		public bool Closed { get; private set; }

		public void UpdateProgress(double value, double maximum, string? message = null) =>
			Updates.Add(new ToastProgressUpdate(value, maximum, message ?? string.Empty));

		public void Transition(ToastRequest request) => Transitions.Add(request);

		public void Close() => Closed = true;

		public void Dispose() => Close();
	}

	private sealed class FakeScheduler : INotificationDelayScheduler {
		private Action? _callback;

		public void Schedule(TimeSpan delay, Action callback) => _callback = callback;

		public void Cancel() => _callback = null;

		public void RunScheduled() {
			Action? callback = _callback;
			_callback = null;
			callback?.Invoke();
		}

		public void Dispose() => _callback = null;
	}

	private sealed record ToastProgressUpdate(double Value, double Maximum, string Message);
}
