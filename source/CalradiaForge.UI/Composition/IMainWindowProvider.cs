namespace CalradiaForge.UI.Composition;

using CalradiaForge.UI.Views;

/// <summary>
/// Narrow typed boundary that defers shell construction until startup gates pass.
/// </summary>
public interface IMainWindowProvider {
	MainWindow GetMainWindow();
}
