namespace CalradiaForge.UI.Composition;

using CalradiaForge.UI.Views;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Resolves the retained shell from the sole application provider.
/// </summary>
public sealed class MainWindowProvider : IMainWindowProvider {
	private readonly IServiceProvider _services;

	public MainWindowProvider(IServiceProvider services) {
		_services = services ?? throw new ArgumentNullException(nameof(services));
	}

	public MainWindow GetMainWindow() => _services.GetRequiredService<MainWindow>();
}
