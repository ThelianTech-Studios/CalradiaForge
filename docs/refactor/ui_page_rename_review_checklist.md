# UI Page Rename Review Checklist

Status: planning template
Scope: owner-review support only. This checklist does not authorize UI page renames.

## Purpose

Prepare an owner-approved rename map for current WPF page `.xaml` files before broad MVVM extraction or future Nexus UI work.

## Do Not Rename Yet

This checklist does not authorize renames. It exists to gather candidates and risks for owner approval.

No UI page `.xaml` file may be renamed until the owner fills or approves an explicit rename map.

## Inventory Table

| Current XAML file | Code-behind class | Current navigation key/reference | Proposed name | Reason | Risk | Owner approved? |
|---|---|---|---|---|---|---|
| `source/CalradiaForge.UI/Views/MainWindow.xaml` | `CalradiaForge.UI.Views.MainWindow` | `None` | Deferred - owner map required | Inventory only for future MVVM/Nexus UI review. | `Add Risk` | No |
| `source/CalradiaForge.UI/ModsPage.xaml` | `CalradiaForge.UI.Pages.ModsPage.cs` | `MainWindow._pages[0]`; main nav item index 0 | Deferred - owner map required | Inventory only for future MVVM/Nexus UI review. | Medium: navigation labels, resource styles, localized bindings, and generated partial class references. | No |
| `source/CalradiaForge.UI/Pages/ModpacksPage.xaml` | `CalradiaForge.UI.Pages.ModpacksPage` | `MainWindow._pages[1]`; main nav item index 1 | Deferred - owner map required | Inventory only for future MVVM/Nexus UI review. | High: modpack workflow bindings, resource styles, active load-order sync, and generated partial class references. | No |
| `source/CalradiaForge.UI/Pages/FaqPage.xaml` | `CalradiaForge.UI.Pages.FaqPage` | `MainWindow._pages[2]`; main nav item index 2 | Deferred - owner map required | Inventory only for future MVVM/Nexus UI review. | Medium: navigation labels, resource styles, localized bindings, and generated partial class references. | No |
| `source/CalradiaForge.UI/Pages/SettingsPage.xaml` | `CalradiaForge.UI.Pages.SettingsPage` | `MainWindow._settingsPage`; `NavBarControler.OptionsItemClick`; options nav item | Deferred - owner map required | Inventory only for future MVVM/Nexus UI review. | High: settings bindings, dialogs, path validation, tool actions, options navigation, resource styles, and generated partial class references. | No |

## Phase 1 Inventory Notes

- This table is an inventory checkpoint only.
- No proposed rename targets are approved or implied.
- `MainWindow` uses index-based navigation for the main pages and a separate options-item click path for Settings.
- Resource dictionaries tied to current page naming include `ModsPageStyles.xaml`, `ModpacksPageStyles.xaml`, and `SettingsPageStyles.xaml`.
- Any future rename review must include XAML `x:Class`, code-behind partial classes, generated files, navigation indices, options navigation, bindings, resource dictionary references, screenshots, and documentation references.

## Review Areas

- `x:Class` names.
- Code-behind partial class names.
- Navigation registration and page construction.
- Resource dictionaries and styles.
- Bindings and commands.
- Tests or smoke-test references.
- Documentation and screenshots.
- Future Nexus UI naming conflicts.

## Approval Rule

No rename may proceed until the owner fills or approves the rename map.
