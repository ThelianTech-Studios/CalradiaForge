namespace CalradiaForge.Core.Infra.Localization {
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Reflection;

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
		/// Builds a dictionary of translation defaults from this class by pairing each
		/// public string translation property with its matching <c>Default{PropertyName}</c> const.
		/// </summary>
		public static Dictionary<string, string> GetDefaultTranslations() {
			Type type = typeof(TranslationStrings);
			Dictionary<string, string> englishTranslations = new(StringComparer.Ordinal);

			foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(p => p.MetadataToken)) {
				if (property.PropertyType != typeof(string)) {
					continue;
				}
				if (property.SetMethod is null || !property.SetMethod.IsPrivate) {
					continue;
				}
				string defaultFieldName = $"Default{property.Name}";
				FieldInfo? defaultField = type.GetField(defaultFieldName, BindingFlags.NonPublic | BindingFlags.Static);
				if (defaultField is null || !defaultField.IsLiteral || defaultField.FieldType != typeof(string)) {
					throw new InvalidOperationException($"Missing Default Translation const for key '{property.Name}'. Expected a private const string field named '{defaultFieldName}'.");
				}
				englishTranslations[property.Name] = (string?)defaultField.GetRawConstantValue() ?? string.Empty;
			}
			return englishTranslations;
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
			Nav_ModsTab = GetOrDefault(translations, nameof(Nav_ModsTab), DefaultNav_ModsTab);
			Nav_ModpacksTab = GetOrDefault(translations, nameof(Nav_ModpacksTab), DefaultNav_ModpacksTab);
			Nav_FaqTab = GetOrDefault(translations, nameof(Nav_FaqTab), DefaultNav_FaqTab);
			Nav_SettingsTab = GetOrDefault(translations, nameof(Nav_SettingsTab), DefaultNav_SettingsTab);

			// ModsPage — Header
			Mods_ModpacksLabel = GetOrDefault(translations, nameof(Mods_ModpacksLabel), DefaultMods_ModpacksLabel);
			Mods_InstallButton = GetOrDefault(translations, nameof(Mods_InstallButton), DefaultMods_InstallButton);
			Mods_LoadOrderHeader = GetOrDefault(translations, nameof(Mods_LoadOrderHeader), DefaultMods_LoadOrderHeader);
			Mods_AvailableModsHeader = GetOrDefault(translations, nameof(Mods_AvailableModsHeader), DefaultMods_AvailableModsHeader);
			Mods_RefreshTooltip = GetOrDefault(translations, nameof(Mods_RefreshTooltip), DefaultMods_RefreshTooltip);

			// ModsPage — Play Button
			Mods_PlayBannerlord = GetOrDefault(translations, nameof(Mods_PlayBannerlord), DefaultMods_PlayBannerlord);
			Mods_PlayWithBLSE = GetOrDefault(translations, nameof(Mods_PlayWithBLSE), DefaultMods_PlayWithBLSE);
			Mods_LaunchTargetTooltip = GetOrDefault(translations, nameof(Mods_LaunchTargetTooltip), DefaultMods_LaunchTargetTooltip);
			Mods_LaunchTargetBannerlord = GetOrDefault(translations, nameof(Mods_LaunchTargetBannerlord), DefaultMods_LaunchTargetBannerlord);
			Mods_LaunchTargetBLSE = GetOrDefault(translations, nameof(Mods_LaunchTargetBLSE), DefaultMods_LaunchTargetBLSE);

			// ModsPage — Status Messages
			Mods_SelectModpackPrompt = GetOrDefault(translations, nameof(Mods_SelectModpackPrompt), DefaultMods_SelectModpackPrompt);
			Mods_InstallInProgress = GetOrDefault(translations, nameof(Mods_InstallInProgress), DefaultMods_InstallInProgress);
			Mods_Launching = GetOrDefault(translations, nameof(Mods_Launching), DefaultMods_Launching);
			Mods_ScanningForMods = GetOrDefault(translations, nameof(Mods_ScanningForMods), DefaultMods_ScanningForMods);
			Mods_NoModsFound = GetOrDefault(translations, nameof(Mods_NoModsFound), DefaultMods_NoModsFound);
			Mods_InstallDialogTitle = GetOrDefault(translations, nameof(Mods_InstallDialogTitle), DefaultMods_InstallDialogTitle);

			// ModpacksPage — Header
			Modpacks_HeaderLabel = GetOrDefault(translations, nameof(Modpacks_HeaderLabel), DefaultModpacks_HeaderLabel);
			Modpacks_CreateNewButton = GetOrDefault(translations, nameof(Modpacks_CreateNewButton), DefaultModpacks_CreateNewButton);
			Modpacks_ImportButton = GetOrDefault(translations, nameof(Modpacks_ImportButton), DefaultModpacks_ImportButton);
			Modpacks_SaveButton = GetOrDefault(translations, nameof(Modpacks_SaveButton), DefaultModpacks_SaveButton);

			// ModpacksPage — Lists
			Modpacks_ActiveLoadOrderHeader = GetOrDefault(translations, nameof(Modpacks_ActiveLoadOrderHeader), DefaultModpacks_ActiveLoadOrderHeader);
			Modpacks_SavedDataHeader = GetOrDefault(translations, nameof(Modpacks_SavedDataHeader), DefaultModpacks_SavedDataHeader);
			Modpacks_ByLabel = GetOrDefault(translations, nameof(Modpacks_ByLabel), DefaultModpacks_ByLabel);
			Modpacks_UpdatedLabel = GetOrDefault(translations, nameof(Modpacks_UpdatedLabel), DefaultModpacks_UpdatedLabel);

			// ModpacksPage — Edit Panel
			Modpacks_ModuleIdLabel = GetOrDefault(translations, nameof(Modpacks_ModuleIdLabel), DefaultModpacks_ModuleIdLabel);
			Modpacks_VersionLabel = GetOrDefault(translations, nameof(Modpacks_VersionLabel), DefaultModpacks_VersionLabel);
			Modpacks_NameLabel = GetOrDefault(translations, nameof(Modpacks_NameLabel), DefaultModpacks_NameLabel);
			Modpacks_UrlLabel = GetOrDefault(translations, nameof(Modpacks_UrlLabel), DefaultModpacks_UrlLabel);
			Modpacks_SaveEntryButton = GetOrDefault(translations, nameof(Modpacks_SaveEntryButton), DefaultModpacks_SaveEntryButton);

			// ModpacksPage — Create Panel
			Modpacks_CreateNameLabel = GetOrDefault(translations, nameof(Modpacks_CreateNameLabel), DefaultModpacks_CreateNameLabel);
			Modpacks_CreatedByLabel = GetOrDefault(translations, nameof(Modpacks_CreatedByLabel), DefaultModpacks_CreatedByLabel);
			Modpacks_ConfirmButton = GetOrDefault(translations, nameof(Modpacks_ConfirmButton), DefaultModpacks_ConfirmButton);
			Modpacks_CancelButton = GetOrDefault(translations, nameof(Modpacks_CancelButton), DefaultModpacks_CancelButton);

			// ModpacksPage — Template Names
			Modpacks_TemplateVanilla = GetOrDefault(translations, nameof(Modpacks_TemplateVanilla), DefaultModpacks_TemplateVanilla);
			Modpacks_TemplateButterLib = GetOrDefault(translations, nameof(Modpacks_TemplateButterLib), DefaultModpacks_TemplateButterLib);
			Modpacks_TemplateVanillaWarSails = GetOrDefault(translations, nameof(Modpacks_TemplateVanillaWarSails), DefaultModpacks_TemplateVanillaWarSails);
			Modpacks_TemplateButterLibWarSails = GetOrDefault(translations, nameof(Modpacks_TemplateButterLibWarSails), DefaultModpacks_TemplateButterLibWarSails);

			// Settings — Nav Tabs
			Settings_GeneralTab = GetOrDefault(translations, nameof(Settings_GeneralTab), DefaultSettings_GeneralTab);
			Settings_GameConfigTab = GetOrDefault(translations, nameof(Settings_GameConfigTab), DefaultSettings_GameConfigTab);
			Settings_ToolsTab = GetOrDefault(translations, nameof(Settings_ToolsTab), DefaultSettings_ToolsTab);
			Settings_WipTab = GetOrDefault(translations, nameof(Settings_WipTab), DefaultSettings_WipTab);
			Settings_AboutTab = GetOrDefault(translations, nameof(Settings_AboutTab), DefaultSettings_AboutTab);

			// Settings — General
			Settings_LanguageHeader = GetOrDefault(translations, nameof(Settings_LanguageHeader), DefaultSettings_LanguageHeader);
			Settings_LanguageLabel = GetOrDefault(translations, nameof(Settings_LanguageLabel), DefaultSettings_LanguageLabel);
			Settings_LanguageHint = GetOrDefault(translations, nameof(Settings_LanguageHint), DefaultSettings_LanguageHint);
			Settings_StartupHeader = GetOrDefault(translations, nameof(Settings_StartupHeader), DefaultSettings_StartupHeader);
			Settings_StartupLabel = GetOrDefault(translations, nameof(Settings_StartupLabel), DefaultSettings_StartupLabel);
			Settings_StartupLastUsed = GetOrDefault(translations, nameof(Settings_StartupLastUsed), DefaultSettings_StartupLastUsed);
			Settings_StartupAlwaysDefault = GetOrDefault(translations, nameof(Settings_StartupAlwaysDefault), DefaultSettings_StartupAlwaysDefault);
			Settings_StartupAlwaysAsk = GetOrDefault(translations, nameof(Settings_StartupAlwaysAsk), DefaultSettings_StartupAlwaysAsk);
			Settings_StartupHint = GetOrDefault(translations, nameof(Settings_StartupHint), DefaultSettings_StartupHint);
			Settings_DiagnosticsHeader = GetOrDefault(translations, nameof(Settings_DiagnosticsHeader), DefaultSettings_DiagnosticsHeader);
			Settings_DebugModeLabel = GetOrDefault(translations, nameof(Settings_DebugModeLabel), DefaultSettings_DebugModeLabel);
			Settings_DebugModeHint = GetOrDefault(translations, nameof(Settings_DebugModeHint), DefaultSettings_DebugModeHint);
			// Settings — Game Config
			Settings_GameInstallHeader = GetOrDefault(translations, nameof(Settings_GameInstallHeader), DefaultSettings_GameInstallHeader);
			Settings_GameFolderLabel = GetOrDefault(translations, nameof(Settings_GameFolderLabel), DefaultSettings_GameFolderLabel);
			Settings_SelectFolderButton = GetOrDefault(translations, nameof(Settings_SelectFolderButton), DefaultSettings_SelectFolderButton);
			Settings_GameExeLabel = GetOrDefault(translations, nameof(Settings_GameExeLabel), DefaultSettings_GameExeLabel);
			Settings_SelectFileButton = GetOrDefault(translations, nameof(Settings_SelectFileButton), DefaultSettings_SelectFileButton);
			Settings_WorkshopFolderLabel = GetOrDefault(translations, nameof(Settings_WorkshopFolderLabel), DefaultSettings_WorkshopFolderLabel);
			Settings_BLSEHeader = GetOrDefault(translations, nameof(Settings_BLSEHeader), DefaultSettings_BLSEHeader);
			Settings_BLSEExeLabel = GetOrDefault(translations, nameof(Settings_BLSEExeLabel), DefaultSettings_BLSEExeLabel);
			Settings_BLSEHint = GetOrDefault(translations, nameof(Settings_BLSEHint), DefaultSettings_BLSEHint);
			Settings_DetectionHeader = GetOrDefault(translations, nameof(Settings_DetectionHeader), DefaultSettings_DetectionHeader);
			Settings_DetectGameButton = GetOrDefault(translations, nameof(Settings_DetectGameButton), DefaultSettings_DetectGameButton);
			Settings_DetectGameHint = GetOrDefault(translations, nameof(Settings_DetectGameHint), DefaultSettings_DetectGameHint);

			// Settings — Tools
			Settings_ModMaintenanceHeader = GetOrDefault(translations, nameof(Settings_ModMaintenanceHeader), DefaultSettings_ModMaintenanceHeader);
			Settings_UnblockDllsButton = GetOrDefault(translations, nameof(Settings_UnblockDllsButton), DefaultSettings_UnblockDllsButton);
			Settings_UnblockDllsHint = GetOrDefault(translations, nameof(Settings_UnblockDllsHint), DefaultSettings_UnblockDllsHint);
			Settings_DataManagementHeader = GetOrDefault(translations, nameof(Settings_DataManagementHeader), DefaultSettings_DataManagementHeader);
			Settings_ClearCacheButton = GetOrDefault(translations, nameof(Settings_ClearCacheButton), DefaultSettings_ClearCacheButton);
			Settings_ClearCacheHint = GetOrDefault(translations, nameof(Settings_ClearCacheHint), DefaultSettings_ClearCacheHint);
			Settings_OpenConfigButton = GetOrDefault(translations, nameof(Settings_OpenConfigButton), DefaultSettings_OpenConfigButton);
			Settings_OpenConfigHint = GetOrDefault(translations, nameof(Settings_OpenConfigHint), DefaultSettings_OpenConfigHint);
			Settings_OpenLogsButton = GetOrDefault(translations, nameof(Settings_OpenLogsButton), DefaultSettings_OpenLogsButton);
			Settings_OpenLogsHint = GetOrDefault(translations, nameof(Settings_OpenLogsHint), DefaultSettings_OpenLogsHint);
			Settings_OpenModpacksButton = GetOrDefault(translations, nameof(Settings_OpenModpacksButton), DefaultSettings_OpenModpacksButton);
			Settings_OpenModpacksHint = GetOrDefault(translations, nameof(Settings_OpenModpacksHint), DefaultSettings_OpenModpacksHint);
			// Settings — WIP
			Settings_WipTitle = GetOrDefault(translations, nameof(Settings_WipTitle), DefaultSettings_WipTitle);
			Settings_WipDescription = GetOrDefault(translations, nameof(Settings_WipDescription), DefaultSettings_WipDescription);
			Settings_WipStayTuned = GetOrDefault(translations, nameof(Settings_WipStayTuned), DefaultSettings_WipStayTuned);
			// Settings — About
			Settings_AboutDescription = GetOrDefault(translations, nameof(Settings_AboutDescription), DefaultSettings_AboutDescription);
			Settings_AboutTagline = GetOrDefault(translations, nameof(Settings_AboutTagline), DefaultSettings_AboutTagline);
			Settings_AboutPublisherLabel = GetOrDefault(translations, nameof(Settings_AboutPublisherLabel), DefaultSettings_AboutPublisherLabel);
			Settings_AboutLicenseLabel = GetOrDefault(translations, nameof(Settings_AboutLicenseLabel), DefaultSettings_AboutLicenseLabel);
			Settings_AboutViewLicense = GetOrDefault(translations, nameof(Settings_AboutViewLicense), DefaultSettings_AboutViewLicense);
			Settings_AboutGitHubLabel = GetOrDefault(translations, nameof(Settings_AboutGitHubLabel), DefaultSettings_AboutGitHubLabel);
			Settings_AboutViewGitHub = GetOrDefault(translations, nameof(Settings_AboutViewGitHub), DefaultSettings_AboutViewGitHub);
			Settings_AboutCopyright = GetOrDefault(translations, nameof(Settings_AboutCopyright), DefaultSettings_AboutCopyright);
			Settings_AboutDisclaimer = GetOrDefault(translations, nameof(Settings_AboutDisclaimer), DefaultSettings_AboutDisclaimer);

			// FAQ Page
			Faq_PageTitle = GetOrDefault(translations, nameof(Faq_PageTitle), DefaultFaq_PageTitle);
			Faq_Q1_Title = GetOrDefault(translations, nameof(Faq_Q1_Title), DefaultFaq_Q1_Title);
			Faq_Q1_Answer1 = GetOrDefault(translations, nameof(Faq_Q1_Answer1), DefaultFaq_Q1_Answer1);
			Faq_Q1_Answer2 = GetOrDefault(translations, nameof(Faq_Q1_Answer2), DefaultFaq_Q1_Answer2);
			Faq_Q1_Hint = GetOrDefault(translations, nameof(Faq_Q1_Hint), DefaultFaq_Q1_Hint);
			Faq_Q2_Title = GetOrDefault(translations, nameof(Faq_Q2_Title), DefaultFaq_Q2_Title);
			Faq_Q2_Answer1 = GetOrDefault(translations, nameof(Faq_Q2_Answer1), DefaultFaq_Q2_Answer1);
			Faq_Q2_Answer2 = GetOrDefault(translations, nameof(Faq_Q2_Answer2), DefaultFaq_Q2_Answer2);
			Faq_Q3_Title = GetOrDefault(translations, nameof(Faq_Q3_Title), DefaultFaq_Q3_Title);
			Faq_Q3_Answer1 = GetOrDefault(translations, nameof(Faq_Q3_Answer1), DefaultFaq_Q3_Answer1);
			Faq_Q3_Answer2 = GetOrDefault(translations, nameof(Faq_Q3_Answer2), DefaultFaq_Q3_Answer2);
			Faq_Q3_Hint = GetOrDefault(translations, nameof(Faq_Q3_Hint), DefaultFaq_Q3_Hint);
			Faq_Q4_Title = GetOrDefault(translations, nameof(Faq_Q4_Title), DefaultFaq_Q4_Title);
			Faq_Q4_Answer1 = GetOrDefault(translations, nameof(Faq_Q4_Answer1), DefaultFaq_Q4_Answer1);
			Faq_Q4_Answer2 = GetOrDefault(translations, nameof(Faq_Q4_Answer2), DefaultFaq_Q4_Answer2);
			Faq_Q4_Answer3 = GetOrDefault(translations, nameof(Faq_Q4_Answer3), DefaultFaq_Q4_Answer3);
			Faq_Q4_Hint = GetOrDefault(translations, nameof(Faq_Q4_Hint), DefaultFaq_Q4_Hint);
			Faq_Q5_Title = GetOrDefault(translations, nameof(Faq_Q5_Title), DefaultFaq_Q5_Title);
			Faq_Q5_Answer1 = GetOrDefault(translations, nameof(Faq_Q5_Answer1), DefaultFaq_Q5_Answer1);
			Faq_Q5_Answer2 = GetOrDefault(translations, nameof(Faq_Q5_Answer2), DefaultFaq_Q5_Answer2);
			Faq_Q5_Hint = GetOrDefault(translations, nameof(Faq_Q5_Hint), DefaultFaq_Q5_Hint);
			Faq_Q6_Title = GetOrDefault(translations, nameof(Faq_Q6_Title), DefaultFaq_Q6_Title);
			Faq_Q6_Answer1 = GetOrDefault(translations, nameof(Faq_Q6_Answer1), DefaultFaq_Q6_Answer1);
			Faq_Q6_Answer2 = GetOrDefault(translations, nameof(Faq_Q6_Answer2), DefaultFaq_Q6_Answer2);
			Faq_Q6_Answer3 = GetOrDefault(translations, nameof(Faq_Q6_Answer3), DefaultFaq_Q6_Answer3);
			Faq_Q6_Hint = GetOrDefault(translations, nameof(Faq_Q6_Hint), DefaultFaq_Q6_Hint);
			Faq_Q7_Title = GetOrDefault(translations, nameof(Faq_Q7_Title), DefaultFaq_Q7_Title);
			Faq_Q7_Answer1 = GetOrDefault(translations, nameof(Faq_Q7_Answer1), DefaultFaq_Q7_Answer1);
			Faq_Q7_Answer2 = GetOrDefault(translations, nameof(Faq_Q7_Answer2), DefaultFaq_Q7_Answer2);
			Faq_Q8_Title = GetOrDefault(translations, nameof(Faq_Q8_Title), DefaultFaq_Q8_Title);
			Faq_Q8_Answer1 = GetOrDefault(translations, nameof(Faq_Q8_Answer1), DefaultFaq_Q8_Answer1);
			Faq_Q8_OpenIssuesButton = GetOrDefault(translations, nameof(Faq_Q8_OpenIssuesButton), DefaultFaq_Q8_OpenIssuesButton);
			Faq_Q8_OpenIssuesHint = GetOrDefault(translations, nameof(Faq_Q8_OpenIssuesHint), DefaultFaq_Q8_OpenIssuesHint);
			Faq_Q8_Hint = GetOrDefault(translations, nameof(Faq_Q8_Hint), DefaultFaq_Q8_Hint);

			// Toast Messages
			Toast_InstallInProgress = GetOrDefault(translations, nameof(Toast_InstallInProgress), DefaultToast_InstallInProgress);
			Toast_InstallComplete = GetOrDefault(translations, nameof(Toast_InstallComplete), DefaultToast_InstallComplete);
			Toast_InstallCompleteWithErrors = GetOrDefault(translations, nameof(Toast_InstallCompleteWithErrors), DefaultToast_InstallCompleteWithErrors);
			Toast_InstallingMods = GetOrDefault(translations, nameof(Toast_InstallingMods), DefaultToast_InstallingMods);
			Toast_GameLaunched = GetOrDefault(translations, nameof(Toast_GameLaunched), DefaultToast_GameLaunched);
			Toast_LaunchFailed = GetOrDefault(translations, nameof(Toast_LaunchFailed), DefaultToast_LaunchFailed);
			Toast_MissingMods = GetOrDefault(translations, nameof(Toast_MissingMods), DefaultToast_MissingMods);
			Toast_ModListUpdated = GetOrDefault(translations, nameof(Toast_ModListUpdated), DefaultToast_ModListUpdated);
			Toast_RefreshFailed = GetOrDefault(translations, nameof(Toast_RefreshFailed), DefaultToast_RefreshFailed);
			Toast_NoModsFound = GetOrDefault(translations, nameof(Toast_NoModsFound), DefaultToast_NoModsFound);
			Toast_AutoScanFailed = GetOrDefault(translations, nameof(Toast_AutoScanFailed), DefaultToast_AutoScanFailed);
			// Common / Shared
			Common_AlmostDone = GetOrDefault(translations, nameof(Common_AlmostDone), DefaultCommon_AlmostDone);
			Common_Remaining = GetOrDefault(translations, nameof(Common_Remaining), DefaultCommon_Remaining);
			Common_Extracting = GetOrDefault(translations, nameof(Common_Extracting), DefaultCommon_Extracting);
			Common_FilesExtracted = GetOrDefault(translations, nameof(Common_FilesExtracted), DefaultCommon_FilesExtracted);

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
		public string Nav_ModsTab { get; private set; }
		private const string DefaultNav_ModsTab = "Mods";
		public string Nav_ModpacksTab { get; private set; }
		private const string DefaultNav_ModpacksTab = "Modpacks";
		public string Nav_FaqTab { get; private set; }
		private const string DefaultNav_FaqTab = "FAQ";
		public string Nav_SettingsTab { get; private set; }
		private const string DefaultNav_SettingsTab = "Settings";
		#endregion

		#region ModsPage — Header
		public string Mods_ModpacksLabel { get; private set; }
		private const string DefaultMods_ModpacksLabel = "Mod Packs:";
		public string Mods_InstallButton { get; private set; }
		private const string DefaultMods_InstallButton = "Install Mods";
		public string Mods_LoadOrderHeader { get; private set; }
		private const string DefaultMods_LoadOrderHeader = "Load Order";
		public string Mods_AvailableModsHeader { get; private set; }
		private const string DefaultMods_AvailableModsHeader = "Available Mods";
		public string Mods_RefreshTooltip { get; private set; }
		private const string DefaultMods_RefreshTooltip = "Refresh Mods";
		#endregion

		#region ModsPage — Play Button
		public string Mods_PlayBannerlord { get; private set; }
		private const string DefaultMods_PlayBannerlord = "Play Bannerlord";
		public string Mods_PlayWithBLSE { get; private set; }
		private const string DefaultMods_PlayWithBLSE = "Play with BLSE";
		public string Mods_LaunchTargetTooltip { get; private set; }
		private const string DefaultMods_LaunchTargetTooltip = "Choose launch target";
		public string Mods_LaunchTargetBannerlord { get; private set; }
		private const string DefaultMods_LaunchTargetBannerlord = "Bannerlord";
		public string Mods_LaunchTargetBLSE { get; private set; }
		private const string DefaultMods_LaunchTargetBLSE = "BLSE";
		#endregion

		#region ModsPage — Status Messages
		public string Mods_SelectModpackPrompt { get; private set; }
		private const string DefaultMods_SelectModpackPrompt = "No modpack selected. Choose one from the dropdown above.";
		public string Mods_InstallInProgress { get; private set; }
		private const string DefaultMods_InstallInProgress = "Installation in progress...";
		public string Mods_Launching { get; private set; }
		private const string DefaultMods_Launching = "Launching...";
		public string Mods_ScanningForMods { get; private set; }
		private const string DefaultMods_ScanningForMods = "Scanning for mods...";
		public string Mods_NoModsFound { get; private set; }
		private const string DefaultMods_NoModsFound = "No mods found. Install mods or check your game path in Settings.";
		public string Mods_InstallDialogTitle { get; private set; }
		private const string DefaultMods_InstallDialogTitle = "Select Mod Archives to Install";
		#endregion

		#region ModpacksPage — Header
		public string Modpacks_HeaderLabel { get; private set; }
		private const string DefaultModpacks_HeaderLabel = "Mod Packs:";
		public string Modpacks_CreateNewButton { get; private set; }
		private const string DefaultModpacks_CreateNewButton = "Create New";
		public string Modpacks_ImportButton { get; private set; }
		private const string DefaultModpacks_ImportButton = "Import";
		public string Modpacks_SaveButton { get; private set; }
		private const string DefaultModpacks_SaveButton = "Save to Modpack";
		#endregion

		#region ModpacksPage — Lists
		public string Modpacks_ActiveLoadOrderHeader { get; private set; }
		private const string DefaultModpacks_ActiveLoadOrderHeader = "Active Load Order";
		public string Modpacks_SavedDataHeader { get; private set; }
		private const string DefaultModpacks_SavedDataHeader = "Saved Modpack Data";
		public string Modpacks_ByLabel { get; private set; }
		private const string DefaultModpacks_ByLabel = "By:";
		public string Modpacks_UpdatedLabel { get; private set; }
		private const string DefaultModpacks_UpdatedLabel = "Updated:";
		#endregion

		#region ModpacksPage — Edit Panel
		public string Modpacks_ModuleIdLabel { get; private set; }
		private const string DefaultModpacks_ModuleIdLabel = "Module ID:";
		public string Modpacks_VersionLabel { get; private set; }
		private const string DefaultModpacks_VersionLabel = "Version:";
		public string Modpacks_NameLabel { get; private set; }
		private const string DefaultModpacks_NameLabel = "Name:";
		public string Modpacks_UrlLabel { get; private set; }
		private const string DefaultModpacks_UrlLabel = "URL:";
		public string Modpacks_SaveEntryButton { get; private set; }
		private const string DefaultModpacks_SaveEntryButton = "Save Entry";
		#endregion

		#region ModpacksPage — Create Panel
		public string Modpacks_CreateNameLabel { get; private set; }
		private const string DefaultModpacks_CreateNameLabel = "Name:";
		public string Modpacks_CreatedByLabel { get; private set; }
		private const string DefaultModpacks_CreatedByLabel = "Created By:";
		public string Modpacks_ConfirmButton { get; private set; }
		private const string DefaultModpacks_ConfirmButton = "Confirm";
		public string Modpacks_CancelButton { get; private set; }
		private const string DefaultModpacks_CancelButton = "Cancel";
		#endregion

		#region ModpacksPage — Template Names
		public string Modpacks_TemplateVanilla { get; private set; }
		private const string DefaultModpacks_TemplateVanilla = "Default Modpack";
		public string Modpacks_TemplateButterLib { get; private set; }
		private const string DefaultModpacks_TemplateButterLib = "ButterLib Suite";
		public string Modpacks_TemplateVanillaWarSails { get; private set; }
		private const string DefaultModpacks_TemplateVanillaWarSails = "Vanilla - WarSails";
		public string Modpacks_TemplateButterLibWarSails { get; private set; }
		private const string DefaultModpacks_TemplateButterLibWarSails = "ButterLib - WarSails";
		#endregion

		#region Settings — Nav Tabs
		public string Settings_GeneralTab { get; private set; }
		private const string DefaultSettings_GeneralTab = "General";
		public string Settings_GameConfigTab { get; private set; }
		private const string DefaultSettings_GameConfigTab = "Game Config";
		public string Settings_ToolsTab { get; private set; }
		private const string DefaultSettings_ToolsTab = "Tools";
		public string Settings_WipTab { get; private set; }
		private const string DefaultSettings_WipTab = "WIP";
		public string Settings_AboutTab { get; private set; }
		private const string DefaultSettings_AboutTab = "About";
		#endregion

		#region Settings — General
		public string Settings_LanguageHeader { get; private set; }
		private const string DefaultSettings_LanguageHeader = "Language";
		public string Settings_LanguageLabel { get; private set; }
		private const string DefaultSettings_LanguageLabel = "Language";
		public string Settings_LanguageHint { get; private set; }
		private const string DefaultSettings_LanguageHint = "Select the display language for CalradiaForge.";
		public string Settings_StartupHeader { get; private set; }
		private const string DefaultSettings_StartupHeader = "Startup Behavior";
		public string Settings_StartupLabel { get; private set; }
		private const string DefaultSettings_StartupLabel = "On startup, select modpack:";
		public string Settings_StartupLastUsed { get; private set; }
		private const string DefaultSettings_StartupLastUsed = "Last Used Modpack";
		public string Settings_StartupAlwaysDefault { get; private set; }
		private const string DefaultSettings_StartupAlwaysDefault = "Always Use Default (Vanilla)";
		public string Settings_StartupAlwaysAsk { get; private set; }
		private const string DefaultSettings_StartupAlwaysAsk = "Always Ask";
		public string Settings_StartupHint { get; private set; }
		private const string DefaultSettings_StartupHint = "Controls which modpack is automatically selected when CalradiaForge starts.";
		public string Settings_DiagnosticsHeader { get; private set; }
		private const string DefaultSettings_DiagnosticsHeader = "Diagnostics";
		public string Settings_DebugModeLabel { get; private set; }
		private const string DefaultSettings_DebugModeLabel = "Enable Debug Mode";
		public string Settings_DebugModeHint { get; private set; }
		private const string DefaultSettings_DebugModeHint = "Enables verbose logging and diagnostic data output. Only enable this when requested for troubleshooting.";
		#endregion

		#region Settings — Game Config
		public string Settings_GameInstallHeader { get; private set; }
		private const string DefaultSettings_GameInstallHeader = "Game Installation";
		public string Settings_GameFolderLabel { get; private set; }
		private const string DefaultSettings_GameFolderLabel = "Game Folder";
		public string Settings_SelectFolderButton { get; private set; }
		private const string DefaultSettings_SelectFolderButton = "Select Folder";
		public string Settings_GameExeLabel { get; private set; }
		private const string DefaultSettings_GameExeLabel = "Game Executable";
		public string Settings_SelectFileButton { get; private set; }
		private const string DefaultSettings_SelectFileButton = "Select File";
		public string Settings_WorkshopFolderLabel { get; private set; }
		private const string DefaultSettings_WorkshopFolderLabel = "Steam Workshop Folder";
		public string Settings_BLSEHeader { get; private set; }
		private const string DefaultSettings_BLSEHeader = "Script Extender (BLSE)";
		public string Settings_BLSEExeLabel { get; private set; }
		private const string DefaultSettings_BLSEExeLabel = "BLSE Standalone Executable";
		public string Settings_BLSEHint { get; private set; }
		private const string DefaultSettings_BLSEHint = "The Bannerlord Software Extender enables advanced script mods. If installed, CalradiaForge auto-detects it in the game's bin folder. You can also select it manually.";
		public string Settings_DetectionHeader { get; private set; }
		private const string DefaultSettings_DetectionHeader = "Detection";
		public string Settings_DetectGameButton { get; private set; }
		private const string DefaultSettings_DetectGameButton = "\U0001f504  Detect Game";
		public string Settings_DetectGameHint { get; private set; }
		private const string DefaultSettings_DetectGameHint = "Re-runs auto-detection for Steam, Epic, and StandAlone installations. Also detects BLSE if installed. Overwrites current paths.";
		#endregion

		#region Settings — Tools
		public string Settings_ModMaintenanceHeader { get; private set; }
		private const string DefaultSettings_ModMaintenanceHeader = "Mod Maintenance";
		public string Settings_UnblockDllsButton { get; private set; }
		private const string DefaultSettings_UnblockDllsButton = "\U0001f513  Unblock DLLs";
		public string Settings_UnblockDllsHint { get; private set; }
		private const string DefaultSettings_UnblockDllsHint = "Removes the Zone.Identifier alternate data stream from all DLL files in the Modules folder. Required for mods downloaded from the internet.";
		public string Settings_DataManagementHeader { get; private set; }
		private const string DefaultSettings_DataManagementHeader = "Data Management";
		public string Settings_ClearCacheButton { get; private set; }
		private const string DefaultSettings_ClearCacheButton = "\U0001f5d1  Clear Mod Cache";
		public string Settings_ClearCacheHint { get; private set; }
		private const string DefaultSettings_ClearCacheHint = "Deletes the cached mod list and forces a fresh rescan on next launch.";
		public string Settings_OpenConfigButton { get; private set; }
		private const string DefaultSettings_OpenConfigButton = "\U0001f4c2  Open Config Folder";
		public string Settings_OpenConfigHint { get; private set; }
		private const string DefaultSettings_OpenConfigHint = "Opens the CalradiaForge Config directory in Windows Explorer.";
		public string Settings_OpenLogsButton { get; private set; }
		private const string DefaultSettings_OpenLogsButton = "\U0001f4c2  Open Logs Folder";
		public string Settings_OpenLogsHint { get; private set; }
		private const string DefaultSettings_OpenLogsHint = "Opens the Logs directory in Windows Explorer.";
		public string Settings_OpenModpacksButton { get; private set; }
		private const string DefaultSettings_OpenModpacksButton = "\U0001f4c2  Open Modpacks Folder";
		public string Settings_OpenModpacksHint { get; private set; }
		private const string DefaultSettings_OpenModpacksHint = "Opens the Modpacks directory in Windows Explorer.";
		#endregion

		#region Settings — WIP
		public string Settings_WipTitle { get; private set; }
		private const string DefaultSettings_WipTitle = "This tab is reserved for future development.";
		public string Settings_WipDescription { get; private set; }
		private const string DefaultSettings_WipDescription = "If there is enough community interest in advanced features\nand functionality beyond the current project scope,\nfuture settings will appear here.";
		public string Settings_WipStayTuned { get; private set; }
		private const string DefaultSettings_WipStayTuned = "Stay tuned for updates!";
		#endregion

		#region Settings — About
		public string Settings_AboutDescription { get; private set; }
		private const string DefaultSettings_AboutDescription = "A modern mod launcher for Mount & Blade II: Bannerlord.";
		public string Settings_AboutTagline { get; private set; }
		private const string DefaultSettings_AboutTagline = "Built by players, for players.";
		public string Settings_AboutPublisherLabel { get; private set; }
		private const string DefaultSettings_AboutPublisherLabel = "Publisher";
		public string Settings_AboutLicenseLabel { get; private set; }
		private const string DefaultSettings_AboutLicenseLabel = "License";
		public string Settings_AboutViewLicense { get; private set; }
		private const string DefaultSettings_AboutViewLicense = "View License";
		public string Settings_AboutGitHubLabel { get; private set; }
		private const string DefaultSettings_AboutGitHubLabel = "GitHub";
		public string Settings_AboutViewGitHub { get; private set; }
		private const string DefaultSettings_AboutViewGitHub = "View on GitHub";
		public string Settings_AboutCopyright { get; private set; }
		private const string DefaultSettings_AboutCopyright = "\u00a9 2026 ThelianTech\u2122 \u2014 All rights reserved.";
		public string Settings_AboutDisclaimer { get; private set; }
		private const string DefaultSettings_AboutDisclaimer = "Not affiliated with TaleWorlds Entertainment.";
		#endregion

		#region FAQ Page
		public string Faq_PageTitle { get; private set; }
		private const string DefaultFaq_PageTitle = "Frequently Asked Questions";
		public string Faq_Q1_Title { get; private set; }
		private const string DefaultFaq_Q1_Title = "Why isn't the game detecting my mods?";
		public string Faq_Q1_Answer1 { get; private set; }
		private const string DefaultFaq_Q1_Answer1 = "Make sure your mods are in the Active load order on the Mods page. Only mods listed in the Load Order panel are passed to the game at launch.";
		public string Faq_Q1_Answer2 { get; private set; }
		private const string DefaultFaq_Q1_Answer2 = "Verify that your game installation path is set correctly under Settings \u2192 Game Config. Use the Re-detect button if you're unsure.";
		public string Faq_Q1_Hint { get; private set; }
		private const string DefaultFaq_Q1_Hint = "Tip: If mods were downloaded from the internet, Windows may block their DLLs. Go to Settings \u2192 Tools \u2192 Unblock DLLs to fix this.";
		public string Faq_Q2_Title { get; private set; }
		private const string DefaultFaq_Q2_Title = "What does 'Unblock DLLs' do?";
		public string Faq_Q2_Answer1 { get; private set; }
		private const string DefaultFaq_Q2_Answer1 = "When you download files from the internet, Windows adds a hidden security marker (Zone.Identifier) to them. This can prevent Bannerlord from loading mod DLLs.";
		public string Faq_Q2_Answer2 { get; private set; }
		private const string DefaultFaq_Q2_Answer2 = "The Unblock DLLs tool in Settings \u2192 Tools removes this marker from all DLL files in your Modules folder. CalradiaForge also does this automatically when you install mods through the app.";
		public string Faq_Q3_Title { get; private set; }
		private const string DefaultFaq_Q3_Title = "I'm getting errors when installing mods from an archive.";
		public string Faq_Q3_Answer1 { get; private set; }
		private const string DefaultFaq_Q3_Answer1 = "CalradiaForge extracts mod archives directly into the Bannerlord Modules folder. Make sure the archive contains a valid mod structure with a SubModule.xml file.";
		public string Faq_Q3_Answer2 { get; private set; }
		private const string DefaultFaq_Q3_Answer2 = "If the archive is nested (a folder inside a folder), CalradiaForge will attempt to detect the correct root. If extraction still fails, try extracting the mod manually.";
		public string Faq_Q3_Hint { get; private set; }
		private const string DefaultFaq_Q3_Hint = "Supported archive formats: .zip, .rar \u2014 .7z is temporarily disabled (see below).";
		public string Faq_Q4_Title { get; private set; }
		private const string DefaultFaq_Q4_Title = "How do modpacks work?";
		public string Faq_Q4_Answer1 { get; private set; }
		private const string DefaultFaq_Q4_Answer1 = "A modpack is a saved snapshot of your active mods and their load order. You can create multiple modpacks for different playstyles (e.g., Vanilla+, Overhaul, Hardcore).";
		public string Faq_Q4_Answer2 { get; private set; }
		private const string DefaultFaq_Q4_Answer2 = "Switch between modpacks using the dropdown on the Mods page. The selected modpack's load order is applied immediately.";
		public string Faq_Q4_Answer3 { get; private set; }
		private const string DefaultFaq_Q4_Answer3 = "To save changes to a modpack, go to the Mod Packs page and click 'Save to Modpack'. This overwrites the saved data with your current active load order.";
		public string Faq_Q4_Hint { get; private set; }
		private const string DefaultFaq_Q4_Hint = "Make sure you have selected the correct modpack on the Mods Page before you add/remove mods to your load order there, before you go and save any changes in the Modpacks Page else any changes will be lost.";
		public string Faq_Q5_Title { get; private set; }
		private const string DefaultFaq_Q5_Title = "Why can't I launch the game from CalradiaForge on Epic or GamePass?";
		public string Faq_Q5_Answer1 { get; private set; }
		private const string DefaultFaq_Q5_Answer1 = "TaleWorlds requires authentication through the Epic or Xbox client. This is a platform-level restriction that third-party launchers cannot bypass.";
		public string Faq_Q5_Answer2 { get; private set; }
		private const string DefaultFaq_Q5_Answer2 = "CalradiaForge still provides full mod management, load order arrangement, and modpack features for these platforms \u2014 you just need to press Play from the platform's own launcher.";
		public string Faq_Q5_Hint { get; private set; }
		private const string DefaultFaq_Q5_Hint = "Direct launch support for Epic and GamePass is planned for a future release. For now load orders are not passed to the game's native launcher.";
		public string Faq_Q6_Title { get; private set; }
		private const string DefaultFaq_Q6_Title = "Can I import presets from Novus Launcher?";
		public string Faq_Q6_Answer1 { get; private set; }
		private const string DefaultFaq_Q6_Answer1 = "Yes. Go to the Mod Packs page and click Import. Select a Novus Launcher preset file (.xml) and CalradiaForge will convert it into a CalradiaForge modpack automatically.";
		public string Faq_Q6_Answer2 { get; private set; }
		private const string DefaultFaq_Q6_Answer2 = "You can also import native CalradiaForge modpack files (.json) the same way.";
		public string Faq_Q6_Answer3 { get; private set; }
		private const string DefaultFaq_Q6_Answer3 = "You can also place CalradiaForge Modpacks directly in the modpacks subdirectory in the app's directory, and the app will automatically refresh the selectable modpacks.";
		public string Faq_Q6_Hint { get; private set; }
		private const string DefaultFaq_Q6_Hint = "Refresh or switch pages for the app to automatically refresh the dropdown menus'.";
		public string Faq_Q7_Title { get; private set; }
		private const string DefaultFaq_Q7_Title = "When should I clear the mod cache?";
		public string Faq_Q7_Answer1 { get; private set; }
		private const string DefaultFaq_Q7_Answer1 = "Clear the mod cache if you've manually added or removed mods from the Modules folder outside of CalradiaForge, or if the mod list appears stale or incorrect.";
		public string Faq_Q7_Answer2 { get; private set; }
		private const string DefaultFaq_Q7_Answer2 = "Go to Settings \u2192 Tools \u2192 Clear Mod Cache. On next launch, CalradiaForge will rescan the Modules folder and rebuild the cache.";
		public string Faq_Q8_Title { get; private set; }
		private const string DefaultFaq_Q8_Title = "How do I report a bug or request a feature?";
		public string Faq_Q8_Answer1 { get; private set; }
		private const string DefaultFaq_Q8_Answer1 = "Open an issue on the CalradiaForge GitHub repository. Include a short description of the problem, steps to reproduce it, and any relevant log output.";
		public string Faq_Q8_OpenIssuesButton { get; private set; }
		private const string DefaultFaq_Q8_OpenIssuesButton = "Open GitHub Issues";
		public string Faq_Q8_OpenIssuesHint { get; private set; }
		private const string DefaultFaq_Q8_OpenIssuesHint = "Opens the CalradiaForge Issues page on GitHub in your browser.";
		public string Faq_Q8_Hint { get; private set; }
		private const string DefaultFaq_Q8_Hint = "Tip: Enable Debug Mode in Settings \u2192 General before reproducing the issue. This gives more detailed logs for troubleshooting.";
		#endregion

		#region Toast Messages
		public string Toast_InstallInProgress { get; private set; }
		private const string DefaultToast_InstallInProgress = "Install in Progress";
		public string Toast_InstallComplete { get; private set; }
		private const string DefaultToast_InstallComplete = "Install Complete";
		public string Toast_InstallCompleteWithErrors { get; private set; }
		private const string DefaultToast_InstallCompleteWithErrors = "Install Completed with Errors";
		public string Toast_InstallingMods { get; private set; }
		private const string DefaultToast_InstallingMods = "Installing Mods";
		public string Toast_GameLaunched { get; private set; }
		private const string DefaultToast_GameLaunched = "Game Launched";
		public string Toast_LaunchFailed { get; private set; }
		private const string DefaultToast_LaunchFailed = "Launch Failed";
		public string Toast_MissingMods { get; private set; }
		private const string DefaultToast_MissingMods = "Missing Mod(s)";
		public string Toast_ModListUpdated { get; private set; }
		private const string DefaultToast_ModListUpdated = "Mod List Updated";
		public string Toast_RefreshFailed { get; private set; }
		private const string DefaultToast_RefreshFailed = "Refresh Failed";
		public string Toast_NoModsFound { get; private set; }
		private const string DefaultToast_NoModsFound = "No Mods Found";
		public string Toast_AutoScanFailed { get; private set; }
		private const string DefaultToast_AutoScanFailed = "Auto-Scan Failed";
		#endregion

		#region Common / Shared
		public string Common_AlmostDone { get; private set; }
		private const string DefaultCommon_AlmostDone = "almost done";
		public string Common_Remaining { get; private set; }
		private const string DefaultCommon_Remaining = "remaining";
		public string Common_Extracting { get; private set; }
		private const string DefaultCommon_Extracting = "Extracting";
		public string Common_FilesExtracted { get; private set; }
		private const string DefaultCommon_FilesExtracted = "files extracted";
		#endregion
	}
}
