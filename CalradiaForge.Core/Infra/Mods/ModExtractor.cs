namespace CalradiaForge.Core.Infra.Mods {
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	using CalradiaForge.Core.Infra.Logging;

	using SharpCompress.Archives;
	using SharpCompress.Common;

	/// <summary>
	/// Handles extraction of mod archives using SharpCompress and locates
	/// the true mod root directory (handling lazy nested folder structures).
	/// </summary>
	public static class ModExtractor {
		private static readonly Logger _logger = Logger.Instance;

		/// <summary>
		/// Extracts the archive to a temporary directory.
		/// SharpCompress auto-detects format (zip, rar, 7z, tar, etc.).
		/// </summary>
		/// <param name="archivePath">Full path to the archive file.</param>
		/// <param name="token">Cancellation token.</param>
		/// <returns>The path to the temp directory containing extracted contents, or <c>null</c> on failure.</returns>
		public static async Task<string?> ExtractToTempAsync(string archivePath, CancellationToken token = default) {
			if (string.IsNullOrWhiteSpace(archivePath) || !File.Exists(archivePath)) {
				_logger.Warning($"ModExtractor: Archive not found: '{archivePath}'");
				return null;
			}
			string tempDir = Path.Combine(Path.GetTempPath(), "CalradiaForge", Guid.NewGuid().ToString());
			try {
				Directory.CreateDirectory(tempDir);
				await Task.Run(() => {
					token.ThrowIfCancellationRequested();
					using IArchive archive = ArchiveFactory.Open(archivePath);
					foreach (IArchiveEntry entry in archive.Entries) {
						token.ThrowIfCancellationRequested();
						if (!entry.IsDirectory) {
							entry.WriteToDirectory(tempDir, new ExtractionOptions {
								ExtractFullPath = true,
								Overwrite = true
							});
						}
					}
				}, token);

				_logger.Info($"ModExtractor: Extracted '{Path.GetFileName(archivePath)}' to temp: '{tempDir}'");
				return tempDir;
			} catch (OperationCanceledException) {
				CleanupTempDirectory(tempDir);
				throw;
			} catch (Exception ex) {
				_logger.Error(ex, $"ModExtractor: Failed to extract '{archivePath}'");
				CleanupTempDirectory(tempDir);
				return null;
			}
		}

		/// <summary>
		/// Walks the extracted directory tree to find the actual mod root —
		/// the directory that directly contains <c>SubModule.xml</c>.
		/// Handles lazy mod devs who wrap the mod in extra folders.
		/// </summary>
		/// <param name="extractedDir">The temp directory where the archive was extracted.</param>
		/// <returns>The path to the mod root directory, or <c>null</c> if no <c>SubModule.xml</c> was found.</returns>
		public static string? FindModRoot(string extractedDir) {
			if (string.IsNullOrWhiteSpace(extractedDir) || !Directory.Exists(extractedDir)) {
				return null;
			}

			// Check if SubModule.xml is directly in the extracted directory
			if (File.Exists(Path.Combine(extractedDir, "SubModule.xml"))) {
				return extractedDir;
			}

			// Walk downward through single-child directories looking for SubModule.xml
			// This handles: Archive → FolderA → FolderB → [SubModule.xml + mod files]
			string currentDir = extractedDir;
			while (true) {
				string[] subDirs;
				try {
					subDirs = Directory.GetDirectories(currentDir);
				} catch {
					break;
				}

				// Check each subdirectory for SubModule.xml
				foreach (string subDir in subDirs) {
					if (File.Exists(Path.Combine(subDir, "SubModule.xml"))) {
						return subDir;
					}
				}

				// If there's exactly one subdirectory and no SubModule.xml at this level,
				// drill deeper (this is the lazy nesting pattern)
				if (subDirs.Length == 1) {
					currentDir = subDirs[0];
					continue;
				}

				// Multiple subdirs or none — SubModule.xml wasn't found
				break;
			}

			_logger.Warning($"ModExtractor: No SubModule.xml found in extracted archive at: '{extractedDir}'");
			return null;
		}

		/// <summary>
		/// Safely deletes a temporary extraction directory.
		/// </summary>
		public static void CleanupTempDirectory(string tempDir) {
			try {
				if (Directory.Exists(tempDir)) {
					Directory.Delete(tempDir, recursive: true);
				}
			} catch (Exception ex) {
				_logger.Warning($"ModExtractor: Failed to clean up temp directory '{tempDir}': {ex.Message}");
			}
		}
	}
}
