namespace CalradiaForge.Tests.UI.Lifecycle;

using CalradiaForge.Core.Infra.Localization;
using CalradiaForge.Core.Models;
using CalradiaForge.UI.Dialogs;
using CalradiaForge.UI.Lifecycle;

public sealed class ApplicationLifecycleConfirmationPolicyTests {
	[Fact]
	public void ShutdownWithoutActiveWork_SkipsDialogAndCommits() {
		RecordingWorkController work = new(ModPipelineOperation.None);
		RecordingDialogs dialogs = new();

		bool result = ApplicationLifecycleConfirmationPolicy.ConfirmShutdown(
			work,
			dialogs,
			new TranslationStrings());

		Assert.True(result);
		Assert.Equal(1, work.CaptureCount);
		Assert.Equal(0, dialogs.ShutdownCount);
		AssertNoCommitActions(work);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ShutdownWithActiveWork_UsesOneWarningResult(bool confirmationResult) {
		RecordingWorkController work = new(ModPipelineOperation.Install);
		RecordingDialogs dialogs = new() { ShutdownResult = confirmationResult };

		bool result = ApplicationLifecycleConfirmationPolicy.ConfirmShutdown(
			work,
			dialogs,
			new TranslationStrings());

		Assert.Equal(confirmationResult, result);
		Assert.Equal(1, dialogs.ShutdownCount);
		Assert.Equal("A mod installation is currently running.", dialogs.LastShutdownDetails);
		AssertNoCommitActions(work);
	}

	[Fact]
	public void RestartCancelledAtOrdinaryPrompt_DoesNotCaptureActiveWork() {
		RecordingWorkController work = new(ModPipelineOperation.Install);
		RecordingDialogs dialogs = new();
		dialogs.RestartResults.Enqueue(false);

		bool result = ApplicationLifecycleConfirmationPolicy.ConfirmRestart(
			RestartReason.DebugModeChanged,
			work,
			dialogs,
			new TranslationStrings());

		Assert.False(result);
		Assert.Equal(0, work.CaptureCount);
		Assert.Single(dialogs.RestartDetails);
		Assert.Null(dialogs.RestartDetails[0]);
		AssertNoCommitActions(work);
	}

	[Fact]
	public void RestartAcceptedWithoutActiveWork_CommitsWithoutSecondWarning() {
		RecordingWorkController work = new(ModPipelineOperation.None);
		RecordingDialogs dialogs = new();
		dialogs.RestartResults.Enqueue(true);

		bool result = ApplicationLifecycleConfirmationPolicy.ConfirmRestart(
			RestartReason.DebugModeChanged,
			work,
			dialogs,
			new TranslationStrings());

		Assert.True(result);
		Assert.Equal(1, work.CaptureCount);
		Assert.Single(dialogs.RestartDetails);
		AssertNoCommitActions(work);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void RestartWithActiveWork_UsesExactlyOneSecondWarning(bool secondResult) {
		RecordingWorkController work = new(ModPipelineOperation.RefreshScan);
		RecordingDialogs dialogs = new();
		dialogs.RestartResults.Enqueue(true);
		dialogs.RestartResults.Enqueue(secondResult);

		bool result = ApplicationLifecycleConfirmationPolicy.ConfirmRestart(
			RestartReason.DebugModeChanged,
			work,
			dialogs,
			new TranslationStrings());

		Assert.Equal(secondResult, result);
		Assert.Equal(1, work.CaptureCount);
		Assert.Equal(2, dialogs.RestartDetails.Count);
		Assert.Null(dialogs.RestartDetails[0]);
		Assert.Equal("A mod scan is currently running.", dialogs.RestartDetails[1]);
		AssertNoCommitActions(work);
	}

	[Theory]
	[InlineData(ModPipelineOperation.None, null)]
	[InlineData(ModPipelineOperation.StartupScan, "A mod scan is currently running.")]
	[InlineData(ModPipelineOperation.RefreshScan, "A mod scan is currently running.")]
	[InlineData(ModPipelineOperation.Install, "A mod installation is currently running.")]
	public void ActiveOperationSummary_UsesOnlyAuthoritativeCategories(
		ModPipelineOperation operation,
		string? expected) {
		Assert.Equal(
			expected,
			ApplicationLifecycleConfirmationPolicy.FormatActiveOperation(
				operation,
				new TranslationStrings()));
	}

	[Fact]
	public void ActiveOperationSummary_RejectsUnsupportedOperation() {
		ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
			() => ApplicationLifecycleConfirmationPolicy.FormatActiveOperation(
				(ModPipelineOperation)99,
				new TranslationStrings()));

		Assert.Equal("operation", exception.ParamName);
	}

	[Theory]
	[InlineData(true, DelayedShutdownChoice.ExitAnyway)]
	[InlineData(false, DelayedShutdownChoice.ContinueWaiting)]
	[InlineData(null, DelayedShutdownChoice.ContinueWaiting)]
	public void DelayedShutdownResult_UsesLockedMapping(
		bool? dialogResult,
		DelayedShutdownChoice expected) {
		Assert.Equal(expected, ApplicationDialogService.MapDelayedShutdownChoice(dialogResult));
	}

	private static void AssertNoCommitActions(RecordingWorkController work) {
		Assert.Equal(0, work.StopCount);
		Assert.Equal(0, work.CancelCount);
		Assert.Equal(0, work.WaitCount);
	}

	private sealed class RecordingWorkController : IApplicationWorkController {
		private readonly ModPipelineOperation _operation;

		public RecordingWorkController(ModPipelineOperation operation) {
			_operation = operation;
		}

		public int CaptureCount { get; private set; }
		public int StopCount { get; private set; }
		public int CancelCount { get; private set; }
		public int WaitCount { get; private set; }

		public ModPipelineOperation CaptureActiveOperation() {
			CaptureCount++;
			return _operation;
		}

		public void StopAcceptingNewWork() => StopCount++;
		public void RequestCancellation() => CancelCount++;
		public Task WaitForQuiescenceAsync(CancellationToken cancellationToken) {
			WaitCount++;
			return Task.CompletedTask;
		}
	}

	private sealed class RecordingDialogs : IApplicationDialogService {
		public bool ShutdownResult { get; init; } = true;
		public int ShutdownCount { get; private set; }
		public string? LastShutdownDetails { get; private set; }
		public Queue<bool> RestartResults { get; } = new();
		public List<string?> RestartDetails { get; } = [];

		public bool ConfirmShutdown(string? activeOperationDetails = null) {
			ShutdownCount++;
			LastShutdownDetails = activeOperationDetails;
			return ShutdownResult;
		}

		public bool ConfirmRestart(RestartReason reason, string? activeOperationDetails = null) {
			RestartDetails.Add(activeOperationDetails);
			return RestartResults.Dequeue();
		}

		public DelayedShutdownChoice ChooseDelayedShutdown() =>
			DelayedShutdownChoice.ContinueWaiting;
	}
}
