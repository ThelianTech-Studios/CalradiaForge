namespace CalradiaForge.Core.Models {
	using System.Collections.Generic;

	using Newtonsoft.Json;

	public sealed class ModLists {

		public ModLists() { }

		public ModLists(string name, string createdBy) {
			ModListName = name;
			CreatedBy = createdBy;
			LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
		}

		[JsonProperty("mod_list_name")]
		public string? ModListName { get; set; }

		[JsonProperty("user_creation_name")]
		public string? CreatedBy { get; set; }

		[JsonProperty("date_updated")]
		public string? LastUpdated { get; set; }

		[JsonProperty("mods_list_items")]
		public List<ModListItem>? ListItems { get; set; }

		public void AddModToList(ModListItem item) {
			if (item != null && !ListItems.Exists(x => x.ModId == item.ModId)) {
				ListItems.Add(item);
				UpdateLastUpdated();

			}
		}

		public void RemoveModFromList(ModListItem item) {
			if (item != null) {
				ListItems.RemoveAll(x => x.ModId == item.ModId);
			}
		}

		public void UpdateLastUpdated() {
			LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
		}

		public ModLists Clone() {
			List<ModListItem> list = new();
			foreach (ModListItem item in ListItems) {
					list.Add(item.Clone());
			}
			return new ModLists {
				ListItems = list,
				ModListName = ModListName,
				CreatedBy = CreatedBy,
				LastUpdated = LastUpdated
			};
		}
	}
}
