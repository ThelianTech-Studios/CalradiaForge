namespace CalradiaForge.Core.Infra.Mods {
	using System;
	using System.Collections.Generic;
	using System.IO;

	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Models;

	using Newtonsoft.Json;

	/// <summary>
	/// Handles low-level JSON read/write operations for mod cache files.
	/// Thread-safe via lock.
	/// Never contains business logic — only file I/O.
	/// </summary>
	public sealed class ModsData
		{
		private readonly object _lock = new object();
		private readonly string _currentFilePath;
		private readonly string _backupFilePath;
		private readonly Logger _logger = Logger.Instance;

		public ModsData(string currentFilePath,string backupFilePath) {
			if (string.IsNullOrWhiteSpace(currentFilePath)) {
				throw new ArgumentException("Current file path cannot be null or whitespace.",nameof(currentFilePath));
				}
			if (string.IsNullOrWhiteSpace(backupFilePath)) {
				throw new ArgumentException("Backup file path cannot be null or whitespace.",nameof(backupFilePath));
				}
			_currentFilePath=currentFilePath;
			_backupFilePath=backupFilePath;
			EnsureDirectoryExists(_currentFilePath);
			EnsureDirectoryExists(_backupFilePath);
			}
		#region Current Mods Data
		public void SaveCurrent(List<ModuleModel> mods) {
			lock (_lock) {
				var json = JsonConvert.SerializeObject(mods,Formatting.Indented);
				File.WriteAllText(_currentFilePath,json);
				}
			}
		public List<ModuleModel> LoadCurrent() {
			lock (_lock) {
				if (!File.Exists(_currentFilePath)) {
					return new List<ModuleModel>();
					}
				var json = File.ReadAllText(_currentFilePath);
				return JsonConvert.DeserializeObject<List<ModuleModel>>(json)??new List<ModuleModel>();
				}
			}
		#endregion
		#region Old Mods Data (Snapshot for Change Detection)
		public void SaveBackup(List<ModuleModel> mods) {
			lock (_lock) {
				var json = JsonConvert.SerializeObject(mods,Formatting.Indented);
				File.WriteAllText(_backupFilePath,json);
				}
			}
		public List<ModuleModel> LoadBackup() {
			lock (_lock) {
				if (!File.Exists(_backupFilePath)) {
					return new List<ModuleModel>();
					}
				var json = File.ReadAllText(_backupFilePath);
				return JsonConvert.DeserializeObject<List<ModuleModel>>(json)??new List<ModuleModel>();
				}
			}
		#endregion
		#region Rotation
		public void RotateDataFiles() {
			lock (_lock) {
				if (File.Exists(_currentFilePath)) {
					File.Copy(_currentFilePath,_backupFilePath,overwrite: true);
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
					_logger.Info("ModsData: Cache cleared successfully.");
					return true;
					} catch (Exception ex) {
					_logger.Error(ex,"ModsData: Failed to clear cache.");
					return false;
					}
				}
			}

		#endregion

		#region Helpers
		private void EnsureDirectoryExists(string filePath) {
			string? directory = Path.GetDirectoryName(filePath);
			if (directory!=null&&!Directory.Exists(directory)) {
				Directory.CreateDirectory(directory);
				}
			}
		#endregion

		}
	}
