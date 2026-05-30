namespace CalradiaForge.Core.Infra.Localization {
	using System;
	using System.Collections.Generic;

	using CalradiaForge.Core.Infra.Config;
	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Models;

	/// <summary>
	/// Orchestrates language loading, switching, and exposes the bindable
	/// <see cref="Strings"/> instance for UI consumption.
	/// Owned as a singleton by <c>App.xaml.cs</c> and passed via dependency injection.
	/// </summary>
	public sealed class TranslationService {
		private static readonly Logger _logger = Logger.Instance;
		private readonly TranslationManager _manager;
		private readonly AppConfigSettings _config;

		/// <summary>
		/// The bindable translation string provider. UI pages bind to properties on this object.
		/// </summary>
		public TranslationStrings Strings { get; } = new();

		/// <summary>
		/// Available languages loaded from the <c>languages.json</c> manifest.
		/// Bind directly to a ComboBox <c>ItemsSource</c>.
		/// </summary>
		public List<LanguageOption> AvailableLanguages { get; private set; } = [];

		/// <summary>
		/// The currently active language code (e.g. <c>"en-US"</c>).
		/// </summary>
		public string ActiveLanguageCode { get; private set; } = "en-US";

		/// <summary>
		/// Initializes a new translation service with the provided manager and configuration.
		/// </summary>
		public TranslationService(TranslationManager manager, AppConfigSettings config) {
			_manager = manager ?? throw new ArgumentNullException(nameof(manager));
			_config = config ?? throw new ArgumentNullException(nameof(config));
		}

		/// <summary>
		/// Initializes the service by loading the manifest and applying
		/// the persisted language from <see cref="AppConfigSettings.Language"/>.
		/// Call once during app startup after config is loaded.
		/// </summary>
		public void Initialize() {
			AvailableLanguages = _manager.LoadManifest();
			_logger.Info($"TranslationService: Loaded {AvailableLanguages.Count} language(s) from manifest.");
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("TranslationService: Manifest loaded.", new { LanguageCount = AvailableLanguages.Count });
			}
			string savedLanguage = _config.Language;
			if (string.IsNullOrWhiteSpace(savedLanguage)) {
				savedLanguage = "en-US";
			}
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("TranslationService: Initial language resolved.", new { SavedLanguage = savedLanguage });
			}
			// Validate the saved language has a file on disk
			if (!_manager.LanguageFileExists(savedLanguage)) {
				_logger.Warning($"TranslationService: Language file for '{savedLanguage}' not found. Falling back to en-US defaults.");
				ActiveLanguageCode = "en-US";
				return;
			}
			SetLanguage(savedLanguage);
		}
		/// <summary>
		/// Switches the active language. Loads the translation file from disk,
		/// applies it to <see cref="Strings"/>, and persists the choice to config.
		/// If the language code is <c>"en-US"</c>, the hardcoded defaults are used
		/// without reading a file (zero-cost for the default language).
		/// </summary>
		public void SetLanguage(string languageCode) {
			if (string.IsNullOrWhiteSpace(languageCode)) {
				_logger.Warning("TranslationService: Attempted to set null/empty language code.");
				return;
			}

			ActiveLanguageCode = languageCode;
			_config.Language = languageCode;

			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("TranslationService: Setting language.", new { LanguageCode = languageCode });
			}
			Dictionary<string, string> translations = _manager.LoadLanguageFile(languageCode);
			if (translations.Count == 0) {
				_logger.Warning($"TranslationService: Language file for '{languageCode}' was empty or failed to load. Keeping current strings.");
				return;
			}

			Strings.Apply(translations);
			_logger.Info($"TranslationService: Applied '{languageCode}' with {translations.Count} key(s).");
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("TranslationService: Language applied.", new { LanguageCode = languageCode, KeyCount = translations.Count });
			}
		}
	}
}