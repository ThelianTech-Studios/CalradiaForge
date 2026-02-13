namespace CalradiaForge.Core.Infra.Modpacks
	{
	using System;
	using System.Text.RegularExpressions;

	/// <summary>
	/// Pure static helper for modpack filename sanitization and path resolution.
	/// Stateless — receives all required data as parameters.
	/// </summary>
	public static partial class ModpackFileHelper
		{
		/// <summary>
		/// Characters allowed in sanitized filenames: letters, digits, hyphen, underscore, period.
		/// Everything else is replaced with underscore.
		/// </summary>
		/// <remarks>
		/// Replaced characters include: space, &lt; &gt; : " / \ | ? * ! @ # $ % ^ &amp; + = ` ~ { } [ ] ( ) ; ' , and others.
		/// Leading/trailing periods and underscores are trimmed.
		/// Consecutive underscores are collapsed to a single underscore.
		/// </remarks>
		private static readonly Regex _invalidCharsRegex = InvalidFilenameCharsRegex();

		private static readonly Regex _consecutiveUnderscores = ConsecutiveUnderscoresRegex();

		/// <summary>
		/// Sanitizes a modpack display name into a safe filesystem filename (without extension).
		/// </summary>
		/// <param name="modpackName">The raw display name entered by the user.</param>
		/// <returns>A filesystem-safe filename string. Returns "unnamed_modpack" if the result would be empty.</returns>
		public static string SanitizeFileName(string modpackName) {
			if (string.IsNullOrWhiteSpace(modpackName)) {
				return "unnamed_modpack";
				}
			string sanitized = _invalidCharsRegex.Replace(modpackName,"_");
			sanitized=_consecutiveUnderscores.Replace(sanitized,"_");
			sanitized=sanitized.Trim('_','.');
			if (string.IsNullOrWhiteSpace(sanitized)) {
				return "unnamed_modpack";
				}
			return sanitized;
			}

		/// <summary>
		/// Builds the full file path for a modpack JSON file given the directory and display name.
		/// </summary>
		/// <param name="modpacksDirectory">The directory where modpack files are stored.</param>
		/// <param name="modpackName">The raw display name of the modpack.</param>
		/// <returns>Full path including the .json extension.</returns>
		public static string GetModpackFilePath(string modpacksDirectory,string modpackName) {
			string fileName = SanitizeFileName(modpackName);
			return Path.Combine(modpacksDirectory,fileName+".json");
			}

		/// <summary>
		/// Builds the full file path for a modpack using a pre-sanitized filename.
		/// </summary>
		/// <param name="modpacksDirectory">The directory where modpack files are stored.</param>
		/// <param name="sanitizedFileName">Already-sanitized filename (without extension).</param>
		/// <returns>Full path including the .json extension.</returns>
		public static string GetModpackFilePathFromFileName(string modpacksDirectory,string sanitizedFileName) {
			return Path.Combine(modpacksDirectory,sanitizedFileName+".json");
			}

		[GeneratedRegex(@"[^a-zA-Z0-9\-_.]")]
		private static partial Regex InvalidFilenameCharsRegex();

		[GeneratedRegex(@"_{2,}")]
		private static partial Regex ConsecutiveUnderscoresRegex();
		}
	}
