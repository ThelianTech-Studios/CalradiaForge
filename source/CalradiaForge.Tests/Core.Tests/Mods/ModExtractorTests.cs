namespace CalradiaForge.Tests.Core.Mods;

using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Infra.Paths;
using CalradiaForge.Core.Models;
using CalradiaForge.Tests.Core.Support;

public sealed class ModExtractorTests {
	[Fact]
	public void FindModRoot_FollowsSingleDirectoryWrappers() {
		using TestDirectory temp = new();
		string expectedRoot = temp.CreateDirectory("WrapperA", "WrapperB", "ExampleMod");
		File.WriteAllText(Path.Combine(expectedRoot, "SubModule.xml"), TestDirectory.ModuleXml("Example", "Example"));

		Assert.Equal(expectedRoot, ModExtractor.FindModRoot(temp.RootPath));
	}

	[Fact]
	public void CleanupTempDirectory_RefusesUnmanagedDirectory() {
		using TestDirectory temp = new();
		string unmanaged = temp.CreateDirectory("DoNotDelete");
		File.WriteAllText(Path.Combine(unmanaged, "sentinel.txt"), "keep");

		ModExtractor.CleanupTempDirectory(unmanaged);

		Assert.True(File.Exists(Path.Combine(unmanaged, "sentinel.txt")));
	}

	[Fact]
	public async Task ExtractToTempResultAsync_ExtractsContainedEntriesAndSupportsManagedCleanup() {
		using TestDirectory temp = new();
		string archivePath = temp.CreateZip(
			"safe.zip",
			("ExampleMod/SubModule.xml", TestDirectory.ModuleXml("Example", "Example")),
			("ExampleMod/bin/payload.txt", "payload"));

		ArchiveExtractionResult result = await ModExtractor.ExtractToTempResultAsync(archivePath);

		Assert.True(result.Success, result.Message);
		Assert.NotNull(result.TempDirectory);
		Assert.True(File.Exists(Path.Combine(result.TempDirectory!, "ExampleMod", "bin", "payload.txt")));
		ModExtractor.CleanupTempDirectory(result.TempDirectory!);
		Assert.False(Directory.Exists(result.TempDirectory));
	}

	[Fact]
	public async Task ExtractToTempResultAsync_BlocksParentTraversalBeforeWritingOutsideDestination() {
		using TestDirectory temp = new();
		string outsideFileName = $"outside-{Guid.NewGuid():N}.txt";
		string outsidePath = Path.Combine(AppPaths.ExtractionDirectory, outsideFileName);
		string archivePath = temp.CreateZip("unsafe.zip", ($"../{outsideFileName}", "blocked"));

		ArchiveExtractionResult result = await ModExtractor.ExtractToTempResultAsync(archivePath);

		Assert.False(result.Success);
		Assert.Contains("Unsafe archive blocked", result.Message, StringComparison.OrdinalIgnoreCase);
		Assert.False(File.Exists(outsidePath));
	}
}
