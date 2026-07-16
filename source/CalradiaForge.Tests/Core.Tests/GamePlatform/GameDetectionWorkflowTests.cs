namespace CalradiaForge.Tests.Core.GamePlatform;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.GamePlatform;
using CalradiaForge.Core.Infra.GamePlatform.Steam;
using CalradiaForge.Core.Infra.Paths;
using CalradiaForge.Core.Models;
using CalradiaForge.Tests.Core.Support;

using static GamePlatformTestFixture;

public sealed class GameDetectionWorkflowTests {
	[Fact]
	public void InitializeForStartup_WhenConfiguredGameIsValid_ReusesStateWithoutDetectionOrNotification() {
		using TestDirectory temp = new();
		string gameRoot = CreateValidManualGame(temp, "ExistingGame");
		string workshopRoot = temp.CreateDirectory("ExistingWorkshop");
		AppConfigSettings settings = CreateSettings(temp);
		settings.GameProvider = GameProvider.Steam;
		settings.GameFolderPath = gameRoot;
		settings.GameLauncherFilePath = GetLauncherPath(gameRoot);
		settings.SteamWorkshopFolderPath = workshopRoot;
		FakeSteamClientRootProvider rootProvider = new(temp.GetPath("UnusedSteam"));
		StartupNotificationQueue queue = new();
		GameDetectionService workflow = CreateWorkflow(rootProvider, new SteamInstallationResolver(), queue);

		workflow.InitializeForStartup(settings);

		Assert.Equal(GameProvider.Steam, settings.GameProvider);
		Assert.Equal(workshopRoot, settings.SteamWorkshopFolderPath);
		Assert.Equal(0, rootProvider.CallCount);
		Assert.Equal(0, queue.Count);
	}

	[Fact]
	public async Task InitializeForStartup_WhenConfigurationIsInvalid_DetectsAndQueuesOneResult() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Steam");
		string gameLibrary = CreateLibrary(temp, "D", "SteamLibrary");
		WriteLibraryFolders(clientRoot, gameLibrary);
		CreateValidBannerlordInstall(temp, gameLibrary);
		AppConfigSettings settings = CreateSettings(temp);
		StartupNotificationQueue queue = new();
		GameDetectionService workflow = CreateWorkflow(
			new FakeSteamClientRootProvider(clientRoot),
			new SteamInstallationResolver(),
			queue);

		workflow.InitializeForStartup(settings);

