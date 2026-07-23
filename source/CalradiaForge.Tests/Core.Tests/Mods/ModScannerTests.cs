namespace CalradiaForge.Tests.Core.Mods;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Infra.Paths;
using CalradiaForge.Core.Models;
using CalradiaForge.Tests.Core.Support;

public sealed class ModScannerTests {
	[Fact]
	public async Task ScanAsync_WhenSteamWorkshopIsNotConfigured_ReportsCompleteLocalOnlyState() {
		using TestDirectory temp = new();
		string gameRoot = temp.CreateDirectory("Game");
		string modulesPath = temp.CreateDirectory("Game", "Modules");
		temp.WriteModule(modulesPath, "Local", "Local.Only");
		AppConfigSettings settings = CreateScannerSettings(temp, gameRoot, string.Empty, GameProvider.Steam);

		ModScanResult result = await new ModScanner().ScanAsync(settings);

		Assert.True(result.IsComplete);
		Assert.Equal(ModScanRootStatus.Succeeded, result.LocalRoot.Status);
		Assert.Equal(ModScanRootStatus.NotConfigured, result.WorkshopRoot.Status);
		Assert.Equal("Local.Only", Assert.Single(result.Modules).ModuleId);
	}

	[Fact]
	public async Task ScanAsync_WhenConfiguredWorkshopIsMissing_ReportsIncompleteInsteadOfZero() {
		using TestDirectory temp = new();
		string gameRoot = temp.CreateDirectory("Game");
		string modulesPath = temp.CreateDirectory("Game", "Modules");
		temp.WriteModule(modulesPath, "Local", "Local.Only");
		AppConfigSettings settings = CreateScannerSettings(
			temp, gameRoot, temp.GetPath("MissingWorkshop"), GameProvider.Steam);

		ModScanResult result = await new ModScanner().ScanAsync(settings);

		Assert.False(result.IsComplete);
		Assert.Equal(ModScanRootStatus.Missing, result.WorkshopRoot.Status);
		Assert.Equal("Local.Only", Assert.Single(result.Modules).ModuleId);
	}

	[Fact]
	public async Task ScanAsync_WhenLocalRootCannotBeEnumerated_ReportsInaccessible() {
		using TestDirectory temp = new();
		string gameRoot = temp.CreateDirectory("Game");
		File.WriteAllText(temp.GetPath("Game", "Modules"), "not a directory");
		AppConfigSettings settings = CreateScannerSettings(temp, gameRoot, string.Empty, GameProvider.StandAlone);

		ModScanResult result = await new ModScanner().ScanAsync(settings);

		Assert.False(result.IsComplete);
		Assert.Equal(ModScanRootStatus.Inaccessible, result.LocalRoot.Status);
	}

	[Fact]
	public async Task ScanAsync_WhenConfiguredWorkshopCannotBeEnumerated_ReportsInaccessible() {
		using TestDirectory temp = new();
		string gameRoot = temp.CreateDirectory("Game");
		string modulesPath = temp.CreateDirectory("Game", "Modules");
		temp.WriteModule(modulesPath, "Local", "Local.Only");
		string workshopPath = temp.GetPath("WorkshopFile");
		File.WriteAllText(workshopPath, "not a directory");
		AppConfigSettings settings = CreateScannerSettings(temp, gameRoot, workshopPath, GameProvider.Steam);

		ModScanResult result = await new ModScanner().ScanAsync(settings);

		Assert.False(result.IsComplete);
		Assert.Equal(ModScanRootStatus.Inaccessible, result.WorkshopRoot.Status);
		Assert.Equal("Local.Only", Assert.Single(result.Modules).ModuleId);
	}

	[Fact]
	public async Task ScanAsync_WhenConfiguredWorkshopIsEmpty_ReportsCompleteZeroResult() {
		using TestDirectory temp = new();
		string gameRoot = temp.CreateDirectory("Game");
		string modulesPath = temp.CreateDirectory("Game", "Modules");
		temp.WriteModule(modulesPath, "Local", "Local.Only");
		string workshopRoot = temp.CreateDirectory("Workshop");
		AppConfigSettings settings = CreateScannerSettings(temp, gameRoot, workshopRoot, GameProvider.Steam);

		ModScanResult result = await new ModScanner().ScanAsync(settings);

		Assert.True(result.IsComplete);
		Assert.Equal(ModScanRootStatus.Succeeded, result.WorkshopRoot.Status);
		Assert.Equal(0, result.WorkshopModuleCount);
		Assert.Equal("Local.Only", Assert.Single(result.Modules).ModuleId);
	}

