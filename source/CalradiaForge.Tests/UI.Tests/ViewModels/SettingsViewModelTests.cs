namespace CalradiaForge.Tests.UI.ViewModels;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.GamePlatform;
using CalradiaForge.Core.Infra.GamePlatform.Steam;
using CalradiaForge.Core.Infra.Localization;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Infra.Paths;
using CalradiaForge.Core.Models;
using CalradiaForge.Tests.Core.Support;
using CalradiaForge.UI.Interactions;
using CalradiaForge.UI.Lifecycle;
using CalradiaForge.UI.Threading;
using CalradiaForge.UI.Toasts;
using CalradiaForge.UI.ViewModels;

using Newtonsoft.Json;

public sealed class SettingsViewModelTests {
	[Fact]
	public async Task InitializeAsync_MapsPersistedPathsModesAndValidation() {
		using TestContext context = new();
		context.Settings.ModpackStartupMode = ModpackStartupMode.AlwaysAsk;
		context.Settings.LastUnblockRunDate = "2026-07-30";
		context.Settings.LastUnblockRunResult = "2 file(s) unblocked";

		await context.ViewModel.InitializeAsync();

		Assert.Equal(context.Settings.GameFolderPath, context.ViewModel.GameFolderPath);
		Assert.Equal(
			context.Settings.GameLauncherFilePath,
			context.ViewModel.GameLauncherFilePath);
		Assert.True(context.ViewModel.IsAlwaysAskStartupMode);
		Assert.False(context.ViewModel.ShowGameExecutablePath);
		Assert.True(context.ViewModel.ShowWorkshopPath);
		Assert.True(context.ViewModel.IsGameFolderValid);
		Assert.Contains("2026-07-30", context.ViewModel.UnblockLastRunText);
		Assert.Contains("2 file(s)", context.ViewModel.UnblockResultText);
	}

	[Fact]
	public async Task StartupModeBinding_PersistsSelectedMode() {
		using TestContext context = new();
		await context.ViewModel.InitializeAsync();

		context.ViewModel.IsAlwaysDefaultStartupMode = true;

		Assert.Equal(
			ModpackStartupMode.AlwaysDefault,
			context.Settings.ModpackStartupMode);
		Assert.True(context.ViewModel.IsAlwaysDefaultStartupMode);
		Assert.False(context.ViewModel.IsLastUsedStartupMode);
	}

	[Fact]
	public async Task DebugCommand_PersistsAndAwaitsOneRestartRequest() {
		using TestContext context = new();
		await context.ViewModel.InitializeAsync();

		await context.ViewModel.SetDebugModeCommand.ExecuteAsync(true);

		Assert.True(context.LoggingSettings.DebugMode);
		Assert.True(context.ViewModel.IsDebugModeEnabled);
		Assert.Equal([RestartReason.DebugModeChanged], context.Lifetime.Restarts);

		await context.ViewModel.SetDebugModeCommand.ExecuteAsync(true);
		Assert.Single(context.Lifetime.Restarts);
	}

	[Fact]
	public async Task GameExecutablePicker_ValidatesBeforePersisting() {
		using TestContext context = new();
		await context.ViewModel.InitializeAsync();
		string original = context.Settings.GameLauncherFilePath;
		context.FilePicker.GameExecutable = context.Directory.GetPath("invalid.txt");
		File.WriteAllText(context.FilePicker.GameExecutable, string.Empty);

		context.ViewModel.SelectGameExecutableCommand.Execute(null);

		Assert.Equal(original, context.Settings.GameLauncherFilePath);
		Assert.Equal("Invalid Executable", Assert.Single(context.Notifications.Requests).Title);

		context.Notifications.Requests.Clear();
		string valid = context.Directory.GetPath("selected.exe");
		File.WriteAllText(valid, string.Empty);
		context.FilePicker.GameExecutable = valid;
		context.ViewModel.SelectGameExecutableCommand.Execute(null);

		Assert.Equal(valid, context.Settings.GameLauncherFilePath);
		Assert.Equal(valid, context.ViewModel.GameLauncherFilePath);
		Assert.Empty(context.Notifications.Requests);
	}

