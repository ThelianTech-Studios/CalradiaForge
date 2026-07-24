namespace CalradiaForge.UI.Dialogs;

using CalradiaForge.Core.Infra.Localization;
using CalradiaForge.UI.Views;

/// <summary>
/// Default EULA acceptance dialog service.
/// </summary>
public sealed class EulaDialogService : IEulaDialogService {
	private readonly TranslationService _translationService;

	public EulaDialogService(TranslationService translationService) {
		_translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
	}

	public bool RequestAcceptance(string eulaText) {
		EulaWindow window = new(eulaText, _translationService);
		DialogOwner.Assign(window);
		return window.ShowDialog() == true && window.Accepted;
	}
}
