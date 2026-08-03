namespace CalradiaForge.Core.Infra.Localization {
	using System;
	using System.Collections.Generic;

	using CalradiaForge.Core.Infra.Config;
	using CalradiaForge.Core.Models;

	using Serilog;

	/// <summary>
	/// Orchestrates language loading, switching, and exposes the bindable
	/// <see cref="Strings"/> instance for UI consumption.
	/// Owned as a singleton by <c>App.xaml.cs</c> and passed via dependency injection.
	/// </summary>
	public sealed class TranslationService {
		private readonly TranslationManager _manager;
		private readonly AppSettings _config;

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
		public TranslationService(TranslationManager manager, AppSettings config) {
			_manager = manager ?? throw new ArgumentNullException(nameof(manager));
			_config = config ?? throw new ArgumentNullException(nameof(config));
		}

		/// <summary>
		/// Initializes the service by loading the manifest and applying
		/// the persisted language from <see cref="AppSettings.Language"/>.
		/// Call once during app startup after config is loaded.
		/// </summary>
		public void Initialize() {
			AvailableLanguages = _manager.LoadManifest();
			Log.Information(
				"TranslationService: Loaded {LanguageCount} language(s) from manifest.",
				AvailableLanguages.Count);
			string savedLanguage = _config.Language;
			if (string.IsNullOrWhiteSpace(savedLanguage)) {
				savedLanguage = "en-US";
			}
			Log.Debug(
				"TranslationService: Initial language resolved. SavedLanguage={SavedLanguage}",
				savedLanguage);
			// Validate the saved language has a file on disk
			if (!_manager.LanguageFileExists(savedLanguage)) {
				Log.Warning(
					"TranslationService: Language file for {LanguageCode} not found. Falling back to en-US defaults.",
					savedLanguage);
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
				Log.Warning("TranslationService: Attempted to set null/empty language code.");
				return;
			}

			ActiveLanguageCode = languageCode;
			_config.Language = languageCode;

			Log.Debug(
				"TranslationService: Setting language. LanguageCode={LanguageCode}",
				languageCode);
			Dictionary<string, string> translations = _manager.LoadLanguageFile(languageCode);
			if (translations.Count == 0) {
				Log.Warning(
					"TranslationService: Language file for {LanguageCode} was empty or failed to load. Keeping current strings.",
					languageCode);
				return;
			}

			Strings.Apply(translations);
			Log.Debug(
				"TranslationService: Language applied. LanguageCode={LanguageCode} KeyCount={KeyCount}",
				languageCode,
				translations.Count);
		}
	}
}