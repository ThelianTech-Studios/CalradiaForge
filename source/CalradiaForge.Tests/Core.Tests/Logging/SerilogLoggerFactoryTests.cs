namespace CalradiaForge.Tests.Core.Logging;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Logging;
using CalradiaForge.Tests.Core.Support;

using Serilog.Events;

public sealed class SerilogLoggerFactoryTests {
	[Theory]
	[InlineData(false, false)]
	[InlineData(true, true)]
	public void Create_UsesPersistedMinimumLevelForSerilogAndTransitionalLegacyLogger(
		bool debugMode,
		bool debugEnabled) {
		using TestDirectory temp = new();
		string logsDirectory = temp.CreateDirectory("Logs");
		string latest = Path.Combine(logsDirectory, "CalradiaForge_Latest.log");
		LoggingSettings settings = CreateLoggingSettings(temp, debugMode);
		SerilogLoggerFactory factory = new(
			settings,
			new LogFileLifecycle(logsDirectory, latest),
			latest);
		Logger.LogLevel originalLegacyLevel = Logger.Instance.MinimumLevel;

		Serilog.ILogger logger = factory.Create();
		try {
			Assert.Equal(debugEnabled, logger.IsEnabled(LogEventLevel.Debug));
			Assert.True(logger.IsEnabled(LogEventLevel.Information));
			Assert.Equal(
				debugMode ? Logger.LogLevel.Debug : Logger.LogLevel.Info,
				Logger.Instance.MinimumLevel);
		} finally {
			((IDisposable)logger).Dispose();
			Logger.Instance.MinimumLevel = originalLegacyLevel;
		}
	}

	[Fact]
	public void Create_RejectsASecondLoggerFromTheSameFactory() {
		using TestDirectory temp = new();
		string logsDirectory = temp.CreateDirectory("Logs");
		string latest = Path.Combine(logsDirectory, "CalradiaForge_Latest.log");
		LoggingSettings settings = CreateLoggingSettings(temp, debugMode: false);
		SerilogLoggerFactory factory = new(
			settings,
			new LogFileLifecycle(logsDirectory, latest),
			latest);
		Serilog.ILogger logger = factory.Create();

		try {
			Assert.Throws<InvalidOperationException>(() => factory.Create());
		} finally {
			((IDisposable)logger).Dispose();
		}
	}

	[Fact]
	public void Dispose_FlushesAndReleasesLatestWithoutArchivingIt() {
		using TestDirectory temp = new();
		string logsDirectory = temp.CreateDirectory("Logs");
		string latest = Path.Combine(logsDirectory, "CalradiaForge_Latest.log");
		LoggingSettings settings = CreateLoggingSettings(temp, debugMode: false);
		SerilogLoggerFactory factory = new(
			settings,
			new LogFileLifecycle(logsDirectory, latest),
			latest);
		Serilog.ILogger logger = factory.Create();

		logger.Information("Factory lifecycle {Value}", 42);
		((IDisposable)logger).Dispose();

		Assert.True(File.Exists(latest));
		Assert.Contains("Factory lifecycle 42", File.ReadAllText(latest));
		using FileStream exclusive = new(latest, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
		Assert.True(exclusive.CanWrite);
		Assert.Empty(Directory.GetFiles(logsDirectory, "CalradiaForge_????-??-??_??-??.log"));
	}

	private static LoggingSettings CreateLoggingSettings(TestDirectory temp, bool debugMode) {
		ConfigFileManager manager = new(temp.GetPath($"config-{Guid.NewGuid():N}.json"));
		manager.Load();
		LoggingSettings settings = new(manager) {
			DebugMode = debugMode
		};
		return settings;
	}
}
