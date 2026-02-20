namespace CalradiaForge.Core.Infra.Paths {
	using System.IO;

	using CalradiaForge.Core.Infra.Logging;

	/// <summary>
	/// Validates user-selected game paths using marker file/folder detection.
	/// Pure static helper — receives all data as parameters.
	/// </summary>
	public static class GamePathValidator {
		private static readonly Logger _logger = Logger.Instance;

		/// <summary>
		/// Validates that a folder path points to a real Bannerlord installation
		/// by checking for known marker files and directories.
		/// </summary>
		/// <param name="gameFolderPath">The user-selected game folder path.</param>
		/// <param name="errorMessage">User-friendly message describing the validation failure.</param>
		/// <returns><c>true</c> if the folder contains a valid Bannerlord installation.</returns>
		public static bool ValidateGameFolder(string gameFolderPath, out string errorMessage) {
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("GamePathValidator: Validating game folder.", new { GameFolderPath = gameFolderPath });
			}
			if (string.IsNullOrWhiteSpace(gameFolderPath)) {
				errorMessage = "No folder was selected. Please try again.";
				return false;
			}
			if (!Directory.Exists(gameFolderPath)) {
				errorMessage = "The selected folder does not exist. Please try again.";
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("GamePathValidator: Game folder missing.", new { GameFolderPath = gameFolderPath });
				}
				return false;
			}

			// Marker 1: Modules directory must exist
			string modulesPath = Path.Combine(gameFolderPath, "Modules");
			bool modulesExists = Directory.Exists(modulesPath);
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("GamePathValidator: Modules marker check.", new { ModulesPath = modulesPath, Exists = modulesExists });
			}
			if (!modulesExists) {
				errorMessage = "That folder doesn't appear to be a valid Bannerlord installation — no Modules folder was found. Please try again.";
				return false;
			}

			// Marker 2: Native module must exist with SubModule.xml
			string nativeXml = Path.Combine(modulesPath, "Native", "SubModule.xml");
			bool nativeExists = File.Exists(nativeXml);
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("GamePathValidator: Native marker check.", new { NativeXml = nativeXml, Exists = nativeExists });
			}
			if (!nativeExists) {
				errorMessage = "That folder doesn't appear to be a valid Bannerlord installation — the Native module was not found. Please try again.";
				return false;
			}

			// Marker 3: bin directory should exist
			string binPath = Path.Combine(gameFolderPath, "bin");
			bool binExists = Directory.Exists(binPath);
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("GamePathValidator: Bin marker check.", new { BinPath = binPath, Exists = binExists });
			}
			if (!binExists) {
				errorMessage = "That folder doesn't appear to be a valid Bannerlord installation — no bin folder was found. Please try again.";
				return false;
			}

			errorMessage = string.Empty;
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("GamePathValidator: Game folder validated.", new { GameFolderPath = gameFolderPath });
			}
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
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("GamePathValidator: Validating game executable.", new { ExePath = exePath });
			}
			if (string.IsNullOrWhiteSpace(exePath)) {
				errorMessage = "No file was selected. Please try again.";
				return false;
			}
			if (!File.Exists(exePath)) {
				errorMessage = "The selected file does not exist. Please try again.";
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("GamePathValidator: Executable missing.", new { ExePath = exePath });
				}
				return false;
			}
			if (!exePath.EndsWith(".exe", System.StringComparison.OrdinalIgnoreCase)) {
				errorMessage = "The selected file is not an executable (.exe). Please try again.";
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("GamePathValidator: Executable extension invalid.", new { ExePath = exePath });
				}
				return false;
			}

			errorMessage = string.Empty;
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("GamePathValidator: Executable validated.", new { ExePath = exePath });
			}
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
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("GamePathValidator: Validating workshop folder.", new { WorkshopFolderPath = workshopFolderPath });
			}
			if (string.IsNullOrWhiteSpace(workshopFolderPath)) {
				errorMessage = "No folder was selected. Please try again.";
				return false;
			}
			if (!Directory.Exists(workshopFolderPath)) {
				errorMessage = "The selected folder does not exist. Please try again.";
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("GamePathValidator: Workshop folder missing.", new { WorkshopFolderPath = workshopFolderPath });
				}
				return false;
			}

			errorMessage = string.Empty;
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("GamePathValidator: Workshop folder validated.", new { WorkshopFolderPath = workshopFolderPath });
			}
			return true;
		}
	}
}
