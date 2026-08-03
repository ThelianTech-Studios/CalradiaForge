namespace CalradiaForge.Core.Infra.Persistence {
	using System;
	using System.IO;
	using System.Text;
	using System.Threading;

	/// <summary>
	/// Writes text through a temporary file in the destination directory and then
	/// atomically replaces the destination.
	/// </summary>
	internal static class AtomicFileWriter {
		private const int ErrorSharingViolation = 32;
		private const int ErrorLockViolation = 33;
		private const int ErrorUnableToRemoveReplaced = 1175;
		private static readonly TimeSpan[] _replaceRetryDelays = [
			TimeSpan.FromMilliseconds(25),
			TimeSpan.FromMilliseconds(50),
			TimeSpan.FromMilliseconds(100),
			TimeSpan.FromMilliseconds(200)
		];

		/// <summary>
		/// Writes <paramref name="contents"/> without exposing a partially written
		/// destination file.
		/// </summary>
		/// <param name="destinationFilePath">The file that should receive the completed contents.</param>
		/// <param name="contents">The complete text to write.</param>
		public static void WriteAllText(string destinationFilePath, string contents) {
			if (string.IsNullOrWhiteSpace(destinationFilePath)) {
				throw new ArgumentException("Destination file path cannot be null or whitespace.", nameof(destinationFilePath));
			}

			ArgumentNullException.ThrowIfNull(contents);

			string fullDestinationPath = Path.GetFullPath(destinationFilePath);
			string destinationDirectory = Path.GetDirectoryName(fullDestinationPath)
				?? throw new ArgumentException("Destination file path must include a valid directory.", nameof(destinationFilePath));
			string temporaryFilePath = Path.Combine(
				destinationDirectory,
				$".{Path.GetFileName(fullDestinationPath)}.{Guid.NewGuid():N}.tmp");

			try {
				using (FileStream stream = new(
					temporaryFilePath,
					FileMode.CreateNew,
					FileAccess.Write,
					FileShare.None,
					bufferSize: 4096,
					FileOptions.WriteThrough)) {
					using StreamWriter writer = new(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
					writer.Write(contents);
					writer.Flush();
					stream.Flush(flushToDisk: true);
				}

				if (File.Exists(fullDestinationPath)) {
					ReplaceFileWithRetry(temporaryFilePath, fullDestinationPath);
				} else {
					File.Move(temporaryFilePath, fullDestinationPath);
				}
			} finally {
				if (File.Exists(temporaryFilePath)) {
					File.Delete(temporaryFilePath);
				}
			}
		}

		private static void ReplaceFileWithRetry(string temporaryFilePath, string fullDestinationPath) {
			for (int attempt = 0; ; attempt++) {
				try {
					File.Replace(temporaryFilePath, fullDestinationPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
					return;
				} catch (IOException ex) when (attempt < _replaceRetryDelays.Length && IsRetryableReplaceError(ex)) {
					Thread.Sleep(_replaceRetryDelays[attempt]);
				}
			}
		}

		private static bool IsRetryableReplaceError(IOException exception) {
			int errorCode = exception.HResult & 0xFFFF;
			return errorCode is
				ErrorSharingViolation or
				ErrorLockViolation or
				ErrorUnableToRemoveReplaced;
		}
	}
}