	[Fact]
	public async Task ManualGameFolder_UsesCoreWorkflowAndRefreshesDerivedState() {
		using TestContext context = new();
		context.Settings.GameProvider = GameProvider.ManualConfiguration;
		context.Settings.GameFolderPath = string.Empty;
		context.Settings.GameLauncherFilePath = string.Empty;
		await context.ViewModel.InitializeAsync();
		string selectedRoot = context.CreateValidGame("SelectedGame");
		context.FolderPicker.GameFolder = selectedRoot;

		context.ViewModel.SelectGameFolderCommand.Execute(null);

		Assert.Equal(selectedRoot, context.Settings.GameFolderPath);
		Assert.Equal(selectedRoot, context.ViewModel.GameFolderPath);
		Assert.True(context.ViewModel.IsGameFolderValid);
		Assert.Equal(
			GamePlatformDetectionResolver.ResolveStandardLauncherPath(selectedRoot),
			context.ViewModel.GameLauncherFilePath);
		Assert.False(context.ViewModel.SelectGameFolderCommand.CanExecute(null));
	}

	[Fact]
	public async Task ValidDetectedPaths_DisableFolderSelectionCommands() {
		using TestContext context = new();
		context.Settings.SteamWorkshopFolderPath =
			context.Directory.CreateDirectory("Workshop", "261550");

		await context.ViewModel.InitializeAsync();

		Assert.False(context.ViewModel.SelectGameFolderCommand.CanExecute(null));
		Assert.False(context.ViewModel.SelectWorkshopFolderCommand.CanExecute(null));
		Assert.True(context.ViewModel.IsWorkshopFolderValid);
		Assert.Equal(
			context.Translator.Strings.Settings_WorkshopFolderValid,
			context.ViewModel.WorkshopFolderValidationText);
	}

	[Fact]
	public async Task ManualConfiguration_EnablesGameFolderSelectionOnly() {
		using TestContext context = new();
		context.Settings.GameProvider = GameProvider.ManualConfiguration;
		context.Settings.GameFolderPath = string.Empty;
		context.Settings.GameLauncherFilePath = string.Empty;

		await context.ViewModel.InitializeAsync();

		Assert.True(context.ViewModel.SelectGameFolderCommand.CanExecute(null));
		Assert.False(context.ViewModel.SelectWorkshopFolderCommand.CanExecute(null));
	}

	[Fact]
	public async Task ManualSteamGameSelection_EnablesMissingWorkshopSelection() {
		using TestContext context = new();
		context.Settings.GameProvider = GameProvider.ManualConfiguration;
		context.Settings.GameFolderPath = string.Empty;
		context.Settings.GameLauncherFilePath = string.Empty;
		await context.ViewModel.InitializeAsync();
		string steamGame = context.CreateValidGame(
			Path.Combine("Steam", "steamapps", "common", "Bannerlord"));
		context.FolderPicker.GameFolder = steamGame;

		context.ViewModel.SelectGameFolderCommand.Execute(null);

		Assert.Equal(GameProvider.Steam, context.Settings.GameProvider);
		Assert.False(context.ViewModel.SelectGameFolderCommand.CanExecute(null));
		Assert.True(context.ViewModel.SelectWorkshopFolderCommand.CanExecute(null));
		Assert.False(context.ViewModel.IsWorkshopFolderValid);
	}

	[Fact]
	public async Task UnblockCommand_RejectsInvalidFolderAndPersistsSuccessSummary() {
		using TestContext context = new();
		await context.ViewModel.InitializeAsync();
		context.Settings.GameFolderPath = string.Empty;

		await context.ViewModel.UnblockDllsCommand.ExecuteAsync(null);

		Assert.Equal(0, context.Unblocker.CallCount);
		Assert.Equal("Cannot Unblock DLLs", Assert.Single(context.Notifications.Requests).Title);

		context.Notifications.Requests.Clear();
		context.Settings.GameFolderPath = context.GameRoot;
		context.Unblocker.Result = new UnblockResult {
			Succeeded = true,
			UnblockedCount = 2
		};
		await context.ViewModel.UnblockDllsCommand.ExecuteAsync(null);

		Assert.Equal(1, context.Unblocker.CallCount);
		Assert.Equal("2 file(s) unblocked", context.Settings.LastUnblockRunResult);
		Assert.Contains("2 file(s) unblocked", context.ViewModel.UnblockResultText);
		Assert.Equal("DLL Unblock Complete", Assert.Single(context.Notifications.Requests).Title);
	}

