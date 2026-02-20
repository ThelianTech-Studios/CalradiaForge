namespace CalradiaForge.Core.Infra.Mods {
	using System;
	using System.Collections.Generic;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Threading.Tasks;

	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Models;

	/// <summary>
	/// Removes the <c>Zone.Identifier</c> alternate data stream (ADS) from files
	/// in a given directory. This "unblocks" files that Windows marks as downloaded
	/// from the internet, which prevents Bannerlord from loading them.
	/// </summary>
	public static class DLLUnblocker {
		private static readonly Logger _logger = Logger.Instance;
		private const string ZoneIdentifierSuffix = ":Zone.Identifier";

		/// <summary>
		/// Win32 API to delete a file. Used to remove ADS (alternate data streams)
		/// which cannot be deleted with <see cref="System.IO.File.Delete"/>.
		/// </summary>
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeleteFile(string lpFileName);

		/// <summary>
		/// Scans the specified directory recursively for DLL files and removes
		/// the <c>Zone.Identifier</c> ADS from any that are blocked.
		/// </summary>
		/// <param name="directoryPath">The root directory to scan (typically the Modules folder).</param>
		/// <param name="token">Cancellation token for cooperative cancellation.</param>
		public static async Task<UnblockResult> UnblockAllAsync(
			string directoryPath,
			CancellationToken token = default) {
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("DllUnblocker: Starting unblock scan.", new { DirectoryPath = directoryPath });
			}
			if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath)) {
				_logger.Warning($"DllUnblocker: Directory does not exist: '{directoryPath}'");
				return new UnblockResult();
			}
			return await Task.Run(() => UnblockDirectory(directoryPath, token), token);
		}

		/// <summary>
		/// Unblocks <b>all</b> files in the specified directory for a BLSE installation.
		/// Unlike <see cref="UnblockAllAsync"/> which targets only <c>*.dll</c> files,
		/// this method strips the <c>Zone.Identifier</c> ADS from every file type
		/// (<c>.exe</c>, <c>.dll</c>, <c>.exe.config</c>, etc.) because BLSE archives
		/// contain a mix of file types that all carry ADS when downloaded via a browser.
		/// <para>
		/// Intended to be called on the temp extraction directory <b>before</b> copying
		/// files to the game bin, so they arrive already unblocked.
		/// </para>
		/// </summary>
		/// <param name="sourceBinDir">
		/// The platform-specific BLSE bin folder inside the temp extraction directory
		/// (e.g., <c>Win64_Shipping_Client</c> or <c>Gaming.Desktop.x64_Shipping_Client</c>).
		/// </param>
		/// <param name="token">Cancellation token for cooperative cancellation.</param>
		/// <returns>An <see cref="UnblockResult"/> summarising how many files were unblocked.</returns>
		public static async Task<UnblockResult> UnblockBLSEFilesAsync(
			string sourceBinDir,
			CancellationToken token = default) {
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("DllUnblocker: Starting BLSE unblock scan.", new { SourceBinDir = sourceBinDir });
			}
			if (string.IsNullOrWhiteSpace(sourceBinDir) || !Directory.Exists(sourceBinDir)) {
				_logger.Warning($"DllUnblocker: BLSE source directory does not exist: '{sourceBinDir}'");
				return new UnblockResult();
			}
			return await Task.Run(() => {
				UnblockResult result = new();
				string[] allFiles;
				try {
					allFiles = Directory.GetFiles(sourceBinDir, "*.*", SearchOption.TopDirectoryOnly);
				} catch (Exception ex) {
					_logger.Error(ex, $"DllUnblocker: Failed to enumerate BLSE files in: '{sourceBinDir}'");
					return result;
				}

				foreach (string filePath in allFiles) {
					token.ThrowIfCancellationRequested();

					bool blocked = IsFileBlocked(filePath);
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("DllUnblocker: Checking BLSE file.", new { FilePath = filePath, Blocked = blocked });
					}
					if (!blocked) {
						continue;
					}

					if (TryUnblockFile(filePath)) {
						result.UnblockedCount++;
					} else {
						result.FailedCount++;
						result.FailedFiles.Add(filePath);
					}
				}

				_logger.Info($"DllUnblocker: BLSE unblock — {result.ToSummaryString()} in '{sourceBinDir}'");
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("DllUnblocker: BLSE unblock complete.", new { SourceBinDir = sourceBinDir, Unblocked = result.UnblockedCount, Failed = result.FailedCount });
				}
				return result;
			}, token);
		}

		#region Private Logic

		private static UnblockResult UnblockDirectory(string directoryPath, CancellationToken token) {
			UnblockResult result = new();
			string[] dllFiles;
			try {
				dllFiles = Directory.GetFiles(directoryPath, "*.dll", SearchOption.AllDirectories);
			} catch (Exception ex) {
				_logger.Error(ex, $"DllUnblocker: Failed to enumerate DLL files in: '{directoryPath}'");
				return result;
			}

			foreach (string dllPath in dllFiles) {
				token.ThrowIfCancellationRequested();

				bool blocked = IsFileBlocked(dllPath);
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("DllUnblocker: Checking DLL file.", new { FilePath = dllPath, Blocked = blocked });
				}
				if (!blocked) {
					continue;
				}

				if (TryUnblockFile(dllPath)) {
					result.UnblockedCount++;
				} else {
					result.FailedCount++;
					result.FailedFiles.Add(dllPath);
				}
			}

			_logger.Info($"DllUnblocker: {result.ToSummaryString()} in '{directoryPath}'");
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("DllUnblocker: Unblock complete.", new { DirectoryPath = directoryPath, Unblocked = result.UnblockedCount, Failed = result.FailedCount });
			}
			return result;
		}

		/// <summary>
		/// Checks whether a file has a <c>Zone.Identifier</c> ADS by attempting
		/// to read it. If the stream exists, the file is blocked.
		/// </summary>
		private static bool IsFileBlocked(string filePath) {
			string adsPath = filePath + ZoneIdentifierSuffix;
			try {
				return File.Exists(adsPath);
			} catch {
				return false;
			}
		}

		/// <summary>
		/// Removes the <c>Zone.Identifier</c> ADS from a file using the Win32
		/// <c>DeleteFile</c> API. <see cref="System.IO.File.Delete"/> does not
		/// support deleting alternate data streams.
		/// </summary>
		private static bool TryUnblockFile(string filePath) {
			string adsPath = filePath + ZoneIdentifierSuffix;
			try {
				bool deleted = DeleteFile(adsPath);
				if (!deleted) {
					int errorCode = Marshal.GetLastWin32Error();
					_logger.Warning($"DllUnblocker: Failed to unblock '{filePath}'. Win32 error: {errorCode}");
				}
				return deleted;
			} catch (Exception ex) {
				_logger.Error(ex, $"DllUnblocker: Exception unblocking '{filePath}'");
				return false;
			}
		}

		#endregion
	}
}
