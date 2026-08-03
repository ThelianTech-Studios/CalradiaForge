namespace CalradiaForge.Tests.Core.GamePlatform;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.GamePlatform;
using CalradiaForge.Core.Infra.GamePlatform.Steam;
using CalradiaForge.Core.Infra.Paths;
using CalradiaForge.Tests.Core.Support;

using static GamePlatformTestFixture;

public sealed class GamePlatformDetectionResolverTests {
	[Fact]
	public void DetectGame_WhenBannerlordUsesAlternateLibrary_CommitsResolvedSteamState() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Steam");
		string gameLibrary = CreateLibrary(temp, "D", "SteamLibrary");
		WriteLibraryFolders(clientRoot, gameLibrary);
		string gameRoot = CreateValidBannerlordInstall(temp, gameLibrary);
		string workshopRoot = CreateWorkshopContent(gameLibrary);
		string blsePath = CreateBlseExecutable(gameRoot);
		AppSettings settings = CreateSettings(temp);
		GamePlatformDetectionResolver resolver = CreateResolver(clientRoot);

		GameProvider provider = resolver.DetectGame(settings);

		Assert.Equal(GameProvider.Steam, provider);
		Assert.Equal(GameProvider.Steam, settings.GameProvider);
		Assert.Equal(gameRoot, settings.GameFolderPath);
		Assert.Equal(GetLauncherPath(gameRoot), settings.GameLauncherFilePath);
		Assert.Equal(workshopRoot, settings.SteamWorkshopFolderPath);
		Assert.Equal(blsePath, settings.BLSEExePath);
	}

	[Fact]
	public void DetectGame_WhenSteamGameHasNoWorkshop_CommitsSteamAndClearsOptionalPaths() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Steam");
		string gameLibrary = CreateLibrary(temp, "D", "SteamLibrary");
		WriteLibraryFolders(clientRoot, gameLibrary);
		string gameRoot = CreateValidBannerlordInstall(temp, gameLibrary);
		AppSettings settings = CreateSettings(temp);
		settings.SteamWorkshopFolderPath = temp.CreateDirectory("OldWorkshop");
		settings.BLSEExePath = temp.GetPath("OldBlse.exe");

		GameProvider provider = CreateResolver(clientRoot).DetectGame(settings);

		Assert.Equal(GameProvider.Steam, provider);
		Assert.Equal(gameRoot, settings.GameFolderPath);
		Assert.Empty(settings.SteamWorkshopFolderPath);
		Assert.Empty(settings.BLSEExePath);
	}

	[Fact]
	public void DetectGame_WhenResolutionIsRejected_AppliesOnlyCompleteFailureState() {
		using TestDirectory temp = new();
		AppSettings settings = CreateSettings(temp);
		settings.GameProvider = GameProvider.Steam;
		settings.GameFolderPath = temp.CreateDirectory("OldGame");
		settings.GameLauncherFilePath = temp.GetPath("OldLauncher.exe");
		settings.SteamWorkshopFolderPath = temp.CreateDirectory("OldWorkshop");
		settings.BLSEExePath = temp.GetPath("OldBlse.exe");
		string clientRoot = temp.CreateDirectory("Steam");
		FakeSteamInstallationResolver steamResolver = new((root, options) =>
			Resolution(root, temp.GetPath("MissingGame"), temp.GetPath("MissingWorkshop")));
		GamePlatformDetectionResolver resolver = new(
			new FakeSteamClientRootProvider(clientRoot),
			steamResolver);

		GameProvider provider = resolver.DetectGame(settings);

		Assert.Equal(GameProvider.ManualConfiguration, provider);
		Assert.Empty(settings.GameFolderPath);
		Assert.Empty(settings.GameLauncherFilePath);
		Assert.Empty(settings.SteamWorkshopFolderPath);
		Assert.Empty(settings.BLSEExePath);
		Assert.Equal(1, steamResolver.CallCount);
	}

	[Fact]
	public void DetectGame_WhenNoSteamRootExists_ClearsStaleProviderDerivedState() {
		using TestDirectory temp = new();
		AppSettings settings = CreateSettings(temp);
		settings.GameProvider = GameProvider.GOG;
		settings.GameFolderPath = temp.CreateDirectory("OldGame");
		settings.GameLauncherFilePath = temp.GetPath("OldLauncher.exe");
		settings.SteamWorkshopFolderPath = temp.CreateDirectory("OldWorkshop");
		settings.BLSEExePath = temp.GetPath("OldBlse.exe");
		GamePlatformDetectionResolver resolver = new(
			new FakeSteamClientRootProvider(null),
			new SteamInstallationResolver());

		GameProvider provider = resolver.DetectGame(settings);

		Assert.Equal(GameProvider.ManualConfiguration, provider);
		Assert.Empty(settings.GameFolderPath);
		Assert.Empty(settings.GameLauncherFilePath);
		Assert.Empty(settings.SteamWorkshopFolderPath);
		Assert.Empty(settings.BLSEExePath);
	}

	[Fact]
	public void DetectGame_WhenSteamRootProviderThrows_FailsSafelyToManualConfiguration() {
		using TestDirectory temp = new();
		AppSettings settings = CreateSettings(temp);
		GamePlatformDetectionResolver resolver = new(
			new ThrowingSteamClientRootProvider(),
			new SteamInstallationResolver());

		GameProvider provider = resolver.DetectGame(settings);

		Assert.Equal(GameProvider.ManualConfiguration, provider);
		Assert.Empty(settings.GameFolderPath);
	}

	[Fact]
	public void DetectGame_DoesNotPreserveConfiguredWorkshopDuringAutomaticDetection() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Steam");
		WriteLibraryFolders(clientRoot, clientRoot);
		CreateValidBannerlordInstall(temp, clientRoot);
		string resolvedWorkshop = CreateWorkshopContent(clientRoot);
		string configuredWorkshop = temp.CreateDirectory("ManualWorkshop");
		AppSettings settings = CreateSettings(temp);
		settings.SteamWorkshopFolderPath = configuredWorkshop;

		CreateResolver(clientRoot).DetectGame(settings);

		Assert.Equal(resolvedWorkshop, settings.SteamWorkshopFolderPath);
		Assert.NotEqual(configuredWorkshop, settings.SteamWorkshopFolderPath);
	}

	private static GamePlatformDetectionResolver CreateResolver(string clientRoot) {
		return new GamePlatformDetectionResolver(
			new FakeSteamClientRootProvider(clientRoot),
			new SteamInstallationResolver());
	}
}
