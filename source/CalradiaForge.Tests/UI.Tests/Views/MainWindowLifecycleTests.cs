namespace CalradiaForge.Tests.UI.Views;

using CalradiaForge.UI.Lifecycle;
using CalradiaForge.UI.Views;

public sealed class MainWindowLifecycleTests {
	[Fact]
	public async Task Transition_InitializesTargetThenDeactivatesDisplaysAndActivates() {
		List<string> events = [];
		FakeLifecycle current = new("current", events, initialized: true, active: true);
		FakeLifecycle target = new("target", events);
		IViewModelLifecycle? displayed = current;
		using SemaphoreSlim gate = new(1, 1);

		await MainWindow.ExecuteNavigationLifecycleAsync(
			gate,
			() => displayed,
			target,
			() => {
				events.Add("display");
				displayed = target;
			});

		Assert.Equal(
			[
				"target.initialize",
				"current.deactivate",
				"display",
				"target.activate"
			],
			events);
		Assert.True(target.IsInitialized);
		Assert.True(target.IsActive);
		Assert.False(current.IsActive);
	}

	[Fact]
	public async Task OverlappingTransitions_AreSerializedAndUseLatestDisplayedLifecycle() {
		List<string> events = [];
		FakeLifecycle first = new("first", events, blockActivation: true);
		FakeLifecycle second = new("second", events);
		IViewModelLifecycle? displayed = null;
		using SemaphoreSlim gate = new(1, 1);

		Task firstTransition = MainWindow.ExecuteNavigationLifecycleAsync(
			gate,
			() => displayed,
			first,
			() => {
				events.Add("display.first");
				displayed = first;
			});
		await first.ActivationEntered;

		Task secondTransition = MainWindow.ExecuteNavigationLifecycleAsync(
			gate,
			() => displayed,
			second,
			() => {
				events.Add("display.second");
				displayed = second;
			});

		Assert.DoesNotContain("second.initialize", events);
		first.ReleaseActivation();
		await Task.WhenAll(firstTransition, secondTransition);

		Assert.Equal(
			[
				"first.initialize",
				"display.first",
				"first.activate",
				"second.initialize",
				"first.deactivate",
				"display.second",
				"second.activate"
			],
			events);
		Assert.Same(second, displayed);
	}

	[Fact]
	public async Task InitializationFailure_DoesNotDeactivateOrDisplayTarget() {
		List<string> events = [];
		FakeLifecycle current = new("current", events, initialized: true, active: true);
		FakeLifecycle target = new("target", events, failInitialization: true);
		IViewModelLifecycle? displayed = current;
		using SemaphoreSlim gate = new(1, 1);

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			MainWindow.ExecuteNavigationLifecycleAsync(
				gate,
				() => displayed,
				target,
				() => displayed = target));

