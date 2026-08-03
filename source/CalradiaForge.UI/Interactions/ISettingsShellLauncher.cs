namespace CalradiaForge.UI.Interactions;

/// <summary>Opens application data folders selected by Settings commands.</summary>
public interface ISettingsShellLauncher {
	bool OpenFolder(string folderPath);
}
