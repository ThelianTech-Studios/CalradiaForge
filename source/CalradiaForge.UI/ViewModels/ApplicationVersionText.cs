namespace CalradiaForge.UI.ViewModels;

using System.Reflection;

/// <summary>Formats the entry assembly version for the Settings/About surface.</summary>
internal static class ApplicationVersionText {
	public static string FromEntryAssembly() {
		Assembly? entryAssembly = Assembly.GetEntryAssembly();
		return entryAssembly is null
			? Format(informationalVersion: null, numericVersion: null)
			: FromAssembly(entryAssembly);
	}

	internal static string FromAssembly(Assembly assembly) {
		ArgumentNullException.ThrowIfNull(assembly);

		string? informationalVersion = assembly
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
			?.InformationalVersion;
		return Format(informationalVersion, assembly.GetName().Version);
	}

	internal static string Format(
		string? informationalVersion,
		Version? numericVersion) {
		if (!string.IsNullOrWhiteSpace(informationalVersion)) {
			int metadataSeparator = informationalVersion.IndexOf('+');
			string displayVersion = metadataSeparator >= 0
				? informationalVersion[..metadataSeparator]
				: informationalVersion;
			return $"Version {displayVersion}";
		}

		return numericVersion is not null
			? $"Version {numericVersion}"
			: "Version unavailable";
	}
}
