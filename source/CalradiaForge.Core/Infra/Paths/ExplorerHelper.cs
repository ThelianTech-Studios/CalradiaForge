namespace CalradiaForge.Core.Infra.Paths {
	using System;
	using System.Diagnostics;
	using System.IO;

	using Serilog;

	/// <summary>
	/// Opens folders and URLs via the OS shell.
	/// Pure helper — receives all data as parameters.
	/// Defensively ensures directories exist before opening.
	/// </summary>
	public static class ExplorerHelper {
		/// <summary>
		/// Opens a folder in Windows Explorer.
		/// Creates the directory if it does not exist.
		/// </summary>
		/// <param name="folderPath">Full path to the folder to open.</param>
		/// <returns><c>true</c> when the folder was opened successfully.</returns>
		public static bool OpenFolder(string folderPath) {
			if (string.IsNullOrWhiteSpace(folderPath)) {
				Log.Warning("ExplorerHelper: Cannot open folder — path is null or empty.");
				return false;
			}
			try {
				if (!Directory.Exists(folderPath)) {
					Directory.CreateDirectory(folderPath);
				}
				Process.Start(new ProcessStartInfo {
					FileName = folderPath,
					UseShellExecute = true,
					Verb = "open"
				});
				return true;
			} catch (Exception ex) {
				Log.Error(ex, "ExplorerHelper: Failed to open folder {FolderPath}", folderPath);
				return false;
			}
		}

		/// <summary>
		/// Opens a URL in the default system browser.
		/// </summary>
		/// <param name="url">The URL to open.</param>
		/// <returns><c>true</c> when the URL was opened successfully.</returns>
		public static bool OpenUrl(string url) {
			if (string.IsNullOrWhiteSpace(url)) {
				Log.Warning("ExplorerHelper: Cannot open URL — value is null or empty.");
				return false;
			}
			try {
				Process.Start(new ProcessStartInfo {
					FileName = url,
					UseShellExecute = true
				});
				return true;
			} catch (Exception ex) {
				Log.Error(ex, "ExplorerHelper: Failed to open URL {Url}", url);
				return false;
			}
		}
	}
}
