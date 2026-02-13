namespace CalradiaForge.Core.Infra.Mods {
	using System;
	using System.Collections.Generic;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Threading.Tasks;

	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Models;

	/// <summary>
	/// Removes the <c>Zone.Identifier</c> alternate data stream (ADS) from DLL files
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
			if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath)) {
				_logger.Warning($"DllUnblocker: Directory does not exist: '{directoryPath}'");
				return new UnblockResult();
			}
			return await Task.Run(() => UnblockDirectory(directoryPath, token), token);
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

				if (!IsFileBlocked(dllPath)) {
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
