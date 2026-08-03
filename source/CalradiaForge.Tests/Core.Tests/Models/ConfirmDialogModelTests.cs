namespace CalradiaForge.Tests.Core.Models;

using CalradiaForge.Core.Models.Dialogs;

public sealed class ConfirmDialogModelTests {
	[Fact]
	public void PurposeValues_AreStable() {
		Assert.Equal(1, (int)ConfirmDialogPurpose.Shutdown);
		Assert.Equal(2, (int)ConfirmDialogPurpose.Restart);
		Assert.Equal(3, (int)ConfirmDialogPurpose.DelayedShutdown);
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData("", false)]
	[InlineData("   ", false)]
	[InlineData("Active work", true)]
	public void HasDynamicContent_ReflectsMeaningfulContent(string? dynamicContent, bool expected) {
		ConfirmDialogModel model = new(
			"Window",
			"Heading",
			"Message",
			"Primary",
			"Secondary",
			dynamicContent);

		Assert.Equal(expected, model.HasDynamicContent);
		Assert.Equal(dynamicContent, model.DynamicContent);
	}
}
