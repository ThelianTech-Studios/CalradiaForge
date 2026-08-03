namespace CalradiaForge.Tests.Core.Persistence;

using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Models;
using CalradiaForge.Tests.Core.Support;

public sealed class ModsDataTests {
	[Fact]
	public void SaveAndLoadCurrent_RoundTripsModuleMetadata() {
		using TestDirectory temp = new();
		ModsData data = CreateData(temp, out _, out _);

		data.SaveCurrent([TestDirectory.Module("Example", "v2.1.0")]);
		List<ModuleModel> loaded = data.LoadCurrent();

		ModuleModel module = Assert.Single(loaded);
		Assert.Equal("Example", module.ModuleId);
		Assert.Equal("v2.1.0", module.ModuleVersion);
	}

	[Fact]
	public void SaveCurrentAndBackup_WhenFilesExist_AtomicallyReplaceTheirContents() {
		using TestDirectory temp = new();
		ModsData data = CreateData(temp, out _, out _);
		data.SaveCurrent([TestDirectory.Module("OldCurrent")]);
		data.SaveBackup([TestDirectory.Module("OldBackup")]);

		data.SaveCurrent([TestDirectory.Module("NewCurrent")]);
		data.SaveBackup([TestDirectory.Module("NewBackup")]);

		Assert.Equal("NewCurrent", Assert.Single(data.LoadCurrent()).ModuleId);
		Assert.Equal("NewBackup", Assert.Single(data.LoadBackup()).ModuleId);
		Assert.Empty(Directory.GetFiles(temp.RootPath, "*.tmp"));
	}

	[Fact]
	public void LoadCurrent_WhenCurrentIsCorrupt_RecoversAndRepairsFromBackup() {
		using TestDirectory temp = new();
		ModsData data = CreateData(temp, out string currentPath, out _);
		data.SaveBackup([TestDirectory.Module("Backup")]);
		File.WriteAllText(currentPath, "[null]");

		List<ModuleModel> loaded = data.LoadCurrent();

		Assert.Equal("Backup", Assert.Single(loaded).ModuleId);
		Assert.DoesNotContain("null", File.ReadAllText(currentPath), StringComparison.OrdinalIgnoreCase);
		Assert.Equal("Backup", Assert.Single(data.LoadCurrent()).ModuleId);
	}

	[Fact]
	public void RotateDataFiles_WhenCurrentIsInvalid_PreservesExistingBackup() {
		using TestDirectory temp = new();
		ModsData data = CreateData(temp, out string currentPath, out _);
		data.SaveBackup([TestDirectory.Module("KnownGood")]);
		File.WriteAllText(currentPath, "not-json");

		data.RotateDataFiles();

		Assert.Equal("KnownGood", Assert.Single(data.LoadBackup()).ModuleId);
	}

	[Fact]
	public void ClearCache_RemovesCurrentAndBackupFiles() {
		using TestDirectory temp = new();
		ModsData data = CreateData(temp, out string currentPath, out string backupPath);
		data.SaveCurrent([TestDirectory.Module("Current")]);
		data.SaveBackup([TestDirectory.Module("Backup")]);

		Assert.True(data.ClearCache());

		Assert.False(File.Exists(currentPath));
		Assert.False(File.Exists(backupPath));
	}

	private static ModsData CreateData(TestDirectory temp, out string currentPath, out string backupPath) {
		currentPath = temp.GetPath("mods_current.data");
		backupPath = temp.GetPath("mods_backup.data");
		return new ModsData(currentPath, backupPath);
	}
}
