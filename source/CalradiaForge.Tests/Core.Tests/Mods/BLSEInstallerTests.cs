namespace CalradiaForge.Tests.Core.Mods;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Infra.Paths;
using CalradiaForge.Tests.Core.Support;

public sealed class BLSEInstallerTests {
	[Fact]
	public void IsBLSEArchive_RequiresStandaloneExecutableMarker() {
		using TestDirectory temp = new();
		string extracted = temp.CreateDirectory("Extracted", "bin", "Win64_Shipping_Client");

		Assert.False(BLSEInstaller.IsBLSEArchive(temp.GetPath("Extracted")));
		File.WriteAllText(Path.Combine(extracted, "Bannerlord.BLSE.Standalone.exe"), "fixture");
		Assert.True(BLSEInstaller.IsBLSEArchive(temp.GetPath("Extracted")));
	}

	[Fact]
	public async Task InstallAsync_UsesPlatformBinAndUpdatesConfiguredExecutablePath() {
		using TestDirectory temp = new();
		string extractedRoot = temp.CreateDirectory("Extracted");
		string sourceBin = temp.CreateDirectory("Extracted", "bin", "Win64_Shipping_Client");
		File.WriteAllText(Path.Combine(sourceBin, "Bannerlord.BLSE.Standalone.exe"), "fixture-exe");
		File.WriteAllText(Path.Combine(sourceBin, "Bannerlord.BLSE.dll"), "fixture-dll");
		string gameRoot = temp.CreateDirectory("Game");
		string targetBin = temp.CreateDirectory("Game", "bin", "Win64_Shipping_Client");
		AppConfigSettings settings = new(new AppConfig(temp.GetPath("config.json"))) {
			GameProvider = GameProvider.Steam,
			GameFolderPath = gameRoot
		};

		BLSEInstallResult result = await BLSEInstaller.InstallAsync(extractedRoot, settings);

		Assert.True(result.Success, result.Message);
		Assert.Equal("fixture-exe", File.ReadAllText(Path.Combine(targetBin, "Bannerlord.BLSE.Standalone.exe")));
		Assert.Equal(Path.Combine(targetBin, "Bannerlord.BLSE.Standalone.exe"), settings.BLSEExePath);
	}
}
