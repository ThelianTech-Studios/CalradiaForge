namespace CalradiaForge.Core.Infra.Mods;

using CalradiaForge.Core.Models;

/// <summary>
/// UI-neutral observation boundary for manual archive-install progress and results.
/// </summary>
public interface IModInstallOperationNotificationSource {
	event Action<ModInstallProgress>? InstallProgressChanged;
	event Action<ModInstallOperationResult>? InstallOperationCompleted;
}
