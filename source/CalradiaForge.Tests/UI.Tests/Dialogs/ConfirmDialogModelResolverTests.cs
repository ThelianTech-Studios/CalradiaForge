namespace CalradiaForge.Tests.UI.Dialogs;

using CalradiaForge.Core.Infra.Localization;
using CalradiaForge.Core.Models.Dialogs;
using CalradiaForge.UI.Dialogs;

public sealed class ConfirmDialogModelResolverTests {
	[Fact]
	public void Shutdown_MapsOrdinaryAndActiveModels() {
		TranslationStrings strings = new();

		ConfirmDialogModel ordinary = ConfirmDialogModelResolver.Resolve(
			ConfirmDialogPurpose.Shutdown,
			strings);
		ConfirmDialogModel active = ConfirmDialogModelResolver.Resolve(
			ConfirmDialogPurpose.Shutdown,
			strings,
			"A mod scan is currently running.");

		Assert.Equal(strings.ConfirmDialogShutdownTitleString, ordinary.WindowTitle);
		Assert.Equal(ordinary.WindowTitle, ordinary.Heading);
		Assert.Equal(strings.ConfirmDialogShutdownMessageString, ordinary.Message);
		Assert.Equal("Shut Down", ordinary.PrimaryButtonText);
		Assert.Equal("Cancel", ordinary.SecondaryButtonText);
		Assert.False(ordinary.HasDynamicContent);
		Assert.Equal(strings.ConfirmDialogShutdownActiveOperationsMessageString, active.Message);
		Assert.Equal("A mod scan is currently running.", active.DynamicContent);
		Assert.True(active.HasDynamicContent);
	}

	[Fact]
	public void Restart_MapsOrdinaryAndActiveModels() {
		TranslationStrings strings = new();

		ConfirmDialogModel ordinary = ConfirmDialogModelResolver.Resolve(
			ConfirmDialogPurpose.Restart,
			strings);
		ConfirmDialogModel active = ConfirmDialogModelResolver.Resolve(
			ConfirmDialogPurpose.Restart,
			strings,
			"A mod installation is currently running.");

		Assert.Equal(strings.ConfirmDialogRestartTitleString, ordinary.WindowTitle);
		Assert.Equal(ordinary.WindowTitle, ordinary.Heading);
		Assert.Equal(strings.ConfirmDialogRestartMessageString, ordinary.Message);
		Assert.Equal("Restart", ordinary.PrimaryButtonText);
		Assert.Equal("Cancel", ordinary.SecondaryButtonText);
		Assert.Equal(strings.ConfirmDialogRestartActiveOperationsMessageString, active.Message);
		Assert.Equal("A mod installation is currently running.", active.DynamicContent);
	}

	[Fact]
	public void DelayedShutdown_MapsLockedButtonSemantics() {
		TranslationStrings strings = new();

		ConfirmDialogModel model = ConfirmDialogModelResolver.Resolve(
			ConfirmDialogPurpose.DelayedShutdown,
			strings);

		Assert.Equal(strings.ConfirmDialogDSTitleString, model.WindowTitle);
		Assert.Equal(model.WindowTitle, model.Heading);
		Assert.Equal(strings.ConfirmDialogDSMessageString, model.Message);
		Assert.Equal("Exit Anyway", model.PrimaryButtonText);
		Assert.Equal("Keep Waiting", model.SecondaryButtonText);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void WhitespaceDynamicContent_UsesOrdinaryMessage(string? dynamicContent) {
		TranslationStrings strings = new();

		ConfirmDialogModel model = ConfirmDialogModelResolver.Resolve(
			ConfirmDialogPurpose.Restart,
			strings,
			dynamicContent);

		Assert.Equal(strings.ConfirmDialogRestartMessageString, model.Message);
		Assert.False(model.HasDynamicContent);
	}

	[Fact]
	public void Resolve_UsesCurrentStringsOnEveryInvocation() {
		TranslationStrings strings = new();
		ConfirmDialogModel first = ConfirmDialogModelResolver.Resolve(
			ConfirmDialogPurpose.Restart,
			strings);
		strings.Apply(new Dictionary<string, string> {
			[nameof(TranslationStrings.ConfirmDialogRestartTitleString)] = "Localized restart"
		});

		ConfirmDialogModel second = ConfirmDialogModelResolver.Resolve(
			ConfirmDialogPurpose.Restart,
			strings);

		Assert.Equal("Restart CalradiaForge", first.WindowTitle);
		Assert.Equal("Localized restart", second.WindowTitle);
	}

	[Fact]
	public void Resolve_RejectsUnsupportedPurpose() {
		ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
			() => ConfirmDialogModelResolver.Resolve((ConfirmDialogPurpose)99, new TranslationStrings()));

		Assert.Equal("purpose", exception.ParamName);
	}
}
