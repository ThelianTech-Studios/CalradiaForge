namespace CalradiaForge.Core.Infra.Mods;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Models;

/// <summary>
/// Injectable low-level scanner seam used for deterministic pipeline tests.
/// </summary>
public interface IModScanner {
	Task<ModScanResult> ScanAsync(AppConfigSettings config, CancellationToken token = default);
}