	[Fact]
	public async Task ScanAsync_MalformedModuleMakesRootPartial() {
		using TestDirectory temp = new();
		string gameRoot = temp.CreateDirectory("Game");
		string modulesPath = temp.CreateDirectory("Game", "Modules");
		string broken = temp.CreateDirectory("Game", "Modules", "Broken");
		File.WriteAllText(Path.Combine(broken, "SubModule.xml"), "<not-module />");
		AppConfigSettings settings = CreateScannerSettings(temp, gameRoot, string.Empty, GameProvider.StandAlone);

		ModScanResult result = await new ModScanner().ScanAsync(settings);

		Assert.False(result.IsComplete);
		Assert.Equal(ModScanRootStatus.Partial, result.LocalRoot.Status);
		Assert.Contains(result.Warnings, warning => warning.Contains("Failed to parse", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task ScanAsync_DuplicateIdsPreferStableLocalEntryAndReportDiagnostic() {
		using TestDirectory temp = new();
		string gameRoot = temp.CreateDirectory("Game");
		string modulesPath = temp.CreateDirectory("Game", "Modules");
		string localPath = temp.WriteModule(modulesPath, "A-Local", "Duplicate.Id", version: "v1.0.0");
		string workshopRoot = temp.CreateDirectory("Workshop");
		temp.WriteModule(workshopRoot, "Z-Workshop", "duplicate.id", version: "v9.0.0");
		AppConfigSettings settings = CreateScannerSettings(temp, gameRoot, workshopRoot, GameProvider.Steam);

		ModScanResult result = await new ModScanner().ScanAsync(settings);

		ModuleModel accepted = Assert.Single(result.Modules);
		Assert.True(result.IsComplete);
		Assert.Equal(localPath, accepted.InstallPath);
		Assert.Equal("v1.0.0", accepted.ModuleVersion);
		Assert.Equal("Duplicate.Id", Assert.Single(result.DuplicateModuleIds));
	}

	[Fact]
	public async Task ScanForModsAsync_WhenSteamAndBannerlordUseDifferentSimulatedDrives_FindsAllElevenModules() {
		using TestDirectory temp = new();
		SplitDriveFixture fixture = CreateSplitDriveFixture(temp);
		AppConfigSettings settings = CreateScannerSettings(temp, fixture.GameRoot, fixture.WorkshopRoot, GameProvider.Steam);

		List<ModuleModel> modules = await ModScanner.ScanForModsAsync(settings);

		Assert.Equal(11, modules.Count);
		Assert.Equal(11, modules.Select(module => module.ModuleId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
		Assert.Equal(
			fixture.LocalModuleIds.Concat(fixture.WorkshopModuleIds).Order(),
			modules.Select(module => module.ModuleId).Order());
		Assert.Equal(8, modules.Count(module => module.InstallPath?.StartsWith(fixture.ModulesRoot, StringComparison.OrdinalIgnoreCase) == true));
		Assert.Equal(3, modules.Count(module => module.InstallPath?.StartsWith(fixture.WorkshopRoot, StringComparison.OrdinalIgnoreCase) == true));
		Assert.StartsWith(Path.Combine("C", "programs", "steam"), Path.GetRelativePath(temp.RootPath, fixture.SteamRoot));
		Assert.StartsWith(Path.Combine("D", "games", "Bannerlord"), Path.GetRelativePath(temp.RootPath, fixture.GameRoot));
	}

	[Fact]
	public async Task ScanForModsAsync_WhenProviderIsNotSteam_SkipsConfiguredWorkshopRoot() {
		using TestDirectory temp = new();
		SplitDriveFixture fixture = CreateSplitDriveFixture(temp);
		AppConfigSettings settings = CreateScannerSettings(temp, fixture.GameRoot, fixture.WorkshopRoot, GameProvider.StandAlone);

		List<ModuleModel> modules = await ModScanner.ScanForModsAsync(settings);

		Assert.True(Directory.Exists(fixture.WorkshopRoot));
		Assert.Equal(8, modules.Count);
		Assert.Equal(fixture.LocalModuleIds.Order(), modules.Select(module => module.ModuleId).Order());
		Assert.DoesNotContain(modules, module => fixture.WorkshopModuleIds.Contains(module.ModuleId));
	}

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
		AppConfigSettings settings = CreateScannerSettings(temp, bannerlordLibraryRoot, selectedWorkshopRoot, GameProvider.Steam);

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
		AppConfigSettings settings = CreateScannerSettings(temp, gameRoot, workshopRoot, GameProvider.Steam);

		List<ModuleModel> modules = await ModScanner.ScanForModsAsync(settings);

		Assert.Equal("Valid.Workshop", Assert.Single(modules).ModuleId);
	}

	[Fact]
	public async Task ScanForModsAsync_WhenWorkshopOverrideIsMissing_ReturnsLocalModulesOnly() {
		using TestDirectory temp = new();
		string gameRoot = temp.CreateDirectory("Game");
		string modulesPath = temp.CreateDirectory("Game", "Modules");
		temp.WriteModule(modulesPath, "Local", "Local.Only");
		AppConfigSettings settings = CreateScannerSettings(
			temp,
			gameRoot,
			temp.GetPath("MissingWorkshop"),
			GameProvider.Steam);

		List<ModuleModel> modules = await ModScanner.ScanForModsAsync(settings);

		Assert.Equal("Local.Only", Assert.Single(modules).ModuleId);
	}

	[Fact]
	public async Task ScanForModsAsync_WhenSteamWorkshopPathIsEmpty_ReturnsLocalModulesOnly() {
		using TestDirectory temp = new();
		string gameRoot = temp.CreateDirectory("Game");
		string modulesPath = temp.CreateDirectory("Game", "Modules");
		temp.WriteModule(modulesPath, "Local", "Local.Only");
		AppConfigSettings settings = CreateScannerSettings(temp, gameRoot, string.Empty, GameProvider.Steam);

		List<ModuleModel> modules = await ModScanner.ScanForModsAsync(settings);

		Assert.Equal("Local.Only", Assert.Single(modules).ModuleId);
	}

	private static SplitDriveFixture CreateSplitDriveFixture(TestDirectory temp) {
		// These directories model C:/programs/steam and D:/games/Bannerlord without touching real drives.
		string steamRoot = temp.CreateDirectory("C", "programs", "steam");
		string workshopRoot = temp.CreateDirectory(
			"C",
			"programs",
			"steam",
			"steamapps",
			"workshop",
			"content",
			"261550");
		string gameRoot = temp.CreateDirectory("D", "games", "Bannerlord");
		string modulesRoot = temp.CreateDirectory("D", "games", "Bannerlord", "Modules");
		string[] localModuleIds = Enumerable.Range(1, 8)
			.Select(index => $"Local.Mod.{index}")
			.ToArray();
		string[] workshopModuleIds = Enumerable.Range(1, 3)
			.Select(index => $"Workshop.Mod.{index}")
			.ToArray();

		foreach (string moduleId in localModuleIds) {
			temp.WriteModule(modulesRoot, moduleId, moduleId);
		}
		for (int index = 0; index < workshopModuleIds.Length; index++) {
			string workshopItemRoot = Directory.CreateDirectory(
				Path.Combine(workshopRoot, (100001 + index).ToString())).FullName;
			string moduleId = workshopModuleIds[index];
			temp.WriteModule(workshopItemRoot, moduleId, moduleId);
		}

		return new SplitDriveFixture(
			steamRoot,
			workshopRoot,
			gameRoot,
			modulesRoot,
			localModuleIds,
			workshopModuleIds);
	}

	private static AppConfigSettings CreateScannerSettings(
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

	private sealed record SplitDriveFixture(
		string SteamRoot,
		string WorkshopRoot,
		string GameRoot,
		string ModulesRoot,
		string[] LocalModuleIds,
		string[] WorkshopModuleIds);
}
