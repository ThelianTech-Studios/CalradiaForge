namespace CalradiaForge.Tests.Core.Results;

using CalradiaForge.Core.Models;

public sealed class ModInstallSummaryTests {
	[Fact]
	public void Counts_ExcludeBLSEFromNormalInstalledTotal() {
		ModInstallSummary summary = new() {
			Results = [
				new ModInstallResult { ModuleId = "Normal", Status = ModInstallStatus.Installed },
				new ModInstallResult { ModuleId = "BLSE", Status = ModInstallStatus.Installed }
			]
		};

		Assert.Equal(1, summary.InstalledCount);
		Assert.NotNull(summary.BLSEResult);
		Assert.Contains("BLSE installed", summary.ToSummaryString());
	}

	[Fact]
	public void ToSummaryString_SurfacesFirstNormalFailureReason() {
		ModInstallSummary summary = new() {
			Results = [new ModInstallResult {
				ArchiveFileName = "unsafe.zip",
				Status = ModInstallStatus.Failed,
				Message = "Unsafe archive blocked"
			}]
		};

		Assert.Contains("unsafe.zip: Unsafe archive blocked", summary.ToSummaryString());
	}
}
