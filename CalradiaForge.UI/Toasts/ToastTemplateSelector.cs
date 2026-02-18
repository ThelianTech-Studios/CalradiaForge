namespace CalradiaForge.UI.Toasts {
	using System.Windows;
	using System.Windows.Controls;

	/// <summary>
	/// Selects the appropriate DataTemplate for a toast based on its
	/// <see cref="ToastViewModel.TemplateKey"/>. Falls back to
	/// <see cref="DefaultTemplate"/> when no specific match is found.
	/// </summary>
	public sealed class ToastTemplateSelector : DataTemplateSelector {
		public DataTemplate? DefaultTemplate { get; set; }
		public DataTemplate? InstallSummaryTemplate { get; set; }
		public DataTemplate? InstallProgressTemplate { get; set; }
		public DataTemplate? MissingModsTemplate { get; set; }

		public override DataTemplate SelectTemplate(object item, DependencyObject container) {
			if (item is ToastViewModel viewModel) {
				if (viewModel.TemplateKey == ToastTemplateKeys.InstallSummary) {
					return InstallSummaryTemplate ?? DefaultTemplate ?? base.SelectTemplate(item, container);
				}

				if (viewModel.TemplateKey == ToastTemplateKeys.InstallProgress) {
					return InstallProgressTemplate ?? DefaultTemplate ?? base.SelectTemplate(item, container);
				}

				if (viewModel.TemplateKey == ToastTemplateKeys.MissingMods) {
					return MissingModsTemplate ?? DefaultTemplate ?? base.SelectTemplate(item, container);
				}
			}

			return DefaultTemplate ?? base.SelectTemplate(item, container);
		}
	}
}