namespace CalradiaForge.Core.Infra.Eula {
	using CalradiaForge.Core.Infra.Config;
	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Infra.Paths;

	/// <summary>
	/// Reads the EULA text file and provides acceptance checking against persisted config.
	/// Core-only — no UI references. Receives <see cref="AppConfigSettings"/> by parameter.
	/// </summary>
	public sealed class EulaService {
		private readonly Logger _logger = Logger.Instance;

		/// <summary>
		/// Gets the full EULA text loaded from disk.
		/// </summary>
		public string EulaText { get; private set; } = string.Empty;

		/// <summary>
		/// Loads the EULA text from <see cref="AppPaths.EulaFilePath"/>.
		/// </summary>
		public void Load() {
			string path = AppPaths.EulaFilePath;
			if (!File.Exists(path)) {
				_logger.Error($"EulaService: EULA file not found at '{path}'.");
				EulaText = "EULA file not found. Please reinstall the application.";
				return;
			}

			EulaText = File.ReadAllText(path);
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("EulaService: EULA loaded.", new { Path = path, Length = EulaText.Length });
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