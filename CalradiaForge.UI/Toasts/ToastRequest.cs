namespace CalradiaForge.UI.Toasts {
	using System;

	/// <summary>
	/// Describes a toast notification to show. Created by callers,
	/// consumed by <see cref="ToastService"/>.
	/// </summary>
	public sealed class ToastRequest {
		/// <summary>Title text displayed in the toast header.</summary>
		public string Title { get; init; } = string.Empty;

		/// <summary>Body message displayed below the title.</summary>
		public string Message { get; init; } = string.Empty;

		/// <summary>Visual severity — controls accent color and icon.</summary>
		public ToastSeverity Severity { get; init; } = ToastSeverity.Info;

		/// <summary>
		/// Override the auto-dismiss duration. When null the service
		/// picks a default based on <see cref="Severity"/>.
		/// Ignored when <see cref="IsPersistent"/> is true.
		/// </summary>
		public TimeSpan? Duration { get; init; }

		/// <summary>
		/// When true the toast stays visible until explicitly closed.
		/// Useful for progress toasts.
		/// </summary>
		public bool IsPersistent { get; init; }

		/// <summary>Whether clicking the toast body dismisses it.</summary>
		public bool AllowClickDismiss { get; init; } = true;

		/// <summary>Whether the close (×) button is visible.</summary>
		public bool ShowCloseButton { get; init; } = true;

		/// <summary>
		/// Optional DataTemplate key used to select an alternate visual.
		/// Defaults to <see cref="ToastTemplateKeys.Default"/>.
		/// </summary>
		public string TemplateKey { get; init; } = ToastTemplateKeys.Default;

		/// <summary>Initial progress value (for progress toasts).</summary>
		public double ProgressValue { get; init; }

		/// <summary>Maximum progress value (for progress toasts).</summary>
		public double ProgressMax { get; init; } = 100;
	}
}