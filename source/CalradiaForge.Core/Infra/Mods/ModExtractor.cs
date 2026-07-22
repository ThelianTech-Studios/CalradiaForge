namespace CalradiaForge.Core.Infra.Mods {
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;

	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Infra.Paths;
	using CalradiaForge.Core.Models;

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
		/// <returns>Exact file count from the archive, minimum 1.</returns>
		public static int EstimateFileCount(string archivePath) {
			try {
				using ArchiveFile archive = new(archivePath);
				return Math.Max(1, archive.Entries.Count);
			} catch {
				try {
					long bytes = new FileInfo(archivePath).Length;
					double megabytes = bytes / (1024.0 * 1024.0);
					return Math.Max(1, (int)(megabytes * FilesPerMBEstimate));
				} catch {
					return 1; // Safe fallback
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
			ArchiveExtractionResult result = await ExtractToTempResultAsync(
				archivePath, token, onFileExtracted);
			return result.TempDirectory;
		}

		/// <summary>
		/// Opens an archive, validates that every entry resolves inside the managed
		/// temporary destination, and only then extracts it.
		/// </summary>
		public static async Task<ArchiveExtractionResult> ExtractToTempResultAsync(
			string archivePath,
			CancellationToken token = default,
			Action<int>? onFileExtracted = null) {

			if (string.IsNullOrWhiteSpace(archivePath) || !File.Exists(archivePath)) {
				_logger.Warning($"ModExtractor: Archive not found: '{archivePath}'");
				return ArchiveExtractionResult.Fail("Archive file was not found.");
			}
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ModExtractor: Starting extraction.", new { ArchivePath = archivePath });
			}
			string tempDir = Path.Combine(AppPaths.ExtractionDirectory, Guid.NewGuid().ToString());
			try {
				await Task.Run(() => {
					using (ArchiveFile archive = new(archivePath)) {
						// Entries is lazy. Enumerating it proves the archive can be opened
						// and gives us every relative output name before any write occurs.
						IReadOnlyList<ArchiveEntry> entries = archive.Entries;
						if (!TryValidateEntryContainment(entries, tempDir, out string validationError)) {
							throw new UnsafeArchiveEntryException(validationError);
						}

						Directory.CreateDirectory(tempDir);
						archive.Extract(tempDir, overwrite: true, onFileExtracted, token);
						if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
							_logger.Debug("ModExtractor: Extraction complete.", new { ArchivePath = archivePath, TempDir = tempDir });
						}
					}
				}, token);

				_logger.Info($"ModExtractor: Extracted '{Path.GetFileName(archivePath)}' to temp: '{tempDir}'");
				return ArchiveExtractionResult.Ok(tempDir);
			} catch (OperationCanceledException) {
				CleanupTempDirectory(tempDir);
				throw;
			} catch (UnsafeArchiveEntryException ex) {
				_logger.Warning($"ModExtractor: Blocked unsafe archive '{Path.GetFileName(archivePath)}': {ex.Message}");
				CleanupTempDirectory(tempDir);
				return ArchiveExtractionResult.Fail($"Unsafe archive blocked: {ex.Message}");
			} catch (Exception ex) {
				_logger.Error(ex, $"ModExtractor: Failed to extract '{archivePath}'");
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModExtractor: Extraction failed.", new { ArchivePath = archivePath, TempDir = tempDir }, ex);
				}
				CleanupTempDirectory(tempDir);
				return ArchiveExtractionResult.Fail($"Archive could not be opened or extracted: {ex.Message}");
			}
		}

		private static bool TryValidateEntryContainment(
			IReadOnlyList<ArchiveEntry> entries,
			string destinationRoot,
			out string error) {
			string canonicalRoot = Path.GetFullPath(destinationRoot)
				.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string rootPrefix = canonicalRoot + Path.DirectorySeparatorChar;
			char[] separators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();

			foreach (ArchiveEntry entry in entries) {
				string? entryName = entry.FileName;
				if (string.IsNullOrWhiteSpace(entryName)) {
					error = "An archive entry has no relative file name.";
					return false;
				}

				if (Path.IsPathRooted(entryName) || Path.IsPathFullyQualified(entryName)) {
					error = $"Entry '{entryName}' uses a rooted path.";
					return false;
				}

				foreach (string segment in entryName.Split(separators, StringSplitOptions.RemoveEmptyEntries)) {
					if (segment == "..") {
						error = $"Entry '{entryName}' attempts parent-directory traversal.";
						return false;
					}
					if (segment.IndexOfAny(invalidFileNameChars) >= 0) {
						error = $"Entry '{entryName}' contains an invalid Windows path component.";
						return false;
					}
				}

				string outputPath;
				try {
					outputPath = Path.GetFullPath(Path.Combine(canonicalRoot, entryName));
				} catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) {
					error = $"Entry '{entryName}' does not resolve to a valid output path.";
					return false;
				}

				if (!outputPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase)) {
					error = $"Entry '{entryName}' resolves outside the extraction directory.";
					return false;
				}
			}

			error = string.Empty;
			return true;
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
			if (!TryResolveManagedTempDirectory(tempDir, out string managedTempDir)) {
				_logger.Warning($"ModExtractor: Refused to clean unmanaged temp directory '{tempDir}'.");
				return;
			}

			try {
				if (Directory.Exists(managedTempDir)) {
					Directory.Delete(managedTempDir, recursive: true);
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("ModExtractor: Cleaned temp directory.", new { TempDir = managedTempDir });
					}
				}
			} catch (Exception ex) {
				_logger.Warning($"ModExtractor: Failed to clean up temp directory '{managedTempDir}': {ex.Message}");
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ModExtractor: Temp cleanup failed.", new { TempDir = managedTempDir }, ex);
				}
			}
		}

		private static bool TryResolveManagedTempDirectory(string tempDir, out string managedTempDir) {
			managedTempDir = string.Empty;
			if (string.IsNullOrWhiteSpace(tempDir)) {
				return false;
			}

			try {
				string extractionRoot = Path.GetFullPath(AppPaths.ExtractionDirectory)
					.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				string candidate = Path.GetFullPath(tempDir)
					.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				string? parent = Path.GetDirectoryName(candidate);
				string directoryName = Path.GetFileName(candidate);

				if (!string.Equals(parent, extractionRoot, StringComparison.OrdinalIgnoreCase) ||
					!Guid.TryParseExact(directoryName, "D", out _)) {
					return false;
				}

				managedTempDir = candidate;
				return true;
			} catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) {
				return false;
			}
		}

		private sealed class UnsafeArchiveEntryException : Exception {
			public UnsafeArchiveEntryException(string message) : base(message) { }
		}
	}
}
