namespace CalradiaForge.Benchmarks.Core.Support;

internal sealed class BenchmarkFixtureDirectory : IDisposable {
	public BenchmarkFixtureDirectory() {
		RootPath = Path.Combine(Path.GetTempPath(), "CalradiaForge.Benchmarks", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(RootPath);
	}

	public string RootPath { get; }

	public string GetPath(params string[] segments) {
		return segments.Aggregate(RootPath, Path.Combine);
	}

	public string CreateDirectory(params string[] segments) {
		string path = GetPath(segments);
		Directory.CreateDirectory(path);
		return path;
	}

	public string WriteModule(string parentDirectory, int index, int dependencyCount = 0) {
		string moduleDirectory = Path.Combine(parentDirectory, $"Module{index:D4}");
		Directory.CreateDirectory(moduleDirectory);
		File.WriteAllText(
			Path.Combine(moduleDirectory, "SubModule.xml"),
			ModuleXml($"Fixture.Module.{index:D4}", dependencyCount));
		return moduleDirectory;
	}

	public static string ModuleXml(string moduleId, int dependencyCount = 0) {
		string dependencies = dependencyCount == 0
			? string.Empty
			: $"<DependedModuleMetadatas>{string.Concat(Enumerable.Range(0, dependencyCount).Select(index => $"<DependedModuleMetadata id=\"Dependency.{index:D4}\" version=\"v1.0.0\" optional=\"false\" />"))}</DependedModuleMetadatas>";
		return $"""
			<Module>
			  <Name value="{moduleId}" />
			  <Id value="{moduleId}" />
			  <Version value="v1.0.0" />
			  <SingleplayerModule value="true" />
			  {dependencies}
			</Module>
			""";
	}

	public void Dispose() {
		string expectedRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "CalradiaForge.Benchmarks"))
			.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
			+ Path.DirectorySeparatorChar;
		string resolvedRoot = Path.GetFullPath(RootPath);
		if (!resolvedRoot.StartsWith(expectedRoot, StringComparison.OrdinalIgnoreCase)) {
			throw new InvalidOperationException("Benchmark cleanup refused a directory outside the benchmark-owned temporary root.");
		}
		if (Directory.Exists(RootPath)) {
			Directory.Delete(RootPath, recursive: true);
		}
	}
}
