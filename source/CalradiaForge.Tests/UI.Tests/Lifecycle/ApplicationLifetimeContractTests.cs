namespace CalradiaForge.Tests.UI.Lifecycle;

using CalradiaForge.UI.Lifecycle;

public sealed class ApplicationLifetimeContractTests {
	[Fact]
	public void LifecycleReasons_ContainTheLockedPhase6BValues() {
		Assert.Contains(ShutdownReason.UserRequest, Enum.GetValues<ShutdownReason>());
		Assert.Contains(ShutdownReason.FatalStartup, Enum.GetValues<ShutdownReason>());
		Assert.Contains(ShutdownReason.FatalDispatcher, Enum.GetValues<ShutdownReason>());
		Assert.Contains(ShutdownReason.OperatingSystem, Enum.GetValues<ShutdownReason>());
		Assert.Contains(RestartReason.DebugModeChanged, Enum.GetValues<RestartReason>());
		Assert.Contains(RestartReason.UserRequest, Enum.GetValues<RestartReason>());
	}

	[Fact]
	public async Task ExitFinalizer_DisposalFailureStillShutsDownWithoutStartingReplacement() {
		List<string> calls = [];
		List<Exception> failures = [];

		await ApplicationExitFinalizer.CompleteAsync(
			() => {
				calls.Add("dispose");
				throw new IOException("dispose failed");
			},
			() => calls.Add("restart"),
			() => calls.Add("shutdown"),
			failures.Add);

		Assert.Equal(["dispose", "shutdown"], calls);
		Assert.Single(failures);
	}

	[Fact]
	public async Task ExitFinalizer_ReplacementFailureStillShutsDownAfterDisposal() {
		List<string> calls = [];
		List<Exception> failures = [];

		await ApplicationExitFinalizer.CompleteAsync(
			() => {
				calls.Add("dispose");
				return Task.CompletedTask;
			},
			() => {
				calls.Add("restart");
				throw new InvalidOperationException("restart failed");
			},
			() => calls.Add("shutdown"),
			failures.Add);

		Assert.Equal(["dispose", "restart", "shutdown"], calls);
		Assert.Single(failures);
	}
}
