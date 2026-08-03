namespace CalradiaForge.Tests.Core.GamePlatform;

using CalradiaForge.Core.Infra.GamePlatform.Steam;
using CalradiaForge.Tests.Core.Support;

using static GamePlatformTestFixture;

public sealed class SteamInstallationResolverTests {
	[Fact]
	public void ResolveBannerlord_WhenGameUsesClientLibrary_ResolvesGameAndPrimaryWorkshop() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Steam");
		WriteLibraryFolders(clientRoot, clientRoot);
		string gameRoot = CreateValidBannerlordInstall(temp, clientRoot);
		string workshopRoot = CreateWorkshopContent(clientRoot);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(SteamResolutionStatus.SteamGameResolved, result.Status);
		Assert.Equal(gameRoot, result.GameFolderPath);
		Assert.Equal(workshopRoot, result.WorkshopFolderPath);
		Assert.Equal(WorkshopPathSource.BannerlordLibrary, result.WorkshopPathSource);
	}

	[Fact]
	public void ResolveBannerlord_WhenGameUsesAlternateLibrary_ResolvesManifestBackedPath() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Steam");
		string gameLibrary = CreateLibrary(temp, "D", "SteamLibrary");
		WriteLibraryFolders(clientRoot, gameLibrary);
		string gameRoot = CreateValidBannerlordInstall(temp, gameLibrary);
		string workshopRoot = CreateWorkshopContent(gameLibrary);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(gameLibrary, result.BannerlordLibraryRoot);
		Assert.Equal(gameRoot, result.GameFolderPath);
		Assert.Equal(workshopRoot, result.WorkshopFolderPath);
		Assert.DoesNotContain(result.LibraryRoots, root => string.Equals(root, temp.GetPath("Unregistered")));
	}

	[Fact]
	public void ResolveBannerlord_WithReportedEndUserPaths_ResolvesGameAndWorkshopFromAlternateLibrary() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Program Files (x86)", "Steam");
		string gameLibrary = CreateLibrary(temp, "D", "SteamLibrary");
		WriteLibraryFolders(clientRoot, clientRoot, gameLibrary);
		string gameRoot = CreateValidBannerlordInstall(temp, gameLibrary);
		string workshopRoot = CreateWorkshopContent(gameLibrary);
		WriteWorkshopManifest(gameLibrary);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(SteamResolutionStatus.SteamGameResolved, result.Status);
		Assert.Equal(clientRoot, result.SteamClientRoot);
		Assert.Equal(gameLibrary, result.BannerlordLibraryRoot);
		Assert.Equal(gameRoot, result.GameFolderPath);
		Assert.Equal(workshopRoot, result.WorkshopFolderPath);
		Assert.Equal(WorkshopPathSource.BannerlordLibrary, result.WorkshopPathSource);
	}

	[Fact]
	public void ResolveBannerlord_WhenWorkshopIsMissing_KeepsSteamGameResolution() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Steam");
		string gameLibrary = CreateLibrary(temp, "D", "SteamLibrary");
		WriteLibraryFolders(clientRoot, gameLibrary);
		CreateValidBannerlordInstall(temp, gameLibrary);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.True(result.IsGameResolved);
		Assert.Equal(SteamResolutionStatus.SteamGameResolvedWithoutWorkshop, result.Status);
		Assert.Null(result.WorkshopFolderPath);
		Assert.Equal(WorkshopPathSource.None, result.WorkshopPathSource);
		Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "WorkshopPathUnavailable");
	}

	[Fact]
	public void ResolveBannerlord_WhenAlternatesContainWorkshop_PrefersValidWorkshopManifest() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Steam");
		string gameLibrary = CreateLibrary(temp, "D", "SteamLibrary");
		string manifestless = CreateLibrary(temp, "E", "SteamLibrary");
		string manifestBacked = CreateLibrary(temp, "F", "SteamLibrary");
		WriteLibraryFolders(clientRoot, gameLibrary, manifestless, manifestBacked);
		CreateValidBannerlordInstall(temp, gameLibrary);
		CreateWorkshopContent(manifestless);
		string expectedWorkshop = CreateWorkshopContent(manifestBacked);
		WriteWorkshopManifest(manifestBacked);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(WorkshopPathSource.AlternateSteamLibrary, result.WorkshopPathSource);
		Assert.Equal(expectedWorkshop, result.WorkshopFolderPath);
	}

	[Fact]
	public void ResolveBannerlord_WhenPrimaryAndAlternateWorkshopExist_SelectsOnlyPrimary() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Steam");
		string gameLibrary = CreateLibrary(temp, "D", "SteamLibrary");
		WriteLibraryFolders(clientRoot, gameLibrary);
		CreateValidBannerlordInstall(temp, gameLibrary);
		CreateWorkshopContent(clientRoot);
		WriteWorkshopManifest(clientRoot);
		string primaryWorkshop = CreateWorkshopContent(gameLibrary);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(primaryWorkshop, result.WorkshopFolderPath);
		Assert.Equal(WorkshopPathSource.BannerlordLibrary, result.WorkshopPathSource);
	}

	[Fact]
	public void ResolveBannerlord_WhenLibraryFoldersIsMalformed_StillEvaluatesClientLibrary() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Steam");
		WriteLibraryFoldersContent(clientRoot, "not valid key values {");
		string gameRoot = CreateValidBannerlordInstall(temp, clientRoot);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.True(result.IsGameResolved);
		Assert.Equal(gameRoot, result.GameFolderPath);
		Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "LibraryFoldersParseFailed");
	}

	[Fact]
	public void ResolveBannerlord_WhenManifestIsMalformed_ReturnsManifestInvalid() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Steam");
		WriteLibraryFolders(clientRoot, clientRoot);
		WriteManifestContent(clientRoot, "not valid key values {");

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(SteamResolutionStatus.BannerlordManifestInvalid, result.Status);
		Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "BannerlordManifestParseFailed");
	}

	[Fact]
	public void ResolveBannerlord_WhenManifestHasWrongAppId_ReturnsManifestInvalid() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Steam");
		WriteLibraryFolders(clientRoot, clientRoot);
		WriteAppManifest(clientRoot, "123", BannerlordInstallDirectory);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(SteamResolutionStatus.BannerlordManifestInvalid, result.Status);
		Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "BannerlordManifestAppIdMismatch");
	}

	[Fact]
	public void ResolveBannerlord_WhenManifestHasNoInstallDir_ReturnsManifestInvalid() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Steam");
		WriteLibraryFolders(clientRoot, clientRoot);
		WriteAppManifest(clientRoot, BannerlordAppId, null);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(SteamResolutionStatus.BannerlordManifestInvalid, result.Status);
		Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "BannerlordInstallDirMissing");
	}

	[Fact]
	public void ResolveBannerlord_WhenCatalogContainsCaseVariantDuplicates_EvaluatesRootOnce() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Steam");
		string library = CreateLibrary(temp, "D", "SteamLibrary");
		WriteLibraryFolders(clientRoot, library, library.ToUpperInvariant());
		CreateValidBannerlordInstall(temp, library);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(2, result.LibraryRoots.Count);
		Assert.Equal(2, result.LibraryRoots.Distinct(StringComparer.OrdinalIgnoreCase).Count());
	}

	[Fact]
	public void ResolveBannerlord_WhenRegisteredLibraryIsUnavailable_ContinuesToValidLibrary() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Steam");
		string missingLibrary = temp.GetPath("D", "MissingLibrary");
		string validLibrary = CreateLibrary(temp, "E", "SteamLibrary");
		WriteLibraryFolders(clientRoot, missingLibrary, validLibrary);
		string gameRoot = CreateValidBannerlordInstall(temp, validLibrary);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(gameRoot, result.GameFolderPath);
		Assert.Contains(result.Diagnostics, diagnostic =>
			diagnostic.Code == "LibraryUnavailable" && diagnostic.Path == missingLibrary);
	}

	[Fact]
	public void ResolveBannerlord_WhenFallbackCandidatesHaveEqualEvidence_UsesDiscoveryOrder() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Steam");
		string gameLibrary = CreateLibrary(temp, "D", "SteamLibrary");
		string firstFallback = CreateLibrary(temp, "F", "SteamLibrary");
		string secondFallback = CreateLibrary(temp, "E", "SteamLibrary");
		WriteLibraryFolders(clientRoot, gameLibrary, firstFallback, secondFallback);
		CreateValidBannerlordInstall(temp, gameLibrary);
		string expectedWorkshop = CreateWorkshopContent(firstFallback);
		CreateWorkshopContent(secondFallback);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(expectedWorkshop, result.WorkshopFolderPath);
	}

	[Fact]
	public void ResolveBannerlord_DoesNotSearchUnregisteredDirectories() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Steam");
		string registeredLibrary = CreateLibrary(temp, "D", "SteamLibrary");
		string decoyLibrary = CreateLibrary(temp, "Z", "UnregisteredLibrary");
		WriteLibraryFolders(clientRoot, registeredLibrary);
		string decoyGame = CreateValidBannerlordInstall(temp, decoyLibrary);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.False(result.IsGameResolved);
		Assert.DoesNotContain(decoyLibrary, result.LibraryRoots, StringComparer.OrdinalIgnoreCase);
		Assert.NotEqual(decoyGame, result.GameFolderPath);
	}

	[Theory]
	[InlineData("..", "BannerlordInstallDirEscapesLibrary")]
	[InlineData(@"C:\OutsideBannerlord", "BannerlordInstallDirUnsafe")]
	public void ResolveBannerlord_WhenInstallDirEscapesCommonRoot_RejectsManifest(
		string unsafeInstallDirectory,
		string expectedDiagnosticCode) {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Steam");
		WriteLibraryFolders(clientRoot, clientRoot);
		WriteAppManifest(clientRoot, BannerlordAppId, unsafeInstallDirectory);

		SteamResolutionResult result = new SteamInstallationResolver().ResolveBannerlord(clientRoot);

		Assert.Equal(SteamResolutionStatus.BannerlordManifestInvalid, result.Status);
		Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == expectedDiagnosticCode);
	}
}
