namespace CalradiaForge.Tests.UI.Toasts;

using CalradiaForge.UI.Toasts;

public sealed class ToastServiceTests {
	[Theory]
	[InlineData(ToastSeverity.Info)]
	[InlineData(ToastSeverity.Success)]
	[InlineData(ToastSeverity.Warning)]
	[InlineData(ToastSeverity.Error)]
	public void GetDefaultDuration_UsesEightSecondsForEverySeverity(
		ToastSeverity severity) {
		Assert.Equal(TimeSpan.FromSeconds(8), ToastService.GetDefaultDuration(severity));
	}
}
