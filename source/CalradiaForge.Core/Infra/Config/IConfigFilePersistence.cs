namespace CalradiaForge.Core.Infra.Config;

using CalradiaForge.Core.Infra.Persistence;

/// <summary>Narrow file seam used only by configuration persistence and its tests.</summary>
internal interface IConfigFilePersistence {
	bool FileExists(string path);
	string ReadAllText(string path);
	void WriteAllTextAtomic(string path, string contents);
	void CopyFile(string sourcePath, string destinationPath);
}

internal sealed class ConfigFilePersistence : IConfigFilePersistence {
	public bool FileExists(string path) {
		try {
			_ = File.GetAttributes(path);
			return true;
		} catch (FileNotFoundException) {
			return false;
		} catch (DirectoryNotFoundException) {
			return false;
		}
	}

	public string ReadAllText(string path) => File.ReadAllText(path);

	public void WriteAllTextAtomic(string path, string contents) =>
		AtomicFileWriter.WriteAllText(path, contents);

	public void CopyFile(string sourcePath, string destinationPath) =>
		File.Copy(sourcePath, destinationPath, overwrite: false);
}
