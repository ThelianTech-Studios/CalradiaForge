namespace CalradiaForge.Tests.UI.Lifecycle;

using CalradiaForge.Core.Infra.GamePlatform;
using CalradiaForge.Core.Models;
using CalradiaForge.UI.Lifecycle;

using Serilog;

public sealed class ApplicationShutdownCoordinatorTests {
	[Fact]
	public async Task BeginWaitAndComplete_AreIdempotent() {
		RecordingWorkController work = new();
		RecordingPersistence persistence = new();
		Serilog.ILogger logger = new LoggerConfiguration().CreateLogger();
		StartupNotificationDrainCoordinator drain = CreateDrain(logger);
		ApplicationShutdownCoordinator coordinator = new(work, drain, persistence, logger);

		coordinator.BeginShutdown();
		coordinator.BeginShutdown();
		bool quiescent = await coordinator.WaitForQuiescenceAsync(TimeSpan.FromSeconds(1));
		IReadOnlyList<Exception> firstFailures = await coordinator.CompleteShutdownAsync();
		IReadOnlyList<Exception> secondFailures = await coordinator.CompleteShutdownAsync();

		Assert.True(quiescent);
		Assert.Empty(firstFailures);
		Assert.Empty(secondFailures);
		Assert.Equal(1, work.StopCount);
		Assert.Equal(1, work.CancelCount);
		Assert.Equal(1, work.WaitCount);
		Assert.Equal(1, persistence.PersistCount);
	}

	[Fact]
	public async Task WaitForQuiescenceAsync_ReturnsFalseWhenIntervalExpires() {
		RecordingWorkController work = new(blockUntilCancelled: true);
		Serilog.ILogger logger = new LoggerConfiguration().CreateLogger();
		ApplicationShutdownCoordinator coordinator = new(
			work,
			CreateDrain(logger),
			new RecordingPersistence(),
			logger);

		bool quiescent = await coordinator.WaitForQuiescenceAsync(TimeSpan.FromMilliseconds(25));

		Assert.False(quiescent);
		Assert.Equal(1, work.StopCount);
		Assert.Equal(1, work.CancelCount);
		Assert.Equal(1, work.WaitCount);
	}

	[Fact]
	public async Task CompleteOperatingSystemShutdown_UsesSynchronousBestEffortWithoutWaiting() {
		RecordingWorkController work = new(blockUntilCancelled: true);
		RecordingPersistence persistence = new();
		Serilog.ILogger logger = new LoggerConfiguration().CreateLogger();
		StartupNotificationDrainCoordinator drain = CreateDrain(logger);
		await drain.StartAsync();
		ApplicationShutdownCoordinator coordinator = new(work, drain, persistence, logger);

		IReadOnlyList<Exception> failures = coordinator.CompleteOperatingSystemShutdown();
		await drain.StopAsync();

		Assert.Empty(failures);
		Assert.Equal(1, work.StopCount);
		Assert.Equal(1, work.CancelCount);
		Assert.Equal(0, work.WaitCount);
		Assert.Equal(1, persistence.PersistCount);
	}

	[Fact]
	public void CompleteOperatingSystemShutdown_CollectsAdmissionAndCancellationFailuresThenPersists() {
		ThrowingWorkController work = new();
		RecordingPersistence persistence = new();
		Serilog.ILogger logger = new LoggerConfiguration().CreateLogger();
		ApplicationShutdownCoordinator coordinator = new(
			work,
			CreateDrain(logger),
			persistence,
			logger);

		IReadOnlyList<Exception> failures = coordinator.CompleteOperatingSystemShutdown();

		Assert.Equal(2, failures.Count);
		Assert.Equal(1, work.StopCount);
		Assert.Equal(1, work.CancelCount);
		Assert.Equal(1, persistence.PersistCount);
	}

	private static StartupNotificationDrainCoordinator CreateDrain(Serilog.ILogger logger) {
		return new StartupNotificationDrainCoordinator(
			new StartupNotificationQueue(),
			new NoOpPresenter(),
			logger);
	}

	private sealed class RecordingWorkController : IApplicationWorkController {
		private readonly bool _blockUntilCancelled;

		public RecordingWorkController(bool blockUntilCancelled = false) {
			_blockUntilCancelled = blockUntilCancelled;
		}

		public int StopCount { get; private set; }
		public int CancelCount { get; private set; }
		public int WaitCount { get; private set; }

		public ModPipelineOperation CaptureActiveOperation() => ModPipelineOperation.None;

		public void StopAcceptingNewWork() => StopCount++;

		public void RequestCancellation() => CancelCount++;

		public async Task WaitForQuiescenceAsync(CancellationToken cancellationToken) {
			WaitCount++;
			if (_blockUntilCancelled) {
				await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
			}
		}
	}

	private sealed class RecordingPersistence : IApplicationStatePersistence {
		public int PersistCount { get; private set; }

		public void Persist() => PersistCount++;
	}

	private sealed class ThrowingWorkController : IApplicationWorkController {
		public int StopCount { get; private set; }
		public int CancelCount { get; private set; }

		public ModPipelineOperation CaptureActiveOperation() => ModPipelineOperation.None;

		public void StopAcceptingNewWork() {
			StopCount++;
			throw new IOException("stop failed");
		}

		public void RequestCancellation() {
			CancelCount++;
			throw new InvalidOperationException("cancel failed");
		}

		public Task WaitForQuiescenceAsync(CancellationToken cancellationToken) =>
			Task.CompletedTask;
	}

	private sealed class NoOpPresenter : IStartupNotificationPresenter {
		public Task PresentAsync(
			StartupNotification notification,
			CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