	[Fact]
	public async Task CacheAndFolderCommands_UseAuthoritativeBoundaries() {
		using TestContext context = new();
		await context.ViewModel.InitializeAsync();

		context.ViewModel.ClearModCacheCommand.Execute(null);
		context.ViewModel.OpenConfigFolderCommand.Execute(null);
		context.ViewModel.OpenLogsFolderCommand.Execute(null);
		context.ViewModel.OpenModpacksFolderCommand.Execute(null);

		Assert.Equal("Cache Cleared", Assert.Single(context.Notifications.Requests).Title);
		Assert.Equal(
			[AppPaths.ConfigDirectory, AppPaths.LogsDirectory, AppPaths.ModpacksDirectory],
			context.ShellLauncher.Paths);
	}

	[Fact]
	public async Task FolderCommand_MapsShellLaunchFailureToAnErrorNotification() {
		using TestContext context = new();
		await context.ViewModel.InitializeAsync();
		context.ShellLauncher.Result = false;

		context.ViewModel.OpenLogsFolderCommand.Execute(null);

		Assert.Equal([AppPaths.LogsDirectory], context.ShellLauncher.Paths);
		ToastRequest failure = Assert.Single(context.Notifications.Requests);
		Assert.Equal("Open Folder Failed", failure.Title);
		Assert.Equal(ToastSeverity.Error, failure.Severity);
	}

	[Fact]
	public async Task ClearCache_WhenPipelineAdmissionStopped_ReportsFailure() {
		using TestContext context = new();
		await context.ViewModel.InitializeAsync();
		context.Manager.StopAcceptingNewWork();

		context.ViewModel.ClearModCacheCommand.Execute(null);

		Assert.Equal(
			"Cache Clear Failed",
			Assert.Single(context.Notifications.Requests).Title);
	}

	[Fact]
	public async Task ActivateAsync_ResynchronizesExternallyChangedSettings() {
		using TestContext context = new();
		await context.ViewModel.InitializeAsync();
		context.Settings.GameLauncherFilePath = "X:\\Changed\\Bannerlord.exe";
		context.Settings.ModpackStartupMode = ModpackStartupMode.AlwaysDefault;

		await context.ViewModel.ActivateAsync();

		Assert.Equal(
			"X:\\Changed\\Bannerlord.exe",
			context.ViewModel.GameLauncherFilePath);
		Assert.True(context.ViewModel.IsAlwaysDefaultStartupMode);
	}

	[Fact]
	public async Task LanguageSelection_PersistsSelectedManifestLanguage() {
		using TestContext context = new();
		await context.ViewModel.InitializeAsync();

		context.ViewModel.SelectedLanguageIndex = 1;

		Assert.Equal("fr-FR", context.Translator.ActiveLanguageCode);
		Assert.Equal("fr-FR", context.Settings.Language);
		Assert.Equal(2, context.ViewModel.AvailableLanguages.Count);
		Assert.Equal(
			"Dossier Workshop introuvable",
			context.ViewModel.WorkshopFolderValidationText);
	}

	[Fact]
	public async Task PickerCancellation_PreservesAllConfiguredPaths() {
		using TestContext context = new();
		await context.ViewModel.InitializeAsync();
		string[] original = [
			context.Settings.GameFolderPath,
			context.Settings.GameLauncherFilePath,
			context.Settings.SteamWorkshopFolderPath,
			context.Settings.BLSEExePath
		];

		context.ViewModel.SelectGameFolderCommand.Execute(null);
		context.ViewModel.SelectGameExecutableCommand.Execute(null);
		context.ViewModel.SelectWorkshopFolderCommand.Execute(null);
		context.ViewModel.SelectBlseExecutableCommand.Execute(null);

		Assert.Equal(original[0], context.Settings.GameFolderPath);
		Assert.Equal(original[1], context.Settings.GameLauncherFilePath);
		Assert.Equal(original[2], context.Settings.SteamWorkshopFolderPath);
		Assert.Equal(original[3], context.Settings.BLSEExePath);
		Assert.Empty(context.Notifications.Requests);
	}

