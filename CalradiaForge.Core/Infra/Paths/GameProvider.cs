namespace CalradiaForge.Core.Infra.Paths
	{
	/// <summary>
	/// Identifies the platform/storefront from which Bannerlord was installed.
	/// Determines launch behavior — Steam and StandAlone/GOG support direct EXE launch,
	/// while Epic Games and GamePass require launching through their respective clients
	/// due to authentication/token requirements imposed by TaleWorlds.
	/// </summary>
	public enum GameProvider
		{
		/// <summary>
		/// Standalone or GOG installation. Supports direct EXE launch.
		/// </summary>
		StandAlone,

		/// <summary>
		/// Steam installation. Supports direct EXE launch with <c>SteamAppId</c> env var.
		/// CalradiaForge auto-starts Steam if not running.
		/// </summary>
		Steam,

		/// <summary>
		/// Epic Games Store installation. Direct EXE launch is NOT supported
		/// due to TaleWorlds' authentication changes requiring token exchange
		/// through the Epic Games client. Load order must be written to the
		/// vanilla launcher config and the game launched through Epic.
		/// </summary>
		EpicGames,

		/// <summary>
		/// Xbox / PC Game Pass installation. Direct EXE launch is NOT supported
		/// due to Microsoft account login requirements. The "game" executable
		/// is actually the vanilla launcher, which must be started through
		/// the Xbox app. Load order can be pre-arranged in the launcher config.
		/// </summary>
		GamePass,

		/// <summary>
		/// Platform not yet configured by the user.
		/// </summary>
		NotInitialized
		}
	}
