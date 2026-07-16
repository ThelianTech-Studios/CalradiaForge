namespace CalradiaForge.Tests.Core.GamePlatform;

using CalradiaForge.Core.Infra.GamePlatform;
using CalradiaForge.Core.Models;

public sealed class StartupNotificationQueueTests {
	[Fact]
	public async Task DrainWhenReadyAsync_BeforeReadiness_KeepsNotificationQueued() {
		StartupNotificationQueue queue = new();
		queue.Enqueue(new StartupNotification("Queued", "Waiting"));
		Task drain = queue.DrainWhenReadyAsync((_, _) => Task.CompletedTask);

		await Task.Yield();

		Assert.False(drain.IsCompleted);
		Assert.Equal(1, queue.Count);
		queue.SignalReady();
		await drain;
		Assert.Equal(0, queue.Count);
	}

	[Fact]
	public async Task DrainWhenReadyAsync_AfterReadiness_DeliversInFifoOrderAndRemovesItems() {
		StartupNotificationQueue queue = new();
		queue.Enqueue(new StartupNotification("First", "1"));
		queue.Enqueue(new StartupNotification("Second", "2"));
		List<string> delivered = [];
		queue.SignalReady();

		await queue.DrainWhenReadyAsync((notification, _) => {
			delivered.Add(notification.Title);
			return Task.CompletedTask;
		});

		Assert.Equal(["First", "Second"], delivered);
		Assert.Equal(0, queue.Count);
	}

	[Fact]
	public async Task SignalReady_WhenRepeated_RemainsReadyAndDrainsNormally() {
		StartupNotificationQueue queue = new();
		queue.Enqueue(new StartupNotification("Only", "Message"));

		queue.SignalReady();
		queue.SignalReady();
		await queue.DrainWhenReadyAsync((_, _) => Task.CompletedTask);

		Assert.True(queue.IsReady);
		Assert.Equal(0, queue.Count);
	}

	[Fact]
	public async Task DrainWhenReadyAsync_WhenCancelledBeforeReadiness_LeavesItemQueued() {
		StartupNotificationQueue queue = new();
		queue.Enqueue(new StartupNotification("Queued", "Message"));
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			queue.DrainWhenReadyAsync((_, _) => Task.CompletedTask, cancellation.Token));

		Assert.False(queue.IsReady);
		Assert.Equal(1, queue.Count);
	}

	[Fact]
	public async Task DrainWhenReadyAsync_WhenDeliveryFails_RemovesOnlyPreviouslyDeliveredItems() {
		StartupNotificationQueue queue = new();
		queue.Enqueue(new StartupNotification("First", "1"));
		queue.Enqueue(new StartupNotification("Second", "2"));
		queue.SignalReady();
		int calls = 0;

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			queue.DrainWhenReadyAsync((notification, _) => {
				calls++;
				if (notification.Title == "Second") {
					throw new InvalidOperationException("Synthetic delivery failure.");
				}
				return Task.CompletedTask;
			}));

		Assert.Equal(2, calls);
		Assert.Equal(1, queue.Count);
		List<string> retried = [];
		await queue.DrainWhenReadyAsync((notification, _) => {
			retried.Add(notification.Title);
			return Task.CompletedTask;
		});
		Assert.Equal(["Second"], retried);
		Assert.Equal(0, queue.Count);
	}
}