	[Fact]
	public async Task WorkshopPicker_RejectsMissingAndAcceptsExistingSteamFolder() {
		using TestContext context = new();
		await context.ViewModel.InitializeAsync();
		context.FolderPicker.WorkshopFolder =
			context.Directory.GetPath("MissingWorkshop");

		context.ViewModel.SelectWorkshopFolderCommand.Execute(null);

		Assert.Equal(string.Empty, context.Settings.SteamWorkshopFolderPath);
		Assert.Equal(
			"Invalid Workshop Folder",
			Assert.Single(context.Notifications.Requests).Title);

		context.Notifications.Requests.Clear();
		string valid = context.Directory.CreateDirectory("Workshop", "261550");
		context.FolderPicker.WorkshopFolder = valid;
		context.ViewModel.SelectWorkshopFolderCommand.Execute(null);

		Assert.Equal(valid, context.Settings.SteamWorkshopFolderPath);
		Assert.Equal(valid, context.ViewModel.SteamWorkshopFolderPath);
		Assert.True(context.ViewModel.IsWorkshopFolderValid);
		Assert.Equal(
			context.Translator.Strings.Settings_WorkshopFolderValid,
			context.ViewModel.WorkshopFolderValidationText);
		Assert.False(context.ViewModel.SelectWorkshopFolderCommand.CanExecute(null));

		context.ViewModel.SelectedLanguageIndex = 1;

		Assert.Equal(
			"Dossier Workshop valide",
			context.ViewModel.WorkshopFolderValidationText);
		Assert.Empty(context.Notifications.Requests);
	}

	[Fact]
	public async Task BlsePicker_PreservesOptionalStateAndClassifiesInvalidAndValidPaths() {
		using TestContext context = new();
		await context.ViewModel.InitializeAsync();
		Assert.True(context.ViewModel.IsBlsePathValidOrOptional);
		Assert.Contains("optional", context.ViewModel.BlseValidationText);

		string invalid = context.Directory.GetPath("blse.txt");
		File.WriteAllText(invalid, string.Empty);
		context.FilePicker.BlseExecutable = invalid;
		context.ViewModel.SelectBlseExecutableCommand.Execute(null);

		Assert.Equal(string.Empty, context.Settings.BLSEExePath);
		Assert.Equal(
			"Invalid BLSE Executable",
			Assert.Single(context.Notifications.Requests).Title);

		context.Notifications.Requests.Clear();
		context.Settings.BLSEExePath = context.Directory.GetPath("missing.exe");
		await context.ViewModel.ActivateAsync();
		Assert.False(context.ViewModel.IsBlsePathValidOrOptional);

		string valid = context.Directory.GetPath("blse.exe");
		File.WriteAllText(valid, string.Empty);
		context.FilePicker.BlseExecutable = valid;
		context.ViewModel.SelectBlseExecutableCommand.Execute(null);

		Assert.Equal(valid, context.Settings.BLSEExePath);
		Assert.True(context.ViewModel.IsBlsePathValidOrOptional);
		Assert.Empty(context.Notifications.Requests);
	}

	[Fact]
	public async Task RedetectGame_NoSteamMetadataCommitsManualBoundaryAndNotifies() {
		using TestContext context = new();
		await context.ViewModel.InitializeAsync();

		context.ViewModel.RedetectGameCommand.Execute(null);

		Assert.Equal(GameProvider.ManualConfiguration, context.Settings.GameProvider);
		Assert.Equal(string.Empty, context.ViewModel.GameFolderPath);
		Assert.Equal("Detection Failed", Assert.Single(context.Notifications.Requests).Title);
		Assert.False(context.ViewModel.IsGameFolderValid);
	}

	[Fact]
	public async Task UnblockCommand_ExceptionReportsFailureWithoutPersistingSuccess() {
		using TestContext context = new();
		await context.ViewModel.InitializeAsync();
		context.Unblocker.Failure = new InvalidOperationException("unblock failed");

		await context.ViewModel.UnblockDllsCommand.ExecuteAsync(null);

		Assert.Equal(string.Empty, context.Settings.LastUnblockRunResult);
		Assert.Contains("unblock failed", context.ViewModel.UnblockResultText);
		Assert.Equal("Unblock Failed", Assert.Single(context.Notifications.Requests).Title);
	}

