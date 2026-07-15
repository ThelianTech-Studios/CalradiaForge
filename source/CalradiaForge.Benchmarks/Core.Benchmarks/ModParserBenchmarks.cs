namespace CalradiaForge.Benchmarks.Core;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using CalradiaForge.Benchmarks.Core.Support;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Models;

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
[BenchmarkCategory("Phase4", "ProvisionalPreRefactorBaseline", "Component")]
public class ModParserBenchmarks {
	private BenchmarkFixtureDirectory _fixture = null!;
	private string _xmlPath = null!;
	private string _installPath = null!;

	[Params(0, 10, 100)]
	public int DependencyCount { get; set; }

	[GlobalSetup]
	public void Setup() {
		_fixture = new BenchmarkFixtureDirectory();
		_installPath = _fixture.CreateDirectory("Module");
		_xmlPath = Path.Combine(_installPath, "SubModule.xml");
		File.WriteAllText(_xmlPath, BenchmarkFixtureDirectory.ModuleXml("Fixture.Parser", DependencyCount));
	}

	[Benchmark]
	public ModuleModel? ParseModuleXml() {
		return ModParser.Parse(_xmlPath, _installPath);
	}

	[GlobalCleanup]
	public void Cleanup() {
		_fixture.Dispose();
	}
}
