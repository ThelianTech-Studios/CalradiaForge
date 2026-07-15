namespace CalradiaForge.Core.Infra.GamePlatform.Steam;

using Microsoft.Win32;

/// <summary>
/// Locates the main Steam client installation root.
/// </summary>
public interface ISteamClientRootProvider {
	/// <summary>
	/// Returns the configured Steam client root, or <c>null</c> when it cannot be located.
	/// </summary>
	string? GetSteamClientRoot();
}

/// <summary>
/// Reads the current user's Steam client root from the Windows registry.
/// </summary>
public sealed class WindowsSteamClientRootProvider : ISteamClientRootProvider {
	private const string _steamRegistryKey = @"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam";
	private const string _steamPathValue = "SteamPath";

	/// <inheritdoc />
	public string? GetSteamClientRoot() {
		return Registry.GetValue(_steamRegistryKey, _steamPathValue, null) as string;
	}
}
