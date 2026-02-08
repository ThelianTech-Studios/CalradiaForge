namespace CalradiaForge.Core.Models {
	using Newtonsoft.Json;
	public class DependenciesModulesModel {

		public DependenciesModulesModel() {
		}

		[JsonProperty("dependency_mod_id")]
		public string DependencyModId { get; set; }
		[JsonProperty("dependency_mod_version")]
		public string DependencyModVersion { get; set; }
		[JsonProperty("is_optional")]
		public bool IsOptional { get; set; }
		[JsonProperty("has_version_requirement")]
		public bool HasVersionRequirement { get; set; }

		public DependenciesModulesModel(string depModId, string depModVersion, bool isOptional, bool hasVersionReq) {
			DependencyModId = depModId;
			DependencyModVersion = depModVersion;
			IsOptional = isOptional;
			HasVersionRequirement = hasVersionReq;
		}
	}
}
