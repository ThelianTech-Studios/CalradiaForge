namespace CalradiaForge.Tests.Core.Results;

using CalradiaForge.Core.Models;

public sealed class ModInstallOperationResultTests {
	[Fact]
	public void MutableChildInputsAndRetrievedSnapshots_CannotChangeTerminalObservations() {
		ModInstallSummary sourceSummary = new() {
			Results = [new ModInstallResult {
				ArchiveFileName = "example.zip",
				ModuleId = "Example.Mod",
				Status = ModInstallStatus.Installed
			}]
		};
		UnblockResult sourceUnblock = new() {
			UnblockedCount = 2,
			FailedFiles = ["one.dll"]
		};
		ModInstallDiagnosticCode[] sourceCodes = [ModInstallDiagnosticCode.UnblockCompletedWithFailures];
		string[] sourceDiagnostics = ["original diagnostic"];
		ModInstallOperationResult result = new() {
			OperationId = Guid.NewGuid(),
			Status = ModInstallOperationStatus.SucceededWithWarnings,
			Summary = sourceSummary,
			UnblockResult = sourceUnblock,
			DiagnosticCodes = sourceCodes,
			TechnicalDiagnostics = sourceDiagnostics
		};

		sourceSummary.Results.Clear();
		sourceUnblock.UnblockedCount = 99;
		sourceUnblock.FailedFiles.Clear();
		sourceCodes[0] = ModInstallDiagnosticCode.InstallerFailed;
		sourceDiagnostics[0] = "mutated source";

		ModInstallSummary retrievedSummary = result.Summary;
		retrievedSummary.Results[0].ModuleId = "Mutated.Mod";
		retrievedSummary.Results.Clear();
		UnblockResult retrievedUnblock = Assert.IsType<UnblockResult>(result.UnblockResult);
		retrievedUnblock.UnblockedCount = 77;
		retrievedUnblock.FailedFiles.Clear();

		Assert.Equal("Example.Mod", Assert.Single(result.Summary.Results).ModuleId);
		UnblockResult observedUnblock = Assert.IsType<UnblockResult>(result.UnblockResult);
		Assert.Equal(2, observedUnblock.UnblockedCount);
		Assert.Equal("one.dll", Assert.Single(observedUnblock.FailedFiles));
		Assert.Equal(
			ModInstallDiagnosticCode.UnblockCompletedWithFailures,
			Assert.Single(result.DiagnosticCodes));
		Assert.Equal("original diagnostic", Assert.Single(result.TechnicalDiagnostics));
		Assert.Throws<NotSupportedException>(() =>
			((IList<ModInstallDiagnosticCode>)result.DiagnosticCodes)[0] = ModInstallDiagnosticCode.InstallerFailed);
		Assert.Throws<NotSupportedException>(() =>
			((IList<string>)result.TechnicalDiagnostics)[0] = "mutated observation");
	}
}
