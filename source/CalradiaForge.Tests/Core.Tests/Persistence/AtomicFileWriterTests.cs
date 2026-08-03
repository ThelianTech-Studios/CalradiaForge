namespace CalradiaForge.Tests.Core.Persistence;

using CalradiaForge.Core.Infra.Persistence;
using CalradiaForge.Tests.Core.Support;

public sealed class AtomicFileWriterTests {
	[Fact]
	public async Task WriteAllText_WhenDestinationTemporarilyBlocksDelete_RetriesAndReplaces() {
		using TestDirectory temp = new();
		string destinationPath = temp.GetPath("state.json");
		File.WriteAllText(destinationPath, "old");

		FileStream destinationLock = new(
			destinationPath,
			FileMode.Open,
			FileAccess.Read,
			FileShare.ReadWrite);
		Task writeTask;
		try {
			writeTask = Task.Run(() => AtomicFileWriter.WriteAllText(destinationPath, "new"));
			await Task.Delay(60);
			Assert.False(writeTask.IsCompleted);
		} finally {
			destinationLock.Dispose();
		}

		await writeTask.WaitAsync(TimeSpan.FromSeconds(2));

		Assert.Equal("new", File.ReadAllText(destinationPath));
		Assert.Empty(Directory.GetFiles(temp.RootPath, "*.tmp"));
	}
}
