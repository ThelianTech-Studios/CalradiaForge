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
	using CalradiaForge.UI.ViewModels;

	using MahApps.Metro.Controls;
	using Serilog;

	/// <summary>
	/// Main application window hosting navigation and page content.
	/// </summary>
	public partial class MainWindow : Window {
		private readonly Page[] _pages;
		private readonly IViewModelLifecycle?[] _pageLifecycles;
		private readonly SettingsPage _settingsPage;
		private readonly SettingsViewModel _settingsViewModel;
		private readonly TranslationService _translator;
		private readonly StartupNotificationDrainCoordinator _startupNotificationDrain;
		private readonly IApplicationLifetime _applicationLifetime;
		private readonly SemaphoreSlim _navigationGate = new(1, 1);
		private Page? _currentPage;
		private IViewModelLifecycle? _currentLifecycle;
		private int _committedMainNavigationIndex;
		private bool _initialNavigationCompleted;
		private bool _applicationShutdownPrepared;
		private bool _shutdownRequestInProgress;
		/// <summary>
		/// Initializes the main window and navigation pages.
		/// </summary>
		public MainWindow(
			LauncherPage launcherPage,
			LauncherViewModel launcherViewModel,
			ModpacksPage modpacksPage,
			ModpacksViewModel modpacksViewModel,
			FaqPage faqPage,
			SettingsPage settingsPage,
			SettingsViewModel settingsViewModel,
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
				launcherPage ?? throw new ArgumentNullException(nameof(launcherPage)),
				modpacksPage ?? throw new ArgumentNullException(nameof(modpacksPage)),
				faqPage ?? throw new ArgumentNullException(nameof(faqPage)),
			];
			_pageLifecycles = [
				launcherViewModel ?? throw new ArgumentNullException(nameof(launcherViewModel)),
				modpacksViewModel ?? throw new ArgumentNullException(nameof(modpacksViewModel)),
				null
			];
			_settingsPage = settingsPage ?? throw new ArgumentNullException(nameof(settingsPage));
			_settingsViewModel = settingsViewModel
				?? throw new ArgumentNullException(nameof(settingsViewModel));

			Log.Debug("MainWindow: Initialized {PageCount} pages.", _pages.Length);
			// Wire the Options item (Settings) — separate from ItemsSource
			NavBarControler.OptionsItemClick += NavBarControler_OnOptionsItemClick;

			// Bind the toast overlay to the shared ToastService
			ToastHost.ItemsSource = (toastService ?? throw new ArgumentNullException(nameof(toastService))).VisibleToasts;

			// Apply translated nav labels and re-apply when language changes
			ApplyNavTranslations();
			_translator.Strings.PropertyChanged += (_, _) => Dispatcher.BeginInvoke(ApplyNavTranslations);
		}

		private async void MainWindow_Loaded(object sender, RoutedEventArgs e) {
			if (_initialNavigationCompleted) {
				return;
			}
			bool succeeded = await NavigateAsync(
				_pages[0],
				_pageLifecycles[0],
				mainNavigationIndex: 0,
				isSettings: false);
			CompleteInitialNavigation(
				ref _initialNavigationCompleted,
				succeeded,
				_startupNotificationDrain.SignalReady);
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
				if (mainItems.Count > 0 && mainItems[0] is HamburgerMenuIconItem launcher) {
					launcher.Label = s.Nav_LauncherTab;
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
		private async void NavBarControler_OnItemInvoked(
			object sender,
			HamburgerMenuItemInvokedEventArgs e) {
			int index = NavBarControler.SelectedIndex;
			if (index >= 0 && index < _pages.Length) {
				bool succeeded = await NavigateAsync(
					_pages[index],
					_pageLifecycles[index],
					index,
					isSettings: false);
				CompleteInitialNavigation(
					ref _initialNavigationCompleted,
					succeeded,
					_startupNotificationDrain.SignalReady);
			}
		}

		/// <summary>
		/// Handles navigation to the settings page from the options menu.
		/// </summary>
		private async void NavBarControler_OnOptionsItemClick(object sender, ItemClickEventArgs e) {
			bool succeeded = await NavigateAsync(
				_settingsPage,
				_settingsViewModel,
				mainNavigationIndex: -1,
				isSettings: true);
			CompleteInitialNavigation(
				ref _initialNavigationCompleted,
				succeeded,
				_startupNotificationDrain.SignalReady);
		}

		private async Task<bool> NavigateAsync(
			Page targetPage,
			IViewModelLifecycle? targetLifecycle,
			int mainNavigationIndex,
			bool isSettings) {
			ArgumentNullException.ThrowIfNull(targetPage);
			bool targetDisplayed = false;
			try {
				await ExecuteNavigationLifecycleAsync(
					_navigationGate,
					() => _currentLifecycle,
					targetLifecycle,
					() => {
						MainContentFrame.Navigate(targetPage);
						_currentPage = targetPage;
						_currentLifecycle = targetLifecycle;
						_committedMainNavigationIndex =
							isSettings ? -1 : mainNavigationIndex;
						NavBarControler.SelectedIndex = _committedMainNavigationIndex;
						targetDisplayed = true;
					});
				Log.Debug(
					"MainWindow: Navigated to {PageType}.",
					targetPage.GetType().Name);
				return true;
			} catch (Exception ex) {
				if (!targetDisplayed) {
					NavBarControler.SelectedIndex = SelectionAfterNavigationFailure(
						targetDisplayed,
						_committedMainNavigationIndex,
						mainNavigationIndex);
				}
				Log.Error(
					ex,
					"MainWindow: Lifecycle navigation to {PageType} failed; displayed page is {CurrentPageType}.",
					targetPage.GetType().Name,
					_currentPage?.GetType().Name ?? "none");
				return false;
			}
		}

		internal static int SelectionAfterNavigationFailure(
			bool targetDisplayed,
			int committedSelection,
			int requestedSelection) =>
			targetDisplayed ? requestedSelection : committedSelection;

		internal static void CompleteInitialNavigation(
			ref bool initialNavigationCompleted,
			bool navigationSucceeded,
			Action signalReady) {
			ArgumentNullException.ThrowIfNull(signalReady);
			if (initialNavigationCompleted || !navigationSucceeded) {
				return;
			}
			signalReady();
			initialNavigationCompleted = true;
		}

		internal static async Task ExecuteNavigationLifecycleAsync(
			SemaphoreSlim navigationGate,
			Func<IViewModelLifecycle?> currentLifecycle,
			IViewModelLifecycle? targetLifecycle,
			Action displayTarget,
			CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(navigationGate);
			ArgumentNullException.ThrowIfNull(currentLifecycle);
			ArgumentNullException.ThrowIfNull(displayTarget);
			await navigationGate.WaitAsync(cancellationToken);
			try {
				// Re-selecting the current target deliberately performs a full
				// deactivate/display/activate cycle so repeatable activation can
				// reconcile navigation-return state deterministically.
				if (targetLifecycle is not null) {
					await targetLifecycle.InitializeAsync(cancellationToken);
				}
				IViewModelLifecycle? current = currentLifecycle();
				if (current is { IsInitialized: true }) {
					await current.DeactivateAsync(cancellationToken);
				}
				displayTarget();
				if (targetLifecycle is not null) {
					await targetLifecycle.ActivateAsync(cancellationToken);
				}
			} finally {
				navigationGate.Release();
			}
		}
	}
}