	[Fact]
	public async Task RepeatedActivation_ResynchronizesWithoutDuplicatingLanguageState() {
		using TestContext context = new();
		await context.ViewModel.InitializeAsync();

		await context.ViewModel.ActivateAsync();
		await context.ViewModel.DeactivateAsync();
		await context.ViewModel.ActivateAsync();

		Assert.True(context.ViewModel.IsActive);
		Assert.Equal(2, context.ViewModel.AvailableLanguages.Count);
		Assert.Equal(0, context.ViewModel.SelectedLanguageIndex);
	}

	[Fact]
	public async Task DebugCommand_DisablesEnabledSettingAndRequestsRestart() {
		using TestContext context = new();
		context.LoggingSettings.DebugMode = true;
		await context.ViewModel.InitializeAsync();
		Assert.True(context.ViewModel.IsDebugModeEnabled);

		await context.ViewModel.SetDebugModeCommand.ExecuteAsync(false);

		Assert.False(context.LoggingSettings.DebugMode);
		Assert.False(context.ViewModel.IsDebugModeEnabled);
		Assert.Equal([RestartReason.DebugModeChanged], context.Lifetime.Restarts);
	}

	private sealed class TestContext : IDisposable {
		private readonly TestDirectory _directory = new();

		public TestContext() {
			ConfigFileManager config = new(_directory.GetPath("config.json"));
			Settings = new AppSettings(config);
			LoggingSettings = new LoggingSettings(config);
			GameRoot = CreateValidGame("Game");
			Settings.GameProvider = GameProvider.Steam;
			Settings.GameFolderPath = GameRoot;
			Settings.GameLauncherFilePath =
				GamePlatformDetectionResolver.ResolveStandardLauncherPath(GameRoot);

			ModsData modsData = new(
				_directory.GetPath("mods-current.data"),
				_directory.GetPath("mods-backup.data"));
			modsData.SaveCurrent([TestDirectory.Module("Native")]);
			Manager = new ModPipelineManager(
				Settings,
				modsData,
				new ModInstaller(Settings),
				new EmptyScanner(),
				new SuccessfulModuleUnblocker());
			Manager.LoadAcceptedCache();
			GameDetectionService detection = new(
				new GamePlatformDetectionResolver(
					new MissingSteamRootProvider(),
					new SteamInstallationResolver()),
				new StartupNotificationQueue());
			string languagesDirectory = _directory.CreateDirectory("Languages");
			string manifestPath = _directory.GetPath("Languages", "languages.json");
			string englishPath = _directory.GetPath("Languages", "en-US.json");
			File.WriteAllText(
				manifestPath,
				JsonConvert.SerializeObject(new[] {
					new LanguageOption { Code = "en-US", DisplayName = "English" },
					new LanguageOption { Code = "fr-FR", DisplayName = "French" }
				}));
			File.WriteAllText(
				englishPath,
				"{\"Settings_GeneralTab\":\"General\"}");
			File.WriteAllText(
				_directory.GetPath("Languages", "fr-FR.json"),
				JsonConvert.SerializeObject(new Dictionary<string, string> {
					["Settings_GeneralTab"] = "Général",
					[nameof(TranslationStrings.Settings_WorkshopFolderValid)] =
						"Dossier Workshop valide",
					[nameof(TranslationStrings.Settings_WorkshopFolderInvalid)] =
						"Dossier Workshop introuvable"
				}));
			Translator = new TranslationService(
				new TranslationManager(
					languagesDirectory,
					manifestPath,
					englishPath),
				Settings);
			Translator.Initialize();
			FilePicker = new StubFilePicker();
			FolderPicker = new StubFolderPicker();
			ShellLauncher = new RecordingShellLauncher();
			Unblocker = new StubDllUnblocker();
			Notifications = new RecordingNotificationSink();
			Lifetime = new RecordingLifetime();
			ViewModel = new SettingsViewModel(
				Settings,
				LoggingSettings,
				Manager,
				detection,
				FilePicker,
				FolderPicker,
				ShellLauncher,
				Unblocker,
				Notifications,
				Lifetime,
				new ImmediateUiDispatcher(),
				Translator);
		}

