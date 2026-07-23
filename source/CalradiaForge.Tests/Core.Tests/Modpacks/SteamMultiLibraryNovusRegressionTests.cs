namespace CalradiaForge.Tests.Core.Modpacks;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.GamePlatform;
using CalradiaForge.Core.Infra.GamePlatform.Steam;
using CalradiaForge.Core.Infra.Modpacks;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Infra.Paths;
using CalradiaForge.Core.Models;
using CalradiaForge.Tests.Core.Support;

public sealed class SteamMultiLibraryNovusRegressionTests {
	[Fact]
	public async Task ImportedNovusPreset_AfterAlternateSteamLibraryScan_HasNoMissingWorkshopModules() {
		using TestDirectory temp = new();
		NovusSteamFixture fixture = CreateNovusSteamFixture(temp);
		AppConfigSettings settings = new(new AppConfig(temp.GetPath("config.json")));

		GameDetectionService workflow = new(
			new GamePlatformDetectionResolver(
				new TestSteamClientRootProvider(fixture.SteamClientRoot),
				new SteamInstallationResolver()),
			new StartupNotificationQueue());

		GameProvider provider = workflow.RedetectGame(settings);

		Assert.Equal(GameProvider.Steam, provider);
		Assert.Equal(GameProvider.Steam, settings.GameProvider);
		Assert.Equal(fixture.GameRoot, settings.GameFolderPath);
		Assert.Equal(fixture.WorkshopRoot, settings.SteamWorkshopFolderPath);

		ModsData modsData = new(temp.GetPath("mods_current.data"), temp.GetPath("mods_backup.data"));
		ModPipelineCoordinator pipeline = new(settings, modsData, new ModInstaller(settings));
		pipeline.LoadAcceptedCache();

		ModPipelineResult scanResult = await pipeline.RefreshAsync();
		Assert.True(scanResult.Success, scanResult.TechnicalDiagnostic);

		string[] scannedIds = scanResult.AcceptedSnapshot.Modules
			.Select(module => module.ModuleId)
			.Where(moduleId => !string.IsNullOrWhiteSpace(moduleId))
			.Cast<string>()
			.ToArray();
		Assert.Equal(11, scannedIds.Length);
		Assert.Equal(11, scannedIds.Distinct(StringComparer.OrdinalIgnoreCase).Count());
		Assert.Equal(
			fixture.ExpectedModuleIds.OrderBy(moduleId => moduleId, StringComparer.OrdinalIgnoreCase),
			scannedIds.OrderBy(moduleId => moduleId, StringComparer.OrdinalIgnoreCase));
		Assert.All(
			fixture.WorkshopModuleIds,
			workshopId => Assert.Contains(scannedIds, id => string.Equals(id, workshopId, StringComparison.OrdinalIgnoreCase)));

		string presetPath = temp.GetPath("split-library-novus-preset.xml");
		File.WriteAllText(presetPath, BuildNovusPreset(fixture.ExpectedModuleIds));
		ModpackService modpackService = new(new ModpackData(
			temp.CreateDirectory("Modpacks"),
			temp.GetPath("last_used_mods.data")));

		(bool importedSuccessfully, ModpackModel? imported, string importMessage) = modpackService.Import(presetPath);

		Assert.True(importedSuccessfully, importMessage);
		Assert.NotNull(imported);
		ModpackModel? selected = modpackService.FindByName("Split Library Novus Regression");
		Assert.NotNull(selected);
		Assert.Equal(fixture.ExpectedModuleIds, selected.LoadOrder.Select(entry => entry.ModuleId));

		(List<ModpackEntryModel> validEntries, List<string> missingModNames) =
			ModpackService.ValidateLoadOrder(selected, scanResult.AcceptedSnapshot.Modules);

		Assert.Equal(11, validEntries.Count);
		Assert.Empty(missingModNames);
		Assert.All(
			fixture.WorkshopModuleIds,
			workshopId => Assert.Contains(
				validEntries,
				entry => string.Equals(entry.ModuleId, workshopId, StringComparison.OrdinalIgnoreCase)));
	}

