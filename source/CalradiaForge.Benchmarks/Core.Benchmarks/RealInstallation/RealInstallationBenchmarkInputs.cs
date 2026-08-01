namespace CalradiaForge.Benchmarks.Core.RealInstallation;

internal sealed record RealInstallationBenchmarkInputs(
	string GameRoot,
	string ModulesRoot,
	string WorkshopRoot) {
	private const string GameRootVariable = "CALRADIAFORGE_PHASE9_GAME_ROOT";
	private const string WorkshopRootVariable = "CALRADIAFORGE_PHASE9_WORKSHOP_ROOT";

	public static RealInstallationBenchmarkInputs Load() {
		string gameRoot = RequireDirectory(GameRootVariable, "game-root");
		string modulesRoot = Path.Combine(gameRoot, "Modules");
		if (!Directory.Exists(modulesRoot)
			|| !File.Exists(Path.Combine(modulesRoot, "Native", "SubModule.xml"))) {
			throw new InvalidOperationException("The configured game-root input is not a valid Bannerlord installation.");
		}

		string workshopRoot = RequireDirectory(WorkshopRootVariable, "Workshop-root");
		return new RealInstallationBenchmarkInputs(gameRoot, modulesRoot, workshopRoot);
	}

	private static string RequireDirectory(string variableName, string description) {
		string? value = Environment.GetEnvironmentVariable(variableName);
		if (string.IsNullOrWhiteSpace(value)) {
			throw new InvalidOperationException($"The required {description} benchmark input was not provided.");
		}

		try {
			string normalized = Path.GetFullPath(value);
			if (Directory.Exists(normalized)) {
				return normalized;
			}
		} catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) {
			// The public error below intentionally excludes the supplied path.
		}

		throw new InvalidOperationException($"The configured {description} benchmark input is missing or invalid.");
	}
}
