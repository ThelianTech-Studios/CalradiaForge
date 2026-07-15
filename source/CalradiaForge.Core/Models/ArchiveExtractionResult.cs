namespace CalradiaForge.Core.Models {
	/// <summary>
	/// Result of opening, validating, and extracting an archive into an
	/// application-managed temporary directory.
	/// </summary>
	public sealed class ArchiveExtractionResult {
		public bool Success { get; private init; }
		public string? TempDirectory { get; private init; }
		public string Message { get; private init; } = string.Empty;

		public static ArchiveExtractionResult Ok(string tempDirectory) => new() {
			Success = true,
			TempDirectory = tempDirectory
		};

		public static ArchiveExtractionResult Fail(string message) => new() {
			Success = false,
			Message = message
		};
	}
}
