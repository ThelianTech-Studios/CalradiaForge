namespace CalradiaForge.UI.Views;

using System;
using System.Windows;
using System.Windows.Input;

using CalradiaForge.Core.Models.Dialogs;

/// <summary>
/// Presents one themed lifecycle confirmation.
/// </summary>
public partial class ConfirmDialogWindow : Window {
	internal ConfirmDialogWindow(ConfirmDialogModel model) {
		ArgumentNullException.ThrowIfNull(model);
		InitializeComponent();
		DataContext = model;
	}

	private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
		try {
			DragMove();
		} catch (InvalidOperationException) {
			// Mouse was released outside the window bounds during dragging.
		}
	}

	private void PrimaryButton_Click(object sender, RoutedEventArgs e) {
		DialogResult = true;
	}

	private void SecondaryButton_Click(object sender, RoutedEventArgs e) {
		DialogResult = false;
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e) {
		DialogResult = false;
	}
}
