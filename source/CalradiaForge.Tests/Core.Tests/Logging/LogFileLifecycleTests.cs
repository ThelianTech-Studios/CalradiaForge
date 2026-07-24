namespace CalradiaForge.Tests.Core.Logging;

using CalradiaForge.Core.Infra.Logging;
using CalradiaForge.Tests.Core.Support;

public sealed class LogFileLifecycleTests {
	[Fact]
	public void PrepareForStartup_MissingLatestIsNoOp() {
		using TestDirectory temp = new();
		string logsDirectory = temp.CreateDirectory("Logs");
		string latest = Path.Combine(logsDirectory, "CalradiaForge_Latest.log");

		new LogFileLifecycle(logsDirectory, latest).PrepareForStartup();

		Assert.False(File.Exists(latest));
		Assert.Empty(Directory.GetFiles(logsDirectory));
	}

	[Fact]
	public void PrepareForStartup_ArchivesLatestFromLastWriteTimeAtMinutePrecision() {
		using TestDirectory temp = new();
		string logsDirectory = temp.CreateDirectory("Logs");
		string latest = Path.Combine(logsDirectory, "CalradiaForge_Latest.log");
		File.WriteAllText(latest, "previous session");
		DateTime timestamp = new(2026, 7, 23, 14, 35, 48, DateTimeKind.Local);
		File.SetLastWriteTime(latest, timestamp);

		new LogFileLifecycle(logsDirectory, latest).PrepareForStartup();

		string archive = Path.Combine(logsDirectory, "CalradiaForge_2026-07-23_14-35.log");
		Assert.False(File.Exists(latest));
		Assert.Equal("previous session", File.ReadAllText(archive));
	}

	[Fact]
	public void PrepareForStartup_FallsBackToCreationTimeWhenLastWriteReadFails() {
		using TestDirectory temp = new();
		string logsDirectory = temp.CreateDirectory("Logs");
		string latest = Path.Combine(logsDirectory, "CalradiaForge_Latest.log");
		File.WriteAllText(latest, "previous session");
		DateTime creationTime = new(2026, 7, 22, 9, 8, 7, DateTimeKind.Local);
		LogFileLifecycle lifecycle = new LastWriteFailureLifecycle(
			logsDirectory,
			latest,
			creationTime);

		lifecycle.PrepareForStartup();

		Assert.True(File.Exists(Path.Combine(logsDirectory, "CalradiaForge_2026-07-22_09-08.log")));
		Assert.False(File.Exists(latest));
	}

	[Fact]
	public void PrepareForStartup_CollisionDoesNotOverwriteArchiveAndTruncatesLatest() {
		using TestDirectory temp = new();
		string logsDirectory = temp.CreateDirectory("Logs");
		string latest = Path.Combine(logsDirectory, "CalradiaForge_Latest.log");
		File.WriteAllText(latest, "unarchived previous session");
		DateTime timestamp = new(2026, 7, 23, 16, 10, 42, DateTimeKind.Local);
		File.SetLastWriteTime(latest, timestamp);
		string archive = Path.Combine(logsDirectory, "CalradiaForge_2026-07-23_16-10.log");
		File.WriteAllText(archive, "existing archive");

		new LogFileLifecycle(logsDirectory, latest).PrepareForStartup();

		Assert.Equal("existing archive", File.ReadAllText(archive));
		Assert.True(File.Exists(latest));
		Assert.Equal(0, new FileInfo(latest).Length);
	}

	[Fact]
	public void PrepareForStartup_DeletesExpiredArchivesButPreservesRecentAndLatest() {
		using TestDirectory temp = new();
		string logsDirectory = temp.CreateDirectory("Logs");
		string latest = Path.Combine(logsDirectory, "CalradiaForge_Latest.log");
		string expired = Path.Combine(logsDirectory, "CalradiaForge_2026-07-01_00-00.log");
		string recent = Path.Combine(logsDirectory, "CalradiaForge_2026-07-22_00-00.log");
		DateTime now = new(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);
		File.WriteAllText(expired, "expired");
		File.WriteAllText(recent, "recent");
		File.SetLastWriteTimeUtc(expired, now.AddDays(-8));
		File.SetLastWriteTimeUtc(recent, now.AddDays(-1));
		File.WriteAllText(latest, "latest");
		File.SetLastWriteTime(latest, new DateTime(2026, 7, 23, 12, 1, 0, DateTimeKind.Local));
		File.WriteAllText(
			Path.Combine(logsDirectory, "CalradiaForge_2026-07-23_12-01.log"),
			"collision");

		new LogFileLifecycle(logsDirectory, latest, () => now).PrepareForStartup();

		Assert.False(File.Exists(expired));
		Assert.True(File.Exists(recent));
		Assert.True(File.Exists(latest));
	}

	private sealed class LastWriteFailureLifecycle(
		string logDirectory,
		string activeLogFilePath,
		DateTime creationTime)
		: LogFileLifecycle(logDirectory, activeLogFilePath) {
		protected override DateTime GetLastWriteTime(string path) =>
			throw new IOException("Injected last-write failure.");
		protected override DateTime GetCreationTime(string path) => creationTime;
	}
}
