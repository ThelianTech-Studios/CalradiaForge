namespace CalradiaForge.UI.Pages {
	using System.Diagnostics;
	using System.Windows;
	using System.Windows.Controls;

	using CalradiaForge.Core.Infra.Localization;

	/// <summary>
	/// Interaction logic for FaqPage.xaml
	/// </summary>
	public partial class FaqPage : Page {
		/// <summary>Gets the translation service used by page bindings.</summary>
		public TranslationService Translator { get; }

		/// <summary>
		/// Initializes the FAQ page.
		/// </summary>
		public FaqPage(TranslationService translator) {
			Translator = translator ?? throw new ArgumentNullException(nameof(translator));
			InitializeComponent();
			DataContext = this;
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
