namespace CalradiaForge.Tests.Core.Config;

using System.Collections.Concurrent;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Launch;
using CalradiaForge.Core.Infra.Paths;
using CalradiaForge.Tests.Core.Support;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public sealed class ConfigFileManagerTests {
	[Fact]
	public void Load_WhenFileIsMissing_CreatesCompleteDefaultsInMemoryAndOnDisk() {
		using TestDirectory temp = new();
		string configPath = temp.GetPath("config.json");
		ConfigFileManager config = new(configPath);

		ConfigLoadResult result = config.Load();

		Assert.Equal(ConfigLoadStatus.MissingFileInitialization, result.Status);
		Assert.Equal(ConfigPersistenceStatus.Available, result.PersistenceStatus);
		AssertCompleteDefaults(config);
		AssertCompleteDefaults(JObject.Parse(File.ReadAllText(configPath)));
	}

	[Fact]
	public void Load_WhenMissingFileCannotBeSaved_RetainsCompleteDefaults() {
		FakeConfigPersistence persistence = new() {
			WriteException = new IOException("write unavailable")
		};
		ConfigFileManager config = new("config.json", persistence);

		ConfigLoadResult result = config.Load();

		Assert.Equal(ConfigLoadStatus.MissingFileInitialization, result.Status);
		Assert.Equal(ConfigPersistenceStatus.Unavailable, result.PersistenceStatus);
		Assert.Equal(ConfigSaveStatus.WriteUnavailable, config.LastSaveResult.Status);
		AssertCompleteDefaults(config);
	}

	[Fact]
	public void Load_WhenFileIsComplete_PreservesAllValuesWithoutRewrite() {
		Dictionary<string, string> values = new(ConfigDefaults.Values) {
			["Language"] = "sv-SE",
			["DebugMode"] = bool.TrueString
		};
		FakeConfigPersistence persistence = new(JsonConvert.SerializeObject(values));
		ConfigFileManager config = new("config.json", persistence);

		ConfigLoadResult result = config.Load();

		Assert.Equal(ConfigLoadStatus.NormalLoad, result.Status);
		Assert.Equal("sv-SE", config["Language"]);
		Assert.True(config.GetBool("DebugMode"));
		Assert.Equal(0, persistence.WriteCount);
	}

	[Fact]
	public void Load_WhenFileIsPartial_OverlaysValuesAndPreservesUnknownKeys() {
		FakeConfigPersistence persistence = new("""
			{
			  "Language": "fr-FR",
			  "CustomFutureSetting": "preserve-me"
			}
			""");
		ConfigFileManager config = new("config.json", persistence);

		ConfigLoadResult result = config.Load();

		Assert.Equal(ConfigLoadStatus.PartialDefaultCompletion, result.Status);
		Assert.Equal("fr-FR", config["Language"]);
		Assert.Equal("preserve-me", config["CustomFutureSetting"]);
		foreach (KeyValuePair<string, string> entry in ConfigDefaults.Values.Where(entry => entry.Key != "Language")) {
			Assert.Equal(entry.Value, config[entry.Key]);
		}
		Assert.Equal("preserve-me", (string?)JObject.Parse(persistence.Contents!)["CustomFutureSetting"]);
		Assert.Equal(1, persistence.WriteCount);
	}

	[Fact]
	public void Load_WhenRequiredValueIsNull_RestoresItsDefault() {
		Dictionary<string, string?> values = ConfigDefaults.Values
			.ToDictionary(entry => entry.Key, entry => (string?)entry.Value);
		values["Language"] = null;
		FakeConfigPersistence persistence = new(JsonConvert.SerializeObject(values));
		ConfigFileManager config = new("config.json", persistence);

		ConfigLoadResult result = config.Load();

		Assert.Equal(ConfigLoadStatus.PartialDefaultCompletion, result.Status);
		Assert.Contains("Language", result.CompletedDefaultKeys);
		Assert.Equal("en-US", config["Language"]);
	}

	[Fact]
	public void Load_WhenRecognizedTypedValuesAreInvalid_RestoresCanonicalDefaults() {
		Dictionary<string, string> values = new(ConfigDefaults.Values) {
			["GamePlatform"] = "999",
			["ModpackStartupMode"] = "not-a-mode",
			["DefaultLaunchTarget"] = "999",
			["EulaAccepted"] = "not-a-bool",
			["DebugMode"] = "not-a-bool"
		};
		FakeConfigPersistence persistence = new(JsonConvert.SerializeObject(values));
		ConfigFileManager config = new("config.json", persistence);

		ConfigLoadResult result = config.Load();

		Assert.Equal(ConfigLoadStatus.PartialDefaultCompletion, result.Status);
		Assert.Equal(
			new[] { "DebugMode", "DefaultLaunchTarget", "EulaAccepted", "GamePlatform", "ModpackStartupMode" },
			result.CompletedDefaultKeys.OrderBy(key => key).ToArray());
		Assert.Equal(GameProvider.NotInitialized, new AppSettings(config).GameProvider);
		Assert.Equal(ModpackStartupMode.AlwaysAsk, new AppSettings(config).ModpackStartupMode);
		Assert.Equal(LaunchTarget.Bannerlord, new AppSettings(config).DefaultLaunchTarget);
		Assert.False(new AppSettings(config).EulaAccepted);
		Assert.False(new LoggingSettings(config).DebugMode);
		Assert.Equal(1, persistence.WriteCount);
	}

	[Fact]
	public void Load_RemovesObsoleteKeyAndPreservesUnknownKey() {
		Dictionary<string, string> values = new(ConfigDefaults.Values) {
			["LogFileDaysToKeep"] = "30",
			["Unknown"] = "value"
		};
		FakeConfigPersistence persistence = new(JsonConvert.SerializeObject(values));
		ConfigFileManager config = new("config.json", persistence);

		config.Load();

		JObject saved = JObject.Parse(persistence.Contents!);
		Assert.Null(saved["LogFileDaysToKeep"]);
		Assert.Equal("value", (string?)saved["Unknown"]);
	}

	[Fact]
	public void Load_WhenJsonIsMalformed_PreservesOriginalBeforeRepair() {
		using TestDirectory temp = new();
		string configPath = temp.GetPath("config.json");
		const string malformed = "{not valid json";
		File.WriteAllText(configPath, malformed);
		ConfigFileManager config = new(configPath);

		ConfigLoadResult result = config.Load();

		Assert.Equal(ConfigLoadStatus.MalformedFileRecovery, result.Status);
		Assert.NotNull(result.CorruptionBackupPath);
		Assert.Equal(malformed, File.ReadAllText(result.CorruptionBackupPath!));
		AssertCompleteDefaults(JObject.Parse(File.ReadAllText(configPath)));
	}

	[Fact]
	public void Load_WhenMalformedFileCannotBePreserved_DoesNotOverwriteOriginal() {
		const string malformed = "{still malformed";
		FakeConfigPersistence persistence = new(malformed) {
			CopyException = new IOException("copy unavailable")
		};
		ConfigFileManager config = new("config.json", persistence);

		ConfigLoadResult result = config.Load();

		Assert.Equal(ConfigLoadStatus.MalformedFileRetainedNotRepairable, result.Status);
		Assert.Equal(ConfigPersistenceStatus.Unavailable, result.PersistenceStatus);
		Assert.Equal(malformed, persistence.Contents);
		Assert.Equal(0, persistence.WriteCount);
		AssertCompleteDefaults(config);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Load_WhenReadIsUnavailable_UsesDefaultsAndLeavesFileUntouched(bool accessDenied) {
		const string original = "{\"Language\":\"de-DE\"}";
		FakeConfigPersistence persistence = new(original) {
			ReadException = accessDenied
				? new UnauthorizedAccessException("denied")
				: new IOException("locked")
		};
		ConfigFileManager config = new("config.json", persistence);

		ConfigLoadResult result = config.Load();

		Assert.Equal(
			accessDenied ? ConfigLoadStatus.AccessDenied : ConfigLoadStatus.ReadUnavailable,
			result.Status);
		Assert.Equal(ConfigPersistenceStatus.Unavailable, result.PersistenceStatus);
		Assert.Equal(original, persistence.Contents);
		Assert.Equal(0, persistence.WriteCount);
		AssertCompleteDefaults(config);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Load_WhenFilePresenceCannotBeInspected_DoesNotTreatFileAsMissing(bool accessDenied) {
		FakeConfigPersistence persistence = new("original") {
			FileExistsException = accessDenied
				? new UnauthorizedAccessException("denied")
				: new IOException("path unavailable")
		};
		ConfigFileManager config = new("config.json", persistence);

		ConfigLoadResult result = config.Load();

		Assert.Equal(
			accessDenied ? ConfigLoadStatus.AccessDenied : ConfigLoadStatus.ReadUnavailable,
			result.Status);
		Assert.Equal(ConfigPersistenceStatus.Unavailable, result.PersistenceStatus);
		Assert.Equal("original", persistence.Contents);
		Assert.Equal(0, persistence.WriteCount);
	}

	[Fact]
	public void Setter_WhenSaveFails_RetainsMemoryAndStopsRepeatedWrites() {
		FakeConfigPersistence persistence = new() {
			WriteException = new IOException("disk full")
		};
		ConfigFileManager config = new("config.json", persistence);
		config.Load();
		int writesAfterLoad = persistence.WriteCount;
		AppSettings settings = new(config);

		settings.Language = "it-IT";
		settings.GameFolderPath = "C:\\Games\\Bannerlord";

		Assert.Equal("it-IT", settings.Language);
		Assert.Equal("C:\\Games\\Bannerlord", settings.GameFolderPath);
		Assert.Equal(ConfigPersistenceStatus.Unavailable, config.PersistenceStatus);
		Assert.Equal(ConfigSaveStatus.SkippedPersistenceUnavailable, config.LastSaveResult.Status);
		Assert.Equal(writesAfterLoad, persistence.WriteCount);
	}

	[Fact]
	public void Setter_WhenPersistenceBecomesUnavailable_PublishesOneTransition() {
		FakeConfigPersistence persistence = new(JsonConvert.SerializeObject(ConfigDefaults.Values));
		ConfigFileManager config = new("config.json", persistence);
		config.Load();
		int notificationCount = 0;
		ConfigSaveStatus? observedStatus = null;
		config.PersistenceBecameUnavailable += (_, args) => {
			notificationCount++;
			observedStatus = args.SaveResult.Status;
		};
		persistence.WriteException = new IOException("disk unavailable");

		config["Language"] = "pl-PL";
		config["Language"] = "nl-NL";

		Assert.Equal(1, notificationCount);
		Assert.Equal(ConfigSaveStatus.WriteUnavailable, observedStatus);
		Assert.Equal("nl-NL", config["Language"]);
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
		Assert.True(new LoggingSettings(reloaded).DebugMode);
	}

	[Fact]
	public void ConcurrentAccess_DoesNotCorruptConfigurationMap() {
		FakeConfigPersistence persistence = new();
		ConfigFileManager config = new("config.json", persistence);
		config.Load();
		ConcurrentQueue<Exception> exceptions = new();

		Parallel.For(0, 100, index => {
			try {
				string key = $"Concurrent-{index}";
				config[key] = index.ToString();
				Assert.Equal(index.ToString(), config[key]);
			} catch (Exception ex) {
				exceptions.Enqueue(ex);
			}
		});

		Assert.Empty(exceptions);
		for (int index = 0; index < 100; index++) {
			Assert.Equal(index.ToString(), config[$"Concurrent-{index}"]);
		}
		Assert.NotNull(JObject.Parse(persistence.Contents!));
	}

	private static void AssertCompleteDefaults(ConfigFileManager config) {
		foreach (KeyValuePair<string, string> entry in ConfigDefaults.Values) {
			Assert.Equal(entry.Value, config[entry.Key]);
		}
	}

	private static void AssertCompleteDefaults(JObject values) {
		foreach (KeyValuePair<string, string> entry in ConfigDefaults.Values) {
			Assert.Equal(entry.Value, (string?)values[entry.Key]);
		}
	}

	private sealed class FakeConfigPersistence : IConfigFilePersistence {
		public FakeConfigPersistence(string? contents = null) {
			Contents = contents;
		}

		public string? Contents { get; private set; }
		public Exception? FileExistsException { get; set; }
		public Exception? ReadException { get; set; }
		public Exception? WriteException { get; set; }
		public Exception? CopyException { get; set; }
		public int WriteCount { get; private set; }

		public bool FileExists(string path) {
			if (FileExistsException is not null) {
				throw FileExistsException;
			}
			return string.Equals(path, "config.json", StringComparison.Ordinal) && Contents is not null;
		}

		public string ReadAllText(string path) {
			if (ReadException is not null) {
				throw ReadException;
			}
			return Contents ?? throw new FileNotFoundException();
		}

		public void WriteAllTextAtomic(string path, string contents) {
			WriteCount++;
			if (WriteException is not null) {
				throw WriteException;
			}
			Contents = contents;
		}

		public void CopyFile(string sourcePath, string destinationPath) {
			if (CopyException is not null) {
				throw CopyException;
			}
		}
	}
}
