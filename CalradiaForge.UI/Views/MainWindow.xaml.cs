namespace CalradiaForge.UI.Views {
	using System;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;

	using CalradiaForge.Core.Infra.Localization;
	using CalradiaForge.Core.Infra.Logging;

	using CalradiaForge.UI.Pages;

	using MahApps.Metro.Controls;

	/// <summary>
	/// Main application window hosting navigation and page content.
	/// </summary>
	public partial class MainWindow : Window {
		private readonly Logger _logger = Logger.Instance;
		private readonly Page[] _pages;
		private readonly SettingsPage _settingsPage;
		/// <summary>
		/// Initializes the main window and navigation pages.
		/// </summary>
		public MainWindow() {
			InitializeComponent();
			_pages = [
				new ModsPage(),
				new ModpacksPage(),
				new FaqPage(),
			];
			_settingsPage = new SettingsPage();
			MainContentFrame.Navigate(_pages[0]);

			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("MainWindow: Initialized pages.", new { PageCount = _pages.Length });
			}
			// Wire the Options item (Settings) — separate from ItemsSource
			NavBarControler.OptionsItemClick += NavBarControler_OnOptionsItemClick;

			// Bind the toast overlay to the shared ToastService
			ToastHost.ItemsSource = App.Toasts.VisibleToasts;

			// Apply translated nav labels and re-apply when language changes
			ApplyNavTranslations();
			App.Translator.Strings.PropertyChanged += (_, _) => Dispatcher.BeginInvoke(ApplyNavTranslations);
		}

		/// <summary>
		/// Applies translated labels to navigation items.
		/// </summary>
		private void ApplyNavTranslations() {
			TranslationStrings s = App.Translator.Strings;

			// Main nav items (indices 0–2)
			if (NavBarControler.ItemsSource is HamburgerMenuItemCollection mainItems) {
				if (mainItems.Count > 0 && mainItems[0] is HamburgerMenuIconItem mods) {
					mods.Label = s.Nav_ModsTab;
				}
				if (mainItems.Count > 1 && mainItems[1] is HamburgerMenuIconItem packs) {
					packs.Label = s.Nav_ModpacksTab;
				}
				if (mainItems.Count > 2 && mainItems[2] is HamburgerMenuIconItem faq) {
					faq.Label = s.Nav_FaqTab;
				}
			}

			// Options item (Settings)
			if (NavBarControler.OptionsItemsSource is HamburgerMenuItemCollection optItems) {
				if (optItems.Count > 0 && optItems[0] is HamburgerMenuIconItem settings) {
					settings.Label = s.Nav_SettingsTab;
				}
			}
		}

		/// <summary>
		/// Minimizes the window when the minimize button is clicked.
		/// </summary>
		private void MinimizeButton_Click(object sender, RoutedEventArgs e) {
			this.WindowState = WindowState.Minimized;
		}
		/// <summary>
		/// Closes the window when the close button is clicked.
		/// </summary>
		private void CloseButton_Click(object sender, RoutedEventArgs e) {
			this.Close();
		}
		/// <summary>
		/// Enables drag or maximize behavior on title bar interaction.
		/// </summary>
		private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			if (e.ClickCount == 2) {
				this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
				return;
			}
			try {
				this.DragMove();
			} catch (Exception ex) {
				throw new InvalidOperationException("Failed to move the window. This can happen if the mouse is released outside the window bounds during dragging.", ex);
			}
		}
		/// <summary>
		/// Handles navigation item selection in the main menu.
		/// </summary>
		private void NavBarControler_OnItemInvoked(object sender, HamburgerMenuItemInvokedEventArgs e) {
			int index = NavBarControler.SelectedIndex;
			if (index >= 0 && index < _pages.Length) {
				Page targetPage = _pages[index];
				// Sync modpack and mod data when navigating back to ModsPage.
				// RefreshModpackList rebuilds the ComboBox only (suppressApply)
				// because RefreshAvailableMods will scan, rebuild, and apply
				// the modpack with a single authoritative toast.
				if (targetPage is ModsPage modspage) {
					modspage.RefreshModpackList(suppressApply: true);
					modspage.RefreshAvailableMods();
				}
				MainContentFrame.Navigate(_pages[index]);
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("MainWindow: Navigated to page.", new { PageIndex = index, PageType = targetPage.GetType().Name });
				}
			}

		}

		/// <summary>
		/// Handles navigation to the settings page from the options menu.
		/// </summary>
		private void NavBarControler_OnOptionsItemClick(object sender, ItemClickEventArgs e) {
			// Deselect the main nav so Settings appears as the active context
			NavBarControler.SelectedIndex = -1;
			MainContentFrame.Navigate(_settingsPage);
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("MainWindow: Navigated to settings.");
			}
		}
	}
}