	private static NovusSteamFixture CreateNovusSteamFixture(TestDirectory temp) {
		string steamClientRoot = temp.CreateDirectory("C", "Program Files (x86)", "Steam");
		string steamAppsRoot = temp.CreateDirectory("C", "Program Files (x86)", "Steam", "steamapps");
		string alternateLibraryRoot = temp.CreateDirectory("D", "SteamLibrary");
		string alternateSteamAppsRoot = temp.CreateDirectory("D", "SteamLibrary", "steamapps");
		string gameRoot = temp.CreateDirectory(
			"D",
			"SteamLibrary",
			"steamapps",
			"common",
			"Mount & Blade II Bannerlord");
		string modulesRoot = temp.CreateDirectory(
			"D",
			"SteamLibrary",
			"steamapps",
			"common",
			"Mount & Blade II Bannerlord",
			"Modules");
		string launcherRoot = temp.CreateDirectory(
			"D",
			"SteamLibrary",
			"steamapps",
			"common",
			"Mount & Blade II Bannerlord",
			"bin",
			"Win64_Shipping_Client");
		File.WriteAllText(Path.Combine(launcherRoot, "Bannerlord.exe"), string.Empty);
		string workshopRoot = temp.CreateDirectory(
			"D",
			"SteamLibrary",
			"steamapps",
			"workshop",
			"content",
			"261550");

		string[] localModuleIds = [
			"Native",
			.. Enumerable.Range(1, 7).Select(index => $"Local.Mod.{index}")
		];
		string[] workshopModuleIds = Enumerable.Range(1, 3)
			.Select(index => $"Workshop.Mod.{index}")
			.ToArray();
		string[] expectedModuleIds = [.. localModuleIds, .. workshopModuleIds];

		temp.WriteModule(modulesRoot, "Native", "Native");
		foreach (string moduleId in localModuleIds.Skip(1)) {
			temp.WriteModule(modulesRoot, moduleId, moduleId);
		}
		for (int index = 0; index < workshopModuleIds.Length; index++) {
			string workshopItemRoot = Directory.CreateDirectory(
				Path.Combine(workshopRoot, (100001 + index).ToString())).FullName;
			string moduleId = workshopModuleIds[index];
			temp.WriteModule(workshopItemRoot, moduleId, moduleId);
		}

		File.WriteAllText(
			Path.Combine(steamAppsRoot, "libraryfolders.vdf"),
			$$"""
			"libraryfolders"
			{
			    "0"
			    {
			        "path" "{{EscapeNovusMetadataValue(steamClientRoot)}}"
			    }
			    "1"
			    {
			        "path" "{{EscapeNovusMetadataValue(alternateLibraryRoot)}}"
			        "apps"
			        {
			            "261550" "1"
			        }
			    }
			}
			""");
		File.WriteAllText(
			Path.Combine(alternateSteamAppsRoot, "appmanifest_261550.acf"),
			"""
			"AppState"
			{
			    "appid" "261550"
			    "installdir" "Mount & Blade II Bannerlord"
			}
			""");

		return new NovusSteamFixture(
			steamClientRoot,
			gameRoot,
			workshopRoot,
			expectedModuleIds,
			workshopModuleIds);
	}

	private static string BuildNovusPreset(IEnumerable<string> moduleIds) {
		string modules = string.Join(
			Environment.NewLine,
			moduleIds.Select(moduleId => $"  <PresetModule Id=\"{moduleId}\" RequiredVersion=\"v1.0.0\" />"));
		return $"""
			<Preset Name="Split Library Novus Regression" CreatedBy="CalradiaForge.Tests">
			{modules}
			</Preset>
			""";
	}

	private static string EscapeNovusMetadataValue(string value) {
		return value.Replace("\\", "\\\\", StringComparison.Ordinal);
	}

	private sealed class TestSteamClientRootProvider(string steamClientRoot) : ISteamClientRootProvider {
		public string? GetSteamClientRoot() {
			return steamClientRoot;
		}
	}

	private sealed record NovusSteamFixture(
		string SteamClientRoot,
		string GameRoot,
		string WorkshopRoot,
		string[] ExpectedModuleIds,
		string[] WorkshopModuleIds);
}
