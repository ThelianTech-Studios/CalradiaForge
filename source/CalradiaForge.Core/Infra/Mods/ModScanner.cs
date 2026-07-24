namespace CalradiaForge.Core.Infra.Mods;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Logging;
using CalradiaForge.Core.Models;

/// <summary>
/// Low-level scanner that discovers modules and reports per-root completeness.
/// It does not authorize persistence or publish application state.
/// </summary>
public sealed class ModScanner : IModScanner {
	private static readonly Logger _logger = Logger.Instance;

	public async Task<ModScanResult> ScanAsync(AppSettings config, CancellationToken token = default) {
		ArgumentNullException.ThrowIfNull(config);
		string localRoot = config.ModulesDirectoryPath;
		bool scanWorkshop = config.IsGameFromSteam;
		string workshopRoot = config.SteamWorkshopFolderPath;
		try {
			RootScan local = await ScanRootAsync(localRoot, "local Modules", token);
			RootScan workshop;
			if (!scanWorkshop) {
				workshop = RootScan.NotApplicable();
			} else if (string.IsNullOrWhiteSpace(workshopRoot)) {
				workshop = RootScan.NotConfigured();
			} else {
				workshop = await ScanRootAsync(workshopRoot, "Steam Workshop", token);
			}

			List<string> warnings = [.. local.Diagnostics, .. workshop.Diagnostics];
			List<string> duplicateIds = [];
			Dictionary<string, ModuleModel> accepted = new(StringComparer.OrdinalIgnoreCase);
			foreach (ModuleModel module in local.Modules.Concat(workshop.Modules)) {
				string? moduleId = module.ModuleId?.Trim();
				if (string.IsNullOrWhiteSpace(moduleId)) {
					warnings.Add($"Module at '{module.InstallPath}' has no identifier and was not accepted.");
					continue;
				}
				if (!accepted.TryAdd(moduleId, module)) {
					duplicateIds.Add(accepted.Keys.First(
						acceptedId => string.Equals(acceptedId, moduleId, StringComparison.OrdinalIgnoreCase)));
				}
			}

			IReadOnlyList<ModuleModel> modules = accepted.Values
				.OrderBy(module => module.ModuleId, StringComparer.OrdinalIgnoreCase)
				.ThenBy(module => module.InstallPath, StringComparer.OrdinalIgnoreCase)
				.ToArray();
			IReadOnlyList<string> distinctDuplicates = duplicateIds
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
				.ToArray();
			if (distinctDuplicates.Count > 0) {
				warnings.Add($"Duplicate module identifiers were resolved deterministically: {string.Join(", ", distinctDuplicates)}.");
			}

			return new ModScanResult {
				Modules = modules,
				LocalRoot = local.ToResult(),
				WorkshopRoot = workshop.ToResult(),
				Warnings = warnings,
				DuplicateModuleIds = distinctDuplicates,
				TechnicalDiagnostics = [
					$"Local={local.Status} ({local.Modules.Count})",
					$"Workshop={workshop.Status} ({workshop.Modules.Count})",
					$"Accepted={modules.Count}"
				]
			};
		} catch (OperationCanceledException) when (token.IsCancellationRequested) {
			return new ModScanResult {
				IsCancelled = true,
				LocalRoot = new ModScanRootResult { Status = ModScanRootStatus.Cancelled },
				WorkshopRoot = new ModScanRootResult { Status = ModScanRootStatus.Cancelled },
				Warnings = ["Module scan was cancelled."]
			};
		}
	}

	/// <summary>
	/// Compatibility discovery API. Pipeline callers must use <see cref="ScanAsync"/>
	/// so they can evaluate completeness before committing results.
	/// </summary>
	public static async Task<List<ModuleModel>> ScanForModsAsync(
		AppSettings config,
		CancellationToken token = default,
		List<ModuleModel>? allMods = null) {
		ModScanResult result = await new ModScanner().ScanAsync(config, token);
		allMods ??= [];
		allMods.AddRange(result.Modules);
		return allMods;
	}

	private static Task<RootScan> ScanRootAsync(string path, string description, CancellationToken token) {
		return Task.Run(() => ScanRoot(path, description, token), token);
	}

	private static RootScan ScanRoot(string path, string description, CancellationToken token) {
		if (string.IsNullOrWhiteSpace(path)) {
			return new RootScan(ModScanRootStatus.Missing, [], [$"The configured {description} root is missing or invalid: '{path}'."]);
		}

		try {
			List<ModuleModel> modules = [];
			List<string> diagnostics = [];
			string[] directories = Directory.GetDirectories(path)
				.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
				.ToArray();
			foreach (string directory in directories) {
				token.ThrowIfCancellationRequested();
				string? xmlPath = FindModuleXml(directory);
				if (xmlPath is null) {
					continue;
				}
				ModuleModel? module = ModParser.Parse(xmlPath, Path.GetDirectoryName(xmlPath) ?? directory);
				if (module is null || string.IsNullOrWhiteSpace(module.ModuleId)) {
					diagnostics.Add($"Failed to parse module metadata at '{xmlPath}'.");
					continue;
				}
				if (module.IsSinglePlayerMod) {
					modules.Add(module);
				}
			}

			ModScanRootStatus status = diagnostics.Count == 0
				? ModScanRootStatus.Succeeded
				: ModScanRootStatus.Partial;
			return new RootScan(status, modules, diagnostics);
		} catch (DirectoryNotFoundException ex) {
			_logger.Error(ex, $"ModScanner: {description} root '{path}' is missing.");
			return new RootScan(ModScanRootStatus.Missing, [], [$"The configured {description} root is missing: '{path}'."]);
		} catch (UnauthorizedAccessException ex) {
			_logger.Error(ex, $"ModScanner: Cannot access {description} root '{path}'.");
			return new RootScan(ModScanRootStatus.Inaccessible, [], [$"The configured {description} root is inaccessible."]);
		} catch (IOException ex) {
			_logger.Error(ex, $"ModScanner: Cannot enumerate {description} root '{path}'.");
			return new RootScan(ModScanRootStatus.Inaccessible, [], [$"The configured {description} root could not be enumerated."]);
		} catch (OperationCanceledException) {
			throw;
		} catch (Exception ex) {
			_logger.Error(ex, $"ModScanner: Unexpected failure scanning {description} root '{path}'.");
			return new RootScan(ModScanRootStatus.Failed, [], [$"The configured {description} root failed unexpectedly."]);
		}
	}

	private static string? FindModuleXml(string directory) {
		string direct = Path.Combine(directory, "SubModule.xml");
		if (File.Exists(direct)) {
			return direct;
		}
		try {
			foreach (string inner in Directory.GetDirectories(directory).OrderBy(value => value, StringComparer.OrdinalIgnoreCase)) {
				string nested = Path.Combine(inner, "SubModule.xml");
				if (File.Exists(nested)) {
					return nested;
				}
			}
			return null;
		} catch (Exception ex) when (ex is UnauthorizedAccessException or IOException) {
			throw new IOException($"Failed to inspect module directory '{directory}'.", ex);
		}
	}

	private sealed record RootScan(
		ModScanRootStatus Status,
		IReadOnlyList<ModuleModel> Modules,
		IReadOnlyList<string> Diagnostics) {
		public static RootScan NotApplicable() => new(ModScanRootStatus.NotApplicable, [], []);
		public static RootScan NotConfigured() => new(ModScanRootStatus.NotConfigured, [], []);
		public ModScanRootResult ToResult() => new() {
			Status = Status,
			ModuleCount = Modules.Count,
			Diagnostics = Diagnostics
		};
	}
}
