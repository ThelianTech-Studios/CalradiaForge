namespace CalradiaForge.UI.Interactions;

/// <summary>Returns folder choices for Settings path workflows.</summary>
public interface ISettingsFolderPicker {
	string? PickGameFolder();
	string? PickWorkshopFolder();
}
