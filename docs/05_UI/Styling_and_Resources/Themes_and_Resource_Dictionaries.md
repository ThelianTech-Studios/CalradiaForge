# Themes and Resource Dictionaries

The UI includes shared theme/text/title-bar resources and page-specific
dictionaries for Launcher, Modpacks, Settings, navigation, dialogs, and toasts.
`MainWindow` supplies the shared presentation host. Style changes must preserve
the page/ViewModel boundary and should be validated through the UI-focused test
and manual smoke evidence paths.
