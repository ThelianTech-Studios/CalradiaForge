namespace CalradiaForge.Core.Infra.Paths {
	using System;
	using System.Text.RegularExpressions;

	using Newtonsoft.Json.Linq;

	internal static class EpicManifestReader {
		private static readonly string ManifestDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data", "Manifests");
		private static readonly Regex BannerlordRegex = new(@"bannerlord", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		public static string? TryGetInstallLocation() {
			if (!Directory.Exists(ManifestDirectory)) {
				return null;
			}
			foreach (var file in Directory.EnumerateFiles(ManifestDirectory, "*.item")) {
				try { 
				var json = File.ReadAllText(file);
				var obj = JObject.Parse(json);
				var displayName = obj["DisplayName"]?.ToString();
				var installLocation = obj["InstallLocation"]?.ToString();

				if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(installLocation)) {
						continue;
					}
				if (BannerlordRegex.IsMatch(displayName) && Directory.Exists(installLocation)) {
						return installLocation;
					}
				}
				catch {
					// Implement logging to log this exception
				}
			}
			return null;
		}
	}
}