		public TestDirectory Directory => _directory;
		public AppSettings Settings { get; }
		public LoggingSettings LoggingSettings { get; }
		public ModPipelineManager Manager { get; }
		public string GameRoot { get; }
		public StubFilePicker FilePicker { get; }
		public StubFolderPicker FolderPicker { get; }
		public RecordingShellLauncher ShellLauncher { get; }
		public StubDllUnblocker Unblocker { get; }
		public RecordingNotificationSink Notifications { get; }
		public RecordingLifetime Lifetime { get; }
		public TranslationService Translator { get; }
		public SettingsViewModel ViewModel { get; }

		public string CreateValidGame(string folderName) {
			string root = _directory.CreateDirectory(folderName);
			string modules = _directory.CreateDirectory(folderName, "Modules");
			_directory.WriteModule(modules, "Native", "Native");
			string launcherDirectory = _directory.CreateDirectory(
				folderName,
				"bin",
				"Win64_Shipping_Client");
			File.WriteAllText(
				Path.Combine(launcherDirectory, "Bannerlord.exe"),
				string.Empty);
			return root;
		}

		public void Dispose() => _directory.Dispose();
	}

	private sealed class StubFilePicker : ISettingsFilePicker {
		public string? GameExecutable { get; set; }
		public string? BlseExecutable { get; set; }
		public string? PickGameExecutable() => GameExecutable;
		public string? PickBlseExecutable() => BlseExecutable;
	}

	private sealed class StubFolderPicker : ISettingsFolderPicker {
		public string? GameFolder { get; set; }
		public string? WorkshopFolder { get; set; }
		public string? PickGameFolder() => GameFolder;
		public string? PickWorkshopFolder() => WorkshopFolder;
	}

	private sealed class RecordingShellLauncher : ISettingsShellLauncher {
		public List<string> Paths { get; } = [];
		public bool Result { get; set; } = true;
		public bool OpenFolder(string folderPath) {
			Paths.Add(folderPath);
			return Result;
		}
	}

	private sealed class StubDllUnblocker : ISettingsDllUnblocker {
		public int CallCount { get; private set; }
		public UnblockResult Result { get; set; } = new();
		public Exception? Failure { get; set; }

		public Task<UnblockResult> UnblockAllAsync(
			string directoryPath,
			CancellationToken cancellationToken = default) {
			CallCount++;
			if (Failure is not null) {
				return Task.FromException<UnblockResult>(Failure);
			}
			return Task.FromResult(Result);
		}
	}

	private sealed class RecordingNotificationSink : IToastNotificationSink {
		public List<ToastRequest> Requests { get; } = [];
		public IToastNotificationHandle Open(ToastRequest request) {
			Requests.Add(request);
			return new NoOpToastHandle();
		}
	}

	private sealed class NoOpToastHandle : IToastNotificationHandle {
		public void UpdateProgress(double value, double maximum, string? message = null) { }
		public void Transition(ToastRequest request) { }
		public void Close() { }
		public void Dispose() { }
	}

	private sealed class RecordingLifetime : IApplicationLifetime {
		public List<RestartReason> Restarts { get; } = [];
		public Task RequestShutdownAsync(ShutdownReason reason) => Task.CompletedTask;
		public Task RequestRestartAsync(RestartReason reason) {
			Restarts.Add(reason);
			return Task.CompletedTask;
		}
	}

	private sealed class ImmediateUiDispatcher : IUiDispatcher {
		public bool CheckAccess() => true;
		public Task InvokeAsync(
			Action action,
			CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			action();
			return Task.CompletedTask;
		}
	}

	private sealed class MissingSteamRootProvider : ISteamClientRootProvider {
		public string? GetSteamClientRoot() => null;
	}

	private sealed class EmptyScanner : IModScanner {
		public Task<ModScanResult> ScanAsync(
			AppSettings config,
			CancellationToken token = default) =>
			Task.FromResult(new ModScanResult {
				LocalRoot = new ModScanRootResult { Status = ModScanRootStatus.Succeeded },
				WorkshopRoot = new ModScanRootResult {
					Status = ModScanRootStatus.NotApplicable
				}
			});
	}

	private sealed class SuccessfulModuleUnblocker : IModuleUnblocker {
		public Task<UnblockResult> UnblockModulesAsync(
			string modulesDirectoryPath,
			CancellationToken token = default) =>
			Task.FromResult(new UnblockResult { Succeeded = true });
	}
}
