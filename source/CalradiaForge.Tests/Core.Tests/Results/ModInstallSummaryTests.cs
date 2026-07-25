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
		Assert.Equal(ModInstallStatus.Installed, Assert.IsType<ModInstallResult>(summary.BLSEResult).Status);
	}

	[Fact]
	public void FailedArchive_RemainsAvailableForUiOwnedFormatting() {
		ModInstallSummary summary = new() {
			Results = [new ModInstallResult {
				ArchiveFileName = "unsafe.zip",
				Status = ModInstallStatus.Failed,
				Message = "Unsafe archive blocked"
			}]
		};

		ModInstallResult failure = Assert.Single(summary.Results);
		Assert.Equal("unsafe.zip", failure.ArchiveFileName);
		Assert.Equal("Unsafe archive blocked", failure.Message);
		Assert.Equal(ModInstallStatus.Failed, failure.Status);
	}
}
