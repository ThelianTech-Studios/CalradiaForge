namespace CalradiaForge.Core.Models {
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	/// <summary>
	/// Represents a saved modpack containing an ordered list of modules and metadata.
	/// Modpacks are serialized to JSON files in the Modpacks directory.
	/// The filename on disk is derived from <see cref="ModpackName"/> via sanitization.
	/// </summary>
	public sealed class ModpackModel {

		/// <summary>
		/// Initializes an empty modpack model for serialization.
		/// </summary>
		public ModpackModel() { }

		/// <summary>
		/// Initializes a modpack model with metadata and load order.
		/// </summary>
		public ModpackModel(string modpackName, string createdBy, List<ModpackEntryModel> loadOrder) {
			ModpackName = modpackName;
			CreatedBy = createdBy;
			LastUpdated = DateTime.Now.ToString("yyyy-MM-dd");
			LoadOrder = loadOrder;
		}

		/// <summary>
		/// Human-friendly display name of the modpack (shown in UI lists).
		/// </summary>
		[JsonProperty("modpack_name")]
		public string ModpackName { get; set; } = string.Empty;

		/// <summary>
		/// Identifier or name of the person who created this modpack.
		/// </summary>
		[JsonProperty("created_by")]
		public string CreatedBy { get; set; } = string.Empty;

		/// <summary>
		/// ISO date string of when the modpack was last updated.
		/// </summary>
		[JsonProperty("last_updated")]
		public string LastUpdated { get; set; } = string.Empty;

		/// <summary>
		/// Ordered list of module entries that compose this modpack's load order.
		/// </summary>
		[JsonProperty("load_order")]
		public List<ModpackEntryModel> LoadOrder { get; set; } = [];

		/// <summary>
		/// The sanitized filename used to store this modpack on disk (without extension).
		/// Not serialized — computed at save time by <see cref="Infra.Modpacks.ModpackFileHelper"/>.
		/// </summary>
		[JsonIgnore]
		public string FileName { get; set; } = string.Empty;

		/// <summary>
		/// Creates a deep copy of this modpack including all load order entries.
		/// </summary>
		public ModpackModel Clone() {
			return new ModpackModel {
				ModpackName = ModpackName,
				CreatedBy = CreatedBy,
				LastUpdated = LastUpdated,
				FileName = FileName,
				LoadOrder = LoadOrder.Select(e => e.Clone()).ToList()
			};
		}
	}
}
