namespace CalradiaForge.Tests.Core.Config;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Tests.Core.Support;

using Newtonsoft.Json.Linq;

public sealed class ConfigFileManagerTests {
	[Fact]
	public void Load_WhenFileIsMissing_DoesNotCreatePlaceholderFile() {
		using TestDirectory temp = new();
		string configPath = temp.GetPath("config.json");
		ConfigFileManager config = new(configPath);

		config.Load();

		Assert.False(File.Exists(configPath));
	}

	[Fact]
	public void Settings_WhenFileIsMissing_CreatesOneCompleteDefaultFile() {
		using TestDirectory temp = new();
		string configPath = temp.GetPath("config.json");
		ConfigFileManager config = new(configPath);

		config.Load();
		Assert.False(File.Exists(configPath));

		_ = new AppSettings(config);
		_ = new LoggingSettings(config);

		Assert.True(File.Exists(configPath));
		JObject saved = JObject.Parse(File.ReadAllText(configPath));
		Assert.Equal(13, saved.Count);
		Assert.Equal("en-US", (string?)saved["Language"]);
		Assert.Equal("Bannerlord", (string?)saved["DefaultLaunchTarget"]);
		Assert.Equal("False", (string?)saved["EulaAccepted"]);
		Assert.Equal("False", (string?)saved["DebugMode"]);
		Assert.Null(saved["LogFileDaysToKeep"]);
	}

	[Fact]
	public void ApplyDefaults_WhenOptionalValueIsAlreadyEmpty_DoesNotApplyOrSaveItAgain() {
		using TestDirectory temp = new();
		string configPath = temp.GetPath("config.json");
		ConfigFileManager config = new(configPath);
		config["OptionalPath"] = string.Empty;
		using (FileStream destinationLock = new(
			configPath,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read)) {
			IReadOnlyList<string> appliedKeys = config.ApplyDefaults([
				new KeyValuePair<string, string>("OptionalPath", string.Empty)
			]);

			Assert.Empty(appliedKeys);
		}

		Assert.Equal(string.Empty, config["OptionalPath"]);
	}

	[Fact]
	public void ApplyDefaults_WhenRequiredValueIsEmpty_RepairsAndPersistsIt() {
		using TestDirectory temp = new();
		string configPath = temp.GetPath("config.json");
		ConfigFileManager config = new(configPath);
		config["Language"] = string.Empty;

		IReadOnlyList<string> appliedKeys = config.ApplyDefaults([
			new KeyValuePair<string, string>("Language", "en-US")
		]);

		Assert.Equal(["Language"], appliedKeys);
		Assert.Equal("en-US", config["Language"]);
		Assert.Equal("en-US", (string?)JObject.Parse(File.ReadAllText(configPath))["Language"]);
	}

	[Fact]
	public void Settings_WhenConfigIsCorrupt_SeedTypedDefaultsWithoutCrashing() {
		using TestDirectory temp = new();
		string configPath = temp.GetPath("config.json");
		File.WriteAllText(configPath, "{not valid json");
		ConfigFileManager config = new(configPath);

		config.Load();
		AppSettings settings = new(config);
		LoggingSettings loggingSettings = new(config);

		Assert.Equal("en-US", settings.Language);
		Assert.False(loggingSettings.DebugMode);
		Assert.Equal(ModpackStartupMode.AlwaysAsk, settings.ModpackStartupMode);
		Assert.NotNull(JObject.Parse(File.ReadAllText(configPath)));
	}

	[Fact]
	public void LoggingSettings_DebugModePersistsImmediatelyThroughSharedManager() {
		using TestDirectory temp = new();
		string configPath = temp.GetPath("config.json");
		ConfigFileManager config = new(configPath);
		config.Load();
		LoggingSettings settings = new(config);

		settings.DebugMode = true;

		ConfigFileManager reloaded = new(configPath);
		reloaded.Load();
		LoggingSettings persisted = new(reloaded);
		Assert.True(persisted.DebugMode);
	}

	[Fact]
	public void LoggingSettings_RemovesObsoleteConfigurableRetentionKey() {
		using TestDirectory temp = new();
		string configPath = temp.GetPath("config.json");
		ConfigFileManager config = new(configPath);
		config["LogFileDaysToKeep"] = "30";

		_ = new LoggingSettings(config);

		Assert.Null(JObject.Parse(File.ReadAllText(configPath))["LogFileDaysToKeep"]);
	}

	[Fact]
	public void Indexer_PersistsUpdatedValuesImmediately() {
		using TestDirectory temp = new();
		string configPath = temp.GetPath("config.json");
		ConfigFileManager config = new(configPath);

		config["Language"] = "sv-SE";

		ConfigFileManager reloaded = new(configPath);
		reloaded.Load();
		Assert.Equal("sv-SE", reloaded["Language"]);
	}
}
