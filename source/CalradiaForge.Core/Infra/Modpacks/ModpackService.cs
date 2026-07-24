namespace CalradiaForge.Core.Infra.Modpacks {
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using CalradiaForge.Core.Models;

	using Serilog;
	using Serilog.Events;

	/// <summary>
	/// Enumerates the available modpack templates used when creating a new modpack.
	/// </summary>
	public enum ModpackTemplate {
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
	public sealed class ModpackService {
		private readonly ModpackData _modpackData;

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

		/// <summary>
		/// Initializes a new modpack service with the provided data layer.
		/// </summary>
		public ModpackService(ModpackData modpackData) {
			_modpackData = modpackData ?? throw new ArgumentNullException(nameof(modpackData));
		}

		#region Startup

		/// <summary>
		/// Loads all modpacks from disk and the "Last Used" data.
		/// Should be called once during application startup.
		/// </summary>
		public void LoadAll() {
			AllModpacks = _modpackData.LoadAllModpacks();
			LastUsedModpack = _modpackData.LoadLastUsed();
			EnsureDefaultModpackExists();
			Log.Information(
				"ModpackService: Loaded {ModpackCount} modpack(s). Last used: {LastUsedState}.",
				AllModpacks.Count,
				LastUsedModpack is not null ? "found" : "none");
			Log.Debug(
				"ModpackService: Load complete with {ModpackCount} modpack(s); last used present: {HasLastUsed}.",
				AllModpacks.Count,
				LastUsedModpack is not null);
		}

		/// <summary>
		/// Ensures the built-in "Vanilla" modpack file exists on disk.
		/// If missing, creates and saves it automatically.
		/// </summary>
		private void EnsureDefaultModpackExists() {
			bool vanillaExists = AllModpacks.Any(
				m => string.Equals(m.ModpackName, VanillaModules.DefaultModpackName, StringComparison.OrdinalIgnoreCase));
			if (!vanillaExists) {
				ModpackModel vanilla = VanillaModules.CreateDefaultModpack();
				_modpackData.SaveModpack(vanilla);
				AllModpacks.Insert(0, vanilla);
				Log.Information("ModpackService: Created default Vanilla modpack.");
				Log.Debug(
					"ModpackService: Default modpack {ModpackName} created with {EntryCount} entries.",
					vanilla.ModpackName,
					vanilla.LoadOrder.Count);
			}
		}

		#endregion

		#region Refresh

		/// <summary>
		/// Reloads all modpacks from disk. Call after external changes (import, delete, etc.).
		/// </summary>
		public void Refresh() {
			AllModpacks = _modpackData.LoadAllModpacks();
			EnsureDefaultModpackExists();
			Log.Information("ModpackService: Refreshed. {ModpackCount} modpack(s) loaded.", AllModpacks.Count);
			Log.Debug("ModpackService: Refresh complete with {ModpackCount} modpack(s).", AllModpacks.Count);
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
		public bool Save(ModpackModel modpack, List<ModpackEntryModel> currentLoadOrder) {
			modpack.LoadOrder = currentLoadOrder.Select(e => e.Clone()).ToList();
			modpack.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd");
			bool result = _modpackData.SaveModpack(modpack);
			if (result) {
				Refresh();
			}
			Log.Debug(
				"ModpackService: Saved modpack {ModpackName} with {EntryCount} entries; success: {Success}.",
				modpack.ModpackName,
				modpack.LoadOrder.Count,
				result);
			return result;
		}

		/// <summary>
		/// Creates and saves a new modpack with the given name and the current working load order.
		/// </summary>
		/// <param name="modpackName">Display name for the new modpack.</param>
		/// <param name="createdBy">Author name.</param>
		/// <param name="currentLoadOrder">The current load order entries from ModsPage.</param>
		/// <returns><c>true</c> when saved successfully; <c>false</c> if the name already exists or save fails.</returns>
		public bool SaveAs(string modpackName, string createdBy, List<ModpackEntryModel> currentLoadOrder) {
			if (_modpackData.ModpackExists(modpackName)) {
				Log.Warning("ModpackService: Cannot SaveAs — modpack {ModpackName} already exists.", modpackName);
				Log.Debug("ModpackService: SaveAs rejected for {ModpackName}.", modpackName);
				return false;
			}
			ModpackModel newModpack = new(modpackName, createdBy, currentLoadOrder.Select(e => e.Clone()).ToList());
			bool result = _modpackData.SaveModpack(newModpack);
			if (result) {
				Refresh();
			}
			Log.Debug(
				"ModpackService: SaveAs complete for {ModpackName} with {EntryCount} entries; success: {Success}.",
				modpackName,
				newModpack.LoadOrder.Count,
				result);
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
				Log.Warning("ModpackService: Cannot create — modpack {ModpackName} already exists.", modpackName);
				Log.Debug("ModpackService: Create rejected for {ModpackName}.", modpackName);
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
			Log.Debug(
				"ModpackService: Created new modpack {ModpackName} from {Template} with {EntryCount} entries; success: {Success}.",
				modpackName,
				template,
				defaultOrder.Count,
				result);
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

			Log.Debug(
				"ModpackService: Import parsed from {ImportFilePath} with extension {Extension}; modpack: {ModpackName}.",
				importFilePath,
				extension,
				imported?.ModpackName);
			if (imported is null) {
				return (false, null, "Failed to read the modpack file.");
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
		public bool Export(ModpackModel modpack, string targetFilePath) {
			try {
				string json = Newtonsoft.Json.JsonConvert.SerializeObject(modpack, Newtonsoft.Json.Formatting.Indented);
				File.WriteAllText(targetFilePath, json);
				Log.Information("ModpackService: Exported {ModpackName} to {TargetFilePath}.", modpack.ModpackName, targetFilePath);
				return true;
			} catch (Exception ex) {
				Log.Error(ex, "ModpackService: Failed to export {ModpackName}.", modpack.ModpackName);
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
				ModpackName = "Last Used",
				CreatedBy = "User",
				LastUpdated = DateTime.Now.ToString("yyyy-MM-dd"),
				LoadOrder = currentLoadOrder.Select(e => e.Clone()).ToList()
			};
			bool result = _modpackData.SaveLastUsed(lastUsed);
			if (result) {
				LastUsedModpack = lastUsed;
			}
			Log.Debug(
				"ModpackService: Saved last-used load order with {EntryCount} entries; success: {Success}.",
				lastUsed.LoadOrder.Count,
				result);
			return result;
		}

		public void LoadLastUsedFromDisk() {
			LastUsedModpack = _modpackData.LoadLastUsed();
			Log.Debug("ModpackService: Loaded last used from disk; present: {HasLastUsed}.", LastUsedModpack is not null);
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
		/// <param name="installedMods">Modules from one accepted pipeline snapshot.</param>
		/// <returns>
		/// A tuple of (validEntries, missingModNames).
		/// <c>validEntries</c> contains only entries whose ModuleId exists in <paramref name="installedMods"/>.
		/// <c>missingModNames</c> contains display names of entries not found.
		/// </returns>
		public static (List<ModpackEntryModel> ValidEntries, List<string> MissingModNames) ValidateLoadOrder(
			ModpackModel modpack,
			IReadOnlyList<ModuleModel> installedMods) {

			HashSet<string> installedIds = new(
				installedMods
					.Where(m => !string.IsNullOrEmpty(m.ModuleId))
					.Select(m => m.ModuleId!),
				StringComparer.OrdinalIgnoreCase);

			List<ModpackEntryModel> validEntries = [];
			List<string> missingModNames = [];

			foreach (ModpackEntryModel entry in modpack.LoadOrder) {
				if (installedIds.Contains(entry.ModuleId)) {
					validEntries.Add(entry);
				} else {
					missingModNames.Add(entry.ModuleName);
				}
			}
			if (Log.IsEnabled(LogEventLevel.Debug)) {
				Log.Debug(
					"ModpackService: Load order validation for {ModpackName}; valid: {ValidCount}; missing: {MissingCount}; missing modules: {MissingModules}.",
					modpack.ModpackName,
					validEntries.Count,
					missingModNames.Count,
					string.Join(", ", missingModNames));
			}
			return (validEntries, missingModNames);
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
				ModuleId = m.ModuleId!,
				ModuleName = m.ModuleName!,
				RequiredVersion = m.ModuleVersion!,
				ModuleURL = m.ModuleURL!
			}).ToList();
		}

		/// <summary>
		/// Finds the modpack in <see cref="AllModpacks"/> that matches the given name.
		/// </summary>
		/// <param name="modpackName">The display name to search for.</param>
		/// <returns>The matching modpack, or <c>null</c> if not found.</returns>
		public ModpackModel? FindByName(string modpackName) {
			return AllModpacks.FirstOrDefault(
				m => string.Equals(m.ModpackName, modpackName, StringComparison.OrdinalIgnoreCase));
		}

		#endregion
	}
}
