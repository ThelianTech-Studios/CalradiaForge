namespace CalradiaForge.UI.Interactions;

/// <summary>Returns executable choices for Settings path workflows.</summary>
public interface ISettingsFilePicker {
	string? PickGameExecutable();
	string? PickBlseExecutable();
}
