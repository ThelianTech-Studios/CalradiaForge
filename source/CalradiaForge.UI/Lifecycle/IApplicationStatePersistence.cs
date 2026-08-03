namespace CalradiaForge.UI.Lifecycle;

using CalradiaForge.Core.Infra.Modpacks;

/// <summary>
/// Persists the application state that is authorized after workflow quiescence.
/// </summary>
public interface IApplicationStatePersistence {
	void Persist();
}

/// <summary>
/// Persists the current last-used modpack state through its owning Core service.
/// </summary>
public sealed class ModpackApplicationStatePersistence : IApplicationStatePersistence {
	private readonly ModpackService _modpackService;

	public ModpackApplicationStatePersistence(ModpackService modpackService) {
		_modpackService = modpackService ?? throw new ArgumentNullException(nameof(modpackService));
	}

	public void Persist() {
		if (_modpackService.CurrentLoadOrderEntries.Count > 0) {
			_modpackService.SaveLastUsed(_modpackService.CurrentLoadOrderEntries);
		}
	}
}
