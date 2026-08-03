namespace CalradiaForge.Benchmarks.Core.RealInstallation;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using CalradiaForge.Benchmarks.Core.Support;
using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Infra.Paths;
using CalradiaForge.Core.Models;

using Serilog;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0, launchCount: 1, warmupCount: 3, iterationCount: 7)]
[BenchmarkCategory("Phase9", "OwnerRealInstallation", "ReadOnly", "WarmCache", "Scanner")]
public class RealInstallationModScannerBenchmarks {
	private BenchmarkFixtureDirectory _fixture = null!;
	private AppSettings _localSettings = null!;
	private AppSettings _workshopSettings = null!;
	private AppSettings _combinedSettings = null!;
	private readonly ModScanner _scanner = new();
	private ILogger _previousLogger = null!;
	private ILogger _silentLogger = null!;

	[GlobalSetup]
	public void Setup() {
		RealInstallationBenchmarkInputs inputs = RealInstallationBenchmarkInputs.Load();
		_fixture = new BenchmarkFixtureDirectory();
		string emptyGameRoot = _fixture.CreateDirectory("EmptyGame");
		_fixture.CreateDirectory("EmptyGame", "Modules");

		_localSettings = CreateSettings("local-config.json", inputs.GameRoot, GameProvider.StandAlone, string.Empty);
		_workshopSettings = CreateSettings("workshop-config.json", emptyGameRoot, GameProvider.Steam, inputs.WorkshopRoot);
		_combinedSettings = CreateSettings("combined-config.json", inputs.GameRoot, GameProvider.Steam, inputs.WorkshopRoot);

		_previousLogger = Log.Logger;
		_silentLogger = new LoggerConfiguration().MinimumLevel.Fatal().CreateLogger();
		Log.Logger = _silentLogger;

		ValidatePreflight(_scanner.ScanAsync(_localSettings).GetAwaiter().GetResult(), requireLocal: true, requireWorkshop: false);
		ValidatePreflight(_scanner.ScanAsync(_workshopSettings).GetAwaiter().GetResult(), requireLocal: false, requireWorkshop: true);
		ValidatePreflight(_scanner.ScanAsync(_combinedSettings).GetAwaiter().GetResult(), requireLocal: true, requireWorkshop: true);
	}

	[Benchmark]
	public Task<ModScanResult> ScanGameModules() => _scanner.ScanAsync(_localSettings);

	[Benchmark]
	public Task<ModScanResult> ScanWorkshopWithEmptySyntheticLocalRoot() => _scanner.ScanAsync(_workshopSettings);

	[Benchmark]
	public Task<ModScanResult> ScanGameModulesAndWorkshop() => _scanner.ScanAsync(_combinedSettings);

	[GlobalCleanup]
	public void Cleanup() {
		Log.Logger = _previousLogger;
		(_silentLogger as IDisposable)?.Dispose();
		_fixture.Dispose();
	}

	private AppSettings CreateSettings(string configName, string gameRoot, GameProvider provider, string workshopRoot) {
		return new AppSettings(new ConfigFileManager(_fixture.GetPath(configName))) {
			GameFolderPath = gameRoot,
			GameProvider = provider,
			SteamWorkshopFolderPath = workshopRoot
		};
	}

	private static void ValidatePreflight(ModScanResult result, bool requireLocal, bool requireWorkshop) {
		bool valid = result.IsComplete
			&& (!requireLocal || result.LocalModuleCount > 0)
			&& (!requireWorkshop || result.WorkshopModuleCount > 0);
		if (!valid) {
			throw new InvalidOperationException(
				$"Real-installation scan preflight failed. LocalStatus={result.LocalRoot.Status}; LocalCount={result.LocalModuleCount}; WorkshopStatus={result.WorkshopRoot.Status}; WorkshopCount={result.WorkshopModuleCount}.");
		}
	}
}
