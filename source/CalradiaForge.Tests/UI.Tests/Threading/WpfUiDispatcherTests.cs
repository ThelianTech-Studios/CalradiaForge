namespace CalradiaForge.Tests.UI.Threading;

using System.Windows.Threading;

using CalradiaForge.UI.Threading;

public sealed class WpfUiDispatcherTests {
	[Fact]
	public async Task InvokeAsync_WithAccess_CompletesInline() {
		WpfUiDispatcher dispatcher = new(Dispatcher.CurrentDispatcher);
		bool invoked = false;

		await dispatcher.InvokeAsync(() => invoked = true);

		Assert.True(dispatcher.CheckAccess());
		Assert.True(invoked);
	}

	[Fact]
	public async Task InvokeAsync_PropagatesExceptionsAndCancellation() {
		WpfUiDispatcher dispatcher = new(Dispatcher.CurrentDispatcher);
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();

		await Assert.ThrowsAsync<InvalidOperationException>(
			() => dispatcher.InvokeAsync(
				() => throw new InvalidOperationException("dispatcher failure")));
		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => dispatcher.InvokeAsync(() => { }, cancellation.Token));
	}

	[Fact]
	public async Task InvokeAsync_WithoutAccess_QueuesAndAwaitsDispatcherWork() {
		await using DispatcherThreadHost host = await DispatcherThreadHost.StartAsync();
		WpfUiDispatcher dispatcher = new(host.Dispatcher);
		int invokedThreadId = -1;

		await dispatcher.InvokeAsync(
			() => invokedThreadId = Environment.CurrentManagedThreadId);

		Assert.False(dispatcher.CheckAccess());
		Assert.Equal(host.ThreadId, invokedThreadId);
	}

	[Fact]
	public async Task InvokeAsync_QueuedWorkPropagatesExceptionAndCancellation() {
		await using DispatcherThreadHost host = await DispatcherThreadHost.StartAsync();
		WpfUiDispatcher dispatcher = new(host.Dispatcher);

		await Assert.ThrowsAsync<InvalidOperationException>(
			() => dispatcher.InvokeAsync(
				() => throw new InvalidOperationException("queued failure")));

		using ManualResetEventSlim blockerStarted = new(initialState: false);
		using ManualResetEventSlim releaseDispatcher = new(initialState: false);
		DispatcherOperation blocker = host.Dispatcher.InvokeAsync(
			() => {
				blockerStarted.Set();
				releaseDispatcher.Wait();
			});
		blockerStarted.Wait();

		using CancellationTokenSource cancellation = new();
		Task queuedInvocation = dispatcher.InvokeAsync(
			() => throw new InvalidOperationException("cancelled work ran"),
			cancellation.Token);
		cancellation.Cancel();
		releaseDispatcher.Set();
		await blocker.Task;

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => queuedInvocation);
	}

	private sealed class DispatcherThreadHost : IAsyncDisposable {
		private readonly Thread _thread;

		private DispatcherThreadHost(Thread thread, Dispatcher dispatcher) {
			_thread = thread;
			Dispatcher = dispatcher;
			ThreadId = thread.ManagedThreadId;
		}

		public Dispatcher Dispatcher { get; }
		public int ThreadId { get; }

		public static async Task<DispatcherThreadHost> StartAsync() {
			TaskCompletionSource<Dispatcher> ready = new(
				TaskCreationOptions.RunContinuationsAsynchronously);
			Thread thread = new(() => {
				Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
				ready.SetResult(dispatcher);
				Dispatcher.Run();
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();

			Dispatcher dispatcher = await ready.Task;
			return new DispatcherThreadHost(thread, dispatcher);
		}

		public async ValueTask DisposeAsync() {
			await Dispatcher.InvokeAsync(
				() => Dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal));
			_thread.Join();
		}
	}
}
