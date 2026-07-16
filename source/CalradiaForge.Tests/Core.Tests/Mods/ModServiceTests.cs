namespace CalradiaForge.Tests.Core.Mods;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Infra.Paths;
using CalradiaForge.Tests.Core.Support;

public sealed class ModServiceTests {
	[Fact]
	public async Task RefreshAsync_WhenGameConfigurationIsInvalid_PreservesMemoryAndDiskCache() {
		using TestDirectory temp = new();
		string currentPath = temp.GetPath("mods_current.data");
		string backupPath = temp.GetPath("mods_backup.data");
		ModsData data = new(currentPath, backupPath);
		data.SaveCurrent([TestDirectory.Module("Current")]);
		data.SaveBackup([TestDirectory.Module("Backup")]);
		AppConfigSettings settings = new(new AppConfig(temp.GetPath("config.json"))) {
			GameProvider = GameProvider.Steam,
			GameFolderPath = temp.GetPath("MissingGame")
		};
		ModService service = new(settings, data);
		service.LoadFromCache();

		bool changed = await service.RefreshAsync();

		Assert.False(changed);
		Assert.Equal("Current", Assert.Single(service.CurrentMods).ModuleId);
		Assert.Equal("Backup", Assert.Single(service.PreviousMods).ModuleId);
		Assert.Empty(service.AddedMods);
		Assert.Empty(service.RemovedMods);
		Assert.False(service.IsRefreshing);
		Assert.Equal("Current", Assert.Single(data.LoadCurrent()).ModuleId);
		Assert.Equal("Backup", Assert.Single(data.LoadBackup()).ModuleId);
	}

	[Fact]
	public async Task RefreshAsync_WhenProviderRequiresManualConfiguration_PreservesValidLookingCache() {
		using TestDirectory temp = new();
		string gameRoot = temp.CreateDirectory("Game");
		string modulesRoot = temp.CreateDirectory("Game", "Modules");
		temp.WriteModule(modulesRoot, "Native", "Native");
		temp.CreateDirectory("Game", "bin");
		ModsData data = new(temp.GetPath("mods_current.data"), temp.GetPath("mods_backup.data"));
		data.SaveCurrent([TestDirectory.Module("Cached")]);
		AppConfigSettings settings = new(new AppConfig(temp.GetPath("config.json"))) {
			GameProvider = GameProvider.ManualConfiguration,
			GameFolderPath = gameRoot
		};
		ModService service = new(settings, data);
		service.LoadFromCache();

		bool changed = await service.RefreshAsync();

		Assert.False(changed);
		Assert.Equal("Cached", Assert.Single(service.CurrentMods).ModuleId);
		Assert.Equal("Cached", Assert.Single(data.LoadCurrent()).ModuleId);
	}
}
