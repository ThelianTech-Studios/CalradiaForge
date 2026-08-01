namespace CalradiaForge.Benchmarks.Core;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using CalradiaForge.Benchmarks.Core.Support;
using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Infra.Paths;
using CalradiaForge.Core.Models;

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
[BenchmarkCategory("Phase9", "AuthoritativePostPhase8Baseline", "EndToEndFilesystem")]
public class ModScannerBenchmarks {
	private BenchmarkFixtureDirectory _fixture = null!;
	private AppSettings _settings = null!;

	[Params(10, 100)]
	public int ModuleCount { get; set; }

	[GlobalSetup]
	public void Setup() {
		_fixture = new BenchmarkFixtureDirectory();
		string gameRoot = _fixture.CreateDirectory("Game");
		string modulesRoot = _fixture.CreateDirectory("Game", "Modules");
		for (int index = 0; index < ModuleCount; index++) {
			_fixture.WriteModule(modulesRoot, index);
		}
		_settings = new AppSettings(new ConfigFileManager(_fixture.GetPath("config.json"))) {
			GameFolderPath = gameRoot,
			GameProvider = GameProvider.StandAlone
		};
	}

	[Benchmark]
	public Task<List<ModuleModel>> ScanModules() {
		return ModScanner.ScanForModsAsync(_settings);
	}

	[GlobalCleanup]
	public void Cleanup() {
		_fixture.Dispose();
	}
}
