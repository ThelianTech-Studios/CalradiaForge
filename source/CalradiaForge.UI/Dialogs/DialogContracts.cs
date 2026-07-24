namespace CalradiaForge.UI.Dialogs;

using CalradiaForge.Core.Models;
using CalradiaForge.UI.Lifecycle;

/// <summary>
/// Outcome selected after a cooperative shutdown interval expires.
/// </summary>
public enum DelayedShutdownChoice {
	ContinueWaiting,
	ExitAnyway
}

/// <summary>
/// Creates a fresh language-selection dialog for each invocation.
/// </summary>
public interface ILanguageSelectionDialogService {
	bool TrySelectLanguage(
		IReadOnlyList<LanguageOption> languages,
		string currentLanguageCode,
		out string selectedLanguageCode);
}

/// <summary>
/// Creates a fresh EULA dialog for each invocation.
/// </summary>
public interface IEulaDialogService {
	bool RequestAcceptance(string eulaText);
}

/// <summary>
/// Owns application-level lifecycle confirmations.
/// Fatal errors remain on WPF's native MessageBox path.
/// </summary>
public interface IApplicationDialogService {
	bool ConfirmShutdown(string? activeOperationDetails = null);
	bool ConfirmRestart(RestartReason reason, string? activeOperationDetails = null);
	DelayedShutdownChoice ChooseDelayedShutdown();
}
