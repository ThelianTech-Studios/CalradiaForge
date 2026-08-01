namespace CalradiaForge.Benchmarks.Core.RealInstallation;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Models;

using Serilog;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0, launchCount: 1, warmupCount: 3, iterationCount: 7)]
[BenchmarkCategory("Phase9", "OwnerRealInstallation", "ReadOnly", "WarmCache", "Parser")]
public class RealInstallationModParserBenchmarks {
	private IReadOnlyList<ModuleDescriptor> _localDescriptors = [];
	private IReadOnlyList<ModuleDescriptor> _workshopDescriptors = [];
	private ILogger _previousLogger = null!;
	private ILogger _silentLogger = null!;

	[GlobalSetup]
	public void Setup() {
		RealInstallationBenchmarkInputs inputs = RealInstallationBenchmarkInputs.Load();
		_localDescriptors = ReadOnlyModuleInventory.Discover(inputs.ModulesRoot);
		_workshopDescriptors = ReadOnlyModuleInventory.Discover(inputs.WorkshopRoot);
		if (_localDescriptors.Count == 0 || _workshopDescriptors.Count == 0) {
			throw new InvalidOperationException("The real-installation parser corpus is empty or incomplete.");
		}

		_previousLogger = Log.Logger;
		_silentLogger = new LoggerConfiguration().MinimumLevel.Fatal().CreateLogger();
		Log.Logger = _silentLogger;
	}

	[Benchmark]
	public ParserBatchResult ParseGameModuleCorpus() => Parse(_localDescriptors);

	[Benchmark]
	public ParserBatchResult ParseWorkshopModuleCorpus() => Parse(_workshopDescriptors);

	[Benchmark]
	public ParserBatchResult ParseCombinedModuleCorpus() => Parse(_localDescriptors.Concat(_workshopDescriptors));

	[GlobalCleanup]
	public void Cleanup() {
		Log.Logger = _previousLogger;
		(_silentLogger as IDisposable)?.Dispose();
	}

	private static ParserBatchResult Parse(IEnumerable<ModuleDescriptor> descriptors) {
		int parsed = 0;
		int dependencies = 0;
		foreach (ModuleDescriptor descriptor in descriptors) {
			ModuleModel? module = ModParser.Parse(descriptor.XmlPath, descriptor.InstallPath);
			if (module is null) {
				continue;
			}
			parsed++;
			dependencies += module.DependencyModules?.Count ?? 0;
		}
		return new ParserBatchResult(parsed, dependencies);
	}
}
