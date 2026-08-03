# Game Configuration Capability

Settings can detect Bannerlord automatically where the current resolver supports
it, select a game folder manually, validate the expected executable, configure a
Steam Workshop folder when the provider is Steam, and expose an optional BLSE
executable. Invalid or unavailable automatic detection falls back to a manual
configuration state and an actionable startup notification.

The current automatic resolver enables the Steam path; unsupported-platform
detection code is present but gated. Manual folder inference can identify known
provider shapes. See [Game Platforms](../../02_Systems/CalradiaForge_Core/Game_Platforms/README.md)
and [Supported Platforms](../Compatibility/Supported_Platforms.md).
