namespace CalradiaForge.Core.Infra.Mods {
	using System;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Threading.Tasks;

	using CalradiaForge.Core.Models;

	using Serilog;

	/// <summary>
	/// Removes the <c>Zone.Identifier</c> alternate data stream (ADS) from files
	/// in a given directory. This "unblocks" files that Windows marks as downloaded
	/// from the internet, which prevents Bannerlord from loading them.
	/// </summary>
	public static class DLLUnblocker {
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
			Log.Debug("DllUnblocker: Starting unblock scan in {DirectoryPath}.", directoryPath);
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
			Log.Debug("DllUnblocker: Starting BLSE unblock scan in {SourceBinDirectory}.", sourceBinDir);
			if (string.IsNullOrWhiteSpace(sourceBinDir) || !Directory.Exists(sourceBinDir)) {
				Log.Warning("DllUnblocker: BLSE source directory does not exist: {SourceBinDirectory}.", sourceBinDir);
				return new UnblockResult();
			}
			return await Task.Run(() => {
				UnblockResult result = new();
				string[] allFiles;
				try {
					allFiles = Directory.GetFiles(sourceBinDir, "*.*", SearchOption.TopDirectoryOnly);
				} catch (Exception ex) {
					Log.Error(ex, "DllUnblocker: Failed to enumerate BLSE files in {SourceBinDirectory}.", sourceBinDir);
					return result;
				}

				foreach (string filePath in allFiles) {
					token.ThrowIfCancellationRequested();

					bool blocked = IsFileBlocked(filePath);
					Log.Debug("DllUnblocker: Checking BLSE file {FilePath}; blocked: {IsBlocked}.", filePath, blocked);
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

				Log.Information(
					"DllUnblocker: BLSE unblock {Summary} in {SourceBinDirectory}.",
					result.ToSummaryString(),
					sourceBinDir);
				Log.Debug(
					"DllUnblocker: BLSE unblock complete in {SourceBinDirectory}; unblocked: {UnblockedCount}; failed: {FailedCount}.",
					sourceBinDir,
					result.UnblockedCount,
					result.FailedCount);
				return result;
			}, token);
		}

		#region Private Logic

		/// <summary>
		/// Recursively scans a directory for blocked DLLs and attempts to unblock them.
		/// </summary>
		private static UnblockResult UnblockDirectory(string directoryPath, CancellationToken token) {
			UnblockResult result = new();
			string[] dllFiles;
			try {
				dllFiles = Directory.GetFiles(directoryPath, "*.dll", SearchOption.AllDirectories);
			} catch (Exception ex) {
				Log.Error(ex, "DllUnblocker: Failed to enumerate DLL files in {DirectoryPath}.", directoryPath);
				result.Succeeded = false;
				result.TechnicalDiagnostic = ex.ToString();
				return result;
			}

			foreach (string dllPath in dllFiles) {
				token.ThrowIfCancellationRequested();

				bool blocked = IsFileBlocked(dllPath);
				Log.Debug("DllUnblocker: Checking DLL file {FilePath}; blocked: {IsBlocked}.", dllPath, blocked);
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

			Log.Information("DllUnblocker: {Summary} in {DirectoryPath}.", result.ToSummaryString(), directoryPath);
			Log.Debug(
				"DllUnblocker: Unblock complete in {DirectoryPath}; unblocked: {UnblockedCount}; failed: {FailedCount}.",
				directoryPath,
				result.UnblockedCount,
				result.FailedCount);
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
					Log.Warning("DllUnblocker: Failed to unblock {FilePath}. Win32 error: {Win32ErrorCode}.", filePath, errorCode);
				}
				return deleted;
			} catch (Exception ex) {
				Log.Error(ex, "DllUnblocker: Exception unblocking {FilePath}.", filePath);
				return false;
			}
		}

		#endregion
	}
}
