namespace CalradiaForge.Core.Models {
	using Newtonsoft.Json;

	public sealed class ModListItem {

		[JsonProperty("mod_id")]
		public string? ModId { get; set; }

		[JsonProperty("required_version")]
		public string? RequiredVersion { get; set; }

		[JsonProperty("url")]
		public string? Url { get; set; }

		public ModListItem() { }

		public ModListItem(string modId, string requiredVersion = "", string url = "") {
			ModId = modId;
			RequiredVersion = requiredVersion;
			Url = url;
		}

		public ModListItem Clone() {
			return new ModListItem {
				ModId = this.ModId,
				RequiredVersion = this.RequiredVersion,
				Url = this.Url
			};
		}
	}
}
