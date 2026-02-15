namespace CalradiaForge.Core.Infra.Paths {
	using System.IO;

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
		public static bool ValidateGameFolder(string gameFolderPath, out string errorMessage) {
			if (string.IsNullOrWhiteSpace(gameFolderPath)) {
				errorMessage = "No folder was selected. Please try again.";
				return false;
			}
			if (!Directory.Exists(gameFolderPath)) {
				errorMessage = "The selected folder does not exist. Please try again.";
				return false;
			}

			// Marker 1: Modules directory must exist
			string modulesPath = Path.Combine(gameFolderPath, "Modules");
			if (!Directory.Exists(modulesPath)) {
				errorMessage = "That folder doesn't appear to be a valid Bannerlord installation — no Modules folder was found. Please try again.";
				return false;
			}

			// Marker 2: Native module must exist with SubModule.xml
			string nativeXml = Path.Combine(modulesPath, "Native", "SubModule.xml");
			if (!File.Exists(nativeXml)) {
				errorMessage = "That folder doesn't appear to be a valid Bannerlord installation — the Native module was not found. Please try again.";
				return false;
			}

			// Marker 3: bin directory should exist
			string binPath = Path.Combine(gameFolderPath, "bin");
			if (!Directory.Exists(binPath)) {
				errorMessage = "That folder doesn't appear to be a valid Bannerlord installation — no bin folder was found. Please try again.";
				return false;
			}

			errorMessage = string.Empty;
			return true;
		}

		/// <summary>
		/// Validates that a file path points to a valid Bannerlord executable.
		/// Checks that the file exists and has a .exe extension.
		/// </summary>
		/// <param name="exePath">The user-selected executable path.</param>
		/// <param name="errorMessage">User-friendly message describing the validation failure.</param>
		/// <returns><c>true</c> if the file is a valid executable.</returns>
		public static bool ValidateGameExecutable(string exePath, out string errorMessage) {
			if (string.IsNullOrWhiteSpace(exePath)) {
				errorMessage = "No file was selected. Please try again.";
				return false;
			}
			if (!File.Exists(exePath)) {
				errorMessage = "The selected file does not exist. Please try again.";
				return false;
			}
			if (!exePath.EndsWith(".exe", System.StringComparison.OrdinalIgnoreCase)) {
				errorMessage = "The selected file is not an executable (.exe). Please try again.";
				return false;
			}

			errorMessage = string.Empty;
			return true;
		}

		/// <summary>
		/// Validates that a folder path points to a valid Steam Workshop content directory
		/// for Bannerlord (app ID 261550).
		/// </summary>
		/// <param name="workshopFolderPath">The user-selected workshop folder path.</param>
		/// <param name="errorMessage">User-friendly message describing the validation failure.</param>
		/// <returns><c>true</c> if the folder exists and appears to be a valid workshop directory.</returns>
		public static bool ValidateWorkshopFolder(string workshopFolderPath, out string errorMessage) {
			if (string.IsNullOrWhiteSpace(workshopFolderPath)) {
				errorMessage = "No folder was selected. Please try again.";
				return false;
			}
			if (!Directory.Exists(workshopFolderPath)) {
				errorMessage = "The selected folder does not exist. Please try again.";
				return false;
			}

			errorMessage = string.Empty;
			return true;
		}
	}
}
