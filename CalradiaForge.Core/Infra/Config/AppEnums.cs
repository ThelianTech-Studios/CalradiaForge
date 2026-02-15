namespace CalradiaForge.Core.Infra.Config
	{
	using System;
	using System.Collections.Generic;
	using System.Text;
	/// <summary>
	/// Defines how the ModsPage ComboBox selects a modpack on application startup.
	/// Persisted in <see cref="AppConfigSettings.ModpackStartupMode"/>.
	/// </summary>
	public enum ModpackStartupMode
		{
		/// <summary>
		/// Restore the exact modpack that was selected when the app last closed.
		/// </summary>
		LastUsed,

		/// <summary>
		/// Always select the built-in Vanilla modpack on startup.
		/// </summary>
		AlwaysDefault,

		/// <summary>
		/// Start with no modpack selected — the user must pick one each session.
		/// </summary>
		AlwaysAsk
		}
	}
