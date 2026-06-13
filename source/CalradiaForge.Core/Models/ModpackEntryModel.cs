namespace CalradiaForge.Core.Models {
	using Newtonsoft.Json;

	/// <summary>
	/// Represents a single module entry inside a modpack's load order.
	/// Contains the minimum data needed to identify and locate the module.
	/// </summary>
	public sealed class ModpackEntryModel {

		/// <summary>
		/// Initializes an empty modpack entry.
		/// </summary>
		public ModpackEntryModel() { }

		/// <summary>
		/// Initializes a modpack entry with module metadata.
		/// </summary>
		public ModpackEntryModel(string moduleId, string moduleName, string requiredVersion, string? moduleUrl = null) {
			ModuleId = moduleId;
			ModuleName = moduleName;
			RequiredVersion = requiredVersion;
			ModuleURL = moduleUrl;
		}

		/// <summary>
		/// Module identifier used to match against installed <see cref="ModuleModel.ModuleId"/>.
		/// </summary>
		[JsonProperty("module_id")]
		public string ModuleId { get; set; } = string.Empty;

		/// <summary>
		/// Human-readable module name for display even when the mod is not installed.
		/// </summary>
		[JsonProperty("module_name")]
		public string ModuleName { get; set; } = string.Empty;

		/// <summary>
		/// Version of the module at the time the modpack was saved.
		/// </summary>
		[JsonProperty("required_version")]
		public string RequiredVersion { get; set; } = string.Empty;

		/// <summary>
		/// Optional URL pointing to a download or information page for the module.
		/// </summary>
		[JsonProperty("module_url")]
		public string? ModuleURL { get; set; }

		/// <summary>
		/// Creates a deep copy of this entry.
		/// </summary>
		public ModpackEntryModel Clone() {
			return new ModpackEntryModel {
				ModuleId = ModuleId,
				ModuleName = ModuleName,
				RequiredVersion = RequiredVersion,
				ModuleURL = ModuleURL
			};
		}
	}
}