		Assert.Same(current, displayed);
		Assert.True(current.IsActive);
		Assert.Equal(["target.initialize"], events);
	}

	[Fact]
	public async Task ActivationFailure_KeepsDisplayedTargetAndReleasesSerializationGate() {
		List<string> events = [];
		FakeLifecycle current = new("current", events, initialized: true, active: true);
		FakeLifecycle target = new("target", events, failActivation: true);
		IViewModelLifecycle? displayed = current;
		using SemaphoreSlim gate = new(1, 1);

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			MainWindow.ExecuteNavigationLifecycleAsync(
				gate,
				() => displayed,
				target,
				() => displayed = target));

		Assert.Same(target, displayed);
		Assert.False(current.IsActive);
		Assert.False(target.IsActive);
		Assert.Equal(1, gate.CurrentCount);
	}

	[Fact]
	public async Task DeactivationFailure_KeepsCurrentDisplayAndReleasesSerializationGate() {
		List<string> events = [];
		FakeLifecycle current = new(
			"current",
			events,
			initialized: true,
			active: true,
			failDeactivation: true);
		FakeLifecycle target = new("target", events);
		IViewModelLifecycle? displayed = current;
		using SemaphoreSlim gate = new(1, 1);

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			MainWindow.ExecuteNavigationLifecycleAsync(
				gate,
				() => displayed,
				target,
				() => displayed = target));

		Assert.Same(current, displayed);
		Assert.True(current.IsActive);
		Assert.False(target.IsActive);
		Assert.Equal(1, gate.CurrentCount);
		Assert.Equal(["target.initialize", "current.deactivate"], events);
	}

	[Fact]
	public async Task SameTargetNavigation_DeliberatelyDeactivatesAndReactivates() {
		List<string> events = [];
		FakeLifecycle target = new(
			"target",
			events,
			initialized: true,
			active: true);
		IViewModelLifecycle? displayed = target;
		using SemaphoreSlim gate = new(1, 1);

		await MainWindow.ExecuteNavigationLifecycleAsync(
			gate,
			() => displayed,
			target,
			() => events.Add("display"));

		Assert.Equal(
			[
				"target.initialize",
				"target.deactivate",
				"display",
				"target.activate"
			],
			events);
		Assert.True(target.IsActive);
	}

	[Theory]
	[InlineData(false, 1, 2, 1)]
	[InlineData(true, 1, 2, 2)]
	public void SelectionFailurePolicy_RestoresCommittedStateOnlyBeforeDisplay(
		bool targetDisplayed,
		int committed,
		int requested,
		int expected) {
		Assert.Equal(
			expected,
			MainWindow.SelectionAfterNavigationFailure(
				targetDisplayed,
				committed,
				requested));
	}

	[Fact]
	public void InitialReadiness_FailureThenLaterSuccessSignalsExactlyOnce() {
		int readySignals = 0;
		bool completed = false;

		MainWindow.CompleteInitialNavigation(
			ref completed,
			navigationSucceeded: false,
			() => readySignals++);
		Assert.False(completed);
		Assert.Equal(0, readySignals);

		MainWindow.CompleteInitialNavigation(
			ref completed,
			navigationSucceeded: true,
			() => readySignals++);
		MainWindow.CompleteInitialNavigation(
			ref completed,
			navigationSucceeded: true,
			() => readySignals++);

		Assert.True(completed);
		Assert.Equal(1, readySignals);
	}

	private sealed class FakeLifecycle : IViewModelLifecycle {
		private readonly string _name;
		private readonly List<string> _events;
		private readonly bool _failInitialization;
		private readonly bool _failActivation;
		private readonly bool _failDeactivation;
		private readonly TaskCompletionSource<bool>? _activationRelease;
		private readonly TaskCompletionSource<bool> _activationEntered =
			new(TaskCreationOptions.RunContinuationsAsynchronously);

		public FakeLifecycle(
			string name,
			List<string> events,
			bool initialized = false,
			bool active = false,
			bool blockActivation = false,
			bool failInitialization = false,
			bool failActivation = false,
			bool failDeactivation = false) {
			_name = name;
			_events = events;
			IsInitialized = initialized;
			IsActive = active;
			_failInitialization = failInitialization;
			_failActivation = failActivation;
			_failDeactivation = failDeactivation;
			if (blockActivation) {
				_activationRelease =
					new TaskCompletionSource<bool>(
						TaskCreationOptions.RunContinuationsAsynchronously);
			}
		}

		public bool IsInitialized { get; private set; }
		public bool IsActive { get; private set; }
		public Task ActivationEntered => _activationEntered.Task;

		public Task InitializeAsync(CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			_events.Add($"{_name}.initialize");
			if (_failInitialization) {
				throw new InvalidOperationException("Injected initialization failure.");
			}
			IsInitialized = true;
			return Task.CompletedTask;
		}

		public async Task ActivateAsync(CancellationToken cancellationToken = default) {
			_events.Add($"{_name}.activate");
			_activationEntered.TrySetResult(true);
			if (_activationRelease is not null) {
				await _activationRelease.Task.WaitAsync(cancellationToken);
			}
			if (_failActivation) {
				throw new InvalidOperationException("Injected activation failure.");
			}
			IsActive = true;
		}

		public Task DeactivateAsync(CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			_events.Add($"{_name}.deactivate");
			if (_failDeactivation) {
				throw new InvalidOperationException("Injected deactivation failure.");
			}
			IsActive = false;
			return Task.CompletedTask;
		}

		public void ReleaseActivation() => _activationRelease?.TrySetResult(true);
	}
}
