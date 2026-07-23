namespace CalradiaForge.Tests.Core.Modpacks;

using CalradiaForge.Core.Infra.Modpacks;
using CalradiaForge.Core.Models;
using CalradiaForge.Tests.Core.Support;

public sealed class ModpackDataTests {
	[Fact]
	public void SaveModpack_RoundTripsThroughSanitizedFileName() {
		using TestDirectory temp = new();
		string modpacksPath = temp.CreateDirectory("Modpacks");
		ModpackData data = new(modpacksPath, temp.GetPath("last_used.data"));
		ModpackModel expected = CreateModpack("Vanilla + Tools");

		Assert.True(data.SaveModpack(expected));
		ModpackModel loaded = Assert.Single(data.LoadAllModpacks());

		Assert.Equal("Vanilla + Tools", loaded.ModpackName);
		Assert.Equal("Vanilla_Tools", loaded.FileName);
		Assert.Single(loaded.LoadOrder);
	}

	[Fact]
	public void SaveModpack_WhenFileExists_AtomicallyReplacesItsContents() {
		using TestDirectory temp = new();
		string modpacksPath = temp.CreateDirectory("Modpacks");
		ModpackData data = new(modpacksPath, temp.GetPath("last_used.data"));
		ModpackModel modpack = CreateModpack("Replace Me");
		Assert.True(data.SaveModpack(modpack));
		modpack.LoadOrder.Add(new ModpackEntryModel("Sandbox", "Sandbox", "v1.0.0"));

		Assert.True(data.SaveModpack(modpack));

		ModpackModel loaded = Assert.Single(data.LoadAllModpacks());
		Assert.Equal(2, loaded.LoadOrder.Count);
		Assert.Empty(Directory.GetFiles(modpacksPath, "*.tmp"));
	}

	[Fact]
	public void LoadAllModpacks_SkipsInvalidJsonWithoutDiscardingValidFiles() {
		using TestDirectory temp = new();
		string modpacksPath = temp.CreateDirectory("Modpacks");
		ModpackData data = new(modpacksPath, temp.GetPath("last_used.data"));
		Assert.True(data.SaveModpack(CreateModpack("Valid")));
		File.WriteAllText(Path.Combine(modpacksPath, "invalid.json"), "not-json");

		List<ModpackModel> loaded = data.LoadAllModpacks();

		Assert.Equal("Valid", Assert.Single(loaded).ModpackName);
	}

	[Fact]
	public void SaveLastUsed_RoundTripsSeparatelyFromNamedModpacks() {
		using TestDirectory temp = new();
		string modpacksPath = temp.CreateDirectory("Modpacks");
		ModpackData data = new(modpacksPath, temp.GetPath("last_used.data"));
		ModpackModel expected = CreateModpack("Last Used");

		Assert.True(data.SaveLastUsed(expected));

		Assert.Equal("Last Used", data.LoadLastUsed()?.ModpackName);
		Assert.Empty(data.LoadAllModpacks());
	}

	private static ModpackModel CreateModpack(string name) {
		return new ModpackModel(name, "Test", [new ModpackEntryModel("Native", "Native", "v1.0.0")]);
	}
}
