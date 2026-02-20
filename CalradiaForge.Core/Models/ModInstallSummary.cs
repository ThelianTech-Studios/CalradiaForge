namespace CalradiaForge.Core.Models {
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Aggregated result of a batch mod installation operation.
	/// BLSE installs are tracked separately from mod counts because
	/// BLSE is a script extender, not a standard Bannerlord module.
	/// </summary>
	public sealed class ModInstallSummary {
		public List<ModInstallResult> Results { get; set; } = [];

		/// <summary>
		/// Number of standard mods installed (excludes BLSE).
		/// </summary>
		public int InstalledCount => Results.Count(r =>
			r.Status == ModInstallStatus.Installed && !IsBLSEResult(r));

		/// <summary>
		/// Number of standard mods upgraded (excludes BLSE).
		/// </summary>
		public int UpgradedCount => Results.Count(r =>
			r.Status == ModInstallStatus.Upgraded && !IsBLSEResult(r));

		public int SkippedCount => Results.Count(r => r.Status == ModInstallStatus.Skipped);
		public int FailedCount => Results.Count(r => r.Status == ModInstallStatus.Failed);
		public int TotalCount => Results.Count;

		/// <summary>
		/// The BLSE install result, if one was processed in this batch.
		/// <c>null</c> if no BLSE archive was included.
		/// </summary>
		public ModInstallResult? BLSEResult => Results.FirstOrDefault(r => IsBLSEResult(r));

		/// <summary>
		/// Returns a user-friendly summary string suitable for a toast notification.
		/// Appends BLSE status separately from mod counts.
		/// </summary>
		public string ToSummaryString() {
			List<string> parts = [];
			if (InstalledCount > 0)
				parts.Add($"{InstalledCount} installed");
			if (UpgradedCount > 0)
				parts.Add($"{UpgradedCount} upgraded");
			if (SkippedCount > 0)
				parts.Add($"{SkippedCount} skipped (already installed)");
			if (FailedCount > 0)
				parts.Add($"{FailedCount} failed");

			// Append BLSE result separately
			ModInstallResult? blse = BLSEResult;
			if (blse is not null) {
				string blseStatus = blse.Status == ModInstallStatus.Installed
					? "BLSE installed to game bin folder"
					: $"BLSE failed: {blse.Message}";
				parts.Add(blseStatus);
			}

			return parts.Count > 0
				? string.Join(", ", parts)
				: "No mods processed.";
		}

		/// <summary>
		/// Identifies a result as a BLSE install by its ModuleId sentinel value.
		/// </summary>
		private static bool IsBLSEResult(ModInstallResult result) {
			return string.Equals(result.ModuleId, "BLSE", StringComparison.OrdinalIgnoreCase);
		}
	}
}
