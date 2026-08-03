namespace CalradiaForge.Tests.Core.Logging;

using CalradiaForge.Core.Infra.Logging;
using CalradiaForge.Tests.Core.Support;

public sealed class EmergencyStartupLogWriterTests {
	[Fact]
	public void TryWrite_AppendsExceptionDetailsToFirstWritableDirectory() {
		using TestDirectory temp = new();
		string primary = temp.GetPath("Logs");

		bool written = EmergencyStartupLogWriter.TryWrite(
			new InvalidOperationException("bootstrap failed"),
			"Provider construction failed.",
			[primary]);

		Assert.True(written);
		string startupLog = Path.Combine(primary, "CalradiaForge_StartupFailure.log");
		Assert.True(File.Exists(startupLog));
		string contents = File.ReadAllText(startupLog);
		Assert.Contains("Provider construction failed.", contents);
		Assert.Contains("InvalidOperationException", contents);
		Assert.Contains("bootstrap failed", contents);
	}

	[Fact]
	public void TryWrite_UsesFallbackWhenPrimaryDirectoryCannotBeCreated() {
		using TestDirectory temp = new();
		string blockedPrimary = temp.GetPath("blocked");
		File.WriteAllText(blockedPrimary, "not a directory");
		string fallback = temp.GetPath("Fallback");

		bool written = EmergencyStartupLogWriter.TryWrite(
			new IOException("sink unavailable"),
			"Logger construction failed.",
			[blockedPrimary, fallback]);

		Assert.True(written);
		Assert.True(File.Exists(Path.Combine(fallback, "CalradiaForge_StartupFailure.log")));
	}

	[Fact]
	public void TryWrite_ReturnsFalseWithoutThrowingWhenEveryCandidateFails() {
		using TestDirectory temp = new();
		string first = temp.GetPath("first");
		string second = temp.GetPath("second");
		File.WriteAllText(first, "not a directory");
		File.WriteAllText(second, "not a directory");
		List<string> debuggerMessages = [];

		bool written = EmergencyStartupLogWriter.TryWrite(
			new UnauthorizedAccessException("denied"),
			"Fatal startup failure.",
			[first, second],
			debuggerMessages.Add);

		Assert.False(written);
		Assert.NotEmpty(debuggerMessages);
	}
}
