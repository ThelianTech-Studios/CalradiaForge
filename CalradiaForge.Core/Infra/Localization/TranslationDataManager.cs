namespace CalradiaForge.Core.Infra.Localization
	{
	using System;
	using System.Collections.Generic;
	using System.IO;

	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Infra.Paths;

	using Newtonsoft.Json;

	/// <summary>
	/// Handles reading the <c>languages.json</c> manifest and individual
	/// language translation files from the <see cref="AppPaths.LanguagesDirectory"/>.
	/// Read-only — never writes or auto-saves.
	/// Analogous to <see cref="Config.AppConfig"/> but without persistence.
	/// </summary>
	public sealed class TranslationManager
		{
		private static readonly Logger _logger = Logger.Instance;
		private readonly string _languagesDirectory;
		private const string ManifestFileName = "languages.json";

		public TranslationManager(string languagesDirectory) {
			if (string.IsNullOrWhiteSpace(languagesDirectory)) {
				throw new ArgumentException("Languages directory path cannot be null or whitespace.",nameof(languagesDirectory));
				}
			_languagesDirectory=languagesDirectory;
			}

		/// <summary>
		/// Reads the <c>languages.json</c> manifest and returns all declared language options.
		/// Returns an empty list if the file is missing or malformed.
		/// </summary>
		public List<LanguageOption> LoadManifest() {
			string manifestPath = Path.Combine(_languagesDirectory,ManifestFileName);
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
				return options?? [];
				} catch (Exception ex) {
				_logger.Error(ex,$"TranslationLoader: Failed to read manifest at '{manifestPath}'.");
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
		public Dictionary<string,string> LoadLanguageFile(string languageCode) {
			if (string.IsNullOrWhiteSpace(languageCode)) {
				_logger.Warning("TranslationLoader: Language code is null or empty.");
				return new Dictionary<string,string>();
				}
			string filePath = Path.Combine(_languagesDirectory,$"{languageCode}.json");
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("TranslationLoader: Loading language file.", new { FilePath = filePath, LanguageCode = languageCode });
			}
			if (!File.Exists(filePath)) {
				_logger.Warning($"TranslationLoader: Language file not found at '{filePath}'.");
				return new Dictionary<string,string>();
				}
			try {
				string json = File.ReadAllText(filePath);
				Dictionary<string,string>? pairs = JsonConvert.DeserializeObject<Dictionary<string,string>>(json);
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("TranslationLoader: Language file loaded.", new { FilePath = filePath, Count = pairs?.Count ?? 0 });
				}
				return pairs??new Dictionary<string,string>();
				} catch (Exception ex) {
				_logger.Error(ex,$"TranslationLoader: Failed to read language file at '{filePath}'.");
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("TranslationLoader: Language file read failed.", new { FilePath = filePath }, ex);
				}
				return new Dictionary<string,string>();
				}
			}

		/// <summary>
		/// Checks whether a translation file exists on disk for the given language code.
		/// </summary>
		public bool LanguageFileExists(string languageCode) {
			if (string.IsNullOrWhiteSpace(languageCode)) {
				return false;
				}
			return File.Exists(Path.Combine(_languagesDirectory,$"{languageCode}.json"));
			}
		}
	}