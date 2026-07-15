namespace CalradiaForge.Tests.Core.Support;

using System.IO.Compression;

using CalradiaForge.Core.Models;

internal sealed class TestDirectory : IDisposable {
	public TestDirectory() {
		RootPath = Path.Combine(Path.GetTempPath(), "CalradiaForge.Tests", Guid.NewGuid().ToString("N"));
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

	public string WriteModule(
		string parentDirectory,
		string folderName,
		string moduleId,
		bool isSinglePlayer = true,
		string version = "v1.0.0") {
		string moduleDirectory = Path.Combine(parentDirectory, folderName);
		Directory.CreateDirectory(moduleDirectory);
		File.WriteAllText(
			Path.Combine(moduleDirectory, "SubModule.xml"),
			ModuleXml(moduleId, folderName, version, isSinglePlayer));
		return moduleDirectory;
	}

	public string CreateZip(string fileName, params (string EntryName, string Contents)[] entries) {
		string archivePath = GetPath(fileName);
		using ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
		foreach ((string entryName, string contents) in entries) {
			ZipArchiveEntry entry = archive.CreateEntry(entryName);
			using StreamWriter writer = new(entry.Open());
			writer.Write(contents);
		}
		return archivePath;
	}

	public static string ModuleXml(
		string moduleId,
		string moduleName,
		string version = "v1.0.0",
		bool isSinglePlayer = true,
		string dependencies = "") {
		return $"""
			<Module>
			  <Name value="{moduleName}" />
			  <Id value="{moduleId}" />
			  <Version value="{version}" />
			  <SingleplayerModule value="{isSinglePlayer.ToString().ToLowerInvariant()}" />
			  {dependencies}
			</Module>
			""";
	}

	public static ModuleModel Module(string id, string version = "v1.0.0") {
		return new ModuleModel {
			ModuleId = id,
			ModuleName = id,
			ModuleVersion = version,
			ModuleURL = $"https://example.invalid/{id}",
			InstallPath = $"X:\\Fake\\{id}",
			IsSinglePlayerMod = true,
			DependencyModules = []
		};
	}

	public void Dispose() {
		if (Directory.Exists(RootPath)) {
			Directory.Delete(RootPath, recursive: true);
		}
	}
}
