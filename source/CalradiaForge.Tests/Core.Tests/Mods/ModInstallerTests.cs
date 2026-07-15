namespace CalradiaForge.Tests.Core.Mods;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Models;
using CalradiaForge.Tests.Core.Support;

public sealed class ModInstallerTests {
	[Theory]
	[InlineData("example.zip", true)]
	[InlineData("example.RAR", true)]
	[InlineData("example.7z", true)]
	[InlineData("example.tar", false)]
	public void IsAcceptedArchive_UsesApprovedExtensionSet(string fileName, bool expected) {
		Assert.Equal(expected, ModInstaller.IsAcceptedArchive(fileName));
	}

	[Fact]
	public async Task StartInstallAsync_ValidModuleArchiveInstallsThroughAuthoritativePipeline() {
		using TestDirectory temp = new();
		string gameRoot = temp.CreateDirectory("Game");
		string modulesRoot = temp.CreateDirectory("Game", "Modules");
		string archivePath = temp.CreateZip(
			"example.zip",
			("ExampleMod/SubModule.xml", TestDirectory.ModuleXml("Example.Mod", "Example Mod")),
			("ExampleMod/bin/payload.txt", "payload"));
		ModInstaller installer = CreateInstaller(temp, gameRoot);

		ModInstallSummary summary = await RunInstallAsync(installer, archivePath);

		ModInstallResult result = Assert.Single(summary.Results);
		Assert.Equal(ModInstallStatus.Installed, result.Status);
		Assert.Equal("Example.Mod", result.ModuleId);
		Assert.True(File.Exists(Path.Combine(modulesRoot, "ExampleMod", "bin", "payload.txt")));
	}

	[Fact]
	public async Task StartInstallAsync_MultipleModulesAreBlockedBeforeDestinationWrites() {
		using TestDirectory temp = new();
		string gameRoot = temp.CreateDirectory("Game");
		string modulesRoot = temp.CreateDirectory("Game", "Modules");
		string archivePath = temp.CreateZip(
			"multiple.zip",
			("ModuleA/SubModule.xml", TestDirectory.ModuleXml("Module.A", "Module A")),
			("ModuleB/SubModule.xml", TestDirectory.ModuleXml("Module.B", "Module B")));
		ModInstaller installer = CreateInstaller(temp, gameRoot);

		ModInstallSummary summary = await RunInstallAsync(installer, archivePath);

		ModInstallResult result = Assert.Single(summary.Results);
		Assert.Equal(ModInstallStatus.Failed, result.Status);
		Assert.Contains("exactly one", result.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Empty(Directory.GetDirectories(modulesRoot));
	}

	[Fact]
	public async Task StartInstallAsync_IdentityMismatchPreservesExistingTarget() {
		using TestDirectory temp = new();
		string gameRoot = temp.CreateDirectory("Game");
		string modulesRoot = temp.CreateDirectory("Game", "Modules");
		string existingTarget = temp.WriteModule(modulesRoot, "ExampleMod", "Different.Mod");
		string sentinelPath = Path.Combine(existingTarget, "sentinel.txt");
		File.WriteAllText(sentinelPath, "preserve");
		string archivePath = temp.CreateZip(
			"mismatch.zip",
			("ExampleMod/SubModule.xml", TestDirectory.ModuleXml("Example.Mod", "Example Mod")),
			("ExampleMod/replacement.txt", "blocked"));
		ModInstaller installer = CreateInstaller(temp, gameRoot);

		ModInstallSummary summary = await RunInstallAsync(installer, archivePath);

		ModInstallResult result = Assert.Single(summary.Results);
		Assert.Equal(ModInstallStatus.Failed, result.Status);
		Assert.Contains("does not match", result.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Equal("preserve", File.ReadAllText(sentinelPath));
		Assert.False(File.Exists(Path.Combine(existingTarget, "replacement.txt")));
	}

	private static ModInstaller CreateInstaller(TestDirectory temp, string gameRoot) {
		AppConfigSettings settings = new(new AppConfig(temp.GetPath($"config-{Guid.NewGuid():N}.json"))) {
			GameFolderPath = gameRoot
		};
		return new ModInstaller(settings);
	}

	private static async Task<ModInstallSummary> RunInstallAsync(ModInstaller installer, string archivePath) {
		TaskCompletionSource<ModInstallSummary> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
		installer.InstallCompleted += summary => completion.TrySetResult(summary);
		Assert.True(installer.StartInstallAsync([archivePath]));
		return await completion.Task.WaitAsync(TimeSpan.FromSeconds(30));
	}
}
