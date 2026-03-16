namespace CalradiaForge.Core.Infra.Eula {
	using System.Reflection;

	using CalradiaForge.Core.Infra.Config;
	using CalradiaForge.Core.Infra.Logging;

	/// <summary>
	/// Reads the embedded EULA text and provides acceptance checking against persisted config.
	/// Core-only — no UI references. Receives <see cref="AppConfigSettings"/> by parameter.
	/// </summary>
	public sealed class EulaService {
		private const string EulaEmbeddedResourceName = "CalradiaForge.Resources.EULA.txt";
		private readonly Logger _logger = Logger.Instance;

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
				_logger.Error("EulaService: Could not resolve entry assembly for embedded EULA.");
				EulaText = "EULA could not be loaded. Please reinstall the application.";
				return;
			}

			using Stream? stream = entryAssembly.GetManifestResourceStream(EulaEmbeddedResourceName);
			if (stream is null) {
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					string[] resources = entryAssembly.GetManifestResourceNames();
					_logger.Debug("EulaService: Embedded EULA resource not found.", new {
						Expected = EulaEmbeddedResourceName,
						Assembly = entryAssembly.FullName,
						Resources = resources
					});
				}

				_logger.Error($"EulaService: Embedded EULA resource not found: '{EulaEmbeddedResourceName}'.");
				EulaText = "EULA could not be loaded. Please reinstall the application.";
				return;
			}

			using StreamReader reader = new(stream);
			EulaText = reader.ReadToEnd();

			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("EulaService: Embedded EULA loaded.", new {
					Resource = EulaEmbeddedResourceName,
					Length = EulaText.Length
				});
			}
		}

		/// <summary>
		/// Determines whether the user needs to accept the EULA.
		/// Returns <c>true</c> when the persisted flag is <c>false</c>.
		/// </summary>
		public bool RequiresAcceptance(AppConfigSettings config) {
			return !config.EulaAccepted;
		}

		/// <summary>
		/// Records EULA acceptance in the provided configuration store.
		/// </summary>
		public void RecordAcceptance(AppConfigSettings config) {
			config.EulaAccepted = true;
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("EulaService: Acceptance recorded.");
			}
		}
	}
}