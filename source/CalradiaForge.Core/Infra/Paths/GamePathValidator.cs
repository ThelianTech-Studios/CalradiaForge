namespace CalradiaForge.Core.Infra.Paths {
	using System.IO;

	using Serilog;

	/// <summary>
	/// Validates user-selected game paths using marker file/folder detection.
	/// Pure static helper — receives all data as parameters.
	/// </summary>
	public static class GamePathValidator {

		/// <summary>
		/// Validates that a folder path points to a real Bannerlord installation
		/// by checking for known marker files and directories.
		/// </summary>
		/// <param name="gameFolderPath">The user-selected game folder path.</param>
		/// <param name="errorMessage">User-friendly message describing the validation failure.</param>
		/// <returns><c>true</c> if the folder contains a valid Bannerlord installation.</returns>
		public static bool ValidateGameFolder(string gameFolderPath) {
			Log.Debug(
				"GamePathValidator: Validating game folder. GameFolderPath={GameFolderPath}",
				gameFolderPath);
			if (string.IsNullOrWhiteSpace(gameFolderPath)) {
				Log.Debug(
					"GamePathValidator: Game folder path is empty or whitespace. GameFolderPath={GameFolderPath}",
					gameFolderPath);
				return false;
			}
			if (!Directory.Exists(gameFolderPath)) {
				Log.Debug(
					"GamePathValidator: Game folder does not exist. GameFolderPath={GameFolderPath}",
					gameFolderPath);
				return false;
			}

			// Marker 1: Modules directory must exist
			string modulesPath = Path.Combine(gameFolderPath, "Modules");
			bool modulesExists = Directory.Exists(modulesPath);
			Log.Debug(
				"GamePathValidator: Modules marker check. ModulesPath={ModulesPath} Exists={Exists}",
				modulesPath,
				modulesExists);
			if (!modulesExists) {
				Log.Debug(
					"GamePathValidator: Game folder doesn't appear to be a valid Bannerlord installation — no Modules folder was found. GameFolderPath={GameFolderPath}",
					gameFolderPath);
				return false;
			}

			// Marker 2: Native module must exist with SubModule.xml
			string nativeXml = Path.Combine(modulesPath, "Native", "SubModule.xml");
			bool nativeExists = File.Exists(nativeXml);
			Log.Debug(
				"GamePathValidator: Native marker check. NativeXml={NativeXml} Exists={Exists}",
				nativeXml,
				nativeExists);
			if (!nativeExists) {
				Log.Debug(
					"GamePathValidator: Game folder doesn't appear to be a valid Bannerlord installation — the Native module was not found. GameFolderPath={GameFolderPath}",
					gameFolderPath);
				return false;
			}

			// Marker 3: bin directory should exist
			string binPath = Path.Combine(gameFolderPath, "bin");
			bool binExists = Directory.Exists(binPath);
			Log.Debug(
				"GamePathValidator: Bin marker check. BinPath={BinPath} Exists={Exists}",
				binPath,
				binExists);
			if (!binExists) {
				Log.Debug(
					"GamePathValidator: Game folder doesn't appear to be a valid Bannerlord installation — no bin folder was found. GameFolderPath={GameFolderPath}",
					gameFolderPath);
				return false;
			}
			Log.Debug(
				"GamePathValidator: Game folder validated. GameFolderPath={GameFolderPath}",
				gameFolderPath);
			return true;
		}

		/// <summary>
		/// Validates that a file path points to a valid Bannerlord executable.
		/// Checks that the file exists and has a .exe extension.
		/// </summary>
		/// <param name="exePath">The user-selected executable path.</param>
		/// <param name="errorMessage">User-friendly message describing the validation failure.</param>
		/// <returns><c>true</c> if the file is a valid executable.</returns>
		public static bool ValidateGameExecutable(string exePath) {
			Log.Debug(
				"GamePathValidator: Validating game executable. ExePath={ExePath}",
				exePath);
			if (string.IsNullOrWhiteSpace(exePath)) {
				Log.Debug(
					"GamePathValidator: Executable path is empty or whitespace. ExePath={ExePath}",
					exePath);
				return false;
			}
			if (!File.Exists(exePath)) {
				Log.Debug(
					"GamePathValidator: Executable missing. ExePath={ExePath}",
					exePath);
				return false;
			}
			if (!exePath.EndsWith(".exe", System.StringComparison.OrdinalIgnoreCase)) {
				Log.Debug(
					"GamePathValidator: Executable extension invalid. ExePath={ExePath}",
					exePath);
				return false;
			}
			Log.Debug(
				"GamePathValidator: Executable validated. ExePath={ExePath}",
				exePath);
			return true;
		}

		/// <summary>
		/// Validates that a folder path points to a valid Steam Workshop content directory
		/// for Bannerlord (app ID 261550).
		/// </summary>
		/// <param name="workshopFolderPath">The user-selected workshop folder path.</param>
		/// <param name="errorMessage">User-friendly message describing the validation failure.</param>
		/// <returns><c>true</c> if the folder exists and appears to be a valid workshop directory.</returns>
		public static bool ValidateWorkshopFolder(string workshopFolderPath) {
			Log.Debug(
				"GamePathValidator: Validating workshop folder. WorkshopFolderPath={WorkshopFolderPath}",
				workshopFolderPath);
			if (string.IsNullOrWhiteSpace(workshopFolderPath)) {
				Log.Debug(
					"GamePathValidator: Workshop folder path is empty or whitespace. WorkshopFolderPath={WorkshopFolderPath}",
					workshopFolderPath);
				return false;
			}
			if (!Directory.Exists(workshopFolderPath)) {
				Log.Debug(
					"GamePathValidator: Workshop folder missing. WorkshopFolderPath={WorkshopFolderPath}",
					workshopFolderPath);
				return false;
			}
			Log.Debug(
				"GamePathValidator: Workshop folder validated. WorkshopFolderPath={WorkshopFolderPath}",
				workshopFolderPath);
			return true;
		}
	}
}