		Assert.Equal(GameProvider.Steam, settings.GameProvider);
		Assert.Equal(1, queue.Count);
		List<StartupNotification> delivered = [];
		queue.SignalReady();
		await queue.DrainWhenReadyAsync((notification, _) => {
			delivered.Add(notification);
			return Task.CompletedTask;
		});
		StartupNotification notification = Assert.Single(delivered);
		Assert.Equal(StartupNotificationSeverity.Warning, notification.Severity);
		Assert.Contains("Workshop", notification.Title, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void RedetectGame_ReplacesConfiguredWorkshopWithCurrentResolution() {
		using TestDirectory temp = new();
		string clientRoot = CreateLibrary(temp, "C", "Steam");
		string gameLibrary = CreateLibrary(temp, "D", "SteamLibrary");
		WriteLibraryFolders(clientRoot, gameLibrary);
		CreateValidBannerlordInstall(temp, gameLibrary);
		string configuredWorkshop = temp.CreateDirectory("OldManualWorkshop");
		AppConfigSettings settings = CreateSettings(temp);
		settings.SteamWorkshopFolderPath = configuredWorkshop;
		GameDetectionService workflow = CreateWorkflow(
			new FakeSteamClientRootProvider(clientRoot),
			new SteamInstallationResolver(),
			new StartupNotificationQueue());

		GameProvider provider = workflow.RedetectGame(settings);

		Assert.Equal(GameProvider.Steam, provider);
		Assert.Empty(settings.SteamWorkshopFolderPath);
		Assert.NotEqual(configuredWorkshop, settings.SteamWorkshopFolderPath);
	}

	[Fact]
	public void ApplyManualGameFolder_WhenPathHasSteamSignature_CommitsSteamAndClearsWorkshop() {
		using TestDirectory temp = new();
		string gameRoot = CreateValidManualGame(
			temp,
			"SteamLibrary",
			"steamapps",
			"common",
			BannerlordInstallDirectory);
		AppConfigSettings settings = CreateSettings(temp);
		settings.SteamWorkshopFolderPath = temp.CreateDirectory("OldWorkshop");
		GameDetectionService workflow = CreateUnusedWorkflow();

		bool applied = workflow.ApplyManualGameFolder(settings, gameRoot);

		Assert.True(applied);
		Assert.Equal(GameProvider.Steam, settings.GameProvider);
		Assert.Equal(gameRoot, settings.GameFolderPath);
		Assert.Equal(GetLauncherPath(gameRoot), settings.GameLauncherFilePath);
		Assert.Empty(settings.SteamWorkshopFolderPath);
	}

	[Fact]
	public void ApplyManualGameFolder_WhenPathHasNoKnownSignature_CommitsStandalone() {
		using TestDirectory temp = new();
		string gameRoot = CreateValidManualGame(temp, "Games", BannerlordInstallDirectory);
		AppConfigSettings settings = CreateSettings(temp);

		bool applied = CreateUnusedWorkflow().ApplyManualGameFolder(settings, gameRoot);

		Assert.True(applied);
		Assert.Equal(GameProvider.StandAlone, settings.GameProvider);
		Assert.Equal(gameRoot, settings.GameFolderPath);
	}

	[Fact]
	public void ApplyManualGameFolder_WhenSelectionIsInvalid_PreservesAllSettings() {
		using TestDirectory temp = new();
		string originalGame = CreateValidManualGame(temp, "ExistingGame");
		string originalWorkshop = temp.CreateDirectory("ExistingWorkshop");
		AppConfigSettings settings = CreateSettings(temp);
		settings.GameProvider = GameProvider.Steam;
		settings.GameFolderPath = originalGame;
		settings.GameLauncherFilePath = GetLauncherPath(originalGame);
		settings.SteamWorkshopFolderPath = originalWorkshop;
		settings.BLSEExePath = temp.GetPath("ExistingBlse.exe");

		bool applied = CreateUnusedWorkflow().ApplyManualGameFolder(
			settings,
			temp.CreateDirectory("InvalidGame"));

		Assert.False(applied);
		Assert.Equal(GameProvider.Steam, settings.GameProvider);
		Assert.Equal(originalGame, settings.GameFolderPath);
		Assert.Equal(GetLauncherPath(originalGame), settings.GameLauncherFilePath);
		Assert.Equal(originalWorkshop, settings.SteamWorkshopFolderPath);
		Assert.Equal(temp.GetPath("ExistingBlse.exe"), settings.BLSEExePath);
	}

	[Fact]
	public void ApplyManualSteamWorkshopFolder_WhenSteamAndValid_UpdatesOnlyWorkshop() {
		using TestDirectory temp = new();
		string gameRoot = CreateValidManualGame(temp, "ExistingGame");
		string workshopRoot = temp.CreateDirectory("SelectedWorkshop");
		AppConfigSettings settings = CreateSettings(temp);
		settings.GameProvider = GameProvider.Steam;
		settings.GameFolderPath = gameRoot;
		settings.GameLauncherFilePath = GetLauncherPath(gameRoot);

		bool applied = CreateUnusedWorkflow().ApplyManualSteamWorkshopFolder(settings, workshopRoot);

		Assert.True(applied);
		Assert.Equal(workshopRoot, settings.SteamWorkshopFolderPath);
		Assert.Equal(GameProvider.Steam, settings.GameProvider);
		Assert.Equal(gameRoot, settings.GameFolderPath);
	}

	[Fact]
	public void ApplyManualSteamWorkshopFolder_WhenInvalid_PreservesPreviousValue() {
		using TestDirectory temp = new();
		string previousWorkshop = temp.CreateDirectory("PreviousWorkshop");
		AppConfigSettings settings = CreateSettings(temp);
		settings.GameProvider = GameProvider.Steam;
		settings.SteamWorkshopFolderPath = previousWorkshop;

		bool applied = CreateUnusedWorkflow().ApplyManualSteamWorkshopFolder(
			settings,
			temp.GetPath("MissingWorkshop"));

		Assert.False(applied);
		Assert.Equal(previousWorkshop, settings.SteamWorkshopFolderPath);
	}

	[Fact]
	public void ApplyManualSteamWorkshopFolder_WhenProviderIsNotSteam_PreservesPreviousValue() {
		using TestDirectory temp = new();
		string previousWorkshop = temp.CreateDirectory("PreviousWorkshop");
		string selectedWorkshop = temp.CreateDirectory("SelectedWorkshop");
		AppConfigSettings settings = CreateSettings(temp);
		settings.GameProvider = GameProvider.StandAlone;
		settings.SteamWorkshopFolderPath = previousWorkshop;

		bool applied = CreateUnusedWorkflow().ApplyManualSteamWorkshopFolder(settings, selectedWorkshop);

		Assert.False(applied);
		Assert.Equal(previousWorkshop, settings.SteamWorkshopFolderPath);
		Assert.Equal(GameProvider.StandAlone, settings.GameProvider);
	}

	private static GameDetectionService CreateWorkflow(
		ISteamClientRootProvider rootProvider,
		ISteamInstallationResolver steamResolver,
		StartupNotificationQueue queue) {
		return new GameDetectionService(
			new GamePlatformDetectionResolver(rootProvider, steamResolver),
			queue);
	}

	private static GameDetectionService CreateUnusedWorkflow() {
		return CreateWorkflow(
			new FakeSteamClientRootProvider(null),
			new SteamInstallationResolver(),
			new StartupNotificationQueue());
	}
}
