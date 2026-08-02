namespace CalradiaForge.Tests.UI.ViewModels;

using CalradiaForge.UI.ViewModels;

public sealed class ApplicationVersionTextTests {
	[Fact]
	public void SourceMetadata_HasNoLegacyHardcodedVersion() {
		DirectoryInfo sourceDirectory = FindSourceDirectory();
		string[] metadataFiles = Directory
			.EnumerateFiles(sourceDirectory.FullName, "*.*", SearchOption.AllDirectories)
			.Where(path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
				|| path.EndsWith(".props", StringComparison.OrdinalIgnoreCase)
				|| path.EndsWith(".targets", StringComparison.OrdinalIgnoreCase)
				|| path.EndsWith(".pubxml", StringComparison.OrdinalIgnoreCase))
			.Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
			.ToArray();

		Assert.DoesNotContain(metadataFiles, path => File.ReadAllText(path).Contains("0.12.15", StringComparison.Ordinal));
	}

	[Fact]
	public void FromAssembly_ReadsSharedInformationalVersion() {
		string text = ApplicationVersionText.FromAssembly(typeof(SettingsViewModel).Assembly);

		Assert.Equal("Version 0.14.0-beta", text);
	}

	[Theory]
	[InlineData("0.14.0-beta", "Version 0.14.0-beta")]
	[InlineData("0.14.0-beta+32ead79", "Version 0.14.0-beta")]
	public void Format_PreservesPrereleaseAndOmitsBuildMetadata(
		string informationalVersion,
		string expected) {
		string text = ApplicationVersionText.Format(
			informationalVersion,
			new Version(0, 14, 0, 0));

		Assert.Equal(expected, text);
	}

	[Fact]
	public void Format_UsesNumericAssemblyVersionOnlyWhenInformationalIsUnavailable() {
		string text = ApplicationVersionText.Format(
			informationalVersion: null,
			new Version(0, 14, 0, 0));

		Assert.Equal("Version 0.14.0.0", text);
	}

	[Fact]
	public void Format_HasNoHardcodedFallbackWhenAssemblyVersionsAreUnavailable() {
		string text = ApplicationVersionText.Format(
			informationalVersion: null,
			numericVersion: null);

		Assert.Equal("Version unavailable", text);
	}

	private static DirectoryInfo FindSourceDirectory() {
		DirectoryInfo? current = new(AppContext.BaseDirectory);
		while (current is not null) {
			if (File.Exists(Path.Combine(current.FullName, "Directory.Build.props"))
				&& File.Exists(Path.Combine(current.FullName, "CalradiaForge.slnx"))) {
				return current;
			}
			current = current.Parent;
		}

		throw new DirectoryNotFoundException("Could not locate the source directory from the test output path.");
	}
}
