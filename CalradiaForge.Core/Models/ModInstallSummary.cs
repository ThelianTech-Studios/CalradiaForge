namespace CalradiaForge.Core.Models {
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Aggregated result of a batch mod installation operation.
	/// </summary>
	public sealed class ModInstallSummary {
		public List<ModInstallResult> Results { get; set; } = [];

		public int InstalledCount => Results.Count(r => r.Status == ModInstallStatus.Installed);
		public int UpgradedCount => Results.Count(r => r.Status == ModInstallStatus.Upgraded);
		public int SkippedCount => Results.Count(r => r.Status == ModInstallStatus.Skipped);
		public int FailedCount => Results.Count(r => r.Status == ModInstallStatus.Failed);
		public int TotalCount => Results.Count;

		/// <summary>
		/// Returns a user-friendly summary string suitable for a toast notification.
		/// </summary>
		public string ToSummaryString() {
			List<string> parts = [];
			if (InstalledCount > 0) parts.Add($"{InstalledCount} installed");
			if (UpgradedCount > 0) parts.Add($"{UpgradedCount} upgraded");
			if (SkippedCount > 0) parts.Add($"{SkippedCount} skipped (already installed)");
			if (FailedCount > 0) parts.Add($"{FailedCount} failed");
			return parts.Count > 0
				? string.Join(", ", parts)
				: "No mods processed.";
		}
	}
}
