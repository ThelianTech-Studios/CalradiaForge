namespace CalradiaForge.Core.Infra.Localization {
	using System.Collections.Generic;
	using System.ComponentModel;

	/// <summary>
	/// Central strongly-typed translation string provider for all UI text in CalradiaForge.
	/// Each property has a hardcoded English default value and a private setter that is
	/// only updated when a language dictionary is applied via <see cref="Apply"/>.
	/// Implements <see cref="INotifyPropertyChanged"/> — a single blanket
	/// <c>PropertyChanged(null)</c> event fires after all properties are updated,
	/// causing WPF to re-read every bound property in one pass.
	/// </summary>
	public sealed class TranslationStrings : INotifyPropertyChanged {
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Raises <see cref="PropertyChanged"/> with a <c>null</c> property name,
		/// signaling WPF that all properties may have changed.
		/// </summary>
		private void NotifyAllPropertiesChanged() {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
		}

		/// <summary>
		/// Applies the given translation dictionary to all properties.
		/// Keys not present in the dictionary retain their hardcoded English defaults.
		/// Fires a single blanket <see cref="PropertyChanged"/> event when complete.
		/// </summary>
		public void Apply(Dictionary<string, string> translations) {
			if (translations is null || translations.Count == 0) {
				return;
			}

			// Navigation
			Nav_ModsTab = GetOrDefault(translations, nameof(Nav_ModsTab), Nav_ModsTab);
			Nav_ModpacksTab = GetOrDefault(translations, nameof(Nav_ModpacksTab), Nav_ModpacksTab);
			Nav_FaqTab = GetOrDefault(translations, nameof(Nav_FaqTab), Nav_FaqTab);
			Nav_SettingsTab = GetOrDefault(translations, nameof(Nav_SettingsTab), Nav_SettingsTab);

			// ModsPage — Header
			Mods_ModpacksLabel = GetOrDefault(translations, nameof(Mods_ModpacksLabel), Mods_ModpacksLabel);
			Mods_InstallButton = GetOrDefault(translations, nameof(Mods_InstallButton), Mods_InstallButton);
			Mods_LoadOrderHeader = GetOrDefault(translations, nameof(Mods_LoadOrderHeader), Mods_LoadOrderHeader);
			Mods_AvailableModsHeader = GetOrDefault(translations, nameof(Mods_AvailableModsHeader), Mods_AvailableModsHeader);
			Mods_RefreshTooltip = GetOrDefault(translations, nameof(Mods_RefreshTooltip), Mods_RefreshTooltip);

			// ModsPage — Play Button
			Mods_PlayBannerlord = GetOrDefault(translations, nameof(Mods_PlayBannerlord), Mods_PlayBannerlord);
			Mods_PlayWithBLSE = GetOrDefault(translations, nameof(Mods_PlayWithBLSE), Mods_PlayWithBLSE);
			Mods_LaunchTargetTooltip = GetOrDefault(translations, nameof(Mods_LaunchTargetTooltip), Mods_LaunchTargetTooltip);
			Mods_LaunchTargetBannerlord = GetOrDefault(translations, nameof(Mods_LaunchTargetBannerlord), Mods_LaunchTargetBannerlord);
			Mods_LaunchTargetBLSE = GetOrDefault(translations, nameof(Mods_LaunchTargetBLSE), Mods_LaunchTargetBLSE);

			// ModsPage — Status Messages
			Mods_SelectModpackPrompt = GetOrDefault(translations, nameof(Mods_SelectModpackPrompt), Mods_SelectModpackPrompt);
			Mods_InstallInProgress = GetOrDefault(translations, nameof(Mods_InstallInProgress), Mods_InstallInProgress);
			Mods_Launching = GetOrDefault(translations, nameof(Mods_Launching), Mods_Launching);
			Mods_ScanningForMods = GetOrDefault(translations, nameof(Mods_ScanningForMods), Mods_ScanningForMods);
			Mods_NoModsFound = GetOrDefault(translations, nameof(Mods_NoModsFound), Mods_NoModsFound);
			Mods_InstallDialogTitle = GetOrDefault(translations, nameof(Mods_InstallDialogTitle), Mods_InstallDialogTitle);

			// ModpacksPage — Header
			Modpacks_HeaderLabel = GetOrDefault(translations, nameof(Modpacks_HeaderLabel), Modpacks_HeaderLabel);
			Modpacks_CreateNewButton = GetOrDefault(translations, nameof(Modpacks_CreateNewButton), Modpacks_CreateNewButton);
			Modpacks_ImportButton = GetOrDefault(translations, nameof(Modpacks_ImportButton), Modpacks_ImportButton);
			Modpacks_SaveButton = GetOrDefault(translations, nameof(Modpacks_SaveButton), Modpacks_SaveButton);

			// ModpacksPage — Lists
			Modpacks_ActiveLoadOrderHeader = GetOrDefault(translations, nameof(Modpacks_ActiveLoadOrderHeader), Modpacks_ActiveLoadOrderHeader);
			Modpacks_SavedDataHeader = GetOrDefault(translations, nameof(Modpacks_SavedDataHeader), Modpacks_SavedDataHeader);
			Modpacks_ByLabel = GetOrDefault(translations, nameof(Modpacks_ByLabel), Modpacks_ByLabel);
			Modpacks_UpdatedLabel = GetOrDefault(translations, nameof(Modpacks_UpdatedLabel), Modpacks_UpdatedLabel);

			// ModpacksPage — Edit Panel
			Modpacks_ModuleIdLabel = GetOrDefault(translations, nameof(Modpacks_ModuleIdLabel), Modpacks_ModuleIdLabel);
			Modpacks_VersionLabel = GetOrDefault(translations, nameof(Modpacks_VersionLabel), Modpacks_VersionLabel);
			Modpacks_NameLabel = GetOrDefault(translations, nameof(Modpacks_NameLabel), Modpacks_NameLabel);
			Modpacks_UrlLabel = GetOrDefault(translations, nameof(Modpacks_UrlLabel), Modpacks_UrlLabel);
			Modpacks_SaveEntryButton = GetOrDefault(translations, nameof(Modpacks_SaveEntryButton), Modpacks_SaveEntryButton);

			// ModpacksPage — Create Panel
			Modpacks_CreateNameLabel = GetOrDefault(translations, nameof(Modpacks_CreateNameLabel), Modpacks_CreateNameLabel);
			Modpacks_CreatedByLabel = GetOrDefault(translations, nameof(Modpacks_CreatedByLabel), Modpacks_CreatedByLabel);
			Modpacks_ConfirmButton = GetOrDefault(translations, nameof(Modpacks_ConfirmButton), Modpacks_ConfirmButton);
			Modpacks_CancelButton = GetOrDefault(translations, nameof(Modpacks_CancelButton), Modpacks_CancelButton);

			// ModpacksPage — Template Names
			Modpacks_TemplateVanilla = GetOrDefault(translations, nameof(Modpacks_TemplateVanilla), Modpacks_TemplateVanilla);
			Modpacks_TemplateButterLib = GetOrDefault(translations, nameof(Modpacks_TemplateButterLib), Modpacks_TemplateButterLib);
			Modpacks_TemplateVanillaWarSails = GetOrDefault(translations, nameof(Modpacks_TemplateVanillaWarSails), Modpacks_TemplateVanillaWarSails);
			Modpacks_TemplateButterLibWarSails = GetOrDefault(translations, nameof(Modpacks_TemplateButterLibWarSails), Modpacks_TemplateButterLibWarSails);

			// Settings — Nav Tabs
			Settings_GeneralTab = GetOrDefault(translations, nameof(Settings_GeneralTab), Settings_GeneralTab);
			Settings_GameConfigTab = GetOrDefault(translations, nameof(Settings_GameConfigTab), Settings_GameConfigTab);
			Settings_ToolsTab = GetOrDefault(translations, nameof(Settings_ToolsTab), Settings_ToolsTab);
			Settings_WipTab = GetOrDefault(translations, nameof(Settings_WipTab), Settings_WipTab);
			Settings_AboutTab = GetOrDefault(translations, nameof(Settings_AboutTab), Settings_AboutTab);

			// Settings — General
			Settings_LanguageHeader = GetOrDefault(translations, nameof(Settings_LanguageHeader), Settings_LanguageHeader);
			Settings_LanguageLabel = GetOrDefault(translations, nameof(Settings_LanguageLabel), Settings_LanguageLabel);
			Settings_LanguageHint = GetOrDefault(translations, nameof(Settings_LanguageHint), Settings_LanguageHint);
			Settings_StartupHeader = GetOrDefault(translations, nameof(Settings_StartupHeader), Settings_StartupHeader);
			Settings_StartupLabel = GetOrDefault(translations, nameof(Settings_StartupLabel), Settings_StartupLabel);
			Settings_StartupLastUsed = GetOrDefault(translations, nameof(Settings_StartupLastUsed), Settings_StartupLastUsed);
			Settings_StartupAlwaysDefault = GetOrDefault(translations, nameof(Settings_StartupAlwaysDefault), Settings_StartupAlwaysDefault);
			Settings_StartupAlwaysAsk = GetOrDefault(translations, nameof(Settings_StartupAlwaysAsk), Settings_StartupAlwaysAsk);
			Settings_StartupHint = GetOrDefault(translations, nameof(Settings_StartupHint), Settings_StartupHint);
			Settings_DiagnosticsHeader = GetOrDefault(translations, nameof(Settings_DiagnosticsHeader), Settings_DiagnosticsHeader);
			Settings_DebugModeLabel = GetOrDefault(translations, nameof(Settings_DebugModeLabel), Settings_DebugModeLabel);
			Settings_DebugModeHint = GetOrDefault(translations, nameof(Settings_DebugModeHint), Settings_DebugModeHint);

			// Settings — Game Config
			Settings_GameInstallHeader = GetOrDefault(translations, nameof(Settings_GameInstallHeader), Settings_GameInstallHeader);
			Settings_GameFolderLabel = GetOrDefault(translations, nameof(Settings_GameFolderLabel), Settings_GameFolderLabel);
			Settings_SelectFolderButton = GetOrDefault(translations, nameof(Settings_SelectFolderButton), Settings_SelectFolderButton);
			Settings_GameExeLabel = GetOrDefault(translations, nameof(Settings_GameExeLabel), Settings_GameExeLabel);
			Settings_SelectFileButton = GetOrDefault(translations, nameof(Settings_SelectFileButton), Settings_SelectFileButton);
			Settings_WorkshopFolderLabel = GetOrDefault(translations, nameof(Settings_WorkshopFolderLabel), Settings_WorkshopFolderLabel);
			Settings_BLSEHeader = GetOrDefault(translations, nameof(Settings_BLSEHeader), Settings_BLSEHeader);
			Settings_BLSEExeLabel = GetOrDefault(translations, nameof(Settings_BLSEExeLabel), Settings_BLSEExeLabel);
			Settings_BLSEHint = GetOrDefault(translations, nameof(Settings_BLSEHint), Settings_BLSEHint);
			Settings_DetectionHeader = GetOrDefault(translations, nameof(Settings_DetectionHeader), Settings_DetectionHeader);
			Settings_DetectGameButton = GetOrDefault(translations, nameof(Settings_DetectGameButton), Settings_DetectGameButton);
			Settings_DetectGameHint = GetOrDefault(translations, nameof(Settings_DetectGameHint), Settings_DetectGameHint);

			// Settings — Tools
			Settings_ModMaintenanceHeader = GetOrDefault(translations, nameof(Settings_ModMaintenanceHeader), Settings_ModMaintenanceHeader);
			Settings_UnblockDllsButton = GetOrDefault(translations, nameof(Settings_UnblockDllsButton), Settings_UnblockDllsButton);
			Settings_UnblockDllsHint = GetOrDefault(translations, nameof(Settings_UnblockDllsHint), Settings_UnblockDllsHint);
			Settings_DataManagementHeader = GetOrDefault(translations, nameof(Settings_DataManagementHeader), Settings_DataManagementHeader);
			Settings_ClearCacheButton = GetOrDefault(translations, nameof(Settings_ClearCacheButton), Settings_ClearCacheButton);
			Settings_ClearCacheHint = GetOrDefault(translations, nameof(Settings_ClearCacheHint), Settings_ClearCacheHint);
			Settings_OpenConfigButton = GetOrDefault(translations, nameof(Settings_OpenConfigButton), Settings_OpenConfigButton);
			Settings_OpenConfigHint = GetOrDefault(translations, nameof(Settings_OpenConfigHint), Settings_OpenConfigHint);
			Settings_OpenLogsButton = GetOrDefault(translations, nameof(Settings_OpenLogsButton), Settings_OpenLogsButton);
			Settings_OpenLogsHint = GetOrDefault(translations, nameof(Settings_OpenLogsHint), Settings_OpenLogsHint);
			Settings_OpenModpacksButton = GetOrDefault(translations, nameof(Settings_OpenModpacksButton), Settings_OpenModpacksButton);
			Settings_OpenModpacksHint = GetOrDefault(translations, nameof(Settings_OpenModpacksHint), Settings_OpenModpacksHint);

			// Settings — WIP
			Settings_WipTitle = GetOrDefault(translations, nameof(Settings_WipTitle), Settings_WipTitle);
			Settings_WipDescription = GetOrDefault(translations, nameof(Settings_WipDescription), Settings_WipDescription);
			Settings_WipStayTuned = GetOrDefault(translations, nameof(Settings_WipStayTuned), Settings_WipStayTuned);

			// Settings — About
			Settings_AboutDescription = GetOrDefault(translations, nameof(Settings_AboutDescription), Settings_AboutDescription);
			Settings_AboutTagline = GetOrDefault(translations, nameof(Settings_AboutTagline), Settings_AboutTagline);
			Settings_AboutPublisherLabel = GetOrDefault(translations, nameof(Settings_AboutPublisherLabel), Settings_AboutPublisherLabel);
			Settings_AboutLicenseLabel = GetOrDefault(translations, nameof(Settings_AboutLicenseLabel), Settings_AboutLicenseLabel);
			Settings_AboutViewLicense = GetOrDefault(translations, nameof(Settings_AboutViewLicense), Settings_AboutViewLicense);
			Settings_AboutGitHubLabel = GetOrDefault(translations, nameof(Settings_AboutGitHubLabel), Settings_AboutGitHubLabel);
			Settings_AboutViewGitHub = GetOrDefault(translations, nameof(Settings_AboutViewGitHub), Settings_AboutViewGitHub);
			Settings_AboutCopyright = GetOrDefault(translations, nameof(Settings_AboutCopyright), Settings_AboutCopyright);
			Settings_AboutDisclaimer = GetOrDefault(translations, nameof(Settings_AboutDisclaimer), Settings_AboutDisclaimer);

			// FAQ Page
			Faq_PageTitle = GetOrDefault(translations, nameof(Faq_PageTitle), Faq_PageTitle);
			Faq_Q1_Title = GetOrDefault(translations, nameof(Faq_Q1_Title), Faq_Q1_Title);
			Faq_Q1_Answer1 = GetOrDefault(translations, nameof(Faq_Q1_Answer1), Faq_Q1_Answer1);
			Faq_Q1_Answer2 = GetOrDefault(translations, nameof(Faq_Q1_Answer2), Faq_Q1_Answer2);
			Faq_Q1_Hint = GetOrDefault(translations, nameof(Faq_Q1_Hint), Faq_Q1_Hint);
			Faq_Q2_Title = GetOrDefault(translations, nameof(Faq_Q2_Title), Faq_Q2_Title);
			Faq_Q2_Answer1 = GetOrDefault(translations, nameof(Faq_Q2_Answer1), Faq_Q2_Answer1);
			Faq_Q2_Answer2 = GetOrDefault(translations, nameof(Faq_Q2_Answer2), Faq_Q2_Answer2);
			Faq_Q3_Title = GetOrDefault(translations, nameof(Faq_Q3_Title), Faq_Q3_Title);
			Faq_Q3_Answer1 = GetOrDefault(translations, nameof(Faq_Q3_Answer1), Faq_Q3_Answer1);
			Faq_Q3_Answer2 = GetOrDefault(translations, nameof(Faq_Q3_Answer2), Faq_Q3_Answer2);
			Faq_Q3_Hint = GetOrDefault(translations, nameof(Faq_Q3_Hint), Faq_Q3_Hint);
			Faq_Q4_Title = GetOrDefault(translations, nameof(Faq_Q4_Title), Faq_Q4_Title);
			Faq_Q4_Answer1 = GetOrDefault(translations, nameof(Faq_Q4_Answer1), Faq_Q4_Answer1);
			Faq_Q4_Answer2 = GetOrDefault(translations, nameof(Faq_Q4_Answer2), Faq_Q4_Answer2);
			Faq_Q4_Answer3 = GetOrDefault(translations, nameof(Faq_Q4_Answer3), Faq_Q4_Answer3);
			Faq_Q4_Hint = GetOrDefault(translations, nameof(Faq_Q4_Hint), Faq_Q4_Hint);
			Faq_Q5_Title = GetOrDefault(translations, nameof(Faq_Q5_Title), Faq_Q5_Title);
			Faq_Q5_Answer1 = GetOrDefault(translations, nameof(Faq_Q5_Answer1), Faq_Q5_Answer1);
			Faq_Q5_Answer2 = GetOrDefault(translations, nameof(Faq_Q5_Answer2), Faq_Q5_Answer2);
			Faq_Q5_Hint = GetOrDefault(translations, nameof(Faq_Q5_Hint), Faq_Q5_Hint);
			Faq_Q6_Title = GetOrDefault(translations, nameof(Faq_Q6_Title), Faq_Q6_Title);
			Faq_Q6_Answer1 = GetOrDefault(translations, nameof(Faq_Q6_Answer1), Faq_Q6_Answer1);
			Faq_Q6_Answer2 = GetOrDefault(translations, nameof(Faq_Q6_Answer2), Faq_Q6_Answer2);
			Faq_Q6_Answer3 = GetOrDefault(translations, nameof(Faq_Q6_Answer3), Faq_Q6_Answer3);
			Faq_Q6_Hint = GetOrDefault(translations, nameof(Faq_Q6_Hint), Faq_Q6_Hint);
			Faq_Q7_Title = GetOrDefault(translations, nameof(Faq_Q7_Title), Faq_Q7_Title);
			Faq_Q7_Answer1 = GetOrDefault(translations, nameof(Faq_Q7_Answer1), Faq_Q7_Answer1);
			Faq_Q7_Answer2 = GetOrDefault(translations, nameof(Faq_Q7_Answer2), Faq_Q7_Answer2);
			Faq_Q8_Title = GetOrDefault(translations, nameof(Faq_Q8_Title), Faq_Q8_Title);
			Faq_Q8_Answer1 = GetOrDefault(translations, nameof(Faq_Q8_Answer1), Faq_Q8_Answer1);
			Faq_Q8_OpenIssuesButton = GetOrDefault(translations, nameof(Faq_Q8_OpenIssuesButton), Faq_Q8_OpenIssuesButton);
			Faq_Q8_OpenIssuesHint = GetOrDefault(translations, nameof(Faq_Q8_OpenIssuesHint), Faq_Q8_OpenIssuesHint);
			Faq_Q8_Hint = GetOrDefault(translations, nameof(Faq_Q8_Hint), Faq_Q8_Hint);
			Faq_Q9_Title = GetOrDefault(translations, nameof(Faq_Q9_Title), Faq_Q9_Title);
			Faq_Q9_Answer1 = GetOrDefault(translations, nameof(Faq_Q9_Answer1), Faq_Q9_Answer1);
			Faq_Q9_Answer2 = GetOrDefault(translations, nameof(Faq_Q9_Answer2), Faq_Q9_Answer2);
			Faq_Q9_Answer3 = GetOrDefault(translations, nameof(Faq_Q9_Answer3), Faq_Q9_Answer3);
			Faq_Q9_Hint = GetOrDefault(translations, nameof(Faq_Q9_Hint), Faq_Q9_Hint);

			// Toast Messages
			Toast_InstallInProgress = GetOrDefault(translations, nameof(Toast_InstallInProgress), Toast_InstallInProgress);
			Toast_InstallComplete = GetOrDefault(translations, nameof(Toast_InstallComplete), Toast_InstallComplete);
			Toast_InstallCompleteWithErrors = GetOrDefault(translations, nameof(Toast_InstallCompleteWithErrors), Toast_InstallCompleteWithErrors);
			Toast_InstallingMods = GetOrDefault(translations, nameof(Toast_InstallingMods), Toast_InstallingMods);
			Toast_GameLaunched = GetOrDefault(translations, nameof(Toast_GameLaunched), Toast_GameLaunched);
			Toast_LaunchFailed = GetOrDefault(translations, nameof(Toast_LaunchFailed), Toast_LaunchFailed);
			Toast_MissingMods = GetOrDefault(translations, nameof(Toast_MissingMods), Toast_MissingMods);
			Toast_ModListUpdated = GetOrDefault(translations, nameof(Toast_ModListUpdated), Toast_ModListUpdated);
			Toast_RefreshFailed = GetOrDefault(translations, nameof(Toast_RefreshFailed), Toast_RefreshFailed);
			Toast_NoModsFound = GetOrDefault(translations, nameof(Toast_NoModsFound), Toast_NoModsFound);
			Toast_AutoScanFailed = GetOrDefault(translations, nameof(Toast_AutoScanFailed), Toast_AutoScanFailed);

			// Common / Shared
			Common_AlmostDone = GetOrDefault(translations, nameof(Common_AlmostDone), Common_AlmostDone);
			Common_Remaining = GetOrDefault(translations, nameof(Common_Remaining), Common_Remaining);
			Common_Extracting = GetOrDefault(translations, nameof(Common_Extracting), Common_Extracting);
			Common_FilesExtracted = GetOrDefault(translations, nameof(Common_FilesExtracted), Common_FilesExtracted);

			NotifyAllPropertiesChanged();
		}

		/// <summary>
		/// Returns the dictionary value if the key exists, otherwise returns the fallback.
		/// </summary>
		private static string GetOrDefault(Dictionary<string, string> translations, string key, string fallback) {
			return translations.TryGetValue(key, out string? value) && !string.IsNullOrEmpty(value)
				? value
				: fallback;
		}

		#region Navigation
		public string Nav_ModsTab { get; private set; } = "Mods";
		public string Nav_ModpacksTab { get; private set; } = "Mod Packs";
		public string Nav_FaqTab { get; private set; } = "F.A.Q.";
		public string Nav_SettingsTab { get; private set; } = "Settings";
		#endregion

		#region ModsPage — Header
		public string Mods_ModpacksLabel { get; private set; } = "ModPacks:";
		public string Mods_InstallButton { get; private set; } = "Install Mods";
		public string Mods_LoadOrderHeader { get; private set; } = "Load Order";
		public string Mods_AvailableModsHeader { get; private set; } = "Available Mods";
		public string Mods_RefreshTooltip { get; private set; } = "Refresh Mods";
		#endregion

		#region ModsPage — Play Button
		public string Mods_PlayBannerlord { get; private set; } = "Play Bannerlord";
		public string Mods_PlayWithBLSE { get; private set; } = "Play with BLSE";
		public string Mods_LaunchTargetTooltip { get; private set; } = "Choose launch target";
		public string Mods_LaunchTargetBannerlord { get; private set; } = "Bannerlord";
		public string Mods_LaunchTargetBLSE { get; private set; } = "BLSE";
		#endregion

		#region ModsPage — Status Messages
		public string Mods_SelectModpackPrompt { get; private set; } = "No modpack selected. Choose one from the dropdown above.";
		public string Mods_InstallInProgress { get; private set; } = "Installation in progress...";
		public string Mods_Launching { get; private set; } = "Launching...";
		public string Mods_ScanningForMods { get; private set; } = "Scanning for mods...";
		public string Mods_NoModsFound { get; private set; } = "No mods found. Install mods or check your game path in Settings.";
		public string Mods_InstallDialogTitle { get; private set; } = "Select Mod Archives to Install";
		#endregion

		#region ModpacksPage — Header
		public string Modpacks_HeaderLabel { get; private set; } = "Mod Packs:";
		public string Modpacks_CreateNewButton { get; private set; } = "Create New";
		public string Modpacks_ImportButton { get; private set; } = "Import";
		public string Modpacks_SaveButton { get; private set; } = "Save to Modpack";
		#endregion

		#region ModpacksPage — Lists
		public string Modpacks_ActiveLoadOrderHeader { get; private set; } = "Active Load Order";
		public string Modpacks_SavedDataHeader { get; private set; } = "Saved Modpack Data";
		public string Modpacks_ByLabel { get; private set; } = "By:";
		public string Modpacks_UpdatedLabel { get; private set; } = "Updated:";
		#endregion

		#region ModpacksPage — Edit Panel
		public string Modpacks_ModuleIdLabel { get; private set; } = "Module ID:";
		public string Modpacks_VersionLabel { get; private set; } = "Version:";
		public string Modpacks_NameLabel { get; private set; } = "Name:";
		public string Modpacks_UrlLabel { get; private set; } = "URL:";
		public string Modpacks_SaveEntryButton { get; private set; } = "Save Entry";
		#endregion

		#region ModpacksPage — Create Panel
		public string Modpacks_CreateNameLabel { get; private set; } = "Name:";
		public string Modpacks_CreatedByLabel { get; private set; } = "Created By:";
		public string Modpacks_ConfirmButton { get; private set; } = "Confirm";
		public string Modpacks_CancelButton { get; private set; } = "Cancel";
		#endregion

		#region ModpacksPage — Template Names
		public string Modpacks_TemplateVanilla { get; private set; } = "Default Modpack";
		public string Modpacks_TemplateButterLib { get; private set; } = "ButterLib Suite";
		public string Modpacks_TemplateVanillaWarSails { get; private set; } = "Vanilla - WarSails";
		public string Modpacks_TemplateButterLibWarSails { get; private set; } = "ButterLib - WarSails";
		#endregion

		#region Settings — Nav Tabs
		public string Settings_GeneralTab { get; private set; } = "General";
		public string Settings_GameConfigTab { get; private set; } = "Game Config";
		public string Settings_ToolsTab { get; private set; } = "Tools";
		public string Settings_WipTab { get; private set; } = "WIP";
		public string Settings_AboutTab { get; private set; } = "About";
		#endregion

		#region Settings — General
		public string Settings_LanguageHeader { get; private set; } = "Language";
		public string Settings_LanguageLabel { get; private set; } = "Language";
		public string Settings_LanguageHint { get; private set; } = "Select the display language for CalradiaForge.";
		public string Settings_StartupHeader { get; private set; } = "Startup Behavior";
		public string Settings_StartupLabel { get; private set; } = "On startup, select modpack:";
		public string Settings_StartupLastUsed { get; private set; } = "Last Used Modpack";
		public string Settings_StartupAlwaysDefault { get; private set; } = "Always Use Default (Vanilla)";
		public string Settings_StartupAlwaysAsk { get; private set; } = "Always Ask";
		public string Settings_StartupHint { get; private set; } = "Controls which modpack is automatically selected when CalradiaForge starts.";
		public string Settings_DiagnosticsHeader { get; private set; } = "Diagnostics";
		public string Settings_DebugModeLabel { get; private set; } = "Enable Debug Mode";
		public string Settings_DebugModeHint { get; private set; } = "Enables verbose logging and diagnostic data output. Only enable this when requested for troubleshooting.";
		#endregion

		#region Settings — Game Config
		public string Settings_GameInstallHeader { get; private set; } = "Game Installation";
		public string Settings_GameFolderLabel { get; private set; } = "Game Folder";
		public string Settings_SelectFolderButton { get; private set; } = "Select Folder";
		public string Settings_GameExeLabel { get; private set; } = "Game Executable";
		public string Settings_SelectFileButton { get; private set; } = "Select File";
		public string Settings_WorkshopFolderLabel { get; private set; } = "Steam Workshop Folder";
		public string Settings_BLSEHeader { get; private set; } = "Script Extender (BLSE)";
		public string Settings_BLSEExeLabel { get; private set; } = "BLSE Standalone Executable";
		public string Settings_BLSEHint { get; private set; } = "The Bannerlord Software Extender enables advanced script mods. If installed, CalradiaForge auto-detects it in the game's bin folder. You can also select it manually.";
		public string Settings_DetectionHeader { get; private set; } = "Detection";
		public string Settings_DetectGameButton { get; private set; } = "\U0001f504  Detect Game";
		public string Settings_DetectGameHint { get; private set; } = "Re-runs auto-detection for Steam, Epic, and StandAlone installations. Also detects BLSE if installed. Overwrites current paths.";
		#endregion

		#region Settings — Tools
		public string Settings_ModMaintenanceHeader { get; private set; } = "Mod Maintenance";
		public string Settings_UnblockDllsButton { get; private set; } = "\U0001f513  Unblock DLLs";
		public string Settings_UnblockDllsHint { get; private set; } = "Removes the Zone.Identifier alternate data stream from all DLL files in the Modules folder. Required for mods downloaded from the internet.";
		public string Settings_DataManagementHeader { get; private set; } = "Data Management";
		public string Settings_ClearCacheButton { get; private set; } = "\U0001f5d1  Clear Mod Cache";
		public string Settings_ClearCacheHint { get; private set; } = "Deletes the cached mod list and forces a fresh rescan on next launch.";
		public string Settings_OpenConfigButton { get; private set; } = "\U0001f4c2  Open Config Folder";
		public string Settings_OpenConfigHint { get; private set; } = "Opens the CalradiaForge Config directory in Windows Explorer.";
		public string Settings_OpenLogsButton { get; private set; } = "\U0001f4c2  Open Logs Folder";
		public string Settings_OpenLogsHint { get; private set; } = "Opens the Logs directory in Windows Explorer.";
		public string Settings_OpenModpacksButton { get; private set; } = "\U0001f4c2  Open Modpacks Folder";
		public string Settings_OpenModpacksHint { get; private set; } = "Opens the Modpacks directory in Windows Explorer.";
		#endregion

		#region Settings — WIP
		public string Settings_WipTitle { get; private set; } = "This tab is reserved for future development.";
		public string Settings_WipDescription { get; private set; } = "If there is enough community interest in advanced features\nand functionality beyond the current project scope,\nfuture settings will appear here.";
		public string Settings_WipStayTuned { get; private set; } = "Stay tuned for updates!";
		#endregion

		#region Settings — About
		public string Settings_AboutDescription { get; private set; } = "A modern mod launcher for Mount & Blade II: Bannerlord.";
		public string Settings_AboutTagline { get; private set; } = "Built by players, for players.";
		public string Settings_AboutPublisherLabel { get; private set; } = "Publisher";
		public string Settings_AboutLicenseLabel { get; private set; } = "License";
		public string Settings_AboutViewLicense { get; private set; } = "View License";
		public string Settings_AboutGitHubLabel { get; private set; } = "GitHub";
		public string Settings_AboutViewGitHub { get; private set; } = "View on GitHub";
		public string Settings_AboutCopyright { get; private set; } = "\u00a9 2026 ThelianTech\u2122 \u2014 All rights reserved.";
		public string Settings_AboutDisclaimer { get; private set; } = "Not affiliated with TaleWorlds Entertainment.";
		#endregion

		#region FAQ Page
		public string Faq_PageTitle { get; private set; } = "Frequently Asked Questions";
		public string Faq_Q1_Title { get; private set; } = "Why isn't the game detecting my mods?";
		public string Faq_Q1_Answer1 { get; private set; } = "Make sure your mods are in the Active load order on the Mods page. Only mods listed in the Load Order panel are passed to the game at launch.";
		public string Faq_Q1_Answer2 { get; private set; } = "Verify that your game installation path is set correctly under Settings \u2192 Game Config. Use the Re-detect button if you're unsure.";
		public string Faq_Q1_Hint { get; private set; } = "Tip: If mods were downloaded from the internet, Windows may block their DLLs. Go to Settings \u2192 Tools \u2192 Unblock DLLs to fix this.";
		public string Faq_Q2_Title { get; private set; } = "What does 'Unblock DLLs' do?";
		public string Faq_Q2_Answer1 { get; private set; } = "When you download files from the internet, Windows adds a hidden security marker (Zone.Identifier) to them. This can prevent Bannerlord from loading mod DLLs.";
		public string Faq_Q2_Answer2 { get; private set; } = "The Unblock DLLs tool in Settings \u2192 Tools removes this marker from all DLL files in your Modules folder. CalradiaForge also does this automatically when you install mods through the app.";
		public string Faq_Q3_Title { get; private set; } = "I'm getting errors when installing mods from an archive.";
		public string Faq_Q3_Answer1 { get; private set; } = "CalradiaForge extracts mod archives directly into the Bannerlord Modules folder. Make sure the archive contains a valid mod structure with a SubModule.xml file.";
		public string Faq_Q3_Answer2 { get; private set; } = "If the archive is nested (a folder inside a folder), CalradiaForge will attempt to detect the correct root. If extraction still fails, try extracting the mod manually.";
		public string Faq_Q3_Hint { get; private set; } = "Supported archive formats: .zip, .rar \u2014 .7z is temporarily disabled (see below).";
		public string Faq_Q4_Title { get; private set; } = "How do modpacks work?";
		public string Faq_Q4_Answer1 { get; private set; } = "A modpack is a saved snapshot of your active mods and their load order. You can create multiple modpacks for different playstyles (e.g., Vanilla+, Overhaul, Hardcore).";
		public string Faq_Q4_Answer2 { get; private set; } = "Switch between modpacks using the dropdown on the Mods page. The selected modpack's load order is applied immediately.";
		public string Faq_Q4_Answer3 { get; private set; } = "To save changes to a modpack, go to the Mod Packs page and click 'Save to Modpack'. This overwrites the saved data with your current active load order.";
		public string Faq_Q4_Hint { get; private set; } = "Make sure you have selected the correct modpack on the Mods Page before you add/remove mods to your load order there, before you go and save any changes in the Modpacks Page else any changes will be lost.";
		public string Faq_Q5_Title { get; private set; } = "Why can't I launch the game from CalradiaForge on Epic or GamePass?";
		public string Faq_Q5_Answer1 { get; private set; } = "TaleWorlds requires authentication through the Epic or Xbox client. This is a platform-level restriction that third-party launchers cannot bypass.";
		public string Faq_Q5_Answer2 { get; private set; } = "CalradiaForge still provides full mod management, load order arrangement, and modpack features for these platforms \u2014 you just need to press Play from the platform's own launcher.";
		public string Faq_Q5_Hint { get; private set; } = "Direct launch support for Epic and GamePass is planned for a future release. For now load orders are not passed to the game's native launcher.";
		public string Faq_Q6_Title { get; private set; } = "Can I import presets from Novus Launcher?";
		public string Faq_Q6_Answer1 { get; private set; } = "Yes. Go to the Mod Packs page and click Import. Select a Novus Launcher preset file (.xml) and CalradiaForge will convert it into a CalradiaForge modpack automatically.";
		public string Faq_Q6_Answer2 { get; private set; } = "You can also import native CalradiaForge modpack files (.json) the same way.";
		public string Faq_Q6_Answer3 { get; private set; } = "You can also place CalradiaForge Modpacks directly in the modpacks subdirectory in the app's directory, and the app will automatically refresh the selectable modpacks.";
		public string Faq_Q6_Hint { get; private set; } = "Refresh or switch pages for the app to automatically refresh the dropdown menus'.";
		public string Faq_Q7_Title { get; private set; } = "When should I clear the mod cache?";
		public string Faq_Q7_Answer1 { get; private set; } = "Clear the mod cache if you've manually added or removed mods from the Modules folder outside of CalradiaForge, or if the mod list appears stale or incorrect.";
		public string Faq_Q7_Answer2 { get; private set; } = "Go to Settings \u2192 Tools \u2192 Clear Mod Cache. On next launch, CalradiaForge will rescan the Modules folder and rebuild the cache.";
		public string Faq_Q8_Title { get; private set; } = "How do I report a bug or request a feature?";
		public string Faq_Q8_Answer1 { get; private set; } = "Open an issue on the CalradiaForge GitHub repository. Include a short description of the problem, steps to reproduce it, and any relevant log output.";
		public string Faq_Q8_OpenIssuesButton { get; private set; } = "Open GitHub Issues";
		public string Faq_Q8_OpenIssuesHint { get; private set; } = "Opens the CalradiaForge Issues page on GitHub in your browser.";
		public string Faq_Q8_Hint { get; private set; } = "Tip: Enable Debug Mode in Settings \u2192 General before reproducing the issue. This gives more detailed logs for troubleshooting.";
		public string Faq_Q9_Title { get; private set; } = "Why are .7z archives not supported?";
		public string Faq_Q9_Answer1 { get; private set; } = "The .7z format uses block compression (LZMA/LZMA2), which the current extraction library (SharpCompress) must decompress sequentially and entirely in-memory. For large Bannerlord mods with 1,000+ files, this causes extraction times of approximately 25 minutes for a single mod \u2014 making the install experience unacceptable.";
		public string Faq_Q9_Answer2 { get; private set; } = "The .zip and .rar formats use per-file compression, allowing the library to extract each file individually without this bottleneck. These formats install in seconds, even for large mods.";
		public string Faq_Q9_Answer3 { get; private set; } = "Support for .7z archives has been temporarily disabled while a replacement extraction method is developed. A future update will integrate native 7-Zip extraction to handle .7z archives at full speed.";
		public string Faq_Q9_Hint { get; private set; } = "Workaround: If your mod is only available as a .7z file, extract it manually using 7-Zip (7-zip.org) and re-archive it as a .zip before installing through CalradiaForge.";
		#endregion

		#region Toast Messages
		public string Toast_InstallInProgress { get; private set; } = "Install in Progress";
		public string Toast_InstallComplete { get; private set; } = "Install Complete";
		public string Toast_InstallCompleteWithErrors { get; private set; } = "Install Completed with Errors";
		public string Toast_InstallingMods { get; private set; } = "Installing Mods";
		public string Toast_GameLaunched { get; private set; } = "Game Launched";
		public string Toast_LaunchFailed { get; private set; } = "Launch Failed";
		public string Toast_MissingMods { get; private set; } = "Missing Mod(s)";
		public string Toast_ModListUpdated { get; private set; } = "Mod List Updated";
		public string Toast_RefreshFailed { get; private set; } = "Refresh Failed";
		public string Toast_NoModsFound { get; private set; } = "No Mods Found";
		public string Toast_AutoScanFailed { get; private set; } = "Auto-Scan Failed";
		#endregion

		#region Common / Shared
		public string Common_AlmostDone { get; private set; } = "almost done";
		public string Common_Remaining { get; private set; } = "remaining";
		public string Common_Extracting { get; private set; } = "Extracting";
		public string Common_FilesExtracted { get; private set; } = "files extracted";
		#endregion
	}
}
