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
		/// Returns the retained English diagnostic summary used by Core logging and
		/// compatibility tests. This is not an authoritative user-notification
		/// formatter; localized install presentation belongs to the UI presenter.
		/// </summary>
		[Obsolete("Install notification formatting belongs to the UI install notification presenter.")]
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

			// Surface one concise normal-archive failure reason through the existing
			// status/toast pipeline without moving presentation ownership into Core.
			ModInstallResult? firstNormalFailure = Results.FirstOrDefault(r =>
				r.Status == ModInstallStatus.Failed && !IsBLSEResult(r));
			if (firstNormalFailure is not null && !string.IsNullOrWhiteSpace(firstNormalFailure.Message)) {
				string label = string.IsNullOrWhiteSpace(firstNormalFailure.ModuleName)
					? firstNormalFailure.ArchiveFileName
					: firstNormalFailure.ModuleName;
				parts.Add($"{label}: {firstNormalFailure.Message}");
			}

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
