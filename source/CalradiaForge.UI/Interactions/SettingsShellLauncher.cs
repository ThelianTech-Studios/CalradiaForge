namespace CalradiaForge.UI.Interactions;

using CalradiaForge.Core.Infra.Paths;

/// <summary>Adapts the existing operating-system folder launcher.</summary>
public sealed class SettingsShellLauncher : ISettingsShellLauncher {
	public bool OpenFolder(string folderPath) => ExplorerHelper.OpenFolder(folderPath);
}
