namespace CalradiaForge.Tests.Core.Modpacks;

using CalradiaForge.Core.Infra.Modpacks;
using CalradiaForge.Core.Models;
using CalradiaForge.Tests.Core.Support;

using Newtonsoft.Json;

public sealed class ModpackServiceTests {
	[Fact]
	public void LoadAll_WhenVanillaIsMissing_CreatesDefaultOnDisk() {
		using TestDirectory temp = new();
		string modpacksPath = temp.CreateDirectory("Modpacks");
		ModpackData data = new(modpacksPath, temp.GetPath("last_used.data"));
		ModpackService service = new(data);

		service.LoadAll();

		Assert.Equal(VanillaModules.DefaultModpackName, Assert.Single(service.AllModpacks).ModpackName);
		Assert.Single(Directory.GetFiles(modpacksPath, "*.json"));
	}

	[Fact]
	public void ValidateLoadOrder_ReturnsMissingEntriesWithoutMutatingSavedOrder() {
		ModpackModel modpack = new(
			"Mixed",
			"Test",
			[
				new ModpackEntryModel("native", "Native", "v1"),
				new ModpackEntryModel("missing", "Missing Mod", "v1")
			]);

		(List<ModpackEntryModel> valid, List<string> missing) = ModpackService.ValidateLoadOrder(
			modpack,
			[TestDirectory.Module("NATIVE")]);

		Assert.Equal("native", Assert.Single(valid).ModuleId);
		Assert.Equal("Missing Mod", Assert.Single(missing));
		Assert.Equal(2, modpack.LoadOrder.Count);
	}

	[Fact]
	public void SaveAs_ClonesCallerEntriesBeforePersisting() {
		using TestDirectory temp = new();
		ModpackData data = new(temp.CreateDirectory("Modpacks"), temp.GetPath("last_used.data"));
		ModpackService service = new(data);
		List<ModpackEntryModel> entries = [new("Example", "Example", "v1")];

		Assert.True(service.SaveAs("Example Pack", "Test", entries));
		entries[0].ModuleName = "Changed After Save";

		Assert.Equal("Example", service.FindByName("Example Pack")?.LoadOrder[0].ModuleName);
	}

	[Fact]
	public void ImportAndExport_RoundTripThroughServiceWorkflow() {
		using TestDirectory temp = new();
		string modpacksPath = temp.CreateDirectory("Modpacks");
		ModpackService service = new(new ModpackData(modpacksPath, temp.GetPath("last_used.data")));
		ModpackModel source = new("Imported Pack", "Fixture", [new ModpackEntryModel("Example", "Example", "v1")]);
		string importPath = temp.GetPath("import.json");
		File.WriteAllText(importPath, JsonConvert.SerializeObject(source));

		(bool success, ModpackModel? imported, string message) = service.Import(importPath);
		string exportPath = temp.GetPath("export.json");

		Assert.True(success, message);
		Assert.NotNull(imported);
		Assert.True(service.Export(imported, exportPath));
		ModpackModel? exported = JsonConvert.DeserializeObject<ModpackModel>(File.ReadAllText(exportPath));
		Assert.Equal("Imported Pack", exported?.ModpackName);
		Assert.True(File.Exists(Path.Combine(modpacksPath, "Imported_Pack.json")));
	}
}
