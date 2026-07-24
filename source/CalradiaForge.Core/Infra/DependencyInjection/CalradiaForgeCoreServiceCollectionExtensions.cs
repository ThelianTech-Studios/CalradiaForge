namespace CalradiaForge.Core.Infra.DependencyInjection;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Eula;
using CalradiaForge.Core.Infra.GamePlatform;
using CalradiaForge.Core.Infra.GamePlatform.Steam;
using CalradiaForge.Core.Infra.Launch;
using CalradiaForge.Core.Infra.Localization;
using CalradiaForge.Core.Infra.Logging;
using CalradiaForge.Core.Infra.Modpacks;
using CalradiaForge.Core.Infra.Mods;

using Microsoft.Extensions.DependencyInjection;

using Serilog;

/// <summary>Registers the Core-owned portion of the application graph on one shared collection.</summary>
public static class CalradiaForgeCoreServiceCollectionExtensions {
	public static IServiceCollection AddCalradiaForgeCore(
		this IServiceCollection services,
		CalradiaForgeCoreOptions? options = null) {
		ArgumentNullException.ThrowIfNull(services);
		options ??= new CalradiaForgeCoreOptions();
		options.Validate();

		services.AddSingleton(options);
		services.AddSingleton(provider => {
			CalradiaForgeCoreOptions paths = provider.GetRequiredService<CalradiaForgeCoreOptions>();
			ConfigFileManager manager = new(paths.ConfigFilePath);
			manager.Load();
			return manager;
		});
		services.AddSingleton<AppSettings>();
		services.AddSingleton<LoggingSettings>();
		services.AddSingleton(provider => {
			CalradiaForgeCoreOptions paths = provider.GetRequiredService<CalradiaForgeCoreOptions>();
			return new LogFileLifecycle(paths.LogsDirectory, paths.LogsFilePath);
		});
		services.AddSingleton(provider => {
			CalradiaForgeCoreOptions paths = provider.GetRequiredService<CalradiaForgeCoreOptions>();
			return new SerilogLoggerFactory(
				provider.GetRequiredService<LoggingSettings>(),
				provider.GetRequiredService<LogFileLifecycle>(),
				paths.LogsFilePath);
		});
		services.AddSingleton<Serilog.ILogger>(provider => {
			Serilog.ILogger logger = provider.GetRequiredService<SerilogLoggerFactory>().Create();
			Log.Logger = logger;
			return logger;
		});

		services.AddSingleton<ISteamClientRootProvider, WindowsSteamClientRootProvider>();
		services.AddSingleton<ISteamInstallationResolver, SteamInstallationResolver>();
		services.AddSingleton<GamePlatformDetectionResolver>();
		services.AddSingleton<StartupNotificationQueue>();
		services.AddSingleton<GameDetectionService>();

		services.AddSingleton(provider => {
			CalradiaForgeCoreOptions paths = provider.GetRequiredService<CalradiaForgeCoreOptions>();
			return new ModsData(paths.ModsCurrentFilePath, paths.ModsBackupFilePath);
		});
		services.AddSingleton<IModScanner, ModScanner>();
		services.AddSingleton<ModInstaller>();
		services.AddSingleton<ModPipelineManager>();

		services.AddSingleton(provider => {
			CalradiaForgeCoreOptions paths = provider.GetRequiredService<CalradiaForgeCoreOptions>();
			return new ModpackData(paths.ModpacksDirectory, paths.LastUsedModsFilePath);
		});
		services.AddSingleton<ModpackService>();
		services.AddSingleton<GameLauncher>();
		services.AddSingleton<EulaService>();

		services.AddSingleton(provider => {
			CalradiaForgeCoreOptions paths = provider.GetRequiredService<CalradiaForgeCoreOptions>();
			return new TranslationManager(
				paths.LanguagesDirectory,
				paths.LanguagesManifestFilePath,
				paths.DefaultLanguageFilePath);
		});
		services.AddSingleton<TranslationService>();

		return services;
	}
}
