namespace CalradiaForge.Core.Infra.Mods {
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Infra.Paths;

	// DEPRECATED: SharpCompress implementation (replaced by SevenZipWrapper)
	// using SharpCompress.Archives;
	// using SharpCompress.Common;

	using SevenZipWrapper;

	/// <summary>
	/// Handles extraction of mod archives using SevenZipWrapper (7z.dll COM interop)
	/// and locates the true mod root directory (handling lazy nested folder structures).
	/// </summary>
	public static class ModExtractor {
		private static readonly Logger _logger = Logger.Instance;

		/// <summary>
		/// Heuristic for estimating file count from archive size.
		/// Used as a fallback if the archive cannot be opened for exact counting.
		/// ~33 files per megabyte based on typical Bannerlord mod archives.
		/// </summary>
		public const double FilesPerMBEstimate = 33.0;

		/// <summary>
		/// Gets the exact file count for an archive by opening it with SevenZipWrapper.
		/// Falls back to a size-based heuristic if the archive cannot be read.
		/// </summary>
		/// <param name="archivePath">Full path to the archive file.</param>
		/// <returns>Exact file count from the archive, minimum 10.</returns>
		public static int EstimateFileCount(string archivePath) {
			try {
				using ArchiveFile archive = new(archivePath);
				return Math.Max(1, archive.Entries.Count);
			} catch {
				// Fallback to heuristic if archive cannot be opened (corrupt, locked, etc.)
				try {
					long bytes = new FileInfo(archivePath).Length;
					double megabytes = bytes / (1024.0 * 1024.0);
					return Math.Max(10, (int)(megabytes * FilesPerMBEstimate));
				} catch {
					return 100; // Safe fallback
				}
			}
		}

		/// <summary>
		/// Extracts the archive to a temporary directory.
		/// SevenZipWrapper auto-detects format (zip, rar, 7z, tar, etc.) via 7z.dll.
		/// </summary>
		/// <param name="archivePath">Full path to the archive file.</param>
		/// <param name="token">Cancellation token.</param>
		/// <param name="onFileExtracted">
		/// Optional callback invoked after each file is extracted.
		/// Parameter is the cumulative count of files extracted from this archive so far.
		/// Called on the extraction thread — callers must dispatch to the UI thread.
		/// </param>
		/// <returns>The path to the temp directory containing extracted contents, or <c>null</c> on failure.</returns>
		public static async Task<string?> ExtractToTempAsync(
			string archivePath,
			CancellationToken token = default,
			Action<int>? onFileExtracted = null) {

			if (string.IsNullOrWhiteSpace(archivePath) || !File.Exists(archivePath)) {
				_logger.Warning($"ModExtractor: Archive not found: '{archivePath}'");
				return null;
			}
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModExtractor: Starting extraction.", new { ArchivePath = archivePath });
			}
			string tempDir = Path.Combine(AppPaths.ExtractionDirectory, Guid.NewGuid().ToString());
			try {
				Directory.CreateDirectory(tempDir);
				await Task.Run(() => {
					using (ArchiveFile archive = new(archivePath)) {
						archive.Extract(tempDir, overwrite: true, onFileExtracted, token);
						if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
							_logger.Debug("ModExtractor: Extraction complete.", new { ArchivePath = archivePath, TempDir = tempDir });
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
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModExtractor: Extraction failed.", new { ArchivePath = archivePath, TempDir = tempDir }, ex);
				}
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

			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModExtractor: Searching for mod root.", new { ExtractedDir = extractedDir });
			}
			// Check if SubModule.xml is directly in the extracted directory
			if (File.Exists(Path.Combine(extractedDir, "SubModule.xml"))) {
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModExtractor: Found SubModule.xml at root.", new { ExtractedDir = extractedDir });
				}
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
						if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
							_logger.Debug("ModExtractor: Found SubModule.xml in subdirectory.", new { ExtractedDir = extractedDir, ModRoot = subDir });
						}
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
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModExtractor: Mod root not found.", new { ExtractedDir = extractedDir });
			}
			return null;
		}

		/// <summary>
		/// Safely deletes a temporary extraction directory.
		/// </summary>
		public static void CleanupTempDirectory(string tempDir) {
			try {
				if (Directory.Exists(tempDir)) {
					Directory.Delete(tempDir, recursive: true);
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("ModExtractor: Cleaned temp directory.", new { TempDir = tempDir });
					}
				}
			} catch (Exception ex) {
				_logger.Warning($"ModExtractor: Failed to clean up temp directory '{tempDir}': {ex.Message}");
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModExtractor: Temp cleanup failed.", new { TempDir = tempDir }, ex);
				}
			}
		}
	}
}
