namespace CalradiaForge.UI.Pages {
	using System.Diagnostics;
	using System.Windows;
	using System.Windows.Controls;

	/// <summary>
	/// Interaction logic for FaqPage.xaml
	/// </summary>
	public partial class FaqPage : Page {
		/// <summary>
		/// Initializes the FAQ page.
		/// </summary>
		public FaqPage() {
			InitializeComponent();
		}

		/// <summary>
		/// Opens the GitHub issues page in the default browser.
		/// </summary>
		private void OpenGitHubIssues_Click(object sender, RoutedEventArgs e) {
			Process.Start(new ProcessStartInfo {
				FileName = "https://github.com/ThelianTech/CalradiaForge/issues",
				UseShellExecute = true,
			});
		}
	}
}
