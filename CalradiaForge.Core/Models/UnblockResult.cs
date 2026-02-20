namespace CalradiaForge.Core.Models {
	using System.Collections.Generic;

	/// <summary>
	/// Result of a DLL unblock operation across a directory.
	/// </summary>
	public sealed class UnblockResult {
		public int UnblockedCount { get; set; }
		public int FailedCount { get; set; }
		public List<string> FailedFiles { get; set; } = [];

		/// <summary>
		/// Returns a user-friendly summary string suitable for a toast notification.
		/// </summary>
		public string ToSummaryString() {
			if (UnblockedCount == 0 && FailedCount == 0) {
				return "No files needed to be unblocked.";
			}
			List<string> parts = [];
			if (UnblockedCount > 0)
				parts.Add($"{UnblockedCount} file(s) unblocked");
			if (FailedCount > 0)
				parts.Add($"{FailedCount} file(s) failed to unblock");
			return string.Join(", ", parts);
		}
	}
}
