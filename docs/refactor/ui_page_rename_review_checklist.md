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
