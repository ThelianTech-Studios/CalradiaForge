namespace CalradiaForge.UI.Views {
	using System;
	using System.ComponentModel;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;

	using CalradiaForge.Core.Infra.Localization;
	using CalradiaForge.UI.Lifecycle;
	using CalradiaForge.UI.Pages;
	using CalradiaForge.UI.Toasts;

	using MahApps.Metro.Controls;
	using Serilog;

	/// <summary>
	/// Main application window hosting navigation and page content.
	/// </summary>
	public partial class MainWindow : Window {
		private readonly Page[] _pages;
		private readonly SettingsPage _settingsPage;
		private readonly TranslationService _translator;
		private readonly StartupNotificationDrainCoordinator _startupNotificationDrain;
		private readonly IApplicationLifetime _applicationLifetime;
		private bool _applicationShutdownPrepared;
		private bool _shutdownRequestInProgress;
		/// <summary>
		/// Initializes the main window and navigation pages.
		/// </summary>
		public MainWindow(
			ModsPage modsPage,
			ModpacksPage modpacksPage,
			FaqPage faqPage,
			SettingsPage settingsPage,
			ToastService toastService,
			TranslationService translator,
			StartupNotificationDrainCoordinator startupNotificationDrain,
			IApplicationLifetime applicationLifetime) {
			InitializeComponent();
			Loaded += MainWindow_Loaded;
			Closing += MainWindow_Closing;
			_translator = translator ?? throw new ArgumentNullException(nameof(translator));
			_startupNotificationDrain = startupNotificationDrain
				?? throw new ArgumentNullException(nameof(startupNotificationDrain));
			_applicationLifetime = applicationLifetime
				?? throw new ArgumentNullException(nameof(applicationLifetime));
			_pages = [
				modsPage ?? throw new ArgumentNullException(nameof(modsPage)),
				modpacksPage ?? throw new ArgumentNullException(nameof(modpacksPage)),
				faqPage ?? throw new ArgumentNullException(nameof(faqPage)),
			];
			_settingsPage = settingsPage ?? throw new ArgumentNullException(nameof(settingsPage));
			MainContentFrame.Navigate(_pages[0]);

			Log.Debug("MainWindow: Initialized {PageCount} pages.", _pages.Length);
			// Wire the Options item (Settings) — separate from ItemsSource
			NavBarControler.OptionsItemClick += NavBarControler_OnOptionsItemClick;

			// Bind the toast overlay to the shared ToastService
			ToastHost.ItemsSource = (toastService ?? throw new ArgumentNullException(nameof(toastService))).VisibleToasts;

			// Apply translated nav labels and re-apply when language changes
			ApplyNavTranslations();
			_translator.Strings.PropertyChanged += (_, _) => Dispatcher.BeginInvoke(ApplyNavTranslations);
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
			_startupNotificationDrain.SignalReady();
		}

		private async void MainWindow_Closing(object? sender, CancelEventArgs e) {
			if (_applicationShutdownPrepared) {
				return;
			}

			e.Cancel = true;
			if (_shutdownRequestInProgress) {
				return;
			}

			_shutdownRequestInProgress = true;
			try {
				await _applicationLifetime.RequestShutdownAsync(ShutdownReason.UserRequest);
			} catch (Exception ex) {
				Log.Error(ex, "MainWindow: Shutdown request failed.");
			} finally {
				if (!_applicationShutdownPrepared) {
					_shutdownRequestInProgress = false;
				}
			}
		}

		/// <summary>
		/// Allows the app-owned lifecycle to complete the final WPF shutdown.
		/// </summary>
		public void PrepareForApplicationShutdown() {
			_applicationShutdownPrepared = true;
		}

		/// <summary>
		/// Applies translated labels to navigation items.
		/// </summary>
		private void ApplyNavTranslations() {
			TranslationStrings s = _translator.Strings;

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
				Log.Debug(
					"MainWindow: Navigated to page {PageIndex} of type {PageType}.",
					index,
					targetPage.GetType().Name);
			}

		}

		/// <summary>
		/// Handles navigation to the settings page from the options menu.
		/// </summary>
		private void NavBarControler_OnOptionsItemClick(object sender, ItemClickEventArgs e) {
			// Deselect the main nav so Settings appears as the active context
			NavBarControler.SelectedIndex = -1;
			MainContentFrame.Navigate(_settingsPage);
			Log.Debug("MainWindow: Navigated to settings.");
		}
	}
}
