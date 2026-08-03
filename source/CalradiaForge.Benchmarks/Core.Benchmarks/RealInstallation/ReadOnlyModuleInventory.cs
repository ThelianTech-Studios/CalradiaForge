namespace CalradiaForge.Benchmarks.Core.RealInstallation;

internal static class ReadOnlyModuleInventory {
	public static IReadOnlyList<ModuleDescriptor> Discover(string root) {
		List<ModuleDescriptor> descriptors = [];
		foreach (string directory in Directory.GetDirectories(root)
			.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)) {
			string direct = Path.Combine(directory, "SubModule.xml");
			if (File.Exists(direct)) {
				descriptors.Add(new ModuleDescriptor(direct, directory));
				continue;
			}

			foreach (string inner in Directory.GetDirectories(directory)
				.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)) {
				string nested = Path.Combine(inner, "SubModule.xml");
				if (!File.Exists(nested)) {
					continue;
				}
				descriptors.Add(new ModuleDescriptor(nested, inner));
				break;
			}
		}
		return descriptors;
	}
}

internal readonly record struct ModuleDescriptor(string XmlPath, string InstallPath);

public readonly record struct ParserBatchResult(int ParsedModuleCount, int DependencyCount);
