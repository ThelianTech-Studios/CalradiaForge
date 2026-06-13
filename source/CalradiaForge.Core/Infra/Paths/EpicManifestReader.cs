namespace CalradiaForge.Core.Infra.Paths {
	using System;
	using System.Text.RegularExpressions;

	using CalradiaForge.Core.Infra.Logging;

	using Newtonsoft.Json.Linq;

	/// <summary>
	/// Reads Epic Games launcher manifests to locate a Bannerlord install path.
	/// </summary>
	internal static class EpicManifestReader {
		private static readonly Logger _logger = Logger.Instance;
		private static readonly string ManifestDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data", "Manifests");
		private static readonly Regex BannerlordRegex = new(@"bannerlord", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// Attempts to resolve the Bannerlord install location from Epic manifest files.
		/// </summary>
		public static string? TryGetInstallLocation() {
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("EpicManifestReader: Checking manifest directory.", new { ManifestDirectory });
			}
			if (!Directory.Exists(ManifestDirectory)) {
				return null;
			}
			foreach (var file in Directory.EnumerateFiles(ManifestDirectory, "*.item")) {
				try {
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("EpicManifestReader: Parsing manifest file.", new { File = file });
					}
					var json = File.ReadAllText(file);
					var obj = JObject.Parse(json);
					var displayName = obj["DisplayName"]?.ToString();
					var installLocation = obj["InstallLocation"]?.ToString();

					if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(installLocation)) {
						if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
							_logger.Debug("EpicManifestReader: Missing display name or install location.", new { File = file });
						}
						continue;
					}
					bool matched = BannerlordRegex.IsMatch(displayName);
					bool exists = Directory.Exists(installLocation);
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("EpicManifestReader: Manifest match result.", new { File = file, DisplayName = displayName, InstallLocation = installLocation, Matched = matched, Exists = exists });
					}
					if (matched && exists) {
						return installLocation;
					}
				} catch (Exception ex) {
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("EpicManifestReader: Failed to parse manifest.", new { File = file }, ex);
					}
				}
			}
			return null;
		}
	}
}
