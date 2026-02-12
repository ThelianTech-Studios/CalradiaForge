namespace CalradiaForge.UI.Helpers;

using System.Windows;

/// <summary>
/// Attached properties for fine-tuning individual HamburgerMenu item layout.
/// Set these on each HamburgerMenuIconItem to control icon and text positioning.
/// </summary>
public static class NavMenuProperties {
	// --- IconPadding ---
	public static readonly DependencyProperty IconPaddingProperty =
		DependencyProperty.RegisterAttached(
			"IconPadding", typeof(Thickness), typeof(NavMenuProperties),
			new FrameworkPropertyMetadata(new Thickness(0), FrameworkPropertyMetadataOptions.Inherits));

	public static void SetIconPadding(DependencyObject element, Thickness value)
		=> element.SetValue(IconPaddingProperty, value);

	public static Thickness GetIconPadding(DependencyObject element)
		=> (Thickness)element.GetValue(IconPaddingProperty);

	// --- TextPadding ---
	public static readonly DependencyProperty TextPaddingProperty =
		DependencyProperty.RegisterAttached(
			"TextPadding", typeof(Thickness), typeof(NavMenuProperties),
			new FrameworkPropertyMetadata(new Thickness(0), FrameworkPropertyMetadataOptions.Inherits));

	public static void SetTextPadding(DependencyObject element, Thickness value)
		=> element.SetValue(TextPaddingProperty, value);

	public static Thickness GetTextPadding(DependencyObject element)
		=> (Thickness)element.GetValue(TextPaddingProperty);

	// --- TextMargin ---
	public static readonly DependencyProperty TextMarginProperty =
		DependencyProperty.RegisterAttached(
			"TextMargin", typeof(Thickness), typeof(NavMenuProperties),
			new FrameworkPropertyMetadata(new Thickness(0, 5, 0, 0), FrameworkPropertyMetadataOptions.Inherits));

	public static void SetTextMargin(DependencyObject element, Thickness value)
		=> element.SetValue(TextMarginProperty, value);

	public static Thickness GetTextMargin(DependencyObject element)
		=> (Thickness)element.GetValue(TextMarginProperty);
}