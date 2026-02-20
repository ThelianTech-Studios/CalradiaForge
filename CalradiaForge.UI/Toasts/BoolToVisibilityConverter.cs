namespace CalradiaForge.UI.Toasts {
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;

	/// <summary>
	/// Converts a boolean to <see cref="Visibility"/>.
	/// True = Visible, False = Collapsed.
	/// </summary>
	[ValueConversion(typeof(bool), typeof(Visibility))]
	public sealed class BoolToVisibilityConverter : IValueConverter {
		/// <summary>
		/// Converts a boolean value to a visibility state.
		/// </summary>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return value is true ? Visibility.Visible : Visibility.Collapsed;
		}

		/// <summary>
		/// Converts a visibility state back to a boolean value.
		/// </summary>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			return value is Visibility.Visible;
		}
	}
}