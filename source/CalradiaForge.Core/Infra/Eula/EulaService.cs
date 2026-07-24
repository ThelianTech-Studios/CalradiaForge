namespace CalradiaForge.Core.Infra.Eula {
	using System.Reflection;

	using CalradiaForge.Core.Infra.Config;

	using Serilog;
	using Serilog.Events;

	/// <summary>
	/// Reads the embedded EULA text and provides acceptance checking against persisted config.
	/// Core-only — no UI references. Receives <see cref="AppSettings"/> by parameter.
	/// </summary>
	public sealed class EulaService {
		private const string EulaEmbeddedResourceName = "CalradiaForge.Resources.EULA.txt";

		/// <summary>
		/// Gets the full EULA text loaded from the entry assembly.
		/// </summary>
		public string EulaText { get; private set; } = string.Empty;

		/// <summary>
		/// Loads the EULA text from the embedded resource in the entry assembly.
		/// </summary>
		public void Load() {
			Assembly? entryAssembly = Assembly.GetEntryAssembly();
			if (entryAssembly is null) {
				Log.Error("EulaService: Could not resolve entry assembly for embedded EULA.");
				EulaText = "EULA could not be loaded. Please reinstall the application.";
				return;
			}

			using Stream? stream = entryAssembly.GetManifestResourceStream(EulaEmbeddedResourceName);
			if (stream is null) {
				if (Log.IsEnabled(LogEventLevel.Debug)) {
					string[] resources = entryAssembly.GetManifestResourceNames();
					Log.Debug(
						"EulaService: Embedded EULA resource not found. Expected={ExpectedResource} Assembly={Assembly} Resources={Resources}",
						EulaEmbeddedResourceName,
						entryAssembly.FullName,
						resources);
				}

				Log.Error(
					"EulaService: Embedded EULA resource not found: {ResourceName}.",
					EulaEmbeddedResourceName);
				EulaText = "EULA could not be loaded. Please reinstall the application.";
				return;
			}

			using StreamReader reader = new(stream);
			EulaText = reader.ReadToEnd();

			Log.Debug(
				"EulaService: Embedded EULA loaded. Resource={Resource} Length={Length}",
				EulaEmbeddedResourceName,
				EulaText.Length);
		}

		/// <summary>
		/// Determines whether the user needs to accept the EULA.
		/// Returns <c>true</c> when the persisted flag is <c>false</c>.
		/// </summary>
		public bool RequiresAcceptance(AppSettings config) {
			return !config.EulaAccepted;
		}

		/// <summary>
		/// Records EULA acceptance in the provided configuration store.
		/// </summary>
		public void RecordAcceptance(AppSettings config) {
			config.EulaAccepted = true;
			Log.Debug("EulaService: Acceptance recorded.");
		}
	}
}
