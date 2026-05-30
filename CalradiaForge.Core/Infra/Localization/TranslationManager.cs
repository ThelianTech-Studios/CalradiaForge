namespace CalradiaForge.Core.Infra.Localization {
	using System;
	using System.Collections.Generic;
	using System.IO;

	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Infra.Paths;
	using CalradiaForge.Core.Models;

	using Newtonsoft.Json;

	/// <summary>
	/// Handles reading the <c>languages.json</c> manifest and individual
	/// language translation files from the <see cref="AppPaths.LanguagesDirectory"/>.
	/// Read-only — never writes or auto-saves.
	/// Analogous to <see cref="Config.AppConfig"/> but without persistence.
	/// </summary>
	public sealed class TranslationManager {

		private readonly object _lock = new();
		private readonly string _languagesDirectory;
		private readonly string _manifestFileName;
		private readonly string _defaultLanguageFileName;
		private static readonly Logger _logger = Logger.Instance;

		/// <summary>
		/// Initializes a new translation manager for the specified language directory.
		/// </summary>
		public TranslationManager(string languagesDirectory, string manifestFilepath, string defaultLangFilepath) {
			if (string.IsNullOrWhiteSpace(languagesDirectory)) {
				throw new ArgumentException("Languages directory path cannot be null or whitespace.", nameof(languagesDirectory));
			}
			if (string.IsNullOrWhiteSpace(manifestFilepath)) {
				throw new ArgumentException("Manifest file path cannot be null or whitespace.", nameof(manifestFilepath));
			}
			if (string.IsNullOrWhiteSpace(defaultLangFilepath)) {
				throw new ArgumentException("Default language file path cannot be null or whitespace.", nameof(defaultLangFilepath));
			}
			_languagesDirectory = languagesDirectory;
			_manifestFileName = manifestFilepath;
			_defaultLanguageFileName = defaultLangFilepath;
		}
		#region Loading Methods
		/// <summary>
		/// Reads the <c>languages.json</c> manifest and returns all declared language options.
		/// Returns an empty list if the file is missing or malformed.
		/// </summary>
		public List<LanguageOption> LoadManifest() {
			string manifestPath = _manifestFileName;
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("TranslationLoader: Loading manifest.", new { ManifestPath = manifestPath });
			}
			if (!File.Exists(manifestPath)) {
				_logger.Warning($"TranslationLoader: Manifest not found at '{manifestPath}'.");
				return [];
			}
			try {
				string json = File.ReadAllText(manifestPath);
				List<LanguageOption>? options = JsonConvert.DeserializeObject<List<LanguageOption>>(json);
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("TranslationLoader: Manifest loaded.", new { ManifestPath = manifestPath, Count = options?.Count ?? 0 });
				}
				return options ?? [];
			} catch (Exception ex) {
				_logger.Error(ex, $"TranslationLoader: Failed to read manifest at '{manifestPath}'.");
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("TranslationLoader: Manifest read failed.", new { ManifestPath = manifestPath }, ex);
				}
				return [];
			}
		}

		/// <summary>
		/// Loads a flat <c>Dictionary&lt;string, string&gt;</c> of translation key-value pairs
		/// from the JSON file matching the given language code (e.g. <c>en-US.json</c>).
		/// Returns an empty dictionary if the file is missing or malformed.
		/// </summary>
		public Dictionary<string, string> LoadLanguageFile(string languageCode) {
			if (string.IsNullOrWhiteSpace(languageCode)) {
				_logger.Warning("TranslationLoader: Language code is null or empty.");
				return new Dictionary<string, string>();
			}
			string filePath = Path.Combine(_languagesDirectory, $"{languageCode}.json");
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("TranslationLoader: Loading language file.", new { FilePath = filePath, LanguageCode = languageCode });
			}
			if (!File.Exists(filePath)) {
				_logger.Warning($"TranslationLoader: Language file not found at '{filePath}'.");
				return new Dictionary<string, string>();
			}

			bool isDefaultFile = IsDefaultLanguageFile(filePath);
			try {
				string json;
				if (isDefaultFile) {
					lock (_lock) {
						json = File.ReadAllText(filePath);
					}
				} else {
					json = File.ReadAllText(filePath);
				}
				Dictionary<string, string>? pairs = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("TranslationLoader: Language file loaded.", new { FilePath = filePath, Count = pairs?.Count ?? 0 });
				}
				return pairs ?? new Dictionary<string, string>();
			} catch (Exception ex) {
				_logger.Error(ex, $"TranslationLoader: Failed to read language file at '{filePath}'.");
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("TranslationLoader: Language file read failed.", new { FilePath = filePath }, ex);
				}
				return new Dictionary<string, string>();
			}
		}
		#endregion
		#region Generate Default Language File
		/// <summary>
		/// Writes the default English translation file (en-US.json).
		/// When targeting the configured default language file path, write is synchronized
		/// against reads/writes via a dedicated lock.
		/// </summary>
		public void DefaultEnglishLanguageFile(Dictionary<string, string> defaultTranslations, string filePath) {
			if (defaultTranslations is null) {
				throw new ArgumentException("FilePath Cannot be Null", nameof(defaultTranslations));
			}
			if (string.IsNullOrWhiteSpace(filePath)) {
				throw new ArgumentException("FilePath Cannot be Null or Empty", nameof(filePath));
			}
			void WriteFile() {
				string json = JsonConvert.SerializeObject(defaultTranslations, Formatting.Indented);
				File.WriteAllText(filePath, json);
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("TranslationLoader: Default language file generated.", new { FilePath = filePath, Count = defaultTranslations.Count });
				}
			}

			bool isDefaultFile = IsDefaultLanguageFile(filePath);
			try {
				if (isDefaultFile) {
					lock (_lock) {
						WriteFile();
					}
				}
			} catch (Exception ex) {
				_logger.Error(ex, $"TranslationLoader: Failed to write default language file at '{filePath}'.");
			}
		}
		#endregion
		#region Utility Methods
		/// <summary>
		/// Checks whether a translation file exists on disk for the given language code.
		/// </summary>
		public bool LanguageFileExists(string languageCode) {
			if (string.IsNullOrWhiteSpace(languageCode)) {
				return false;
			}
			return File.Exists(Path.Combine(_languagesDirectory, $"{languageCode}.json"));
		}
		/// <summary>
		/// Determines if the given file path corresponds to the configured default language file.
		/// Used to apply synchronization when reading/writing the default language file.
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		private bool IsDefaultLanguageFile(string filePath) {
			string canidate = Path.GetFullPath(filePath);
			string defaultPath = Path.GetFullPath(_defaultLanguageFileName);
			return string.Equals(canidate, defaultPath, StringComparison.OrdinalIgnoreCase);
		}
		#endregion
	}
}