namespace CalradiaForge.UI.Views {
	using System;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;

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
			if (this.WindowState == WindowState.Maximized && e.ClickCount == 2) {
				var mousePos = e.GetPosition(this);
				double horizonBarRatio = Math.Max(0.0,Math.Min(1.0,mousePos.X / this.ActualWidth));

				var rb = this.RestoreBounds;
				double targetWidth = rb.Width > 0 ? rb.Width : Math.Max(this.MinWidth,1200);
				double targetHeight = rb.Height > 0 ? rb.Height : Math.Max(this.MinHeight,700);

				var screenPoint = this.PointToScreen(mousePos);

				this.WindowState = WindowState.Normal;
				this.Left = screenPoint.X - (horizonBarRatio * targetWidth);
				this.Top = screenPoint.Y - mousePos.Y;
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