namespace CalradiaForge.Tests.UI.ViewModels;

using CalradiaForge.UI.ViewModels;

using CommunityToolkit.Mvvm.Input;

public sealed class ViewModelFoundationTests {
	[Fact]
	public void ExplicitProperty_RaisesOnlyForAChangedValue() {
		ProbeViewModel viewModel = new();
		List<string?> changes = [];
		viewModel.PropertyChanged += (_, args) => changes.Add(args.PropertyName);

		viewModel.Value = 3;
		viewModel.Value = 3;

		Assert.Equal(["Value"], changes);
	}

	[Fact]
	public void RelayCommand_UsesExplicitCanExecuteInvalidation() {
		ProbeViewModel viewModel = new();
		int invalidations = 0;
		viewModel.RunCommand.CanExecuteChanged += (_, _) => invalidations++;

		Assert.False(viewModel.RunCommand.CanExecute(null));
		viewModel.CanRun = true;

		Assert.True(viewModel.RunCommand.CanExecute(null));
		Assert.Equal(1, invalidations);
		viewModel.RunCommand.Execute(null);
		Assert.Equal(1, viewModel.RunCount);
	}

	[Fact]
	public async Task AsyncRelayCommand_IsAwaitableAndPreservesException() {
		ProbeViewModel viewModel = new();
		viewModel.AsyncFailure = new InvalidOperationException("workflow failure");

		InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => viewModel.RunAsyncCommand.ExecuteAsync(null));

