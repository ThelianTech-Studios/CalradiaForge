namespace CalradiaForge.Core.Infra.Modpacks {
	using System;
	using System.Collections.Generic;

	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Models;

	using Newtonsoft.Json;

	/// <summary>
	/// Handles low-level JSON read/write operations for modpack files and the last-used data file.
	/// Thread-safe via lock. Mirrors the <see cref="Mods.ModsData"/> pattern.
	/// Never contains business logic — only file I/O.
	/// </summary>
	public sealed class ModpackData {
		private readonly object _lock = new();
		private readonly string _modpacksDirectory;
		private readonly string _lastUsedFilePath;
		private readonly Logger _logger = Logger.Instance;

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
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("ModpackData: Saving modpack.", new { ModpackName = modpack.ModpackName, FilePath = filePath, EntryCount = modpack.LoadOrder.Count });
					}
					string json = JsonConvert.SerializeObject(modpack, Formatting.Indented);
					File.WriteAllText(filePath, json);
					_logger.Info($"ModpackData: Saved modpack '{modpack.ModpackName}' to '{filePath}'");
					return true;
				} catch (Exception ex) {
					_logger.Error(ex, $"ModpackData: Failed to save modpack '{modpack.ModpackName}'");
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
						_logger.Warning($"ModpackData: Modpack file not found: '{filePath}'");
						if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
							_logger.Debug("ModpackData: Load failed, file missing.", new { FilePath = filePath });
						}
						return null;
					}
					string json = File.ReadAllText(filePath);
					ModpackModel? modpack = JsonConvert.DeserializeObject<ModpackModel>(json);
					if (modpack is not null) {
						modpack.FileName = Path.GetFileNameWithoutExtension(filePath);
					}
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("ModpackData: Loaded modpack.", new { FilePath = filePath, ModpackName = modpack?.ModpackName, EntryCount = modpack?.LoadOrder.Count ?? 0 });
					}
					return modpack;
				} catch (Exception ex) {
					_logger.Error(ex, $"ModpackData: Failed to load modpack from '{filePath}'");
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
							_logger.Warning($"ModpackData: Skipping invalid modpack file '{file}': {ex.Message}");
							if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
								_logger.Debug("ModpackData: Invalid modpack skipped.", new { FilePath = file, Error = ex.Message });
							}
						}
					}
				} catch (Exception ex) {
					_logger.Error(ex, "ModpackData: Failed to enumerate modpack directory.");
				}
				_logger.Info($"ModpackData: Loaded {modpacks.Count} modpack(s) from '{_modpacksDirectory}'");
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModpackData: Load all complete.", new { ModpackCount = modpacks.Count, Directory = _modpacksDirectory });
				}
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
						_logger.Warning($"ModpackData: Cannot delete — file not found: '{filePath}'");
						return false;
					}
					File.Delete(filePath);
					_logger.Info($"ModpackData: Deleted modpack file '{filePath}'");
					return true;
				} catch (Exception ex) {
					_logger.Error(ex, $"ModpackData: Failed to delete modpack '{sanitizedFileName}'");
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
					File.WriteAllText(_lastUsedFilePath, json);
					_logger.Info("ModpackData: Saved last-used load order.");
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("ModpackData: Saved last-used file.", new { FilePath = _lastUsedFilePath, EntryCount = modpack.LoadOrder.Count });
					}
					return true;
				} catch (Exception ex) {
					_logger.Error(ex, "ModpackData: Failed to save last-used load order.");
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
						if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
							_logger.Debug("ModpackData: Last-used file missing.", new { FilePath = _lastUsedFilePath });
						}
						return null;
					}
					string json = File.ReadAllText(_lastUsedFilePath);
					ModpackModel? modpack = JsonConvert.DeserializeObject<ModpackModel>(json);
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("ModpackData: Loaded last-used file.", new { FilePath = _lastUsedFilePath, EntryCount = modpack?.LoadOrder.Count ?? 0 });
					}
					return modpack;
				} catch (Exception ex) {
					_logger.Error(ex, "ModpackData: Failed to load last-used load order.");
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
						_logger.Warning($"ModpackData: Import file not found: '{importFilePath}'");
						return null;
					}
					if (!importFilePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) {
						_logger.Warning($"ModpackData: Import file is not a .json file: '{importFilePath}'");
						return null;
					}
					string json = File.ReadAllText(importFilePath);
					ModpackModel? modpack = JsonConvert.DeserializeObject<ModpackModel>(json);
					if (modpack is null || string.IsNullOrWhiteSpace(modpack.ModpackName)) {
						_logger.Warning($"ModpackData: Import file has invalid or missing modpack data: '{importFilePath}'");
						return null;
					}
					_logger.Info($"ModpackData: Successfully imported modpack '{modpack.ModpackName}' from '{importFilePath}'");
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("ModpackData: Imported modpack file.", new { ImportFilePath = importFilePath, ModpackName = modpack.ModpackName, EntryCount = modpack.LoadOrder.Count });
					}
					return modpack;
				} catch (Exception ex) {
					_logger.Error(ex, $"ModpackData: Failed to import modpack from '{importFilePath}'");
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
