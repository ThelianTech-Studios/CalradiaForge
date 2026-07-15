namespace CalradiaForge.Tests.Core.Mods;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Infra.Paths;
using CalradiaForge.Core.Models;
using CalradiaForge.Tests.Core.Support;

public sealed class ModScannerTests {
	[Fact]
	public async Task ScanForModsAsync_UsesConfiguredWorkshopRootAcrossFakeSteamLibraries() {
		using TestDirectory temp = new();
		string steamClientRoot = temp.CreateDirectory("SteamClient");
		string bannerlordLibraryRoot = temp.CreateDirectory("LibraryB", "steamapps", "common", "Mount & Blade II Bannerlord");
		string modulesPath = Directory.CreateDirectory(Path.Combine(bannerlordLibraryRoot, "Modules")).FullName;
		string unusedWorkshopRoot = temp.CreateDirectory("SteamClient", "steamapps", "workshop", "content", "261550");
		string selectedWorkshopRoot = temp.CreateDirectory("LibraryB", "steamapps", "workshop", "content", "261550");
		temp.WriteModule(modulesPath, "LocalMod", "Local.Mod");
		temp.WriteModule(unusedWorkshopRoot, "UnusedMod", "Unused.Mod");
		temp.WriteModule(selectedWorkshopRoot, "WorkshopMod", "Workshop.Mod");
		AppConfigSettings settings = CreateSettings(temp, bannerlordLibraryRoot, selectedWorkshopRoot, GameProvider.Steam);

		List<ModuleModel> modules = await ModScanner.ScanForModsAsync(settings);

		Assert.Contains(modules, module => module.ModuleId == "Local.Mod");
		Assert.Contains(modules, module => module.ModuleId == "Workshop.Mod");
		Assert.DoesNotContain(modules, module => module.ModuleId == "Unused.Mod");
		Assert.NotEqual(steamClientRoot, bannerlordLibraryRoot);
	}

	[Fact]
	public async Task ScanForModsAsync_SkipsInvalidAndMultiplayerOnlyWorkshopEntries() {
		using TestDirectory temp = new();
		string gameRoot = temp.CreateDirectory("Game");
		Directory.CreateDirectory(Path.Combine(gameRoot, "Modules"));
		string workshopRoot = temp.CreateDirectory("Workshop");
		temp.WriteModule(workshopRoot, "Valid", "Valid.Workshop");
		temp.WriteModule(workshopRoot, "Multiplayer", "Mp.Workshop", isSinglePlayer: false);
		temp.CreateDirectory("Workshop", "Invalid");
		AppConfigSettings settings = CreateSettings(temp, gameRoot, workshopRoot, GameProvider.Steam);

		List<ModuleModel> modules = await ModScanner.ScanForModsAsync(settings);

		Assert.Equal("Valid.Workshop", Assert.Single(modules).ModuleId);
	}

	[Fact]
	public async Task ScanForModsAsync_WhenWorkshopOverrideIsMissing_ReturnsLocalModulesOnly() {
		using TestDirectory temp = new();
		string gameRoot = temp.CreateDirectory("Game");
		string modulesPath = temp.CreateDirectory("Game", "Modules");
		temp.WriteModule(modulesPath, "Local", "Local.Only");
		AppConfigSettings settings = CreateSettings(
			temp,
			gameRoot,
			temp.GetPath("MissingWorkshop"),
			GameProvider.Steam);

		List<ModuleModel> modules = await ModScanner.ScanForModsAsync(settings);

		Assert.Equal("Local.Only", Assert.Single(modules).ModuleId);
	}

	private static AppConfigSettings CreateSettings(
		TestDirectory temp,
		string gameRoot,
		string workshopRoot,
		GameProvider provider) {
		AppConfig config = new(temp.GetPath($"config-{Guid.NewGuid():N}.json"));
		AppConfigSettings settings = new(config) {
			GameFolderPath = gameRoot,
			GameProvider = provider,
			SteamWorkshopFolderPath = workshopRoot
		};
		return settings;
	}
}
