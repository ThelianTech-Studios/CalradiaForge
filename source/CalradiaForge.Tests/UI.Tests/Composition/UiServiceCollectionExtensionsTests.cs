namespace CalradiaForge.Tests.UI.Composition;

using System.Windows.Threading;

using CalradiaForge.Core.Infra.DependencyInjection;
using CalradiaForge.UI.Composition;
using CalradiaForge.UI.Dialogs;
using CalradiaForge.UI.Lifecycle;
using CalradiaForge.UI.Pages;
using CalradiaForge.UI.Views;

using Microsoft.Extensions.DependencyInjection;

using Serilog;

public sealed class UiServiceCollectionExtensionsTests {
	[Fact]
	public void AddCalradiaForgeUi_RegistersRetainedUiGraphAsSingletons() {
		ServiceCollection services = new();

		services.AddCalradiaForgeUi(Dispatcher.CurrentDispatcher);

		Type[] singletonTypes = [
			typeof(ApplicationStartupCoordinator),
			typeof(ApplicationShutdownCoordinator),
			typeof(StartupNotificationDrainCoordinator),
			typeof(ModsPage),
			typeof(ModpacksPage),
			typeof(FaqPage),
			typeof(SettingsPage),
			typeof(MainWindow)
		];

		foreach (Type serviceType in singletonTypes) {
			ServiceDescriptor descriptor = Assert.Single(
				services,
				candidate => candidate.ServiceType == serviceType);
			Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
		}
	}

	[Fact]
	public async Task CoreAndUiRegistrations_BuildOneValidatedProviderAndOneGlobalLogger() {
		string root = Path.Combine(
			Path.GetTempPath(),
			"CalradiaForge.Tests",
			Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(root);
		CalradiaForgeCoreOptions options = CreateOptions(root);
		foreach (string directory in new[] {
			Path.GetDirectoryName(options.ConfigFilePath)!,
			options.LogsDirectory,
			Path.GetDirectoryName(options.ModsCurrentFilePath)!,
			options.ModpacksDirectory,
			options.LanguagesDirectory
		}) {
			Directory.CreateDirectory(directory);
		}

		try {
			ServiceCollection services = new();
			services.AddSingleton<IApplicationLifetime, NoOpApplicationLifetime>();
			services.AddCalradiaForgeCore(options);
			services.AddCalradiaForgeUi(Dispatcher.CurrentDispatcher);

			await using ServiceProvider provider = services.BuildServiceProvider(
				new ServiceProviderOptions {
					ValidateOnBuild = true,
					ValidateScopes = true
				});

			Serilog.ILogger first = provider.GetRequiredService<Serilog.ILogger>();
			Serilog.ILogger second = provider.GetRequiredService<Serilog.ILogger>();
			Assert.Same(first, second);
			Assert.Same(first, Log.Logger);
			Assert.NotNull(provider.GetRequiredService<ApplicationStartupCoordinator>());
			Assert.False(File.Exists(Path.Combine(
				options.LogsDirectory,
				"CalradiaForge_StartupFailure.log")));
		} finally {
			if (Directory.Exists(root)) {
				Directory.Delete(root, recursive: true);
			}
		}
	}

	[Fact]
	public void AddCalradiaForgeUi_UsesNarrowDialogAndDeferredShellContracts() {
		ServiceCollection services = new();

		services.AddCalradiaForgeUi(Dispatcher.CurrentDispatcher);

		AssertSingleton<ILanguageSelectionDialogService, LanguageSelectionDialogService>(services);
		AssertSingleton<IEulaDialogService, EulaDialogService>(services);
		AssertSingleton<IApplicationDialogService, ApplicationDialogService>(services);
		AssertSingleton<IMainWindowProvider, MainWindowProvider>(services);
		Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IServiceProvider));
	}

	private static void AssertSingleton<TService, TImplementation>(IServiceCollection services) {
		ServiceDescriptor descriptor = Assert.Single(
			services,
			candidate => candidate.ServiceType == typeof(TService));
		Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
		Assert.Equal(typeof(TImplementation), descriptor.ImplementationType);
	}

	private static CalradiaForgeCoreOptions CreateOptions(string root) {
		return new CalradiaForgeCoreOptions {
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
	}

	private sealed class NoOpApplicationLifetime : IApplicationLifetime {
		public Task RequestShutdownAsync(ShutdownReason reason) => Task.CompletedTask;

		public Task RequestRestartAsync(RestartReason reason) => Task.CompletedTask;
	}
}
