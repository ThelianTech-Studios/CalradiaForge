namespace CalradiaForge.Tests.UI.Lifecycle;

using CalradiaForge.Core.Infra.GamePlatform;
using CalradiaForge.Core.Models;
using CalradiaForge.UI.Lifecycle;

using Serilog;

public sealed class StartupNotificationDrainCoordinatorTests {
	[Fact]
	public async Task StartAsync_WaitsForReadinessThenDrainsFifo() {
		StartupNotificationQueue queue = new();
		queue.Enqueue(new StartupNotification("First", "one"));
		queue.Enqueue(new StartupNotification("Second", "two"));
		RecordingPresenter presenter = new(expectedCount: 2);
		Serilog.ILogger logger = new LoggerConfiguration().CreateLogger();
		StartupNotificationDrainCoordinator coordinator = new(queue, presenter, logger);

		await coordinator.StartAsync();
		Assert.Empty(presenter.Titles);

		coordinator.SignalReady();
		await presenter.Completed.WaitAsync(TimeSpan.FromSeconds(2));
		await coordinator.StopAsync();

		Assert.Equal(["First", "Second"], presenter.Titles);
		Assert.Equal(0, queue.Count);
	}

	[Fact]
	public async Task StopAsync_BeforeReadiness_CancelsOneTimeWait() {
		StartupNotificationQueue queue = new();
		RecordingPresenter presenter = new(expectedCount: 1);
		Serilog.ILogger logger = new LoggerConfiguration().CreateLogger();
		StartupNotificationDrainCoordinator coordinator = new(queue, presenter, logger);

		await coordinator.StartAsync();
		await coordinator.StopAsync();
		await coordinator.StopAsync();

		Assert.False(queue.IsReady);
		Assert.Empty(presenter.Titles);
	}

	private sealed class RecordingPresenter : IStartupNotificationPresenter {
		private readonly int _expectedCount;
		private readonly TaskCompletionSource _completed =
			new(TaskCreationOptions.RunContinuationsAsynchronously);

		public RecordingPresenter(int expectedCount) {
			_expectedCount = expectedCount;
		}

		public List<string> Titles { get; } = [];
		public Task Completed => _completed.Task;

		public Task PresentAsync(
			StartupNotification notification,
			CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();
			Titles.Add(notification.Title);
			if (Titles.Count >= _expectedCount) {
				_completed.TrySetResult();
			}
			return Task.CompletedTask;
		}
	}
}
