namespace CalradiaForge.Core.Infra.GamePlatform.Epic {
	using System;
	using System.Text.RegularExpressions;

	using Newtonsoft.Json.Linq;

	using Serilog;

	/// <summary>
	/// Reads Epic Games launcher manifests to locate a Bannerlord install path.
	/// </summary>
	internal static class EpicManifestReader {
		private static readonly string ManifestDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data", "Manifests");
		private static readonly Regex BannerlordRegex = new(@"bannerlord", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// Attempts to resolve the Bannerlord install location from Epic manifest files.
		/// </summary>
		public static string? TryGetInstallLocation() {
			Log.Debug(
				"EpicManifestReader: Checking manifest directory. ManifestDirectory={ManifestDirectory}",
				ManifestDirectory);
			if (!Directory.Exists(ManifestDirectory)) {
				return null;
			}
			foreach (var file in Directory.EnumerateFiles(ManifestDirectory, "*.item")) {
				try {
					Log.Debug("EpicManifestReader: Parsing manifest file. File={File}", file);
					var json = File.ReadAllText(file);
					var obj = JObject.Parse(json);
					var displayName = obj["DisplayName"]?.ToString();
					var installLocation = obj["InstallLocation"]?.ToString();

					if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(installLocation)) {
						Log.Debug(
							"EpicManifestReader: Missing display name or install location. File={File}",
							file);
						continue;
					}
					bool matched = BannerlordRegex.IsMatch(displayName);
					bool exists = Directory.Exists(installLocation);
					Log.Debug(
						"EpicManifestReader: Manifest match result. File={File} DisplayName={DisplayName} InstallLocation={InstallLocation} Matched={Matched} Exists={Exists}",
						file,
						displayName,
						installLocation,
						matched,
						exists);
					if (matched && exists) {
						return installLocation;
					}
				} catch (Exception ex) {
					Log.Debug(ex, "EpicManifestReader: Failed to parse manifest. File={File}", file);
				}
			}
			return null;
		}
	}
}
