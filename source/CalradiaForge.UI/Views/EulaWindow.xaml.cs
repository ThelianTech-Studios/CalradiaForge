namespace CalradiaForge.UI.Views {
	using System.Windows;
	using System.Windows.Input;

	using CalradiaForge.Core.Infra.Localization;

	/// <summary>
	/// Modal EULA acceptance window shown on first launch before the main window loads.
	/// </summary>
	public partial class EulaWindow : Window {
		/// <summary>Gets the translation service used by window bindings.</summary>
		public TranslationService Translator { get; }

		/// <summary>
		/// Gets whether the user accepted the EULA.
		/// </summary>
		public bool Accepted { get; private set; }

		/// <summary>
		/// Initializes the EULA window and populates the agreement text.
		/// </summary>
		public EulaWindow(string eulaText, TranslationService translator) {
			Translator = translator ?? throw new ArgumentNullException(nameof(translator));
			InitializeComponent();
			DataContext = this;
			EulaTextBox.Text = eulaText;
		}

		/// <summary>
		/// Enables dragging the window by the title bar.
		/// </summary>
		private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			try {
				DragMove();
			} catch (InvalidOperationException) {
				// Mouse was released outside the window bounds during dragging.
			}
		}

		/// <summary>
		/// Closes the window via the title bar close button without accepting.
		/// </summary>
		private void CloseButton_Click(object sender, RoutedEventArgs e) {
			Accepted = false;
			DialogResult = false;
			Close();
		}

		/// <summary>
		/// Records acceptance and closes the window.
		/// </summary>
		private void AcceptButton_Click(object sender, RoutedEventArgs e) {
			Accepted = true;
			DialogResult = true;
			Close();
		}

		/// <summary>
		/// Closes the window without accepting.
		/// </summary>
		private void DeclineButton_Click(object sender, RoutedEventArgs e) {
			Accepted = false;
			DialogResult = false;
			Close();
		}
	}
}
