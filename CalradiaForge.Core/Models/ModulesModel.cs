namespace CalradiaForge.Core.Models {
	using System.Collections.Generic;

	using Newtonsoft.Json;
	/// <summary>
	/// Represents a parsed Bannerlord module with metadata and dependencies.
	/// </summary>
	public class ModuleModel {

		/// <summary>
		/// Initializes a module model with the provided metadata.
		/// </summary>
		public ModuleModel(string modName, string modVersion, string modId, string modUrl, string installPath, bool isModSp, List<DependenciesModulesModel> dependencies) {
			ModuleName = modName;
			ModuleVersion = modVersion;
			ModuleId = modId;
			ModuleURL = modUrl;
			InstallPath = installPath;
			IsSinglePlayerMod = isModSp;
			DependencyModules = dependencies;
		}

		/// <summary>
		/// Initializes a blank module model for serialization.
		/// </summary>
		public ModuleModel() { }

		[JsonProperty("mod_name")]
		public string? ModuleName { get; set; }

		[JsonProperty("mod_version")]
		public string? ModuleVersion { get; set; }

		[JsonProperty("mod_id")]
		public string? ModuleId { get; set; }

		[JsonProperty("mod_url")]
		public string? ModuleURL { get; set; }

		// TODO: Placeholder for future advanced load order/sorting features.
		[JsonProperty("install_path")]
		public string? InstallPath { get; set; }

		[JsonProperty("is_singleplayer_mod")]
		public bool IsSinglePlayerMod { get; set; }

		// TODO: Placeholder for future advanced load order/sorting features.
		[JsonProperty("dependency_mods_list")]
		public List<DependenciesModulesModel>? DependencyModules { get; set; }


	}
}