		Assert.Same(viewModel.AsyncFailure, exception);
	}

	[Fact]
	public async Task AsyncRelayCommand_DisablesUiReentryWhileRunning() {
		ProbeViewModel viewModel = new();

		Task first = viewModel.OverlapCommand.ExecuteAsync(null);
		await viewModel.AsyncCommandEntered.Task;

		Assert.False(viewModel.OverlapCommand.CanExecute(null));
		Assert.Equal(1, viewModel.AsyncCommandCount);
		viewModel.AsyncCommandRelease.SetResult();
		await first;
		Assert.Equal(1, viewModel.AsyncCommandCount);
		Assert.True(viewModel.OverlapCommand.CanExecute(null));
	}

	[Fact]
	public async Task InitializeAsync_ConcurrentCalls_RunOnce() {
		ProbeViewModel viewModel = new();
		Task first = viewModel.InitializeAsync();
		await viewModel.InitializeEntered.Task;
		Task second = viewModel.InitializeAsync();

		viewModel.InitializeRelease.SetResult();
		await Task.WhenAll(first, second);

		Assert.True(viewModel.IsInitialized);
		Assert.Equal(1, viewModel.InitializeCount);
	}

	[Fact]
	public async Task InitializeAsync_FailureDoesNotRecordSuccessAndCanRetry() {
		ProbeViewModel viewModel = new() {
			InitializeFailure = new InvalidOperationException("initialization failure")
		};
		viewModel.InitializeRelease.SetResult();

		await Assert.ThrowsAsync<InvalidOperationException>(
			() => viewModel.InitializeAsync());
		Assert.False(viewModel.IsInitialized);

		viewModel.InitializeFailure = null;
		await viewModel.InitializeAsync();

		Assert.True(viewModel.IsInitialized);
		Assert.Equal(2, viewModel.InitializeCount);
	}

	[Fact]
	public async Task InitializeAsync_CancellationLeavesGateRetryable() {
		ProbeViewModel viewModel = new();
		using CancellationTokenSource cancellation = new();

		Task initialization = viewModel.InitializeAsync(cancellation.Token);
		await viewModel.InitializeEntered.Task;
		cancellation.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => initialization);
		Assert.False(viewModel.IsInitialized);

		viewModel.InitializeRelease.SetResult();
		await viewModel.InitializeAsync();
		Assert.True(viewModel.IsInitialized);
		Assert.Equal(2, viewModel.InitializeCount);
	}

	[Fact]
	public async Task Activation_IsRepeatableAndOverlapIsSerialized() {
		ProbeViewModel viewModel = new();
		viewModel.InitializeRelease.SetResult();
		await viewModel.InitializeAsync();

		Task first = viewModel.ActivateAsync();
		await viewModel.ActivateEntered.Task;
		Task second = viewModel.ActivateAsync();

		Assert.Equal(1, viewModel.ActivateCount);
		viewModel.ActivateRelease.SetResult();
		await Task.WhenAll(first, second);

		Assert.True(viewModel.IsActive);
		Assert.Equal(2, viewModel.ActivateCount);
		await viewModel.DeactivateAsync();
		Assert.False(viewModel.IsActive);
		Assert.Equal(1, viewModel.DeactivateCount);
	}

	[Fact]
	public async Task Activation_CancellationLeavesGateRetryable() {
		ProbeViewModel viewModel = new();
		viewModel.InitializeRelease.SetResult();
		await viewModel.InitializeAsync();
		using CancellationTokenSource cancellation = new();

		Task activation = viewModel.ActivateAsync(cancellation.Token);
		await viewModel.ActivateEntered.Task;
		cancellation.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => activation);
		Assert.False(viewModel.IsActive);

		viewModel.ActivateRelease.SetResult();
		await viewModel.ActivateAsync();
		Assert.True(viewModel.IsActive);
		Assert.Equal(2, viewModel.ActivateCount);
	}

	[Fact]
	public void LifecycleContract_ContainsNoAsyncVoidMethods() {
		Type lifecycleType = typeof(CalradiaForge.UI.Lifecycle.IViewModelLifecycle);

		Assert.All(
			lifecycleType.GetMethods(),
			method => Assert.NotEqual(typeof(void), method.ReturnType));
	}

	private sealed class ProbeViewModel : ViewModelBase {
		private int _value;
		private bool _canRun;

		public ProbeViewModel() {
			RunCommand = new RelayCommand(
				() => RunCount++,
				() => CanRun);
			RunAsyncCommand = new AsyncRelayCommand(
				() => AsyncFailure is null
					? Task.CompletedTask
					: Task.FromException(AsyncFailure));
			OverlapCommand = new AsyncRelayCommand(async () => {
				AsyncCommandCount++;
				AsyncCommandEntered.TrySetResult();
				await AsyncCommandRelease.Task;
			});
		}

		public int Value {
			get => _value;
			set => SetProperty(ref _value, value);
		}

		public bool CanRun {
			get => _canRun;
			set {
				if (SetProperty(ref _canRun, value)) {
					RunCommand.NotifyCanExecuteChanged();
				}
			}
		}

		public RelayCommand RunCommand { get; }
		public AsyncRelayCommand RunAsyncCommand { get; }
		public AsyncRelayCommand OverlapCommand { get; }
		public int RunCount { get; private set; }
		public int AsyncCommandCount { get; private set; }
		public int InitializeCount { get; private set; }
		public int ActivateCount { get; private set; }
		public int DeactivateCount { get; private set; }
		public Exception? InitializeFailure { get; set; }
		public Exception? AsyncFailure { get; set; }
		public TaskCompletionSource InitializeEntered { get; } = new(
			TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource InitializeRelease { get; } = new(
			TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource ActivateEntered { get; } = new(
			TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource ActivateRelease { get; } = new(
			TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource AsyncCommandEntered { get; } = new(
			TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource AsyncCommandRelease { get; } = new(
			TaskCreationOptions.RunContinuationsAsynchronously);

		protected override async Task OnInitializeAsync(CancellationToken cancellationToken) {
			InitializeCount++;
			InitializeEntered.TrySetResult();
			await InitializeRelease.Task.WaitAsync(cancellationToken);
			if (InitializeFailure is not null) {
				throw InitializeFailure;
			}
		}

		protected override async Task OnActivateAsync(CancellationToken cancellationToken) {
			ActivateCount++;
			ActivateEntered.TrySetResult();
			await ActivateRelease.Task.WaitAsync(cancellationToken);
		}

		protected override Task OnDeactivateAsync(CancellationToken cancellationToken) {
			DeactivateCount++;
			return Task.CompletedTask;
		}
	}
}
