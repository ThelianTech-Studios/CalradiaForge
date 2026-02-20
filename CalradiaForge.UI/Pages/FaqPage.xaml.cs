

namespace CalradiaForge.UI.Pages
	{
	using System.Diagnostics;
	using System.Windows;
	using System.Windows.Controls;

	/// <summary>
	/// Interaction logic for FaqPage.xaml
	/// </summary>
	public partial class FaqPage : Page
		{
		public FaqPage() {
			InitializeComponent();
			}

		private void OpenGitHubIssues_Click(object sender,RoutedEventArgs e) {
			Process.Start(new ProcessStartInfo {
				FileName="https://github.com/ThelianTech/CalradiaForge/issues",
				UseShellExecute=true,
				});
			}
		}
	}
