# Game Launching Capability

When launch validation succeeds, the application starts Bannerlord with the
selected active module identifiers in the exact provided order. It supports the
base Bannerlord executable and an optionally configured BLSE executable. Steam
launches ensure the Steam process is positively verified, start it once when
needed, poll with a bounded timeout, and add the Bannerlord Steam app ID.

Direct launch is blocked for Epic Games and Game Pass because their client
authentication flow is not implemented in the launcher. The load order may be
ready for the user to launch through that platform client. The mechanism is
owned by [GameLauncher](../../02_Systems/CalradiaForge_Core/Game_Platforms/README.md)
and the platform limits are listed in [Compatibility](../Compatibility/README.md).
