namespace CalradiaForge.Core.Infra.Modpacks {
	using System;
	using System.Collections.Generic;

	using CalradiaForge.Core.Infra.Persistence;
	using CalradiaForge.Core.Models;

	using Newtonsoft.Json;
	using Serilog;

	/// <summary>
	/// Handles low-level JSON read/write operations for modpack files and the last-used data file.
	/// Thread-safe via lock. Mirrors the <see cref="Mods.ModsData"/> pattern.
	/// Never contains business logic — only file I/O.
	/// </summary>
	public sealed class ModpackData {
		private readonly object _lock = new();
		private readonly string _modpacksDirectory;
		private readonly string _lastUsedFilePath;

		/// <summary>
		/// Creates a new <see cref="ModpackData"/> instance.
		/// </summary>
		/// <param name="modpacksDirectory">Directory where modpack .json files are stored.</param>
		/// <param name="lastUsedFilePath">Full path to the last_used_mods.data file.</param>
		public ModpackData(string modpacksDirectory, string lastUsedFilePath) {
			if (string.IsNullOrWhiteSpace(modpacksDirectory)) {
				throw new ArgumentException("Modpacks directory path cannot be null or whitespace.", nameof(modpacksDirectory));
			}
			if (string.IsNullOrWhiteSpace(lastUsedFilePath)) {
				throw new ArgumentException("Last used file path cannot be null or whitespace.", nameof(lastUsedFilePath));
			}
			_modpacksDirectory = modpacksDirectory;
			_lastUsedFilePath = lastUsedFilePath;
		}

		#region Modpack CRUD

		/// <summary>
		/// Saves a modpack to a JSON file. The filename is derived from the modpack's name via sanitization.
		/// Overwrites the file if it already exists.
		/// </summary>
		/// <param name="modpack">The modpack to persist.</param>
		/// <returns><c>true</c> when saved successfully; <c>false</c> on error.</returns>
		public bool SaveModpack(ModpackModel modpack) {
			lock (_lock) {
				try {
					string filePath = ModpackFileHelper.GetModpackFilePath(_modpacksDirectory, modpack.ModpackName);
					modpack.FileName = ModpackFileHelper.SanitizeFileName(modpack.ModpackName);
					Log.Debug(
						"ModpackData: Saving modpack {ModpackName} with {EntryCount} entries to {FilePath}.",
						modpack.ModpackName,
						modpack.LoadOrder.Count,
						filePath);
					string json = JsonConvert.SerializeObject(modpack, Formatting.Indented);
					AtomicFileWriter.WriteAllText(filePath, json);
					Log.Information("ModpackData: Saved modpack {ModpackName} to {FilePath}.", modpack.ModpackName, filePath);
					return true;
				} catch (Exception ex) {
					Log.Error(ex, "ModpackData: Failed to save modpack {ModpackName}.", modpack.ModpackName);
					return false;
				}
			}
		}

		/// <summary>
		/// Loads a single modpack from a JSON file path.
		/// </summary>
		/// <param name="filePath">Full path to the .json modpack file.</param>
		/// <returns>The parsed <see cref="ModpackModel"/>, or <c>null</c> if parsing fails.</returns>
		public ModpackModel? LoadModpack(string filePath) {
			lock (_lock) {
				try {
					if (!File.Exists(filePath)) {
						Log.Warning("ModpackData: Modpack file not found: {FilePath}.", filePath);
						Log.Debug("ModpackData: Load failed because {FilePath} is missing.", filePath);
						return null;
					}
					string json = File.ReadAllText(filePath);
					ModpackModel? modpack = JsonConvert.DeserializeObject<ModpackModel>(json);
					if (modpack is not null) {
						modpack.FileName = Path.GetFileNameWithoutExtension(filePath);
					}
					Log.Debug(
						"ModpackData: Loaded modpack {ModpackName} with {EntryCount} entries from {FilePath}.",
						modpack?.ModpackName,
						modpack?.LoadOrder.Count ?? 0,
						filePath);
					return modpack;
				} catch (Exception ex) {
					Log.Error(ex, "ModpackData: Failed to load modpack from {FilePath}.", filePath);
					return null;
				}
			}
		}

		/// <summary>
		/// Loads all modpack JSON files from the modpacks directory.
		/// Invalid or unparsable files are skipped.
		/// </summary>
		/// <returns>List of all successfully parsed modpacks.</returns>
		public List<ModpackModel> LoadAllModpacks() {
			lock (_lock) {
				List<ModpackModel> modpacks = [];
				try {
					if (!Directory.Exists(_modpacksDirectory)) {
						return modpacks;
					}
					string[] files = Directory.GetFiles(_modpacksDirectory, "*.json");
					foreach (string file in files) {
						try {
							string json = File.ReadAllText(file);
							ModpackModel? modpack = JsonConvert.DeserializeObject<ModpackModel>(json);
							if (modpack is not null) {
								modpack.FileName = Path.GetFileNameWithoutExtension(file);
								modpacks.Add(modpack);
							}
						} catch (Exception ex) {
							Log.Warning(ex, "ModpackData: Skipping invalid modpack file {FilePath}.", file);
							Log.Debug(ex, "ModpackData: Invalid modpack skipped at {FilePath}.", file);
						}
					}
				} catch (Exception ex) {
					Log.Error(ex, "ModpackData: Failed to enumerate modpack directory.");
				}
				Log.Information(
					"ModpackData: Loaded {ModpackCount} modpack(s) from {ModpacksDirectory}.",
					modpacks.Count,
					_modpacksDirectory);
				Log.Debug(
					"ModpackData: Load all complete with {ModpackCount} modpack(s) from {ModpacksDirectory}.",
					modpacks.Count,
					_modpacksDirectory);
				return modpacks;
			}
		}

