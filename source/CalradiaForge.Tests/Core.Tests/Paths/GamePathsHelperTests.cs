namespace CalradiaForge.Tests.Core.Paths;

using System.Text;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.GamePlatform;
using CalradiaForge.Core.Infra.GamePlatform.Steam;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Infra.Paths;
using CalradiaForge.Tests.Core.Support;

public sealed class GamePathsHelperTests {
	private const string BannerlordAppId = "261550";
	private const string BannerlordInstallDirectory = "Mount & Blade II Bannerlord";

	[Fact]
	public void ResolveAndApplySteamGamePaths_WhenBannerlordUsesMainSteamLibrary_DetectsSteamPaths() {
		using TestDirectory temp = new();
		string steamClientRoot = CreateLibrary(temp, "C", "Program Files (x86)", "Steam");
		WriteLibraryFolders(steamClientRoot, steamClientRoot);
		string gameRoot = CreateValidBannerlordInstall(temp, steamClientRoot);
		string workshopRoot = CreateWorkshopContent(steamClientRoot);
		AppConfigSettings settings = CreatePathResolutionSettings(temp);

		SteamResolutionResult? result = GamePathsHelper.ResolveAndApplySteamGamePathsWithProvider(
			settings,
			new FakeSteamClientRootProvider(steamClientRoot),
			preserveExistingWorkshopPath: true);

		Assert.NotNull(result);
		Assert.Equal(SteamResolutionStatus.SteamGameResolved, result.Status);
		Assert.Equal(WorkshopPathSource.BannerlordLibrary, result.WorkshopPathSource);
		Assert.Equal(GameProvider.Steam, settings.GameProvider);
		Assert.Equal(gameRoot, settings.GameFolderPath);
		Assert.Equal(GetLauncherPath(gameRoot), settings.GameLauncherFilePath);
		Assert.Equal(workshopRoot, settings.SteamWorkshopFolderPath);
	}

	[Fact]
	public void ResolveAndApplySteamGamePaths_WhenBannerlordExistsOnlyInAlternateSteamLibrary_DetectsSteamPaths() {
		using TestDirectory temp = new();
		SteamPathFixture fixture = CreateAlternateLibraryFixture(temp, includePrimaryWorkshop: true);
		AppConfigSettings settings = CreatePathResolutionSettings(temp);

		SteamResolutionResult? result = GamePathsHelper.ResolveAndApplySteamGamePathsWithProvider(
			settings,
			new FakeSteamClientRootProvider(fixture.SteamClientRoot),
			preserveExistingWorkshopPath: true);

		Assert.NotNull(result);
		Assert.Equal(SteamResolutionStatus.SteamGameResolved, result.Status);
		Assert.Equal(fixture.BannerlordLibraryRoot, result.BannerlordLibraryRoot);
		Assert.Equal(fixture.BannerlordGameRoot, settings.GameFolderPath);
		Assert.Equal(fixture.BannerlordWorkshopRoot, settings.SteamWorkshopFolderPath);
		Assert.Equal(GameProvider.Steam, settings.GameProvider);
		Assert.False(Directory.Exists(Path.Combine(
			fixture.SteamClientRoot,
			"steamapps",
			"common",
			BannerlordInstallDirectory)));
	}

