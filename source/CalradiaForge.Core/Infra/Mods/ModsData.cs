namespace CalradiaForge.Core.Infra.Mods {
	using System;
	using System.Collections.Generic;
	using System.IO;

	using CalradiaForge.Core.Infra.Persistence;
	using CalradiaForge.Core.Models;

	using Newtonsoft.Json;
	using Serilog;

	/// <summary>
	/// Handles low-level JSON read/write operations for mod cache files.
	/// Thread-safe via lock.
	/// Never contains business logic — only file I/O.
	/// </summary>
	public sealed class ModsData {
		private readonly object _lock = new object();
		private readonly string _currentFilePath;
		private readonly string _backupFilePath;

		/// <summary>
		/// Initializes a new cache data helper with current and backup file paths.
		/// </summary>
		public ModsData(string currentFilePath, string backupFilePath) {
			if (string.IsNullOrWhiteSpace(currentFilePath)) {
				throw new ArgumentException("Current file path cannot be null or whitespace.", nameof(currentFilePath));
			}
			if (string.IsNullOrWhiteSpace(backupFilePath)) {
				throw new ArgumentException("Backup file path cannot be null or whitespace.", nameof(backupFilePath));
			}
			_currentFilePath = currentFilePath;
			_backupFilePath = backupFilePath;
		}
		#region Current Mods Data
		/// <summary>
		/// Saves the current mods list to disk.
		/// </summary>
		public void SaveCurrent(List<ModuleModel> mods) {
			lock (_lock) {
				Log.Debug("ModsData: Saving {ModuleCount} current mods to {FilePath}.", mods.Count, _currentFilePath);
				var json = JsonConvert.SerializeObject(mods, Formatting.Indented);
				AtomicFileWriter.WriteAllText(_currentFilePath, json);
			}
		}
		/// <summary>
		/// Loads the current mods list from disk.
		/// </summary>
		public List<ModuleModel> LoadCurrent() {
			lock (_lock) {
				if (!File.Exists(_currentFilePath)) {
					Log.Debug("ModsData: Current mods file is missing at {FilePath}.", _currentFilePath);
					return new List<ModuleModel>();
				}
				if (!TryLoadMods(_currentFilePath, "current", out List<ModuleModel> mods)) {
					if (!File.Exists(_backupFilePath) || !TryLoadMods(_backupFilePath, "backup recovery", out mods)) {
						return new List<ModuleModel>();
					}

					try {
						string recoveredJson = JsonConvert.SerializeObject(mods, Formatting.Indented);
						AtomicFileWriter.WriteAllText(_currentFilePath, recoveredJson);
						Log.Warning("ModsData: Recovered current mods from the backup file.");
					} catch (Exception ex) {
						Log.Error(ex, "ModsData: Loaded backup recovery data but failed to repair the current mods file.");
					}
				}
				Log.Debug("ModsData: Loaded {ModuleCount} current mods from {FilePath}.", mods.Count, _currentFilePath);
				return mods;
			}
		}
		#endregion
		#region Old Mods Data (Snapshot for Change Detection)
		/// <summary>
		/// Saves a backup snapshot of the mods list to disk.
		/// </summary>
		public void SaveBackup(List<ModuleModel> mods) {
			lock (_lock) {
				Log.Debug("ModsData: Saving {ModuleCount} backup mods to {FilePath}.", mods.Count, _backupFilePath);
				var json = JsonConvert.SerializeObject(mods, Formatting.Indented);
				AtomicFileWriter.WriteAllText(_backupFilePath, json);
			}
		}
		/// <summary>
		/// Loads the backup mods snapshot from disk.
		/// </summary>
		public List<ModuleModel> LoadBackup() {
			lock (_lock) {
				if (!File.Exists(_backupFilePath)) {
					Log.Debug("ModsData: Backup mods file is missing at {FilePath}.", _backupFilePath);
					return new List<ModuleModel>();
				}
				if (!TryLoadMods(_backupFilePath, "backup", out List<ModuleModel> mods)) {
					return new List<ModuleModel>();
				}
				Log.Debug("ModsData: Loaded {ModuleCount} backup mods from {FilePath}.", mods.Count, _backupFilePath);
				return mods;
			}
		}
		#endregion
		#region Rotation
		/// <summary>
		/// Rotates the current mods file into the backup file.
		/// </summary>
		public void RotateDataFiles() {
			lock (_lock) {
				if (File.Exists(_currentFilePath)) {
					if (!TryLoadMods(_currentFilePath, "current rotation source", out List<ModuleModel> currentMods)) {
						Log.Warning("ModsData: Skipped cache rotation because the current mods file is invalid. Existing backup was preserved.");
						return;
					}

					string json = JsonConvert.SerializeObject(currentMods, Formatting.Indented);
					AtomicFileWriter.WriteAllText(_backupFilePath, json);
					Log.Debug(
						"ModsData: Rotated current file {CurrentFilePath} to backup file {BackupFilePath}.",
						_currentFilePath,
						_backupFilePath);
				}
			}
		}
		#endregion

		#region Cache Management

		/// <summary>
		/// Deletes both the current and backup mod cache files.
		/// Forces a fresh directory scan on next application launch.
		/// Thread-safe — uses the internal lock.
		/// </summary>
		/// <returns><c>true</c> when both files were successfully deleted or did not exist.</returns>
		public bool ClearCache() {
			lock (_lock) {
				try {
					if (File.Exists(_currentFilePath)) {
						File.Delete(_currentFilePath);
					}
					if (File.Exists(_backupFilePath)) {
						File.Delete(_backupFilePath);
					}
					Log.Information("ModsData: Cache cleared successfully.");
					return true;
				} catch (Exception ex) {
					Log.Error(ex, "ModsData: Failed to clear cache.");
					return false;
				}
			}
		}

		#endregion

		#region Helpers

		private bool TryLoadMods(string filePath, string fileDescription, out List<ModuleModel> mods) {
			try {
				string json = File.ReadAllText(filePath);
				List<ModuleModel?>? loadedMods = JsonConvert.DeserializeObject<List<ModuleModel?>>(json);
				if (loadedMods is null || loadedMods.Exists(static mod => mod is null)) {
					throw new JsonSerializationException("The mods cache must be a JSON array containing only module objects.");
				}

				mods = new List<ModuleModel>(loadedMods.Count);
				foreach (ModuleModel? mod in loadedMods) {
					mods.Add(mod!);
				}
				return true;
			} catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException) {
				Log.Error(ex, "ModsData: Failed to load {FileDescription} mods file {FilePath}.", fileDescription, filePath);
				mods = new List<ModuleModel>();
				return false;
			}
		}

		#endregion
	}
}
