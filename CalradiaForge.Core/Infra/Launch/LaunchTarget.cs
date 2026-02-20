namespace CalradiaForge.Core.Infra.Launch {
	/// <summary>
	/// Identifies which executable to use when launching the game.
	/// </summary>
	public enum LaunchTarget {
		/// <summary>
		/// Launches the base Bannerlord executable.
		/// </summary>
		Bannerlord,
		/// <summary>
		/// Launches Bannerlord via the BLSE standalone executable.
		/// </summary>
		BLSE
	}
}
