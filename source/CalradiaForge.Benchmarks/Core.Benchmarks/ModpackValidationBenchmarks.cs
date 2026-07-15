namespace CalradiaForge.Benchmarks.Core;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using CalradiaForge.Core.Infra.Modpacks;
using CalradiaForge.Core.Models;

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
[BenchmarkCategory("Phase4", "ProvisionalPreRefactorBaseline", "Component")]
public class ModpackValidationBenchmarks {
	private ModpackModel _modpack = null!;
	private List<ModuleModel> _installedMods = null!;

	[Params(10, 100, 500)]
	public int ModuleCount { get; set; }

	[GlobalSetup]
	public void Setup() {
		List<ModpackEntryModel> entries = Enumerable.Range(0, ModuleCount)
			.Select(index => new ModpackEntryModel(
				$"Fixture.Module.{index:D4}",
				$"Fixture Module {index:D4}",
				"v1.0.0"))
			.ToList();
		_modpack = new ModpackModel("Benchmark", "Phase 4", entries);
		_installedMods = Enumerable.Range(0, ModuleCount)
			.Where(index => index % 2 == 0)
			.Select(index => new ModuleModel {
				ModuleId = $"Fixture.Module.{index:D4}",
				ModuleName = $"Fixture Module {index:D4}",
				ModuleVersion = "v1.0.0",
				IsSinglePlayerMod = true,
				DependencyModules = []
			})
			.ToList();
	}

	[Benchmark]
	public (List<ModpackEntryModel> ValidEntries, List<string> MissingModNames) ValidateLoadOrder() {
		return ModpackService.ValidateLoadOrder(_modpack, _installedMods);
	}
}
