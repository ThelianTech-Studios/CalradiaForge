namespace CalradiaForge.Core.Infra.GamePlatform.Steam;
/// <summary>
/// Describes the outcome of resolving Bannerlord through registered Steam libraries.
/// </summary>
public enum SteamResolutionStatus {
	SteamGameResolved,
	SteamGameResolvedWithoutWorkshop,
	SteamClientNotFound,
	SteamLibrariesNotResolved,
	BannerlordManifestNotFound,
	BannerlordManifestInvalid,
	BannerlordGamePathInvalid
}

/// <summary>
/// Identifies how the active Workshop path was selected.
/// </summary>
public enum WorkshopPathSource {
	None,
	ExistingConfiguration,
	BannerlordLibrary,
	AlternateSteamLibrary
}

/// <summary>
/// Records one technical decision made while resolving Steam paths.
/// </summary>
public sealed record SteamPathDiagnostic(
	string Code,
	string Message,
	string? Path = null,
	bool IsWarning = false);

/// <summary>
/// Controls configured Workshop-path preservation during resolution.
/// </summary>
public sealed record SteamResolutionOptions(
	string? ExistingWorkshopPath = null,
	bool PreserveExistingWorkshopPath = true);

/// <summary>
/// Contains resolved Steam paths and structured diagnostic evidence.
/// </summary>
public sealed record SteamResolutionResult(
	SteamResolutionStatus Status,
	string? SteamClientRoot,
	IReadOnlyList<string> LibraryRoots,
	string? BannerlordLibraryRoot,
	string? GameFolderPath,
	string? WorkshopFolderPath,
	WorkshopPathSource WorkshopPathSource,
	IReadOnlyList<SteamPathDiagnostic> Diagnostics) {
	/// <summary>
	/// Indicates whether a validated Steam Bannerlord installation was resolved.
	/// </summary>
	public bool IsGameResolved => Status is SteamResolutionStatus.SteamGameResolved
		or SteamResolutionStatus.SteamGameResolvedWithoutWorkshop;
}
