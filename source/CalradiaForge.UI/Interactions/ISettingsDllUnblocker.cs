namespace CalradiaForge.UI.Interactions;

using CalradiaForge.Core.Models;

/// <summary>Runs the existing Core DLL-unblock workflow for Settings.</summary>
public interface ISettingsDllUnblocker {
	Task<UnblockResult> UnblockAllAsync(
		string directoryPath,
		CancellationToken cancellationToken = default);
}