	[Fact]
	public async Task ResolveAndApplySteamGamePaths_WhenWorkshopIsMissing_KeepsSteamClassificationAndScansLocalModules() {
		using TestDirectory temp = new();
		SteamPathFixture fixture = CreateAlternateLibraryFixture(temp, includePrimaryWorkshop: false);
		AppConfigSettings settings = CreatePathResolutionSettings(temp);

		SteamResolutionResult? result = GamePathsHelper.ResolveAndApplySteamGamePathsWithProvider(
			settings,
			new FakeSteamClientRootProvider(fixture.SteamClientRoot),
			preserveExistingWorkshopPath: true);

		Assert.NotNull(result);
		Assert.Equal(SteamResolutionStatus.SteamGameResolvedWithoutWorkshop, result.Status);
		Assert.Equal(WorkshopPathSource.None, result.WorkshopPathSource);
		Assert.Equal(GameProvider.Steam, settings.GameProvider);
		Assert.Equal(fixture.BannerlordGameRoot, settings.GameFolderPath);
		Assert.Equal(string.Empty, settings.SteamWorkshopFolderPath);
		Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "PrimaryWorkshopPathUnavailable");
		Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "WorkshopPathUnavailable");

		var scannedModules = await ModScanner.ScanForModsAsync(settings);
		Assert.Single(scannedModules);
		Assert.Equal("Native", scannedModules[0].ModuleId);
	}

	[Fact]
	public void ResolveBannerlord_WhenAlternatesContainWorkshop_PrefersValidWorkshopManifest() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Program Files (x86)", "Steam");
		string bannerlordLibrary = CreateLibrary(temp, "D", "SteamLibrary");
		string manifestlessWorkshopLibrary = CreateLibrary(temp, "E", "SteamLibrary");
		string manifestBackedWorkshopLibrary = CreateLibrary(temp, "F", "SteamLibrary");
		WriteLibraryFolders(clientRoot, bannerlordLibrary, manifestlessWorkshopLibrary, manifestBackedWorkshopLibrary);
		CreateValidBannerlordInstall(temp, bannerlordLibrary);
		CreateWorkshopContent(manifestlessWorkshopLibrary);
		string expectedWorkshop = CreateWorkshopContent(manifestBackedWorkshopLibrary);
		WriteWorkshopManifest(manifestBackedWorkshopLibrary);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(SteamResolutionStatus.SteamGameResolved, result.Status);
		Assert.Equal(WorkshopPathSource.AlternateSteamLibrary, result.WorkshopPathSource);
		Assert.Equal(expectedWorkshop, result.WorkshopFolderPath);
		Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AlternateWorkshopPathSelected");
	}

	[Fact]
	public void ResolveBannerlord_WhenPrimaryAndAlternateWorkshopExist_PrefersBannerlordLibrary() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Program Files (x86)", "Steam");
		string bannerlordLibrary = CreateLibrary(temp, "D", "SteamLibrary");
		WriteLibraryFolders(clientRoot, bannerlordLibrary);
		CreateValidBannerlordInstall(temp, bannerlordLibrary);
		CreateWorkshopContent(clientRoot);
		WriteWorkshopManifest(clientRoot);
		string primaryWorkshop = CreateWorkshopContent(bannerlordLibrary);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(WorkshopPathSource.BannerlordLibrary, result.WorkshopPathSource);
		Assert.Equal(primaryWorkshop, result.WorkshopFolderPath);
	}

	[Fact]
	public void ResolveAndApplySteamGamePaths_WhenExistingWorkshopPathIsValid_PreservesIt() {
		using TestDirectory temp = new();
		SteamPathFixture fixture = CreateAlternateLibraryFixture(temp, includePrimaryWorkshop: true);
		string configuredWorkshop = temp.CreateDirectory("ManualWorkshop", "261550");
		AppConfigSettings settings = CreatePathResolutionSettings(temp);
		settings.SteamWorkshopFolderPath = configuredWorkshop;

		SteamResolutionResult? result = GamePathsHelper.ResolveAndApplySteamGamePathsWithProvider(
			settings,
			new FakeSteamClientRootProvider(fixture.SteamClientRoot),
			preserveExistingWorkshopPath: true);

		Assert.NotNull(result);
		Assert.Equal(WorkshopPathSource.ExistingConfiguration, result.WorkshopPathSource);
		Assert.Equal(configuredWorkshop, settings.SteamWorkshopFolderPath);
	}

	[Fact]
	public void ResolveAndApplySteamGamePaths_WhenConfiguredWorkshopPathIsMissing_ReportsRejectionAndUsesPrimary() {
		using TestDirectory temp = new();
		SteamPathFixture fixture = CreateAlternateLibraryFixture(temp, includePrimaryWorkshop: true);
		string missingConfiguredWorkshop = temp.GetPath("MissingWorkshop", "261550");
		AppConfigSettings settings = CreatePathResolutionSettings(temp);
		settings.SteamWorkshopFolderPath = missingConfiguredWorkshop;

		SteamResolutionResult? result = GamePathsHelper.ResolveAndApplySteamGamePathsWithProvider(
			settings,
			new FakeSteamClientRootProvider(fixture.SteamClientRoot),
			preserveExistingWorkshopPath: true);

		Assert.NotNull(result);
		Assert.Equal(WorkshopPathSource.BannerlordLibrary, result.WorkshopPathSource);
		Assert.Equal(fixture.BannerlordWorkshopRoot, settings.SteamWorkshopFolderPath);
		Assert.Contains(result.Diagnostics, diagnostic =>
			diagnostic.Code == "ExistingWorkshopPathRejected"
			&& diagnostic.Path == missingConfiguredWorkshop);
	}

	[Fact]
	public void ResolveAndApplySteamGamePaths_WhenExplicitRedetectRequested_RecomputesWorkshopPath() {
		using TestDirectory temp = new();
		SteamPathFixture fixture = CreateAlternateLibraryFixture(temp, includePrimaryWorkshop: true);
		string configuredWorkshop = temp.CreateDirectory("ManualWorkshop", "261550");
		AppConfigSettings settings = CreatePathResolutionSettings(temp);
		settings.SteamWorkshopFolderPath = configuredWorkshop;

		SteamResolutionResult? result = GamePathsHelper.ResolveAndApplySteamGamePathsWithProvider(
			settings,
			new FakeSteamClientRootProvider(fixture.SteamClientRoot),
			preserveExistingWorkshopPath: false);

		Assert.NotNull(result);
		Assert.Equal(WorkshopPathSource.BannerlordLibrary, result.WorkshopPathSource);
		Assert.Equal(fixture.BannerlordWorkshopRoot, settings.SteamWorkshopFolderPath);
		Assert.NotEqual(configuredWorkshop, settings.SteamWorkshopFolderPath);
	}

	[Fact]
	public void TryRepairSteamProviderForGameFolder_WhenSelectedPathMatchesManifest_RepairsProvider() {
		using TestDirectory temp = new();
		SteamPathFixture fixture = CreateAlternateLibraryFixture(temp, includePrimaryWorkshop: true);
		string configuredWorkshop = temp.CreateDirectory("ConfiguredWorkshop", "261550");
		AppConfigSettings settings = CreatePathResolutionSettings(temp);
		settings.GameProvider = GameProvider.StandAlone;
		settings.GameFolderPath = fixture.BannerlordGameRoot;
		settings.SteamWorkshopFolderPath = configuredWorkshop;

		bool repaired = GamePathsHelper.TryRepairSteamProviderWithResolver(
			settings,
			fixture.BannerlordGameRoot,
			new FakeSteamClientRootProvider(fixture.SteamClientRoot),
			new SteamInstallationResolver());

		Assert.True(repaired);
		Assert.Equal(GameProvider.Steam, settings.GameProvider);
		Assert.Equal(fixture.BannerlordGameRoot, settings.GameFolderPath);
		Assert.Equal(configuredWorkshop, settings.SteamWorkshopFolderPath);
	}

	[Fact]
	public void TryRepairSteamProviderForGameFolder_WhenSelectedPathDoesNotMatchSteam_ClassifiesStandalone() {
		using TestDirectory temp = new();
		SteamPathFixture fixture = CreateAlternateLibraryFixture(temp, includePrimaryWorkshop: true);
		string selectedGameRoot = CreateValidBannerlordInstall(temp, CreateLibrary(temp, "E", "Games"));
		AppConfigSettings settings = CreatePathResolutionSettings(temp);
		settings.GameProvider = GameProvider.Steam;
		settings.SteamWorkshopFolderPath = fixture.BannerlordWorkshopRoot;

		bool repaired = GamePathsHelper.TryRepairSteamProviderWithResolver(
			settings,
			selectedGameRoot,
			new FakeSteamClientRootProvider(fixture.SteamClientRoot),
			new SteamInstallationResolver());

		Assert.False(repaired);
		Assert.Equal(GameProvider.StandAlone, settings.GameProvider);
		Assert.Equal(selectedGameRoot, settings.GameFolderPath);
		Assert.Equal(GetLauncherPath(selectedGameRoot), settings.GameLauncherFilePath);
		Assert.Empty(settings.SteamWorkshopFolderPath);
	}

	[Fact]
	public void ResolveBannerlord_WhenLibraryFoldersIsMalformed_UsesValidMainLibraryFallback() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Program Files (x86)", "Steam");
		WriteLibraryFoldersContent(clientRoot, "\"libraryfolders\" { \"0\" {");
		string gameRoot = CreateValidBannerlordInstall(temp, clientRoot);
		string workshopRoot = CreateWorkshopContent(clientRoot);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(SteamResolutionStatus.SteamGameResolved, result.Status);
		Assert.Equal(gameRoot, result.GameFolderPath);
		Assert.Equal(workshopRoot, result.WorkshopFolderPath);
		Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "LibraryFoldersParseFailed");
	}

	[Fact]
	public void ResolveBannerlord_WhenManifestIsMalformed_ReturnsManifestInvalid() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Program Files (x86)", "Steam");
		WriteLibraryFolders(clientRoot, clientRoot);
		CreateValidGameMarkers(temp, clientRoot, BannerlordInstallDirectory);
		WriteManifestContent(clientRoot, "\"AppState\" { \"appid\" \"261550\"");

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(SteamResolutionStatus.BannerlordManifestInvalid, result.Status);
		Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "BannerlordManifestParseFailed");
	}

	[Fact]
	public void ResolveBannerlord_WhenManifestHasWrongAppId_ReturnsManifestInvalid() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Program Files (x86)", "Steam");
		WriteLibraryFolders(clientRoot, clientRoot);
		CreateValidGameMarkers(temp, clientRoot, BannerlordInstallDirectory);
		WriteAppManifest(clientRoot, appId: "570", installDirectory: BannerlordInstallDirectory);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(SteamResolutionStatus.BannerlordManifestInvalid, result.Status);
		Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "BannerlordManifestAppIdMismatch");
	}

	[Fact]
	public void ResolveBannerlord_WhenManifestHasNoInstallDir_ReturnsManifestInvalid() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Program Files (x86)", "Steam");
		WriteLibraryFolders(clientRoot, clientRoot);
		WriteAppManifest(clientRoot, appId: BannerlordAppId, installDirectory: null);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(SteamResolutionStatus.BannerlordManifestInvalid, result.Status);
		Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "BannerlordInstallDirMissing");
	}

	[Fact]
	public void ResolveBannerlord_WhenLibraryCatalogContainsCaseVariantDuplicates_EvaluatesRootOnce() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Program Files (x86)", "Steam");
		WriteLibraryFolders(
			clientRoot,
			clientRoot,
			clientRoot.ToUpperInvariant(),
			clientRoot + Path.DirectorySeparatorChar);
		CreateValidBannerlordInstall(temp, clientRoot);
		CreateWorkshopContent(clientRoot);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Single(result.LibraryRoots);
		Assert.Equal(clientRoot, result.LibraryRoots[0]);
		Assert.True(result.Diagnostics.Count(diagnostic => diagnostic.Code == "DuplicateLibraryRemoved") >= 2);
	}

	[Fact]
	public void ResolveBannerlord_WhenRegisteredLibraryIsMissing_ContinuesToValidLibrary() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Program Files (x86)", "Steam");
		string missingLibrary = temp.GetPath("D", "MissingSteamLibrary");
		string validLibrary = CreateLibrary(temp, "E", "SteamLibrary");
		WriteLibraryFolders(clientRoot, missingLibrary, validLibrary);
		string gameRoot = CreateValidBannerlordInstall(temp, validLibrary);
		CreateWorkshopContent(validLibrary);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(SteamResolutionStatus.SteamGameResolved, result.Status);
		Assert.Equal(gameRoot, result.GameFolderPath);
		Assert.Contains(result.Diagnostics, diagnostic =>
			diagnostic.Code == "LibraryUnavailable" && diagnostic.Path == missingLibrary);
	}

	[Fact]
	public void ResolveBannerlord_WhenFallbackCandidatesHaveEqualEvidence_UsesDiscoveryOrder() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Program Files (x86)", "Steam");
		string bannerlordLibrary = CreateLibrary(temp, "D", "SteamLibrary");
		string firstFallback = CreateLibrary(temp, "F", "SteamLibrary");
		string secondFallback = CreateLibrary(temp, "E", "SteamLibrary");
		WriteLibraryFolders(clientRoot, bannerlordLibrary, firstFallback, secondFallback);
		CreateValidBannerlordInstall(temp, bannerlordLibrary);
		string expectedWorkshop = CreateWorkshopContent(firstFallback);
		CreateWorkshopContent(secondFallback);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(WorkshopPathSource.AlternateSteamLibrary, result.WorkshopPathSource);
		Assert.Equal(expectedWorkshop, result.WorkshopFolderPath);
	}

	[Theory]
	[InlineData("..", "BannerlordInstallDirEscapesLibrary")]
	[InlineData(@"C:\OutsideBannerlord", "BannerlordInstallDirUnsafe")]
	public void ResolveBannerlord_WhenInstallDirEscapesCommonRoot_RejectsManifest(
		string unsafeInstallDirectory,
		string expectedDiagnosticCode) {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Program Files (x86)", "Steam");
		WriteLibraryFolders(clientRoot, clientRoot);
		WriteAppManifest(clientRoot, appId: BannerlordAppId, installDirectory: unsafeInstallDirectory);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(SteamResolutionStatus.BannerlordManifestInvalid, result.Status);
		Assert.Null(result.GameFolderPath);
		Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == expectedDiagnosticCode);
	}

	private static SteamPathFixture CreateAlternateLibraryFixture(
		TestDirectory temp,
		bool includePrimaryWorkshop) {
		string steamClientRoot = CreateLibrary(temp, "C", "Program Files (x86)", "Steam");
		string bannerlordLibraryRoot = CreateLibrary(temp, "D", "SteamLibrary");
		WriteLibraryFolders(steamClientRoot, bannerlordLibraryRoot);
		string bannerlordGameRoot = CreateValidBannerlordInstall(temp, bannerlordLibraryRoot);
		string bannerlordWorkshopRoot = GetWorkshopPath(bannerlordLibraryRoot);
		if (includePrimaryWorkshop) {
			CreateWorkshopContent(bannerlordLibraryRoot);
		}
		return new SteamPathFixture(
			steamClientRoot,
			bannerlordLibraryRoot,
			bannerlordGameRoot,
			bannerlordWorkshopRoot);
	}

	private static string CreateLibrary(TestDirectory temp, params string[] segments) {
		return temp.CreateDirectory(segments);
	}

	private static string CreateValidBannerlordInstall(
		TestDirectory temp,
		string libraryRoot,
		string installDirectory = BannerlordInstallDirectory) {
		string gameRoot = CreateValidGameMarkers(temp, libraryRoot, installDirectory);
		WriteAppManifest(libraryRoot, BannerlordAppId, installDirectory);
		return gameRoot;
	}

	private static string CreateValidGameMarkers(
		TestDirectory temp,
		string libraryRoot,
		string installDirectory) {
		string gameRoot = Directory.CreateDirectory(Path.Combine(
			libraryRoot,
			"steamapps",
			"common",
			installDirectory)).FullName;
		string modulesRoot = Directory.CreateDirectory(Path.Combine(gameRoot, "Modules")).FullName;
		temp.WriteModule(modulesRoot, "Native", "Native");
		string launcherDirectory = Directory.CreateDirectory(Path.Combine(
			gameRoot,
			"bin",
			"Win64_Shipping_Client")).FullName;
		File.WriteAllText(Path.Combine(launcherDirectory, "Bannerlord.exe"), string.Empty);
		return gameRoot;
	}

	private static string CreateWorkshopContent(string libraryRoot) {
		return Directory.CreateDirectory(GetWorkshopPath(libraryRoot)).FullName;
	}

	private static string GetWorkshopPath(string libraryRoot) {
		return Path.Combine(libraryRoot, "steamapps", "workshop", "content", BannerlordAppId);
	}

	private static string GetLauncherPath(string gameRoot) {
		return Path.Combine(gameRoot, "bin", "Win64_Shipping_Client", "Bannerlord.exe");
	}

	private static void WriteLibraryFolders(string clientRoot, params string[] registeredRoots) {
		StringBuilder content = new();
		content.AppendLine("\"libraryfolders\"");
		content.AppendLine("{");
		for (int index = 0; index < registeredRoots.Length; index++) {
			content.AppendLine($"    \"{index}\"");
			content.AppendLine("    {");
			content.AppendLine($"        \"path\" \"{EscapePathMetadataValue(registeredRoots[index])}\"");
			content.AppendLine("        \"apps\"");
			content.AppendLine("        {");
			content.AppendLine($"            \"{BannerlordAppId}\" \"1\"");
			content.AppendLine("        }");
			content.AppendLine("    }");
		}
		content.AppendLine("}");
		WriteLibraryFoldersContent(clientRoot, content.ToString());
	}

	private static void WriteLibraryFoldersContent(string clientRoot, string content) {
		string steamApps = Directory.CreateDirectory(Path.Combine(clientRoot, "steamapps")).FullName;
		File.WriteAllText(Path.Combine(steamApps, "libraryfolders.vdf"), content);
	}

	private static void WriteAppManifest(
		string libraryRoot,
		string appId,
		string? installDirectory) {
		StringBuilder content = new();
		content.AppendLine("\"AppState\"");
		content.AppendLine("{");
		content.AppendLine($"    \"appid\" \"{EscapePathMetadataValue(appId)}\"");
		if (installDirectory is not null) {
			content.AppendLine($"    \"installdir\" \"{EscapePathMetadataValue(installDirectory)}\"");
		}
		content.AppendLine("}");
		WriteManifestContent(libraryRoot, content.ToString());
	}

	private static void WriteManifestContent(string libraryRoot, string content) {
		string steamApps = Directory.CreateDirectory(Path.Combine(libraryRoot, "steamapps")).FullName;
		File.WriteAllText(Path.Combine(steamApps, "appmanifest_261550.acf"), content);
	}

	private static void WriteWorkshopManifest(string libraryRoot, string appId = BannerlordAppId) {
		string workshopRoot = Directory.CreateDirectory(Path.Combine(
			libraryRoot,
			"steamapps",
			"workshop")).FullName;
		File.WriteAllText(
			Path.Combine(workshopRoot, "appworkshop_261550.acf"),
			$"\"AppWorkshop\"\n{{\n    \"appid\" \"{EscapePathMetadataValue(appId)}\"\n}}");
	}

	private static AppConfigSettings CreatePathResolutionSettings(TestDirectory temp) {
		return new AppConfigSettings(new AppConfig(temp.GetPath($"config-{Guid.NewGuid():N}.json")));
	}

	private static string EscapePathMetadataValue(string value) {
		return value
			.Replace("\\", "\\\\", StringComparison.Ordinal)
			.Replace("\"", "\\\"", StringComparison.Ordinal);
	}

	private sealed class FakeSteamClientRootProvider(string? steamClientRoot) : ISteamClientRootProvider {
		public string? GetSteamClientRoot() {
			return steamClientRoot;
		}
	}

	private sealed record SteamPathFixture(
		string SteamClientRoot,
		string BannerlordLibraryRoot,
		string BannerlordGameRoot,
		string BannerlordWorkshopRoot);
}
