namespace CalradiaForge.UI.Composition;

using System.Windows.Threading;

using CalradiaForge.UI.Dialogs;
using CalradiaForge.UI.Interactions;
using CalradiaForge.UI.Lifecycle;
using CalradiaForge.UI.Pages;
using CalradiaForge.UI.Threading;
using CalradiaForge.UI.Toasts;
using CalradiaForge.UI.ViewModels;
using CalradiaForge.UI.Views;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Adds WPF presentation services to the application's shared service collection.
/// This method never builds or resolves a provider.
/// </summary>
public static class ServiceCollectionExtensions {
	public static IServiceCollection AddCalradiaForgeUi(
		this IServiceCollection services,
		Dispatcher dispatcher) {
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(dispatcher);

		services.AddSingleton(dispatcher);
		services.AddSingleton<IUiDispatcher, WpfUiDispatcher>();
		services.AddSingleton<IModArchiveFilePicker, ModArchiveFilePicker>();
		services.AddSingleton<IModpackImportFilePicker, ModpackImportFilePicker>();
		services.AddSingleton<ISettingsFilePicker, SettingsFilePicker>();
		services.AddSingleton<ISettingsFolderPicker, SettingsFolderPicker>();
		services.AddSingleton<ISettingsShellLauncher, SettingsShellLauncher>();
		services.AddSingleton<ISettingsDllUnblocker, SettingsDllUnblocker>();
		services.AddSingleton<ToastService>();
		services.AddSingleton<IToastNotificationSink, ToastNotificationSink>();
		services.AddSingleton<IInstallNotificationPresenter, InstallNotificationPresenter>();
		services.AddSingleton<IStartupNotificationPresenter, ToastStartupNotificationPresenter>();
		services.AddSingleton<StartupNotificationDrainCoordinator>();

		services.AddSingleton<ILanguageSelectionDialogService, LanguageSelectionDialogService>();
		services.AddSingleton<IEulaDialogService, EulaDialogService>();
		services.AddSingleton<IApplicationDialogService, ApplicationDialogService>();

		services.AddSingleton<IApplicationWorkController, ModPipelineApplicationWorkController>();
		services.AddSingleton<IApplicationStatePersistence, ModpackApplicationStatePersistence>();
		services.AddSingleton<ApplicationStartupCoordinator>();
		services.AddSingleton<ApplicationShutdownCoordinator>();

		services.AddSingleton<LauncherViewModel>();
		services.AddSingleton<ModpacksViewModel>();
		services.AddSingleton<SettingsViewModel>();
		services.AddSingleton<LauncherPage>();
		services.AddSingleton<ModpacksPage>();
		services.AddSingleton<FaqPage>();
		services.AddSingleton<SettingsPage>();
		services.AddSingleton<MainWindow>();
		services.AddSingleton<IMainWindowProvider, MainWindowProvider>();

		return services;
	}
}
