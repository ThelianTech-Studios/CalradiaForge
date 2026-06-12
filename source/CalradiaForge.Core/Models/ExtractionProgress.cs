namespace CalradiaForge.Core.Models {
	using System;

	/// <summary>
	/// Batch-level extraction progress reported during mod installation.
	/// Tracks cumulative file counts across all archives so the UI can
	/// compute a single ETA for the entire batch rather than resetting
	/// per archive. Timestamps are captured on the extraction thread to
	/// avoid dispatcher-queue skew inflating ETA calculations.
	/// </summary>
	public sealed class ExtractionProgress {
		/// <summary>Current file entry being extracted within this archive (1-based).</summary>
		public int CurrentEntry { get; init; }

		/// <summary>Name of the archive file currently being extracted.</summary>
		public string ArchiveFileName { get; init; } = string.Empty;

		/// <summary>1-based index of this archive in the batch.</summary>
		public int ArchiveIndex { get; init; }

		/// <summary>Total archives in the batch.</summary>
		public int TotalArchives { get; init; }

		/// <summary>
		/// Cumulative files extracted across all archives in the batch so far,
		/// including the current archive's <see cref="CurrentEntry"/>.
		/// Used as the progress bar value.
		/// </summary>
		public int BatchFilesExtracted { get; init; }

		/// <summary>
		/// Estimated total files across the entire batch. Starts as a heuristic
		/// based on archive file sizes (~33 files/MB). Refined after each archive
		/// completes by replacing its estimate with the actual count.
		/// Used as the progress bar maximum.
		/// </summary>
		public int EstimatedTotalFiles { get; init; }

		/// <summary>
		/// UTC timestamp captured on the extraction thread when this progress
		/// event was created. The UI must use this instead of
		/// <see cref="DateTime.UtcNow"/> to avoid dispatcher-queue delay
		/// inflating the elapsed time and causing the ETA to climb upward.
		/// </summary>
		public DateTime TimestampUtc { get; init; }

		/// <summary>
		/// UTC timestamp of when the batch started processing. Captured once
		/// at the beginning of <c>InstallModsAsync</c> and passed through
		/// every progress event so the UI can compute a stable batch-level ETA.
		/// </summary>
		public DateTime BatchStartUtc { get; init; }
	}
}