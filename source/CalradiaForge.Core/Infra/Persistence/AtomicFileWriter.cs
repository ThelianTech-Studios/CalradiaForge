namespace CalradiaForge.Core.Infra.Persistence {
	using System;
	using System.IO;
	using System.Text;

	/// <summary>
	/// Writes text through a temporary file in the destination directory and then
	/// atomically replaces the destination.
	/// </summary>
	internal static class AtomicFileWriter {
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
					File.Replace(temporaryFilePath, fullDestinationPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
				} else {
					File.Move(temporaryFilePath, fullDestinationPath);
				}
			} finally {
				if (File.Exists(temporaryFilePath)) {
					File.Delete(temporaryFilePath);
				}
			}
		}
	}
}
