namespace CalradiaForge.Core.Infra.Localization
	{
	using Newtonsoft.Json;

	/// <summary>
	/// Represents a single available language entry from the <c>languages.json</c> manifest.
	/// Used to populate the language selector ComboBox in Settings.
	/// </summary>
	public sealed class LanguageOption
		{
		[JsonProperty("code")]
		public string Code { get; set; } = string.Empty;

		[JsonProperty("displayName")]
		public string DisplayName { get; set; } = string.Empty;

		/// <summary>
		/// Returns the display name for ComboBox <c>DisplayMemberPath</c> binding.
		/// </summary>
		public override string ToString() => DisplayName;
		}
	}