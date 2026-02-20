namespace CalradiaForge.Core.Infra.Modpacks
	{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Models;

	/// <summary>
	/// Enumerates the available modpack templates used when creating a new modpack.
	/// </summary>
	public enum ModpackTemplate
		{
		/// <summary>
		/// Default vanilla Bannerlord modules only.
		/// </summary>
		Vanilla,

		/// <summary>
		/// BUTR ButterLib framework stack followed by vanilla modules.
		/// </summary>
		ButterLib,

		/// <summary>
		/// Vanilla Bannerlord modules including the WarSails (Naval) DLC.
		/// </summary>
		VanillaWarSails,

		/// <summary>
		/// BUTR ButterLib framework stack followed by vanilla modules including the WarSails (Naval) DLC.
		/// </summary>
		ButterLibWarSails
		}

	/// <summary>
	/// High-level modpack management service.
	/// Orchestrates CRUD operations, import/export, validation, and the "Last Used" list.
	/// Receives dependencies explicitly — never accesses global state.
	/// </summary>
	public sealed class ModpackService
		{
		private readonly ModpackData _modpackData;
		private readonly Logger _logger = Logger.Instance;

		/// <summary>
		/// All loaded modpacks (excluding "Last Used").
		/// Populated by <see cref="LoadAll"/> on startup.
		/// </summary>
		public List<ModpackModel> AllModpacks { get; private set; } = [];

		/// <summary>
		/// The "Last Used" modpack representing the user's most recent ad-hoc load order.
		/// Loaded from <c>last_used_mods.data</c> on startup. May be <c>null</c> if no data exists.
		/// </summary>
		public ModpackModel? LastUsedModpack { get; private set; }

		/// <summary>
		/// The current working load order as modpack entries.
		/// Updated by the UI (ModsPage) whenever the load order changes.
		/// Read by ModpacksPage when saving.
		/// </summary>
		public List<ModpackEntryModel> CurrentLoadOrderEntries { get; set; } = [];

		public ModpackService(ModpackData modpackData) {
			_modpackData=modpackData ?? throw new ArgumentNullException(nameof(modpackData));
			}

		#region Startup

		/// <summary>
		/// Loads all modpacks from disk and the "Last Used" data.
		/// Should be called once during application startup.
		/// </summary>
		public void LoadAll() {
			AllModpacks=_modpackData.LoadAllModpacks();
			LastUsedModpack=_modpackData.LoadLastUsed();
			EnsureDefaultModpackExists();
			_logger.Info($"ModpackService: Loaded {AllModpacks.Count} modpack(s). Last Used: {(LastUsedModpack is not null ? "found" : "none")}");
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModpackService: Load complete.", new { ModpackCount = AllModpacks.Count, HasLastUsed = LastUsedModpack is not null });
			}
			}

		/// <summary>
		/// Ensures the built-in "Vanilla" modpack file exists on disk.
		/// If missing, creates and saves it automatically.
		/// </summary>
		private void EnsureDefaultModpackExists() {
			bool vanillaExists = AllModpacks.Any(
				m => string.Equals(m.ModpackName,VanillaModules.DefaultModpackName,StringComparison.OrdinalIgnoreCase));
			if (!vanillaExists) {
				ModpackModel vanilla = VanillaModules.CreateDefaultModpack();
				_modpackData.SaveModpack(vanilla);
				AllModpacks.Insert(0,vanilla);
				_logger.Info("ModpackService: Created default Vanilla modpack.");
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModpackService: Default modpack created.", new { ModpackName = vanilla.ModpackName, EntryCount = vanilla.LoadOrder.Count });
				}
				}
			}

		#endregion

		#region Refresh

		/// <summary>
		/// Reloads all modpacks from disk. Call after external changes (import, delete, etc.).
		/// </summary>
		public void Refresh() {
			AllModpacks=_modpackData.LoadAllModpacks();
			EnsureDefaultModpackExists();
			_logger.Info($"ModpackService: Refreshed. {AllModpacks.Count} modpack(s) loaded.");
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModpackService: Refresh complete.", new { ModpackCount = AllModpacks.Count });
			}
			}

		#endregion

		#region Save

		/// <summary>
		/// Saves (overwrites) an existing modpack with the current working load order from ModsPage.
		/// Updates the <see cref="ModpackModel.LastUpdated"/> timestamp.
		/// </summary>
		/// <param name="modpack">The modpack to overwrite.</param>
		/// <param name="currentLoadOrder">The current load order entries from ModsPage.</param>
		/// <returns><c>true</c> when saved successfully.</returns>
		public bool Save(ModpackModel modpack,List<ModpackEntryModel> currentLoadOrder) {
			modpack.LoadOrder=currentLoadOrder.Select(e => e.Clone()).ToList();
			modpack.LastUpdated=DateTime.Now.ToString("yyyy-MM-dd");
			bool result = _modpackData.SaveModpack(modpack);
			if (result) {
				Refresh();
				}
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModpackService: Saved modpack.", new { ModpackName = modpack.ModpackName, EntryCount = modpack.LoadOrder.Count, Success = result });
			}
			return result;
			}

		/// <summary>
		/// Creates and saves a new modpack with the given name and the current working load order.
		/// </summary>
		/// <param name="modpackName">Display name for the new modpack.</param>
		/// <param name="createdBy">Author name.</param>
		/// <param name="currentLoadOrder">The current load order entries from ModsPage.</param>
		/// <returns><c>true</c> when saved successfully; <c>false</c> if the name already exists or save fails.</returns>
		public bool SaveAs(string modpackName,string createdBy,List<ModpackEntryModel> currentLoadOrder) {
			if (_modpackData.ModpackExists(modpackName)) {
				_logger.Warning($"ModpackService: Cannot SaveAs — modpack '{modpackName}' already exists.");
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModpackService: SaveAs rejected.", new { ModpackName = modpackName });
				}
				return false;
				}
			ModpackModel newModpack = new(modpackName,createdBy,currentLoadOrder.Select(e => e.Clone()).ToList());
			bool result = _modpackData.SaveModpack(newModpack);
			if (result) {
				Refresh();
				}
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModpackService: SaveAs complete.", new { ModpackName = modpackName, EntryCount = newModpack.LoadOrder.Count, Success = result });
			}
			return result;
			}

		#endregion

		#region Create

		/// <summary>
		/// Creates a new modpack pre-populated with the specified template's load order.
		/// Resolves version data from installed mods when available.
		/// </summary>
		/// <param name="modpackName">Display name for the new modpack.</param>
		/// <param name="createdBy">Author name.</param>
		/// <param name="template">The template to use for the initial load order.</param>
		/// <param name="installedMods">
		/// Currently installed mods for version resolution.
		/// When provided, template entries are populated with live version data.
		/// When <c>null</c>, fallback versions are used.
		/// </param>
		/// <returns><c>true</c> when created and saved successfully.</returns>
		public bool CreateNew(string modpackName, string createdBy, ModpackTemplate template = ModpackTemplate.Vanilla, List<ModuleModel>? installedMods = null) {
			if (_modpackData.ModpackExists(modpackName)) {
				_logger.Warning($"ModpackService: Cannot create — modpack '{modpackName}' already exists.");
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModpackService: Create rejected.", new { ModpackName = modpackName });
				}
				return false;
				}
			List<ModpackEntryModel> defaultOrder = template switch {
				ModpackTemplate.ButterLib => VanillaModules.GetButterLibLoadOrder(installedMods),
				ModpackTemplate.VanillaWarSails => VanillaModules.GetDefaultWarSailsLoadOrder(installedMods),
				ModpackTemplate.ButterLibWarSails => VanillaModules.GetButterLibWarSailsLoadOrder(installedMods),
				_ => VanillaModules.GetDefaultLoadOrder(installedMods)
				};
			ModpackModel newModpack = new(modpackName, createdBy, defaultOrder);
			bool result = _modpackData.SaveModpack(newModpack);
			if (result) {
				Refresh();
				}
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModpackService: Created new modpack.", new { ModpackName = modpackName, Template = template.ToString(), EntryCount = defaultOrder.Count, Success = result });
			}
			return result;
			}
		#endregion

		#region Import / Export

		/// <summary>
		/// Imports a modpack from an external file.
		/// Supports CalradiaForge JSON files (.json) and Novus Launcher preset files (.xml).
		/// Novus presets are automatically converted to the CalradiaForge format.
		/// If a modpack with the same name already exists, the import is rejected.
		/// </summary>
		/// <param name="importFilePath">Full path to the external .json or .xml file.</param>
		/// <returns>
		/// A tuple: (success, modpack, message).
		/// On success, the modpack is saved to disk and added to <see cref="AllModpacks"/>.
		/// </returns>
		public (bool Success, ModpackModel? Modpack, string Message) Import(string importFilePath) {
			if (string.IsNullOrWhiteSpace(importFilePath) || !File.Exists(importFilePath)) {
				return (false, null, "Import file not found.");
			}

			string extension = Path.GetExtension(importFilePath);
			ModpackModel? imported;

			if (string.Equals(extension, ".xml", StringComparison.OrdinalIgnoreCase)) {
				// Novus Launcher preset conversion
				imported = NovusPresetConverter.ConvertFromFile(importFilePath);
				if (imported is null) {
					return (false, null, "Failed to convert Novus preset. Ensure it is a valid Novus Launcher XML preset file.");
				}
			} else if (string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase)) {
				// Native CalradiaForge modpack
				imported = _modpackData.ImportFromFile(importFilePath);
				if (imported is null) {
					return (false, null, "Failed to read the modpack file. Ensure it is a valid CalradiaForge modpack JSON file.");
				}
			} else {
				return (false, null, $"Unsupported file type '{extension}'. Use .json (CalradiaForge) or .xml (Novus Launcher).");
			}

			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModpackService: Import parsed.", new { ImportFilePath = importFilePath, Extension = extension, ModpackName = imported?.ModpackName });
			}
			if (_modpackData.ModpackExists(imported.ModpackName)) {
				return (false, imported, $"A modpack named '{imported.ModpackName}' already exists.");
			}

			bool saved = _modpackData.SaveModpack(imported);
			if (!saved) {
				return (false, imported, "Failed to save the imported modpack.");
			}

			Refresh();
			return (true, imported, $"Successfully imported '{imported.ModpackName}'.");
			}

		/// <summary>
		/// Exports a modpack to an external file path chosen by the user.
		/// </summary>
		/// <param name="modpack">The modpack to export.</param>
		/// <param name="targetFilePath">The full path chosen via SaveFileDialog.</param>
		/// <returns><c>true</c> when the export succeeded.</returns>
		public bool Export(ModpackModel modpack,string targetFilePath) {
			try {
				string json = Newtonsoft.Json.JsonConvert.SerializeObject(modpack,Newtonsoft.Json.Formatting.Indented);
				File.WriteAllText(targetFilePath,json);
				_logger.Info($"ModpackService: Exported '{modpack.ModpackName}' to '{targetFilePath}'");
				return true;
				}
			catch (Exception ex) {
				_logger.Error(ex,$"ModpackService: Failed to export '{modpack.ModpackName}'");
				return false;
				}
			}

		#endregion

		#region Last Used

		/// <summary>
		/// Saves the current working load order as the "Last Used" data.
		/// Called before game launch and on application shutdown.
		/// </summary>
		/// <param name="currentLoadOrder">The current load order entries from ModsPage.</param>
		/// <returns><c>true</c> when saved successfully.</returns>
		public bool SaveLastUsed(List<ModpackEntryModel> currentLoadOrder) {
			ModpackModel lastUsed = new() {
				ModpackName="Last Used",
				CreatedBy="User",
				LastUpdated=DateTime.Now.ToString("yyyy-MM-dd"),
				LoadOrder=currentLoadOrder.Select(e => e.Clone()).ToList()
				};
			bool result = _modpackData.SaveLastUsed(lastUsed);
			if (result) {
				LastUsedModpack=lastUsed;
				}
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModpackService: Saved last used load order.", new { EntryCount = lastUsed.LoadOrder.Count, Success = result });
			}
			return result;
			}

		public void LoadLastUsedFromDisk() {
			LastUsedModpack=_modpackData.LoadLastUsed();
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModpackService: Loaded last used from disk.", new { HasLastUsed = LastUsedModpack is not null });
			}
			}

		#endregion

		#region Validation

		/// <summary>
		/// Validates a modpack's load order against the currently installed mods.
		/// Returns a filtered list of entries that have matching installed mods,
		/// and a list of entry names that are missing (for toast notifications).
		/// Missing entries are NOT removed from the modpack's saved data.
		/// </summary>
		/// <param name="modpack">The modpack to validate.</param>
		/// <param name="installedMods">Currently installed mods from <see cref="Mods.ModService.CurrentMods"/>.</param>
		/// <returns>
		/// A tuple of (validEntries, missingModNames).
		/// <c>validEntries</c> contains only entries whose ModuleId exists in <paramref name="installedMods"/>.
		/// <c>missingModNames</c> contains display names of entries not found.
		/// </returns>
		public static (List<ModpackEntryModel> ValidEntries, List<string> MissingModNames) ValidateLoadOrder(
			ModpackModel modpack,
			List<ModuleModel> installedMods) {

			HashSet<string> installedIds = new(
				installedMods
					.Where(m => !string.IsNullOrEmpty(m.ModuleId))
					.Select(m => m.ModuleId),
				StringComparer.OrdinalIgnoreCase);

			List<ModpackEntryModel> validEntries = [];
			List<string> missingModNames = [];

			foreach (ModpackEntryModel entry in modpack.LoadOrder) {
				if (installedIds.Contains(entry.ModuleId)) {
					validEntries.Add(entry);
					}
				else {
					missingModNames.Add(entry.ModuleName);
					}
				}
			if (Logger.Instance.MinimumLevel == Logger.LogLevel.Debug) {
				Logger.Instance.Debug("ModpackService: Load order validation.", new { ModpackName = modpack.ModpackName, ValidCount = validEntries.Count, MissingCount = missingModNames.Count, Missing = string.Join(", ", missingModNames) });
			}
			return (validEntries,missingModNames);
			}

		#endregion

		#region Conversion Helpers

		/// <summary>
		/// Converts a list of <see cref="ModuleModel"/> (from ModsPage load order) into
		/// <see cref="ModpackEntryModel"/> entries suitable for saving in a modpack.
		/// </summary>
		/// <param name="modules">The module models from the active load order.</param>
		/// <returns>A list of modpack entry models.</returns>
		public static List<ModpackEntryModel> BuildEntryListFromModules(List<ModuleModel> modules) {
			return modules.Select(m => new ModpackEntryModel {
				ModuleId=m.ModuleId,
				ModuleName=m.ModuleName,
				RequiredVersion=m.ModuleVersion,
				ModuleURL=m.ModuleURL
				}).ToList();
			}

		/// <summary>
		/// Finds the modpack in <see cref="AllModpacks"/> that matches the given name.
		/// </summary>
		/// <param name="modpackName">The display name to search for.</param>
		/// <returns>The matching modpack, or <c>null</c> if not found.</returns>
		public ModpackModel? FindByName(string modpackName) {
			return AllModpacks.FirstOrDefault(
				m => string.Equals(m.ModpackName,modpackName,StringComparison.OrdinalIgnoreCase));
			}

		#endregion
		}
	}
