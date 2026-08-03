namespace CalradiaForge.Tests.UI.Lifecycle;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.DependencyInjection;
using CalradiaForge.Core.Infra.Eula;
using CalradiaForge.Core.Infra.GamePlatform;
using CalradiaForge.Core.Infra.Localization;
using CalradiaForge.Core.Infra.Modpacks;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Models;
using CalradiaForge.Tests.Core.Support;
using CalradiaForge.Tests.UI.Support;
using CalradiaForge.UI.Composition;
using CalradiaForge.UI.Dialogs;
using CalradiaForge.UI.Lifecycle;
using CalradiaForge.UI.Toasts;
using CalradiaForge.UI.Views;

using Microsoft.Extensions.DependencyInjection;

[Collection(GlobalSerilogCollection.Name)]
public sealed class ApplicationStartupCoordinatorTests {
	[Fact]
	public async Task StartAsync_WhenEulaSaveFails_AcceptsForSessionAndPresentsOneWarning() {
		using TestDirectory directory = new();
		CalradiaForgeCoreOptions options = CreateOptions(directory.RootPath);
		foreach (string requiredDirectory in new[] {
			Path.GetDirectoryName(options.ConfigFilePath)!,
			options.LogsDirectory,
			Path.GetDirectoryName(options.ModsCurrentFilePath)!,
			options.ModpacksDirectory,
			options.LanguagesDirectory
		}) {
			Directory.CreateDirectory(requiredDirectory);
		}

		ConfigFileManager unavailableConfig = new(
			options.ConfigFilePath,
			new WriteFailingConfigPersistence());
		unavailableConfig.Load();
		ServiceCollection services = new();
		services.AddCalradiaForgeCore(options);
		services.AddSingleton(unavailableConfig);
		await using ServiceProvider provider = services.BuildServiceProvider();
		Serilog.ILogger logger = provider.GetRequiredService<Serilog.ILogger>();
		AppSettings settings = provider.GetRequiredService<AppSettings>();
		StartupNotificationQueue queue = provider.GetRequiredService<StartupNotificationQueue>();
		RecordingStartupNotificationPresenter notificationPresenter = new();
		StartupNotificationDrainCoordinator drain = new(queue, notificationPresenter, logger);
		ApplicationStartupCoordinator coordinator = new(
			logger,
			provider.GetRequiredService<TranslationManager>(),
			provider.GetRequiredService<TranslationService>(),
			unavailableConfig,
			settings,
			provider.GetRequiredService<EulaService>(),
			new NoOpLanguageSelectionDialogService(),
			new AcceptingEulaDialogService(),
			provider.GetRequiredService<GameDetectionService>(),
			provider.GetRequiredService<ModPipelineManager>(),
			provider.GetRequiredService<ModpackService>(),
			queue,
			drain,
			new RecordingInstallNotificationPresenter(),
			new ThrowingMainWindowProvider());

		await Assert.ThrowsAsync<ShellResolutionObservedException>(() => coordinator.StartAsync());
		drain.SignalReady();
		await queue.DrainWhenReadyAsync((_, _) => Task.CompletedTask);

		Assert.True(settings.EulaAccepted);
		Assert.Equal(ConfigPersistenceStatus.Unavailable, unavailableConfig.PersistenceStatus);
		StartupNotification warning = Assert.Single(
			notificationPresenter.Notifications,
			notification => notification.Title == "Configuration");
		Assert.Equal(StartupNotificationSeverity.Warning, warning.Severity);
		Assert.Contains("continue for this session", warning.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("may not survive restart", warning.Message, StringComparison.OrdinalIgnoreCase);
		await drain.StopAsync();
	}

	[Fact]
	public async Task StartAsync_ActivatesInstallPresenterBeforeShellResolution() {
		using TestDirectory directory = new();
		CalradiaForgeCoreOptions options = CreateOptions(directory.RootPath);
		foreach (string requiredDirectory in new[] {
			Path.GetDirectoryName(options.ConfigFilePath)!,
			options.LogsDirectory,
			Path.GetDirectoryName(options.ModsCurrentFilePath)!,
			options.ModpacksDirectory,
			options.LanguagesDirectory
		}) {
			Directory.CreateDirectory(requiredDirectory);
		}

		ServiceCollection services = new();
		services.AddCalradiaForgeCore(options);
		await using ServiceProvider provider = services.BuildServiceProvider();
		Serilog.ILogger logger = provider.GetRequiredService<Serilog.ILogger>();
		AppSettings settings = provider.GetRequiredService<AppSettings>();
		settings.EulaAccepted = true;
		RecordingInstallNotificationPresenter installPresenter = new();
		OrderingMainWindowProvider shellProvider = new(installPresenter);
		StartupNotificationDrainCoordinator drain = new(
			provider.GetRequiredService<StartupNotificationQueue>(),
			new NoOpStartupNotificationPresenter(),
			logger);
		ApplicationStartupCoordinator coordinator = new(
			logger,
			provider.GetRequiredService<TranslationManager>(),
			provider.GetRequiredService<TranslationService>(),
			provider.GetRequiredService<ConfigFileManager>(),
			settings,
			provider.GetRequiredService<EulaService>(),
			new NoOpLanguageSelectionDialogService(),
			new RejectingEulaDialogService(),
			provider.GetRequiredService<GameDetectionService>(),
			provider.GetRequiredService<ModPipelineManager>(),
			provider.GetRequiredService<ModpackService>(),
			provider.GetRequiredService<StartupNotificationQueue>(),
			drain,
			installPresenter,
			shellProvider);

		try {
			await Assert.ThrowsAsync<ShellResolutionObservedException>(
				() => coordinator.StartAsync());
			Assert.Equal(1, installPresenter.ActivationCount);
			Assert.True(shellProvider.WasRequested);
			Assert.True(shellProvider.PresenterWasActiveWhenRequested);
		} finally {
			await drain.StopAsync();
		}
	}

	private static CalradiaForgeCoreOptions CreateOptions(string root) => new() {
		ConfigFilePath = Path.Combine(root, "Config", "config.json"),
		LogsDirectory = Path.Combine(root, "Logs"),
		LogsFilePath = Path.Combine(root, "Logs", "CalradiaForge_Latest.log"),
		ModsCurrentFilePath = Path.Combine(root, "Data", "mods_current.data"),
		ModsBackupFilePath = Path.Combine(root, "Data", "mods_backup.data"),
		ModpacksDirectory = Path.Combine(root, "Modpacks"),
		LastUsedModsFilePath = Path.Combine(root, "Data", "last_used_mods.data"),
		LanguagesDirectory = Path.Combine(root, "Languages"),
		LanguagesManifestFilePath = Path.Combine(root, "Languages", "languages.json"),
		DefaultLanguageFilePath = Path.Combine(root, "Languages", "en-US.json")
	};

	private sealed class RecordingInstallNotificationPresenter : IInstallNotificationPresenter {
		public int ActivationCount { get; private set; }
		public bool IsActive => ActivationCount > 0;

		public void Activate() => ActivationCount++;

		public void ReportLauncherCompletion(LauncherInstallPresentationCompletion completion) { }
	}

	private sealed class OrderingMainWindowProvider : IMainWindowProvider {
		private readonly RecordingInstallNotificationPresenter _presenter;

		public OrderingMainWindowProvider(RecordingInstallNotificationPresenter presenter) {
			_presenter = presenter;
		}

		public bool WasRequested { get; private set; }
		public bool PresenterWasActiveWhenRequested { get; private set; }

		public MainWindow GetMainWindow() {
			WasRequested = true;
			PresenterWasActiveWhenRequested = _presenter.IsActive;
			throw new ShellResolutionObservedException();
		}
	}

	private sealed class NoOpLanguageSelectionDialogService
		: ILanguageSelectionDialogService {
		public bool TrySelectLanguage(
			IReadOnlyList<LanguageOption> languages,
			string currentLanguageCode,
			out string selectedLanguageCode) {
			selectedLanguageCode = currentLanguageCode;
			return false;
		}
	}

	private sealed class RejectingEulaDialogService : IEulaDialogService {
		public bool RequestAcceptance(string eulaText) =>
			throw new InvalidOperationException("The accepted-EULA test setup must bypass this dialog.");
	}

	private sealed class AcceptingEulaDialogService : IEulaDialogService {
		public bool RequestAcceptance(string eulaText) => true;
	}

	private sealed class RecordingStartupNotificationPresenter : IStartupNotificationPresenter {
		public List<StartupNotification> Notifications { get; } = [];

		public Task PresentAsync(
			StartupNotification notification,
			CancellationToken cancellationToken) {
			Notifications.Add(notification);
			return Task.CompletedTask;
		}
	}

	private sealed class ThrowingMainWindowProvider : IMainWindowProvider {
		public MainWindow GetMainWindow() => throw new ShellResolutionObservedException();
	}

	private sealed class WriteFailingConfigPersistence : IConfigFilePersistence {
		public bool FileExists(string path) => false;
		public string ReadAllText(string path) => throw new FileNotFoundException();
		public void WriteAllTextAtomic(string path, string contents) =>
			throw new IOException("Configuration is read-only for this test.");
		public void CopyFile(string sourcePath, string destinationPath) =>
			throw new IOException("Configuration is read-only for this test.");
	}

	private sealed class NoOpStartupNotificationPresenter : IStartupNotificationPresenter {
		public Task PresentAsync(
			StartupNotification notification,
			CancellationToken cancellationToken) => Task.CompletedTask;
	}

	private sealed class ShellResolutionObservedException : Exception;
}
