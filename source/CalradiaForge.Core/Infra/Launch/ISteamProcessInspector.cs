namespace CalradiaForge.Core.Infra.Launch;

/// <summary>Reports whether the Steam client process can be positively identified.</summary>
public interface ISteamProcessInspector {
	SteamProcessStatus Inspect();
}

/// <summary>Describes the result of one Steam process inspection.</summary>
public enum SteamProcessStatus {
	Running,
	NotRunning,
	Unknown
}
