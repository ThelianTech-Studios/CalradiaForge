namespace CalradiaForge.Benchmarks.Core;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using CalradiaForge.Benchmarks.Core.Support;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Models;

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
[BenchmarkCategory("Phase4", "ProvisionalPreRefactorBaseline", "EndToEndFilesystem")]
public class ModsDataBenchmarks {
	private BenchmarkFixtureDirectory _fixture = null!;
	private ModsData _data = null!;
	private List<ModuleModel> _modules = null!;

	[Params(10, 100, 500)]
	public int ModuleCount { get; set; }

	[GlobalSetup]
	public void Setup() {
		_fixture = new BenchmarkFixtureDirectory();
		_data = new ModsData(_fixture.GetPath("mods_current.data"), _fixture.GetPath("mods_backup.data"));
		_modules = Enumerable.Range(0, ModuleCount)
			.Select(index => new ModuleModel {
				ModuleId = $"Fixture.Module.{index:D4}",
				ModuleName = $"Fixture Module {index:D4}",
				ModuleVersion = "v1.0.0",
				InstallPath = $"X:\\Fixture\\Module{index:D4}",
				IsSinglePlayerMod = true,
				DependencyModules = []
			})
			.ToList();
		_data.SaveCurrent(_modules);
	}

	[Benchmark]
	public List<ModuleModel> LoadCurrent() {
		return _data.LoadCurrent();
	}

	[Benchmark]
	public void SaveCurrent() {
		_data.SaveCurrent(_modules);
	}

	[GlobalCleanup]
	public void Cleanup() {
		_fixture.Dispose();
	}
}
