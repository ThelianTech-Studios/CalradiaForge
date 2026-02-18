namespace CalradiaForge.Core.Models {
	using Newtonsoft.Json;

	/// <summary>
	/// The outcome of a single mod installation attempt.
	/// </summary>
	public enum ModInstallStatus {
		/// <summary>Mod was extracted and placed into the Modules folder successfully.</summary>
		Installed,
		/// <summary>Mod was already installed with the same or newer version. Skipped.</summary>
		Skipped,
		/// <summary>Mod was installed as an upgrade, replacing an older version.</summary>
		Upgraded,
		/// <summary>Mod installation failed due to an error.</summary>
		Failed
	}

	/// <summary>
	/// Result of a single mod archive installation attempt.
	/// </summary>
	public sealed class ModInstallResult {
		[JsonProperty("archive_file_name")]
		public string ArchiveFileName { get; set; } = string.Empty;

		[JsonProperty("mod_id")]
		public string ModuleId { get; set; } = string.Empty;

		[JsonProperty("mod_name")]
		public string ModuleName { get; set; } = string.Empty;

		[JsonProperty("status")]
		public ModInstallStatus Status { get; set; }

		[JsonProperty("message")]
		public string Message { get; set; } = string.Empty;

		[JsonProperty("installed_version")]
		public string InstalledVersion { get; set; } = string.Empty;

		[JsonProperty("previous_version")]
		public string? PreviousVersion { get; set; }

		/// <summary>
		/// Actual number of files extracted from this archive.
		/// Used by the installer to replace the size-based estimate
		/// with a real count for batch-level progress refinement.
		/// Not serialized — only relevant during the install session.
		/// </summary>
		[JsonIgnore]
		public int ExtractedFileCount { get; set; }
	}
}