		/// <summary>
		/// Deletes a modpack file from disk by its sanitized filename.
		/// </summary>
		/// <param name="sanitizedFileName">Sanitized filename (without extension).</param>
		/// <returns><c>true</c> when deleted; <c>false</c> if the file was not found or an error occurred.</returns>
		public bool DeleteModpack(string sanitizedFileName) {
			lock (_lock) {
				try {
					string filePath = ModpackFileHelper.GetModpackFilePathFromFileName(_modpacksDirectory, sanitizedFileName);
					if (!File.Exists(filePath)) {
						Log.Warning("ModpackData: Cannot delete — file not found: {FilePath}.", filePath);
						return false;
					}
					File.Delete(filePath);
					Log.Information("ModpackData: Deleted modpack file {FilePath}.", filePath);
					return true;
				} catch (Exception ex) {
					Log.Error(ex, "ModpackData: Failed to delete modpack {SanitizedFileName}.", sanitizedFileName);
					return false;
				}
			}
		}

		/// <summary>
		/// Checks whether a modpack file already exists for the given display name.
		/// </summary>
		/// <param name="modpackName">The raw display name to check.</param>
		/// <returns><c>true</c> if a file already exists for this name.</returns>
		public bool ModpackExists(string modpackName) {
			lock (_lock) {
				string filePath = ModpackFileHelper.GetModpackFilePath(_modpacksDirectory, modpackName);
				return File.Exists(filePath);
			}
		}

		#endregion

		#region Last Used

		/// <summary>
		/// Saves the last-used mod load order to <c>last_used_mods.data</c> in JSON format.
		/// </summary>
		/// <param name="modpack">The modpack representing the last-used load order.</param>
		/// <returns><c>true</c> when saved successfully; <c>false</c> on error.</returns>
		public bool SaveLastUsed(ModpackModel modpack) {
			lock (_lock) {
				try {
					string json = JsonConvert.SerializeObject(modpack, Formatting.Indented);
					AtomicFileWriter.WriteAllText(_lastUsedFilePath, json);
					Log.Information("ModpackData: Saved last-used load order.");
					Log.Debug(
						"ModpackData: Saved last-used file with {EntryCount} entries to {FilePath}.",
						modpack.LoadOrder.Count,
						_lastUsedFilePath);
					return true;
				} catch (Exception ex) {
					Log.Error(ex, "ModpackData: Failed to save last-used load order.");
					return false;
				}
			}
		}

		/// <summary>
		/// Loads the last-used mod load order from <c>last_used_mods.data</c>.
		/// </summary>
		/// <returns>The last-used <see cref="ModpackModel"/>, or <c>null</c> if the file does not exist or parsing fails.</returns>
		public ModpackModel? LoadLastUsed() {
			lock (_lock) {
				try {
					if (!File.Exists(_lastUsedFilePath)) {
						Log.Debug("ModpackData: Last-used file is missing at {FilePath}.", _lastUsedFilePath);
						return null;
					}
					string json = File.ReadAllText(_lastUsedFilePath);
					ModpackModel? modpack = JsonConvert.DeserializeObject<ModpackModel>(json);
					Log.Debug(
						"ModpackData: Loaded last-used file with {EntryCount} entries from {FilePath}.",
						modpack?.LoadOrder.Count ?? 0,
						_lastUsedFilePath);
					return modpack;
				} catch (Exception ex) {
					Log.Error(ex, "ModpackData: Failed to load last-used load order.");
					return null;
				}
			}
		}

		#endregion

		#region Import

		/// <summary>
		/// Imports a modpack from an external JSON file path.
		/// This reads the file without copying it — the caller is responsible for
		/// saving it into the modpacks directory via <see cref="SaveModpack"/> if desired.
		/// </summary>
		/// <param name="importFilePath">Full path to the external .json file.</param>
		/// <returns>The parsed <see cref="ModpackModel"/>, or <c>null</c> if parsing fails.</returns>
		public ModpackModel? ImportFromFile(string importFilePath) {
			lock (_lock) {
				try {
					if (!File.Exists(importFilePath)) {
						Log.Warning("ModpackData: Import file not found: {ImportFilePath}.", importFilePath);
						return null;
					}
					if (!importFilePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) {
						Log.Warning("ModpackData: Import file is not a .json file: {ImportFilePath}.", importFilePath);
						return null;
					}
					string json = File.ReadAllText(importFilePath);
					ModpackModel? modpack = JsonConvert.DeserializeObject<ModpackModel>(json);
					if (modpack is null || string.IsNullOrWhiteSpace(modpack.ModpackName)) {
						Log.Warning("ModpackData: Import file has invalid or missing modpack data: {ImportFilePath}.", importFilePath);
						return null;
					}
					Log.Information(
						"ModpackData: Successfully imported modpack {ModpackName} from {ImportFilePath}.",
						modpack.ModpackName,
						importFilePath);
					Log.Debug(
						"ModpackData: Imported modpack {ModpackName} with {EntryCount} entries from {ImportFilePath}.",
						modpack.ModpackName,
						modpack.LoadOrder.Count,
						importFilePath);
					return modpack;
				} catch (Exception ex) {
					Log.Error(ex, "ModpackData: Failed to import modpack from {ImportFilePath}.", importFilePath);
					return null;
				}
			}
		}
		#endregion
		#region Helpers

		//Region for helper methods, if any, can be added here.

		#endregion
	}
}
