namespace CalradiaForge.Core.Infra.Mods;

using CalradiaForge.Core.Models;

/// <summary>
/// Injectable Core seam for required Modules-directory DLL maintenance.
/// </summary>
public interface IModuleUnblocker {
	Task<UnblockResult> UnblockModulesAsync(string modulesDirectoryPath, CancellationToken token = default);
}

/// <summary>
/// Production adapter over the existing DLL-unblocking mechanics.
/// </summary>
public sealed class ModuleUnblocker : IModuleUnblocker {
	public Task<UnblockResult> UnblockModulesAsync(
		string modulesDirectoryPath,
		CancellationToken token = default) =>
		DLLUnblocker.UnblockAllAsync(modulesDirectoryPath, token);
}
