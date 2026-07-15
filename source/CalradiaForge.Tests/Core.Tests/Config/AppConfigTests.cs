namespace CalradiaForge.Tests.Core.Config;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Tests.Core.Support;

using Newtonsoft.Json.Linq;

public sealed class AppConfigTests {
	[Fact]
	public void Load_WhenFileIsMissing_CreatesReadableJson() {
		using TestDirectory temp = new();
		string configPath = temp.GetPath("config.json");
		AppConfig config = new(configPath);

		config.Load();

		Assert.True(File.Exists(configPath));
		Assert.NotNull(JObject.Parse(File.ReadAllText(configPath)));
	}

	[Fact]
	public void Settings_WhenConfigIsCorrupt_SeedTypedDefaultsWithoutCrashing() {
		using TestDirectory temp = new();
		string configPath = temp.GetPath("config.json");
		File.WriteAllText(configPath, "{not valid json");
		AppConfig config = new(configPath);

		config.Load();
		AppConfigSettings settings = new(config);

		Assert.Equal("en-US", settings.Language);
		Assert.False(settings.DebugMode);
		Assert.Equal(ModpackStartupMode.AlwaysAsk, settings.ModpackStartupMode);
		Assert.NotNull(JObject.Parse(File.ReadAllText(configPath)));
	}

	[Fact]
	public void Indexer_PersistsUpdatedValuesImmediately() {
		using TestDirectory temp = new();
		string configPath = temp.GetPath("config.json");
		AppConfig config = new(configPath);

		config["Language"] = "sv-SE";

		AppConfig reloaded = new(configPath);
		reloaded.Load();
		Assert.Equal("sv-SE", reloaded["Language"]);
	}
}
