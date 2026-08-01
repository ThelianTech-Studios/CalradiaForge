namespace CalradiaForge.UI.Interactions;

using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Models;

/// <summary>Adapts the static Core DLL-unblock authority for testable UI use.</summary>
public sealed class SettingsDllUnblocker : ISettingsDllUnblocker {
	public Task<UnblockResult> UnblockAllAsync(
		string directoryPath,
		CancellationToken cancellationToken = default) =>
		DLLUnblocker.UnblockAllAsync(directoryPath, cancellationToken);
}
