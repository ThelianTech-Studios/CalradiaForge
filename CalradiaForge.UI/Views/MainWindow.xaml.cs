namespace CalradiaForge.UI.Views {
	using System;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;

	using CalradiaForge.Core.Infra.Localization;

	using CalradiaForge.UI.Pages;

	using MahApps.Metro.Controls;

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private readonly Page[] _pages;
		private readonly SettingsPage _settingsPage;
		public MainWindow() {
			InitializeComponent();
			_pages = [
				new ModsPage(),
				new ModpacksPage(),
				new FaqPage(),
			];
			_settingsPage = new SettingsPage();
			MainContentFrame.Navigate(_pages[0]);

			// Wire the Options item (Settings) — separate from ItemsSource
			NavBarControler.OptionsItemClick += NavBarControler_OnOptionsItemClick;

			// Bind the toast overlay to the shared ToastService
			ToastHost.ItemsSource = App.Toasts.VisibleToasts;

			// Apply translated nav labels and re-apply when language changes
			ApplyNavTranslations();
			App.Translator.Strings.PropertyChanged += (_, _) => Dispatcher.BeginInvoke(ApplyNavTranslations);
		}

		/// <summary>
		/// Applies translated strings to the HamburgerMenu nav labels.
		/// <see cref="HamburgerMenuIconItem.Label"/> is not a DependencyProperty,
		/// so XAML binding is not supported — we set it in code instead.
		/// Called on startup and whenever the active language changes.
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

		private void MinimizeButton_Click(object sender,RoutedEventArgs e) {
			this.WindowState = WindowState.Minimized;
		}
		private void CloseButton_Click(object sender,RoutedEventArgs e) {
			this.Close();
		}
		private void TitleBar_MouseLeftButtonDown(object sender,MouseButtonEventArgs e) {
			if (e.ClickCount == 2) {
				this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
				return;
			}
			try {
				this.DragMove();
			} catch (Exception ex) {
				throw new InvalidOperationException("Failed to move the window. This can happen if the mouse is released outside the window bounds during dragging.",ex);
			}
		}
		private void NavBarControler_OnItemInvoked(object sender,HamburgerMenuItemInvokedEventArgs e) {
			int index = NavBarControler.SelectedIndex;
			if (index >= 0 && index < _pages.Length) {
				Page targetPage = _pages[index];
				// Sync modpack and mod data when navigating back to ModsPage
				if (targetPage is ModsPage modspage) {
					modspage.RefreshModpackList();
					modspage.RefreshAvailableMods();
					}
				MainContentFrame.Navigate(_pages[index]);
				}
				
		}

		/// <summary>
		/// Handles the Settings item click from the HamburgerMenu OptionsItemsSource.
		/// OptionsItemsSource is separate from ItemsSource — it does not participate
		/// in SelectedIndex and fires its own OptionsItemClick event.
		/// Navigates to the SettingsPage which is held as a standalone instance.
		/// </summary>
		private void NavBarControler_OnOptionsItemClick(object sender, ItemClickEventArgs e) {
			// Deselect the main nav so Settings appears as the active context
			NavBarControler.SelectedIndex = -1;
			MainContentFrame.Navigate(_settingsPage);
		}
	}
}