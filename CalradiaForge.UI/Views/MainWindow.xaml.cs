namespace CalradiaForge.UI {
	using System.Windows;
	using System.Windows.Input;

	using MahApps.Metro.Controls;

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();
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
				throw new InvalidOperationException("Failed to move the window. This can happen if the mouse is released outside the window bounds during dragging.", ex);
			}
		}
		private void NavBarControler_OnItemInvoked(object sender,HamburgerMenuItemInvokedEventArgs e) {

		}
	}
}