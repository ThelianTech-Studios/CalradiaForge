namespace CalradiaForge.Tests.Core.Launch;

using System.Diagnostics;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Launch;
using CalradiaForge.Core.Infra.Paths;
using CalradiaForge.Tests.Core.Support;
using CalradiaForge.Tests.UI.Support;

using Serilog;
using Serilog.Core;
using Serilog.Events;

[Collection(GlobalSerilogCollection.Name)]
public sealed class GameLauncherTests {
	[Fact]
	public async Task LaunchAsync_WhenSteamIsInitiallyRunning_LaunchesGameWithoutStartingOrDelayingSteam() {
		using LaunchFixture fixture = new(GameProvider.Steam, SteamProcessStatus.Running);

		GameLaunchResult result = await fixture.Launcher.LaunchAsync([]);

		Assert.True(result.Success);
		Assert.Single(fixture.Starter.Starts);
		Assert.Equal(fixture.GameExecutable, fixture.Starter.Starts[0].FileName);
		Assert.Equal("261550", fixture.Starter.Starts[0].Environment["SteamAppId"]);
		Assert.Empty(fixture.Delay.Delays);
		Assert.Equal(1, fixture.Inspector.InspectionCount);
	}

	[Fact]
	public async Task LaunchAsync_WhenSteamStartsDuringPolling_WaitsForInitializationThenLaunchesGame() {
		using LaunchFixture fixture = new(
			GameProvider.Steam,
			SteamProcessStatus.NotRunning,
			SteamProcessStatus.Running);

		GameLaunchResult result = await fixture.Launcher.LaunchAsync([]);

		Assert.True(result.Success);
		Assert.Collection(
			fixture.Starter.Starts,
			start => Assert.Equal("steam://open/main", start.FileName),
			start => Assert.Equal(fixture.GameExecutable, start.FileName));
		Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(8)], fixture.Delay.Delays);
	}

	[Fact]
	public async Task LaunchAsync_WhenInitialInspectionIsUnknown_StartsSteamAndRequiresLaterPositiveDetection() {
		using LaunchFixture fixture = new(
			GameProvider.Steam,
			SteamProcessStatus.Unknown,
			SteamProcessStatus.Running);

		GameLaunchResult result = await fixture.Launcher.LaunchAsync([]);

		Assert.True(result.Success);
		Assert.Equal("steam://open/main", fixture.Starter.Starts[0].FileName);
		Assert.Equal(fixture.GameExecutable, fixture.Starter.Starts[1].FileName);
		Assert.Contains(TimeSpan.FromSeconds(8), fixture.Delay.Delays);
	}

	[Fact]
	public async Task LaunchAsync_WhenSteamRemainsNotRunning_TimesOutWithoutLaunchingGame() {
		using LaunchFixture fixture = new(GameProvider.Steam, SteamProcessStatus.NotRunning);

		GameLaunchResult result = await fixture.Launcher.LaunchAsync([]);

		Assert.False(result.Success);
		Assert.Contains("did not start in time", result.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Single(fixture.Starter.Starts);
		Assert.Equal("steam://open/main", fixture.Starter.Starts[0].FileName);
		Assert.Equal(60, fixture.Delay.Delays.Count);
		Assert.DoesNotContain(TimeSpan.FromSeconds(8), fixture.Delay.Delays);
	}

	[Fact]
	public async Task LaunchAsync_WhenSteamInspectionRemainsUnknown_UsesVerificationFailureAndBlocksGame() {
		using LaunchFixture fixture = new(GameProvider.Steam, SteamProcessStatus.Unknown);

		GameLaunchResult result = await fixture.Launcher.LaunchAsync([]);

		Assert.False(result.Success);
		Assert.Contains("could not verify", result.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Single(fixture.Starter.Starts);
		Assert.DoesNotContain(fixture.Starter.Starts, start => start.FileName == fixture.GameExecutable);
	}

	[Fact]
	public async Task LaunchAsync_WhenSteamIsNotConfirmed_BlocksBlseLaunch() {
		using LaunchFixture fixture = new(GameProvider.Steam, SteamProcessStatus.NotRunning);

		GameLaunchResult result = await fixture.Launcher.LaunchAsync([], LaunchTarget.BLSE);

		Assert.False(result.Success);
		Assert.DoesNotContain(fixture.Starter.Starts, start => start.FileName == fixture.BlseExecutable);
	}

	[Fact]
	public async Task LaunchAsync_WhenInspectionsMixUnknownAndNotRunning_NeverTreatsEitherAsSuccess() {
		using LaunchFixture fixture = new(
			GameProvider.Steam,
			SteamProcessStatus.Unknown,
			SteamProcessStatus.NotRunning);

		GameLaunchResult result = await fixture.Launcher.LaunchAsync([]);

		Assert.False(result.Success);
		Assert.Contains("could not verify", result.Message, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain(fixture.Starter.Starts, start => start.FileName == fixture.GameExecutable);
		Assert.DoesNotContain(TimeSpan.FromSeconds(8), fixture.Delay.Delays);
	}

	[Theory]
	[InlineData(GameProvider.StandAlone)]
	[InlineData(GameProvider.GOG)]
	public async Task LaunchAsync_WhenProviderDoesNotRequireSteam_PreservesDirectLaunch(GameProvider provider) {
		using LaunchFixture fixture = new(provider, SteamProcessStatus.Unknown);

		GameLaunchResult result = await fixture.Launcher.LaunchAsync([]);

		Assert.True(result.Success);
		Assert.Single(fixture.Starter.Starts);
		Assert.Equal(fixture.GameExecutable, fixture.Starter.Starts[0].FileName);
		Assert.False(fixture.Starter.Starts[0].Environment.ContainsKey("SteamAppId"));
		Assert.Equal(0, fixture.Inspector.InspectionCount);
		Assert.Empty(fixture.Delay.Delays);
	}

	[Fact]
	public async Task LaunchAsync_WhenSteamProtocolStartFails_ReturnsAccurateFailureAndDoesNotLaunchGame() {
		using LaunchFixture fixture = new(GameProvider.Steam, SteamProcessStatus.NotRunning);
		fixture.Starter.Failure = new InvalidOperationException("Synthetic start failure.");

		GameLaunchResult result = await fixture.Launcher.LaunchAsync([]);

		Assert.False(result.Success);
		Assert.Contains("Failed to start Steam", result.Message, StringComparison.Ordinal);
		Assert.Single(fixture.Starter.Starts);
		Assert.Equal("steam://open/main", fixture.Starter.Starts[0].FileName);
		Assert.Empty(fixture.Delay.Delays);
	}

	[Fact]
	public void Inspect_WhenEnumerationThrows_ReturnsUnknownAndLogsWarning() {
		List<LogEvent> events = [];
		Serilog.ILogger previous = Log.Logger;
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.WriteTo.Sink(new RecordingSink(events))
			.CreateLogger();
		try {
			SteamProcessInspector inspector = new(_ => throw new InvalidOperationException("Synthetic inspection failure."));

			SteamProcessStatus status = inspector.Inspect();

			Assert.Equal(SteamProcessStatus.Unknown, status);
			LogEvent warning = Assert.Single(events, item => item.Level == LogEventLevel.Warning);
			Assert.Contains("state is unknown", warning.RenderMessage(), StringComparison.OrdinalIgnoreCase);
			Assert.IsType<InvalidOperationException>(warning.Exception);
		} finally {
			((IDisposable)Log.Logger).Dispose();
			Log.Logger = previous;
		}
	}

	[Fact]
	public void Inspect_DisposesEveryReturnedProcess_WhenOneDisposalFails() {
		TrackingProcess first = new(throwOnDispose: true);
		TrackingProcess second = new(throwOnDispose: false);
		SteamProcessInspector inspector = new(_ => [first, second]);

		SteamProcessStatus status = inspector.Inspect();

		Assert.Equal(SteamProcessStatus.Unknown, status);
		Assert.True(first.DisposeCalled);
		Assert.True(second.DisposeCalled);
	}

	private sealed class LaunchFixture : IDisposable {
		private readonly TestDirectory _directory = new();

		public LaunchFixture(GameProvider provider, params SteamProcessStatus[] statuses) {
			string gameFolder = _directory.CreateDirectory("Game");
			GameExecutable = _directory.GetPath("Game", "Bannerlord.exe");
			BlseExecutable = _directory.GetPath("Game", "Bannerlord.BLSE.Standalone.exe");
			File.WriteAllText(GameExecutable, string.Empty);
			File.WriteAllText(BlseExecutable, string.Empty);
			ConfigFileManager manager = new(_directory.GetPath("config.json"));
			manager.Load();
			AppSettings settings = new(manager) {
				GameProvider = provider,
				GameFolderPath = gameFolder,
				GameLauncherFilePath = GameExecutable,
				BLSEExePath = BlseExecutable
			};
			Inspector = new SequenceInspector(statuses);
			Starter = new RecordingStarter();
			Delay = new RecordingDelay();
			Launcher = new GameLauncher(settings, Inspector, Starter, Delay);
		}

		public string GameExecutable { get; }
		public string BlseExecutable { get; }
		public SequenceInspector Inspector { get; }
		public RecordingStarter Starter { get; }
		public RecordingDelay Delay { get; }
		public GameLauncher Launcher { get; }

		public void Dispose() => _directory.Dispose();
	}

	private sealed class SequenceInspector(params SteamProcessStatus[] statuses) : ISteamProcessInspector {
		private readonly SteamProcessStatus[] _statuses = statuses.Length > 0
			? statuses
			: throw new ArgumentException("At least one status is required.", nameof(statuses));
		private int _index;

		public int InspectionCount { get; private set; }

		public SteamProcessStatus Inspect() {
			InspectionCount++;
			int index = Math.Min(_index, _statuses.Length - 1);
			_index++;
			return _statuses[index];
		}
	}

	private sealed class RecordingStarter : ILaunchProcessStarter {
		public List<ProcessStartInfo> Starts { get; } = [];
		public Exception? Failure { get; set; }

		public void Start(ProcessStartInfo startInfo) {
			Starts.Add(startInfo);
			if (Failure is not null) {
				throw Failure;
			}
		}
	}

	private sealed class RecordingDelay : ILaunchDelay {
		public List<TimeSpan> Delays { get; } = [];

		public Task DelayAsync(TimeSpan delay) {
			Delays.Add(delay);
			return Task.CompletedTask;
		}
	}

	private sealed class TrackingProcess(bool throwOnDispose) : Process {
		public bool DisposeCalled { get; private set; }

		protected override void Dispose(bool disposing) {
			DisposeCalled = true;
			if (throwOnDispose) {
				throw new InvalidOperationException("Synthetic disposal failure.");
			}
			base.Dispose(disposing);
		}
	}

	private sealed class RecordingSink(List<LogEvent> events) : ILogEventSink {
		public void Emit(LogEvent logEvent) => events.Add(logEvent);
	}
}
