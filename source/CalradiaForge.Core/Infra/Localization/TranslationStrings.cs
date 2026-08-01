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
			translations ??= new Dictionary<string, string>(StringComparer.Ordinal);

			// Navigation
			Nav_LauncherTab = GetOrDefault(translations, nameof(Nav_LauncherTab), DefaultNav_LauncherTab);
			Nav_ModpacksTab = GetOrDefault(translations, nameof(Nav_ModpacksTab), DefaultNav_ModpacksTab);
			Nav_FaqTab = GetOrDefault(translations, nameof(Nav_FaqTab), DefaultNav_FaqTab);
			Nav_SettingsTab = GetOrDefault(translations, nameof(Nav_SettingsTab), DefaultNav_SettingsTab);

			// LauncherPage — Header
			Launcher_ModpacksLabel = GetOrDefault(translations, nameof(Launcher_ModpacksLabel), DefaultLauncher_ModpacksLabel);
			Launcher_GhostModpackLabel = GetOrDefault(translations, nameof(Launcher_GhostModpackLabel), DefaultLauncher_GhostModpackLabel);
			Launcher_InstallButton = GetOrDefault(translations, nameof(Launcher_InstallButton), DefaultLauncher_InstallButton);
			Launcher_LoadOrderHeader = GetOrDefault(translations, nameof(Launcher_LoadOrderHeader), DefaultLauncher_LoadOrderHeader);
			Launcher_AvailableModsHeader = GetOrDefault(translations, nameof(Launcher_AvailableModsHeader), DefaultLauncher_AvailableModsHeader);
			Launcher_RefreshTooltip = GetOrDefault(translations, nameof(Launcher_RefreshTooltip), DefaultLauncher_RefreshTooltip);

			// LauncherPage — Play Button
			Launcher_PlayBannerlord = GetOrDefault(translations, nameof(Launcher_PlayBannerlord), DefaultLauncher_PlayBannerlord);
			Launcher_PlayWithBLSE = GetOrDefault(translations, nameof(Launcher_PlayWithBLSE), DefaultLauncher_PlayWithBLSE);
			Launcher_LaunchTargetTooltip = GetOrDefault(translations, nameof(Launcher_LaunchTargetTooltip), DefaultLauncher_LaunchTargetTooltip);
			Launcher_LaunchTargetBannerlord = GetOrDefault(translations, nameof(Launcher_LaunchTargetBannerlord), DefaultLauncher_LaunchTargetBannerlord);
			Launcher_LaunchTargetBLSE = GetOrDefault(translations, nameof(Launcher_LaunchTargetBLSE), DefaultLauncher_LaunchTargetBLSE);

			// LauncherPage — Status Messages
			Launcher_SelectModpackPrompt = GetOrDefault(translations, nameof(Launcher_SelectModpackPrompt), DefaultLauncher_SelectModpackPrompt);
			Launcher_InstallInProgress = GetOrDefault(translations, nameof(Launcher_InstallInProgress), DefaultLauncher_InstallInProgress);
			Launcher_Launching = GetOrDefault(translations, nameof(Launcher_Launching), DefaultLauncher_Launching);
			Launcher_ScanningForMods = GetOrDefault(translations, nameof(Launcher_ScanningForMods), DefaultLauncher_ScanningForMods);
			Launcher_NoModsFound = GetOrDefault(translations, nameof(Launcher_NoModsFound), DefaultLauncher_NoModsFound);
			Launcher_InstallDialogTitle = GetOrDefault(translations, nameof(Launcher_InstallDialogTitle), DefaultLauncher_InstallDialogTitle);

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
			Settings_WorkshopFolderValid = GetOrDefault(translations, nameof(Settings_WorkshopFolderValid), DefaultSettings_WorkshopFolderValid);
			Settings_WorkshopFolderInvalid = GetOrDefault(translations, nameof(Settings_WorkshopFolderInvalid), DefaultSettings_WorkshopFolderInvalid);
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

			// EULA Window
			Eula_WindowTitle = GetOrDefault(translations, nameof(Eula_WindowTitle), DefaultEula_WindowTitle);
			Eula_CloseTooltip = GetOrDefault(translations, nameof(Eula_CloseTooltip), DefaultEula_CloseTooltip);
			Eula_Header = GetOrDefault(translations, nameof(Eula_Header), DefaultEula_Header);
			Eula_VersionLabel = GetOrDefault(translations, nameof(Eula_VersionLabel), DefaultEula_VersionLabel);
			Eula_AcceptanceText = GetOrDefault(translations, nameof(Eula_AcceptanceText), DefaultEula_AcceptanceText);
			Eula_DeclineButton = GetOrDefault(translations, nameof(Eula_DeclineButton), DefaultEula_DeclineButton);
			Eula_AcceptButton = GetOrDefault(translations, nameof(Eula_AcceptButton), DefaultEula_AcceptButton);

			// Confirmation Dialog
			ConfirmDialogShutdownPrimaryButtonString = GetOrDefault(translations, nameof(ConfirmDialogShutdownPrimaryButtonString), DefaultConfirmDialogShutdownPrimaryButtonString);
			ConfirmDialogRestartPrimaryButtonString = GetOrDefault(translations, nameof(ConfirmDialogRestartPrimaryButtonString), DefaultConfirmDialogRestartPrimaryButtonString);
			ConfirmDialogSecondaryButtonString = GetOrDefault(translations, nameof(ConfirmDialogSecondaryButtonString), DefaultConfirmDialogSecondaryButtonString);
			ConfirmDialogDSPrimaryButtonString = GetOrDefault(translations, nameof(ConfirmDialogDSPrimaryButtonString), DefaultConfirmDialogDSPrimaryButtonString);
			ConfirmDialogDSSecondaryButtonString = GetOrDefault(translations, nameof(ConfirmDialogDSSecondaryButtonString), DefaultConfirmDialogDSSecondaryButtonString);
			ConfirmDialogShutdownTitleString = GetOrDefault(translations, nameof(ConfirmDialogShutdownTitleString), DefaultConfirmDialogShutdownTitleString);
			ConfirmDialogRestartTitleString = GetOrDefault(translations, nameof(ConfirmDialogRestartTitleString), DefaultConfirmDialogRestartTitleString);
			ConfirmDialogDSTitleString = GetOrDefault(translations, nameof(ConfirmDialogDSTitleString), DefaultConfirmDialogDSTitleString);
			ConfirmDialogShutdownMessageString = GetOrDefault(translations, nameof(ConfirmDialogShutdownMessageString), DefaultConfirmDialogShutdownMessageString);
			ConfirmDialogRestartMessageString = GetOrDefault(translations, nameof(ConfirmDialogRestartMessageString), DefaultConfirmDialogRestartMessageString);
			ConfirmDialogDSMessageString = GetOrDefault(translations, nameof(ConfirmDialogDSMessageString), DefaultConfirmDialogDSMessageString);
			ConfirmDialogShutdownActiveOperationsMessageString = GetOrDefault(translations, nameof(ConfirmDialogShutdownActiveOperationsMessageString), DefaultConfirmDialogShutdownActiveOperationsMessageString);
			ConfirmDialogRestartActiveOperationsMessageString = GetOrDefault(translations, nameof(ConfirmDialogRestartActiveOperationsMessageString), DefaultConfirmDialogRestartActiveOperationsMessageString);
			ConfirmDialogActiveScanOperationString = GetOrDefault(translations, nameof(ConfirmDialogActiveScanOperationString), DefaultConfirmDialogActiveScanOperationString);
			ConfirmDialogActiveInstallOperationString = GetOrDefault(translations, nameof(ConfirmDialogActiveInstallOperationString), DefaultConfirmDialogActiveInstallOperationString);

			// Toast Messages
			Toast_InstallInProgress = GetOrDefault(translations, nameof(Toast_InstallInProgress), DefaultToast_InstallInProgress);
			Toast_InstallComplete = GetOrDefault(translations, nameof(Toast_InstallComplete), DefaultToast_InstallComplete);
			Toast_InstallCompleteWithErrors = GetOrDefault(translations, nameof(Toast_InstallCompleteWithErrors), DefaultToast_InstallCompleteWithErrors);
			Toast_InstallingMods = GetOrDefault(translations, nameof(Toast_InstallingMods), DefaultToast_InstallingMods);
			Toast_InstallSucceededWithWarnings = GetOrDefault(translations, nameof(Toast_InstallSucceededWithWarnings), DefaultToast_InstallSucceededWithWarnings);
			Toast_InstallCancelled = GetOrDefault(translations, nameof(Toast_InstallCancelled), DefaultToast_InstallCancelled);
			Toast_InstallRejectedBusy = GetOrDefault(translations, nameof(Toast_InstallRejectedBusy), DefaultToast_InstallRejectedBusy);
			Toast_InstallRejectedAdmissionStopped = GetOrDefault(translations, nameof(Toast_InstallRejectedAdmissionStopped), DefaultToast_InstallRejectedAdmissionStopped);
			Toast_InstallValidationFailed = GetOrDefault(translations, nameof(Toast_InstallValidationFailed), DefaultToast_InstallValidationFailed);
			Toast_InstallValidationArchiveSelectionEmpty = GetOrDefault(translations, nameof(Toast_InstallValidationArchiveSelectionEmpty), DefaultToast_InstallValidationArchiveSelectionEmpty);
			Toast_InstallValidationGameDirectoryNotConfigured = GetOrDefault(translations, nameof(Toast_InstallValidationGameDirectoryNotConfigured), DefaultToast_InstallValidationGameDirectoryNotConfigured);
			Toast_InstallValidationGameDirectoryMissing = GetOrDefault(translations, nameof(Toast_InstallValidationGameDirectoryMissing), DefaultToast_InstallValidationGameDirectoryMissing);
			Toast_InstallValidationModulesDirectoryMissing = GetOrDefault(translations, nameof(Toast_InstallValidationModulesDirectoryMissing), DefaultToast_InstallValidationModulesDirectoryMissing);
			Toast_InstallDiagnosticGeneric = GetOrDefault(translations, nameof(Toast_InstallDiagnosticGeneric), DefaultToast_InstallDiagnosticGeneric);
			Toast_InstallFailed = GetOrDefault(translations, nameof(Toast_InstallFailed), DefaultToast_InstallFailed);
			Toast_InstallProgressFormat = GetOrDefault(translations, nameof(Toast_InstallProgressFormat), DefaultToast_InstallProgressFormat);
			Toast_InstallSummaryFormat = GetOrDefault(translations, nameof(Toast_InstallSummaryFormat), DefaultToast_InstallSummaryFormat);
			Toast_InstallCancelledWithChanges = GetOrDefault(translations, nameof(Toast_InstallCancelledWithChanges), DefaultToast_InstallCancelledWithChanges);
			Toast_InstallCancelledWithoutChanges = GetOrDefault(translations, nameof(Toast_InstallCancelledWithoutChanges), DefaultToast_InstallCancelledWithoutChanges);
			Toast_InstallReconciliationFailed = GetOrDefault(translations, nameof(Toast_InstallReconciliationFailed), DefaultToast_InstallReconciliationFailed);
			Toast_InstallBlseSucceeded = GetOrDefault(translations, nameof(Toast_InstallBlseSucceeded), DefaultToast_InstallBlseSucceeded);
			Toast_InstallBlseFailed = GetOrDefault(translations, nameof(Toast_InstallBlseFailed), DefaultToast_InstallBlseFailed);
			Toast_InstallUnblockWarningFormat = GetOrDefault(translations, nameof(Toast_InstallUnblockWarningFormat), DefaultToast_InstallUnblockWarningFormat);
			Toast_InstallUnblockFailed = GetOrDefault(translations, nameof(Toast_InstallUnblockFailed), DefaultToast_InstallUnblockFailed);
			Toast_InstallReconciliationWarnings = GetOrDefault(translations, nameof(Toast_InstallReconciliationWarnings), DefaultToast_InstallReconciliationWarnings);
			Toast_GameLaunched = GetOrDefault(translations, nameof(Toast_GameLaunched), DefaultToast_GameLaunched);
			Toast_LaunchFailed = GetOrDefault(translations, nameof(Toast_LaunchFailed), DefaultToast_LaunchFailed);
			Toast_MissingMods = GetOrDefault(translations, nameof(Toast_MissingMods), DefaultToast_MissingMods);
			Toast_ModListUpdated = GetOrDefault(translations, nameof(Toast_ModListUpdated), DefaultToast_ModListUpdated);
			Toast_RefreshFailed = GetOrDefault(translations, nameof(Toast_RefreshFailed), DefaultToast_RefreshFailed);
			Toast_NoModsFound = GetOrDefault(translations, nameof(Toast_NoModsFound), DefaultToast_NoModsFound);
			Toast_AutoScanFailed = GetOrDefault(translations, nameof(Toast_AutoScanFailed), DefaultToast_AutoScanFailed);
			Toast_SteamWorkshopNotFoundTitle = GetOrDefault(translations, nameof(Toast_SteamWorkshopNotFoundTitle), DefaultToast_SteamWorkshopNotFoundTitle);
			Toast_SteamWorkshopNotFoundMessage = GetOrDefault(translations, nameof(Toast_SteamWorkshopNotFoundMessage), DefaultToast_SteamWorkshopNotFoundMessage);
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
			return translations.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
		}


		#region Navigation
		public string Nav_LauncherTab { get; private set; } = DefaultNav_LauncherTab;
		private const string DefaultNav_LauncherTab = "Home";
		public string Nav_ModpacksTab { get; private set; } = DefaultNav_ModpacksTab;
		private const string DefaultNav_ModpacksTab = "Mod Packs";
		public string Nav_FaqTab { get; private set; } = DefaultNav_FaqTab;
		private const string DefaultNav_FaqTab = "FAQ";
		public string Nav_SettingsTab { get; private set; } = DefaultNav_SettingsTab;
		private const string DefaultNav_SettingsTab = "Settings";
		#endregion

		#region LauncherPage — Header
		public string Launcher_ModpacksLabel { get; private set; } = DefaultLauncher_ModpacksLabel;
		private const string DefaultLauncher_ModpacksLabel = "Mod Packs:";
		public string Launcher_GhostModpackLabel { get; private set; } = DefaultLauncher_GhostModpackLabel;
		private const string DefaultLauncher_GhostModpackLabel = "— Select a modpack —";
		public string Launcher_InstallButton { get; private set; } = DefaultLauncher_InstallButton;
		private const string DefaultLauncher_InstallButton = "Install Mods";
		public string Launcher_LoadOrderHeader { get; private set; } = DefaultLauncher_LoadOrderHeader;
		private const string DefaultLauncher_LoadOrderHeader = "Load Order";
		public string Launcher_AvailableModsHeader { get; private set; } = DefaultLauncher_AvailableModsHeader;
		private const string DefaultLauncher_AvailableModsHeader = "Available Mods";
		public string Launcher_RefreshTooltip { get; private set; } = DefaultLauncher_RefreshTooltip;
		private const string DefaultLauncher_RefreshTooltip = "Refresh Mods";
		#endregion

		#region LauncherPage — Play Button
		public string Launcher_PlayBannerlord { get; private set; } = DefaultLauncher_PlayBannerlord;
		private const string DefaultLauncher_PlayBannerlord = "Play Bannerlord";
		public string Launcher_PlayWithBLSE { get; private set; } = DefaultLauncher_PlayWithBLSE;
		private const string DefaultLauncher_PlayWithBLSE = "Play with BLSE";
		public string Launcher_LaunchTargetTooltip { get; private set; } = DefaultLauncher_LaunchTargetTooltip;
		private const string DefaultLauncher_LaunchTargetTooltip = "Choose launch target";
		public string Launcher_LaunchTargetBannerlord { get; private set; } = DefaultLauncher_LaunchTargetBannerlord;
		private const string DefaultLauncher_LaunchTargetBannerlord = "Bannerlord";
		public string Launcher_LaunchTargetBLSE { get; private set; } = DefaultLauncher_LaunchTargetBLSE;
		private const string DefaultLauncher_LaunchTargetBLSE = "BLSE";
		#endregion

		#region LauncherPage — Status Messages
		public string Launcher_SelectModpackPrompt { get; private set; } = DefaultLauncher_SelectModpackPrompt;
		private const string DefaultLauncher_SelectModpackPrompt = "No modpack selected. Choose one from the dropdown above.";
		public string Launcher_InstallInProgress { get; private set; } = DefaultLauncher_InstallInProgress;
		private const string DefaultLauncher_InstallInProgress = "Installation in progress...";
		public string Launcher_Launching { get; private set; } = DefaultLauncher_Launching;
		private const string DefaultLauncher_Launching = "Launching...";
		public string Launcher_ScanningForMods { get; private set; } = DefaultLauncher_ScanningForMods;
		private const string DefaultLauncher_ScanningForMods = "Scanning for mods...";
		public string Launcher_NoModsFound { get; private set; } = DefaultLauncher_NoModsFound;
		private const string DefaultLauncher_NoModsFound = "No mods found. Install mods or check your game path in Settings.";
		public string Launcher_InstallDialogTitle { get; private set; } = DefaultLauncher_InstallDialogTitle;
		private const string DefaultLauncher_InstallDialogTitle = "Select Mod Archives to Install";
		#endregion

		#region ModpacksPage — Header
		public string Modpacks_HeaderLabel { get; private set; } = DefaultModpacks_HeaderLabel;
		private const string DefaultModpacks_HeaderLabel = "Mod Packs:";
		public string Modpacks_CreateNewButton { get; private set; } = DefaultModpacks_CreateNewButton;
		private const string DefaultModpacks_CreateNewButton = "Create New";
		public string Modpacks_ImportButton { get; private set; } = DefaultModpacks_ImportButton;
		private const string DefaultModpacks_ImportButton = "Import";
		public string Modpacks_SaveButton { get; private set; } = DefaultModpacks_SaveButton;
		private const string DefaultModpacks_SaveButton = "Save to Modpack";
		#endregion

		#region ModpacksPage — Lists
		public string Modpacks_ActiveLoadOrderHeader { get; private set; } = DefaultModpacks_ActiveLoadOrderHeader;
		private const string DefaultModpacks_ActiveLoadOrderHeader = "Active Load Order";
		public string Modpacks_SavedDataHeader { get; private set; } = DefaultModpacks_SavedDataHeader;
		private const string DefaultModpacks_SavedDataHeader = "Saved Modpack Data";
		public string Modpacks_ByLabel { get; private set; } = DefaultModpacks_ByLabel;
		private const string DefaultModpacks_ByLabel = "By:";
		public string Modpacks_UpdatedLabel { get; private set; } = DefaultModpacks_UpdatedLabel;
		private const string DefaultModpacks_UpdatedLabel = "Updated:";
		#endregion

		#region ModpacksPage — Edit Panel
		public string Modpacks_ModuleIdLabel { get; private set; } = DefaultModpacks_ModuleIdLabel;
		private const string DefaultModpacks_ModuleIdLabel = "Module ID:";
		public string Modpacks_VersionLabel { get; private set; } = DefaultModpacks_VersionLabel;
		private const string DefaultModpacks_VersionLabel = "Version:";
		public string Modpacks_NameLabel { get; private set; } = DefaultModpacks_NameLabel;
		private const string DefaultModpacks_NameLabel = "Name:";
		public string Modpacks_UrlLabel { get; private set; } = DefaultModpacks_UrlLabel;
		private const string DefaultModpacks_UrlLabel = "URL:";
		public string Modpacks_SaveEntryButton { get; private set; } = DefaultModpacks_SaveEntryButton;
		private const string DefaultModpacks_SaveEntryButton = "Save Entry";
		#endregion

		#region ModpacksPage — Create Panel
		public string Modpacks_CreateNameLabel { get; private set; } = DefaultModpacks_CreateNameLabel;
		private const string DefaultModpacks_CreateNameLabel = "Name:";
		public string Modpacks_CreatedByLabel { get; private set; } = DefaultModpacks_CreatedByLabel;
		private const string DefaultModpacks_CreatedByLabel = "Created By:";
		public string Modpacks_ConfirmButton { get; private set; } = DefaultModpacks_ConfirmButton;
		private const string DefaultModpacks_ConfirmButton = "Confirm";
		public string Modpacks_CancelButton { get; private set; } = DefaultModpacks_CancelButton;
		private const string DefaultModpacks_CancelButton = "Cancel";
		#endregion

		#region ModpacksPage — Template Names
		public string Modpacks_TemplateVanilla { get; private set; } = DefaultModpacks_TemplateVanilla;
		private const string DefaultModpacks_TemplateVanilla = "Default Modpack";
		public string Modpacks_TemplateButterLib { get; private set; } = DefaultModpacks_TemplateButterLib;
		private const string DefaultModpacks_TemplateButterLib = "ButterLib Suite";
		public string Modpacks_TemplateVanillaWarSails { get; private set; } = DefaultModpacks_TemplateVanillaWarSails;
		private const string DefaultModpacks_TemplateVanillaWarSails = "Vanilla - WarSails";
		public string Modpacks_TemplateButterLibWarSails { get; private set; } = DefaultModpacks_TemplateButterLibWarSails;
		private const string DefaultModpacks_TemplateButterLibWarSails = "ButterLib - WarSails";
		#endregion

		#region Settings — Nav Tabs
		public string Settings_GeneralTab { get; private set; } = DefaultSettings_GeneralTab;
		private const string DefaultSettings_GeneralTab = "General";
		public string Settings_GameConfigTab { get; private set; } = DefaultSettings_GameConfigTab;
		private const string DefaultSettings_GameConfigTab = "Game Config";
		public string Settings_ToolsTab { get; private set; } = DefaultSettings_ToolsTab;
		private const string DefaultSettings_ToolsTab = "Tools";
		public string Settings_WipTab { get; private set; } = DefaultSettings_WipTab;
		private const string DefaultSettings_WipTab = "WIP";
		public string Settings_AboutTab { get; private set; } = DefaultSettings_AboutTab;
		private const string DefaultSettings_AboutTab = "About";
		#endregion

		#region Settings — General
		public string Settings_LanguageHeader { get; private set; } = DefaultSettings_LanguageHeader;
		private const string DefaultSettings_LanguageHeader = "Language";
		public string Settings_LanguageLabel { get; private set; } = DefaultSettings_LanguageLabel;
		private const string DefaultSettings_LanguageLabel = "Language";
		public string Settings_LanguageHint { get; private set; } = DefaultSettings_LanguageHint;
		private const string DefaultSettings_LanguageHint = "Select the display language for CalradiaForge.";
		public string Settings_StartupHeader { get; private set; } = DefaultSettings_StartupHeader;
		private const string DefaultSettings_StartupHeader = "Startup Behavior";
		public string Settings_StartupLabel { get; private set; } = DefaultSettings_StartupLabel;
		private const string DefaultSettings_StartupLabel = "On startup, select modpack:";
		public string Settings_StartupLastUsed { get; private set; } = DefaultSettings_StartupLastUsed;
		private const string DefaultSettings_StartupLastUsed = "Last Used Modpack";
		public string Settings_StartupAlwaysDefault { get; private set; } = DefaultSettings_StartupAlwaysDefault;
		private const string DefaultSettings_StartupAlwaysDefault = "Always Use Default (Vanilla)";
		public string Settings_StartupAlwaysAsk { get; private set; } = DefaultSettings_StartupAlwaysAsk;
		private const string DefaultSettings_StartupAlwaysAsk = "Always Ask";
		public string Settings_StartupHint { get; private set; } = DefaultSettings_StartupHint;
		private const string DefaultSettings_StartupHint = "Controls which modpack is automatically selected when CalradiaForge starts.";
		public string Settings_DiagnosticsHeader { get; private set; } = DefaultSettings_DiagnosticsHeader;
		private const string DefaultSettings_DiagnosticsHeader = "Diagnostics";
		public string Settings_DebugModeLabel { get; private set; } = DefaultSettings_DebugModeLabel;
		private const string DefaultSettings_DebugModeLabel = "Enable Debug Mode";
		public string Settings_DebugModeHint { get; private set; } = DefaultSettings_DebugModeHint;
		private const string DefaultSettings_DebugModeHint = "Enables verbose logging and diagnostic data output. Only enable this when requested for troubleshooting.";
		#endregion

		#region Settings — Game Config
		public string Settings_GameInstallHeader { get; private set; } = DefaultSettings_GameInstallHeader;
		private const string DefaultSettings_GameInstallHeader = "Game Installation";
		public string Settings_GameFolderLabel { get; private set; } = DefaultSettings_GameFolderLabel;
		private const string DefaultSettings_GameFolderLabel = "Game Folder";
		public string Settings_SelectFolderButton { get; private set; } = DefaultSettings_SelectFolderButton;
		private const string DefaultSettings_SelectFolderButton = "Select Folder";
		public string Settings_GameExeLabel { get; private set; } = DefaultSettings_GameExeLabel;
		private const string DefaultSettings_GameExeLabel = "Game Executable";
		public string Settings_SelectFileButton { get; private set; } = DefaultSettings_SelectFileButton;
		private const string DefaultSettings_SelectFileButton = "Select File";
		public string Settings_WorkshopFolderLabel { get; private set; } = DefaultSettings_WorkshopFolderLabel;
		private const string DefaultSettings_WorkshopFolderLabel = "Steam Workshop Folder";
		public string Settings_WorkshopFolderValid { get; private set; } = DefaultSettings_WorkshopFolderValid;
		private const string DefaultSettings_WorkshopFolderValid = "✓ Valid Steam Workshop folder found.";
		public string Settings_WorkshopFolderInvalid { get; private set; } = DefaultSettings_WorkshopFolderInvalid;
		private const string DefaultSettings_WorkshopFolderInvalid = "✗ Steam Workshop folder not found.";
		public string Settings_BLSEHeader { get; private set; } = DefaultSettings_BLSEHeader;
		private const string DefaultSettings_BLSEHeader = "Script Extender (BLSE)";
		public string Settings_BLSEExeLabel { get; private set; } = DefaultSettings_BLSEExeLabel;
		private const string DefaultSettings_BLSEExeLabel = "BLSE Standalone Executable";
		public string Settings_BLSEHint { get; private set; } = DefaultSettings_BLSEHint;
		private const string DefaultSettings_BLSEHint = "The Bannerlord Software Extender enables advanced script mods. If installed, CalradiaForge auto-detects it in the game's bin folder. You can also select it manually.";
		public string Settings_DetectionHeader { get; private set; } = DefaultSettings_DetectionHeader;
		private const string DefaultSettings_DetectionHeader = "Detection";
		public string Settings_DetectGameButton { get; private set; } = DefaultSettings_DetectGameButton;
		private const string DefaultSettings_DetectGameButton = "\U0001f504  Detect Game";
		public string Settings_DetectGameHint { get; private set; } = DefaultSettings_DetectGameHint;
		private const string DefaultSettings_DetectGameHint = "Re-runs supported automatic game detection and detects BLSE when installed. Overwrites current paths; select the game folder manually if detection fails.";
		#endregion

		#region Settings — Tools
		public string Settings_ModMaintenanceHeader { get; private set; } = DefaultSettings_ModMaintenanceHeader;
		private const string DefaultSettings_ModMaintenanceHeader = "Mod Maintenance";
		public string Settings_UnblockDllsButton { get; private set; } = DefaultSettings_UnblockDllsButton;
		private const string DefaultSettings_UnblockDllsButton = "\U0001f513  Unblock DLLs";
		public string Settings_UnblockDllsHint { get; private set; } = DefaultSettings_UnblockDllsHint;
		private const string DefaultSettings_UnblockDllsHint = "Removes the Zone.Identifier alternate data stream from all DLL files in the Modules folder. Required for mods downloaded from the internet.";
		public string Settings_DataManagementHeader { get; private set; } = DefaultSettings_DataManagementHeader;
		private const string DefaultSettings_DataManagementHeader = "Data Management";
		public string Settings_ClearCacheButton { get; private set; } = DefaultSettings_ClearCacheButton;
		private const string DefaultSettings_ClearCacheButton = "\U0001f5d1  Clear Mod Cache";
		public string Settings_ClearCacheHint { get; private set; } = DefaultSettings_ClearCacheHint;
		private const string DefaultSettings_ClearCacheHint = "Deletes the cached mod list and forces a fresh rescan on next launch.";
		public string Settings_OpenConfigButton { get; private set; } = DefaultSettings_OpenConfigButton;
		private const string DefaultSettings_OpenConfigButton = "\U0001f4c2  Open Config Folder";
		public string Settings_OpenConfigHint { get; private set; } = DefaultSettings_OpenConfigHint;
		private const string DefaultSettings_OpenConfigHint = "Opens the CalradiaForge Config directory in Windows Explorer.";
		public string Settings_OpenLogsButton { get; private set; } = DefaultSettings_OpenLogsButton;
		private const string DefaultSettings_OpenLogsButton = "\U0001f4c2  Open Logs Folder";
		public string Settings_OpenLogsHint { get; private set; } = DefaultSettings_OpenLogsHint;
		private const string DefaultSettings_OpenLogsHint = "Opens the Logs directory in Windows Explorer.";
		public string Settings_OpenModpacksButton { get; private set; } = DefaultSettings_OpenModpacksButton;
		private const string DefaultSettings_OpenModpacksButton = "\U0001f4c2  Open Modpacks Folder";
		public string Settings_OpenModpacksHint { get; private set; } = DefaultSettings_OpenModpacksHint;
		private const string DefaultSettings_OpenModpacksHint = "Opens the Modpacks directory in Windows Explorer.";
		#endregion

		#region Settings — WIP
		public string Settings_WipTitle { get; private set; } = DefaultSettings_WipTitle;
		private const string DefaultSettings_WipTitle = "This tab is reserved for future development.";
		public string Settings_WipDescription { get; private set; } = DefaultSettings_WipDescription;
		private const string DefaultSettings_WipDescription = "If there is enough community interest in advanced features\nand functionality beyond the current project scope,\nfuture settings will appear here.";
		public string Settings_WipStayTuned { get; private set; } = DefaultSettings_WipStayTuned;
		private const string DefaultSettings_WipStayTuned = "Stay tuned for updates!";
		#endregion

		#region Settings — About
		public string Settings_AboutDescription { get; private set; } = DefaultSettings_AboutDescription;
		private const string DefaultSettings_AboutDescription = "A modern mod launcher for Mount & Blade II: Bannerlord.";
		public string Settings_AboutTagline { get; private set; } = DefaultSettings_AboutTagline;
		private const string DefaultSettings_AboutTagline = "Built by players, for players.";
		public string Settings_AboutPublisherLabel { get; private set; } = DefaultSettings_AboutPublisherLabel;
		private const string DefaultSettings_AboutPublisherLabel = "Publisher";
		public string Settings_AboutLicenseLabel { get; private set; } = DefaultSettings_AboutLicenseLabel;
		private const string DefaultSettings_AboutLicenseLabel = "License";
		public string Settings_AboutViewLicense { get; private set; } = DefaultSettings_AboutViewLicense;
		private const string DefaultSettings_AboutViewLicense = "View License";
		public string Settings_AboutGitHubLabel { get; private set; } = DefaultSettings_AboutGitHubLabel;
		private const string DefaultSettings_AboutGitHubLabel = "GitHub";
		public string Settings_AboutViewGitHub { get; private set; } = DefaultSettings_AboutViewGitHub;
		private const string DefaultSettings_AboutViewGitHub = "View on GitHub";
		public string Settings_AboutCopyright { get; private set; } = DefaultSettings_AboutCopyright;
		private const string DefaultSettings_AboutCopyright = "\u00a9 2026 ThelianTech\u2122 \u2014 All rights reserved.";
		public string Settings_AboutDisclaimer { get; private set; } = DefaultSettings_AboutDisclaimer;
		private const string DefaultSettings_AboutDisclaimer = "Not affiliated with TaleWorlds Entertainment.";
		#endregion

		#region FAQ Page
		public string Faq_PageTitle { get; private set; } = DefaultFaq_PageTitle;
		private const string DefaultFaq_PageTitle = "Frequently Asked Questions";
		public string Faq_Q1_Title { get; private set; } = DefaultFaq_Q1_Title;
		private const string DefaultFaq_Q1_Title = "Why isn't the game detecting my mods?";
		public string Faq_Q1_Answer1 { get; private set; } = DefaultFaq_Q1_Answer1;
		private const string DefaultFaq_Q1_Answer1 = "Make sure your mods are in the Active load order on the Home page. Only mods listed in the Load Order panel are passed to the game at launch.";
		public string Faq_Q1_Answer2 { get; private set; } = DefaultFaq_Q1_Answer2;
		private const string DefaultFaq_Q1_Answer2 = "Verify that your game installation path is set correctly under Settings \u2192 Game Config. Use the Re-detect button if you're unsure.";
		public string Faq_Q1_Hint { get; private set; } = DefaultFaq_Q1_Hint;
		private const string DefaultFaq_Q1_Hint = "Tip: If mods were downloaded from the internet, Windows may block their DLLs. Go to Settings \u2192 Tools \u2192 Unblock DLLs to fix this.";
		public string Faq_Q2_Title { get; private set; } = DefaultFaq_Q2_Title;
		private const string DefaultFaq_Q2_Title = "What does 'Unblock DLLs' do?";
		public string Faq_Q2_Answer1 { get; private set; } = DefaultFaq_Q2_Answer1;
		private const string DefaultFaq_Q2_Answer1 = "When you download files from the internet, Windows adds a hidden security marker (Zone.Identifier) to them. This can prevent Bannerlord from loading mod DLLs.";
		public string Faq_Q2_Answer2 { get; private set; } = DefaultFaq_Q2_Answer2;
		private const string DefaultFaq_Q2_Answer2 = "The Unblock DLLs tool in Settings \u2192 Tools removes this marker from all DLL files in your Modules folder. CalradiaForge also does this automatically when you install mods through the app.";
		public string Faq_Q3_Title { get; private set; } = DefaultFaq_Q3_Title;
		private const string DefaultFaq_Q3_Title = "I'm getting errors when installing mods from an archive.";
		public string Faq_Q3_Answer1 { get; private set; } = DefaultFaq_Q3_Answer1;
		private const string DefaultFaq_Q3_Answer1 = "CalradiaForge extracts mod archives directly into the Bannerlord Modules folder. Make sure the archive contains a valid mod structure with a SubModule.xml file.";
		public string Faq_Q3_Answer2 { get; private set; } = DefaultFaq_Q3_Answer2;
		private const string DefaultFaq_Q3_Answer2 = "If the archive is nested (a folder inside a folder), CalradiaForge will attempt to detect the correct root. If extraction still fails, try extracting the mod manually.";
		public string Faq_Q3_Hint { get; private set; } = DefaultFaq_Q3_Hint;
		private const string DefaultFaq_Q3_Hint = "Supported archive formats: .zip, .rar and .7z";
		public string Faq_Q4_Title { get; private set; } = DefaultFaq_Q4_Title;
		private const string DefaultFaq_Q4_Title = "How do modpacks work?";
		public string Faq_Q4_Answer1 { get; private set; } = DefaultFaq_Q4_Answer1;
		private const string DefaultFaq_Q4_Answer1 = "A modpack is a saved snapshot of your active mods and their load order. You can create multiple modpacks for different playstyles (e.g., Vanilla+, Overhaul, Hardcore).";
		public string Faq_Q4_Answer2 { get; private set; } = DefaultFaq_Q4_Answer2;
		private const string DefaultFaq_Q4_Answer2 = "Switch between modpacks using the dropdown on the Home page. The selected modpack's load order is applied immediately.";
		public string Faq_Q4_Answer3 { get; private set; } = DefaultFaq_Q4_Answer3;
		private const string DefaultFaq_Q4_Answer3 = "To save changes to a modpack, go to the Mod Packs page and click 'Save to Modpack'. This overwrites the saved data with your current active load order.";
		public string Faq_Q4_Hint { get; private set; } = DefaultFaq_Q4_Hint;
		private const string DefaultFaq_Q4_Hint = "Make sure you have selected the correct modpack on the Home page before you add/remove mods to your load order there, before you go and save any changes in the Modpacks Page else any changes will be lost.";
		public string Faq_Q5_Title { get; private set; } = DefaultFaq_Q5_Title;
		private const string DefaultFaq_Q5_Title = "Why can't I launch the game from CalradiaForge on Epic or GamePass?";
		public string Faq_Q5_Answer1 { get; private set; } = DefaultFaq_Q5_Answer1;
		private const string DefaultFaq_Q5_Answer1 = "TaleWorlds requires authentication through the Epic or Xbox client. This is a platform-level restriction that third-party launchers cannot bypass.";
		public string Faq_Q5_Answer2 { get; private set; } = DefaultFaq_Q5_Answer2;
		private const string DefaultFaq_Q5_Answer2 = "CalradiaForge still provides full mod management, load order arrangement, and modpack features for these platforms \u2014 you just need to press Play from the platform's own launcher.";
		public string Faq_Q5_Hint { get; private set; } = DefaultFaq_Q5_Hint;
		private const string DefaultFaq_Q5_Hint = "Direct launch support for Epic and GamePass is planned for a future release. For now load orders are not passed to the game's native launcher.";
		public string Faq_Q6_Title { get; private set; } = DefaultFaq_Q6_Title;
		private const string DefaultFaq_Q6_Title = "Can I import presets from Novus Launcher?";
		public string Faq_Q6_Answer1 { get; private set; } = DefaultFaq_Q6_Answer1;
		private const string DefaultFaq_Q6_Answer1 = "Yes. Go to the Mod Packs page and click Import. Select a Novus Launcher preset file (.xml) and CalradiaForge will convert it into a CalradiaForge modpack automatically.";
		public string Faq_Q6_Answer2 { get; private set; } = DefaultFaq_Q6_Answer2;
		private const string DefaultFaq_Q6_Answer2 = "You can also import native CalradiaForge modpack files (.json) the same way.";
		public string Faq_Q6_Answer3 { get; private set; } = DefaultFaq_Q6_Answer3;
		private const string DefaultFaq_Q6_Answer3 = "You can also place CalradiaForge Modpacks directly in the modpacks subdirectory in the app's directory, and the app will automatically refresh the selectable modpacks.";
		public string Faq_Q6_Hint { get; private set; } = DefaultFaq_Q6_Hint;
		private const string DefaultFaq_Q6_Hint = "Refresh or switch pages for the app to automatically refresh the dropdown menus'.";
		public string Faq_Q7_Title { get; private set; } = DefaultFaq_Q7_Title;
		private const string DefaultFaq_Q7_Title = "When should I clear the mod cache?";
		public string Faq_Q7_Answer1 { get; private set; } = DefaultFaq_Q7_Answer1;
		private const string DefaultFaq_Q7_Answer1 = "Clear the mod cache if you've manually added or removed mods from the Modules folder outside of CalradiaForge, or if the mod list appears stale or incorrect.";
		public string Faq_Q7_Answer2 { get; private set; } = DefaultFaq_Q7_Answer2;
		private const string DefaultFaq_Q7_Answer2 = "Go to Settings \u2192 Tools \u2192 Clear Mod Cache. On next launch, CalradiaForge will rescan the Modules folder and rebuild the cache.";
		public string Faq_Q8_Title { get; private set; } = DefaultFaq_Q8_Title;
		private const string DefaultFaq_Q8_Title = "How do I report a bug or request a feature?";
		public string Faq_Q8_Answer1 { get; private set; } = DefaultFaq_Q8_Answer1;
		private const string DefaultFaq_Q8_Answer1 = "Open an issue on the CalradiaForge GitHub repository. Include a short description of the problem, steps to reproduce it, and any relevant log output.";
		public string Faq_Q8_OpenIssuesButton { get; private set; } = DefaultFaq_Q8_OpenIssuesButton;
		private const string DefaultFaq_Q8_OpenIssuesButton = "Open GitHub Issues";
		public string Faq_Q8_OpenIssuesHint { get; private set; } = DefaultFaq_Q8_OpenIssuesHint;
		private const string DefaultFaq_Q8_OpenIssuesHint = "Opens the CalradiaForge Issues page on GitHub in your browser.";
		public string Faq_Q8_Hint { get; private set; } = DefaultFaq_Q8_Hint;
		private const string DefaultFaq_Q8_Hint = "Tip: Enable Debug Mode in Settings \u2192 General before reproducing the issue. This gives more detailed logs for troubleshooting.";
		#endregion

		#region EULA Window
		public string Eula_WindowTitle { get; private set; } = DefaultEula_WindowTitle;
		private const string DefaultEula_WindowTitle = "CalradiaForge - End User License Agreement";

		public string Eula_CloseTooltip { get; private set; } = DefaultEula_CloseTooltip;
		private const string DefaultEula_CloseTooltip = "Close";

		public string Eula_Header { get; private set; } = DefaultEula_Header;
		private const string DefaultEula_Header = "End User License Agreement";

		public string Eula_VersionLabel { get; private set; } = DefaultEula_VersionLabel;
		private const string DefaultEula_VersionLabel = "EULA Version 1.2";

		public string Eula_AcceptanceText { get; private set; } = DefaultEula_AcceptanceText;
		private const string DefaultEula_AcceptanceText = "I have read and agree to the End User License Agreement";

		public string Eula_DeclineButton { get; private set; } = DefaultEula_DeclineButton;
		private const string DefaultEula_DeclineButton = "Decline";

		public string Eula_AcceptButton { get; private set; } = DefaultEula_AcceptButton;
		private const string DefaultEula_AcceptButton = "Accept";
		#endregion

		#region Confirmation Dialog
		public string ConfirmDialogShutdownPrimaryButtonString { get; private set; } = DefaultConfirmDialogShutdownPrimaryButtonString;
		private const string DefaultConfirmDialogShutdownPrimaryButtonString = "Shut Down";

		public string ConfirmDialogRestartPrimaryButtonString { get; private set; } = DefaultConfirmDialogRestartPrimaryButtonString;
		private const string DefaultConfirmDialogRestartPrimaryButtonString = "Restart";

		public string ConfirmDialogSecondaryButtonString { get; private set; } = DefaultConfirmDialogSecondaryButtonString;
		private const string DefaultConfirmDialogSecondaryButtonString = "Cancel";

		public string ConfirmDialogDSPrimaryButtonString { get; private set; } = DefaultConfirmDialogDSPrimaryButtonString;
		private const string DefaultConfirmDialogDSPrimaryButtonString = "Exit Anyway";

		public string ConfirmDialogDSSecondaryButtonString { get; private set; } = DefaultConfirmDialogDSSecondaryButtonString;
		private const string DefaultConfirmDialogDSSecondaryButtonString = "Keep Waiting";

		public string ConfirmDialogShutdownTitleString { get; private set; } = DefaultConfirmDialogShutdownTitleString;
		private const string DefaultConfirmDialogShutdownTitleString = "Shut Down CalradiaForge";

		public string ConfirmDialogRestartTitleString { get; private set; } = DefaultConfirmDialogRestartTitleString;
		private const string DefaultConfirmDialogRestartTitleString = "Restart CalradiaForge";

		public string ConfirmDialogDSTitleString { get; private set; } = DefaultConfirmDialogDSTitleString;
		private const string DefaultConfirmDialogDSTitleString = "CalradiaForge Is Still Shutting Down";

		public string ConfirmDialogShutdownMessageString { get; private set; } = DefaultConfirmDialogShutdownMessageString;
		private const string DefaultConfirmDialogShutdownMessageString = "Are you sure you want to shut down CalradiaForge?";

		public string ConfirmDialogRestartMessageString { get; private set; } = DefaultConfirmDialogRestartMessageString;
		private const string DefaultConfirmDialogRestartMessageString = "Restart CalradiaForge now to apply the requested changes?";

		public string ConfirmDialogDSMessageString { get; private set; } = DefaultConfirmDialogDSMessageString;
		private const string DefaultConfirmDialogDSMessageString = "Active work has not stopped yet. Exit anyway or keep waiting?";

		public string ConfirmDialogShutdownActiveOperationsMessageString { get; private set; } = DefaultConfirmDialogShutdownActiveOperationsMessageString;
		private const string DefaultConfirmDialogShutdownActiveOperationsMessageString = "An operation is still active. Shutting down now will cancel it.";

		public string ConfirmDialogRestartActiveOperationsMessageString { get; private set; } = DefaultConfirmDialogRestartActiveOperationsMessageString;
		private const string DefaultConfirmDialogRestartActiveOperationsMessageString = "An operation is still active. Restarting now will cancel it.";

		public string ConfirmDialogActiveScanOperationString { get; private set; } = DefaultConfirmDialogActiveScanOperationString;
		private const string DefaultConfirmDialogActiveScanOperationString = "A mod scan is currently running.";

		public string ConfirmDialogActiveInstallOperationString { get; private set; } = DefaultConfirmDialogActiveInstallOperationString;
		private const string DefaultConfirmDialogActiveInstallOperationString = "A mod installation is currently running.";
		#endregion

		#region Toast Messages
		public string Toast_InstallInProgress { get; private set; } = DefaultToast_InstallInProgress;
		private const string DefaultToast_InstallInProgress = "Install in Progress";
		public string Toast_InstallComplete { get; private set; } = DefaultToast_InstallComplete;
		private const string DefaultToast_InstallComplete = "Install Complete";
		public string Toast_InstallCompleteWithErrors { get; private set; } = DefaultToast_InstallCompleteWithErrors;
		private const string DefaultToast_InstallCompleteWithErrors = "Install Completed with Errors";
		public string Toast_InstallingMods { get; private set; } = DefaultToast_InstallingMods;
		private const string DefaultToast_InstallingMods = "Installing Mods";
		public string Toast_InstallSucceededWithWarnings { get; private set; } = DefaultToast_InstallSucceededWithWarnings;
		private const string DefaultToast_InstallSucceededWithWarnings = "Install Completed with Warnings";
		public string Toast_InstallCancelled { get; private set; } = DefaultToast_InstallCancelled;
		private const string DefaultToast_InstallCancelled = "Installation Cancelled";
		public string Toast_InstallRejectedBusy { get; private set; } = DefaultToast_InstallRejectedBusy;
		private const string DefaultToast_InstallRejectedBusy = "Another Mod Operation Is Active";
		public string Toast_InstallRejectedAdmissionStopped { get; private set; } = DefaultToast_InstallRejectedAdmissionStopped;
		private const string DefaultToast_InstallRejectedAdmissionStopped = "Mod Operations Are Stopping";
		public string Toast_InstallValidationFailed { get; private set; } = DefaultToast_InstallValidationFailed;
		private const string DefaultToast_InstallValidationFailed = "Installation Cannot Start";
		public string Toast_InstallValidationArchiveSelectionEmpty { get; private set; } = DefaultToast_InstallValidationArchiveSelectionEmpty;
		private const string DefaultToast_InstallValidationArchiveSelectionEmpty = "Select at least one supported mod archive to install.";
		public string Toast_InstallValidationGameDirectoryNotConfigured { get; private set; } = DefaultToast_InstallValidationGameDirectoryNotConfigured;
		private const string DefaultToast_InstallValidationGameDirectoryNotConfigured = "Set the Bannerlord game folder in Settings before installing mods.";
		public string Toast_InstallValidationGameDirectoryMissing { get; private set; } = DefaultToast_InstallValidationGameDirectoryMissing;
		private const string DefaultToast_InstallValidationGameDirectoryMissing = "The configured Bannerlord game folder could not be found. Update it in Settings.";
		public string Toast_InstallValidationModulesDirectoryMissing { get; private set; } = DefaultToast_InstallValidationModulesDirectoryMissing;
		private const string DefaultToast_InstallValidationModulesDirectoryMissing = "The Bannerlord Modules folder could not be found. Verify the game folder in Settings.";
		public string Toast_InstallDiagnosticGeneric { get; private set; } = DefaultToast_InstallDiagnosticGeneric;
		private const string DefaultToast_InstallDiagnosticGeneric = "The installation could not complete normally. Review the selected archives and game folder, then try again.";
		public string Toast_InstallFailed { get; private set; } = DefaultToast_InstallFailed;
		private const string DefaultToast_InstallFailed = "Installation Failed";
		public string Toast_InstallProgressFormat { get; private set; } = DefaultToast_InstallProgressFormat;
		private const string DefaultToast_InstallProgressFormat = "[{0}/{1}] {2}: {3}/{4} files";
		public string Toast_InstallSummaryFormat { get; private set; } = DefaultToast_InstallSummaryFormat;
		private const string DefaultToast_InstallSummaryFormat = "{0} installed, {1} upgraded, {2} skipped, {3} failed.";
		public string Toast_InstallCancelledWithChanges { get; private set; } = DefaultToast_InstallCancelledWithChanges;
		private const string DefaultToast_InstallCancelledWithChanges = "Installation was cancelled after some changes completed.";
		public string Toast_InstallCancelledWithoutChanges { get; private set; } = DefaultToast_InstallCancelledWithoutChanges;
		private const string DefaultToast_InstallCancelledWithoutChanges = "Installation was cancelled before any completed changes.";
		public string Toast_InstallReconciliationFailed { get; private set; } = DefaultToast_InstallReconciliationFailed;
		private const string DefaultToast_InstallReconciliationFailed = "Installed files changed, but the module list could not be fully reconciled.";
		public string Toast_InstallBlseSucceeded { get; private set; } = DefaultToast_InstallBlseSucceeded;
		private const string DefaultToast_InstallBlseSucceeded = "BLSE installed.";
		public string Toast_InstallBlseFailed { get; private set; } = DefaultToast_InstallBlseFailed;
		private const string DefaultToast_InstallBlseFailed = "BLSE installation failed.";
		public string Toast_InstallUnblockWarningFormat { get; private set; } = DefaultToast_InstallUnblockWarningFormat;
		private const string DefaultToast_InstallUnblockWarningFormat = "{0} file(s) could not be unblocked.";
		public string Toast_InstallUnblockFailed { get; private set; } = DefaultToast_InstallUnblockFailed;
		private const string DefaultToast_InstallUnblockFailed = "Installed files could not be unblocked. Use Settings > Tools > Unblock DLLs before launching.";
		public string Toast_InstallReconciliationWarnings { get; private set; } = DefaultToast_InstallReconciliationWarnings;
		private const string DefaultToast_InstallReconciliationWarnings = "The module list was updated with warnings. Review the log if something looks incorrect.";
		public string Toast_GameLaunched { get; private set; } = DefaultToast_GameLaunched;
		private const string DefaultToast_GameLaunched = "Game Launched";
		public string Toast_LaunchFailed { get; private set; } = DefaultToast_LaunchFailed;
		private const string DefaultToast_LaunchFailed = "Launch Failed";
		public string Toast_MissingMods { get; private set; } = DefaultToast_MissingMods;
		private const string DefaultToast_MissingMods = "Missing Mod(s)";
		public string Toast_ModListUpdated { get; private set; } = DefaultToast_ModListUpdated;
		private const string DefaultToast_ModListUpdated = "Mod List Updated";
		public string Toast_RefreshFailed { get; private set; } = DefaultToast_RefreshFailed;
		private const string DefaultToast_RefreshFailed = "Refresh Failed";
		public string Toast_NoModsFound { get; private set; } = DefaultToast_NoModsFound;
		private const string DefaultToast_NoModsFound = "No Mods Found";
		public string Toast_AutoScanFailed { get; private set; } = DefaultToast_AutoScanFailed;
		private const string DefaultToast_AutoScanFailed = "Auto-Scan Failed";
		public string Toast_SteamWorkshopNotFoundTitle { get; private set; } = DefaultToast_SteamWorkshopNotFoundTitle;
		private const string DefaultToast_SteamWorkshopNotFoundTitle = "Steam Workshop Not Found";
		public string Toast_SteamWorkshopNotFoundMessage { get; private set; } = DefaultToast_SteamWorkshopNotFoundMessage;
		private const string DefaultToast_SteamWorkshopNotFoundMessage = "Steam Bannerlord was detected, but no Bannerlord Workshop folder was found. Local mods will still be scanned, and you can select the Workshop folder manually.";
		#endregion

		#region Common / Shared
		public string Common_AlmostDone { get; private set; } = DefaultCommon_AlmostDone;
		private const string DefaultCommon_AlmostDone = "almost done";
		public string Common_Remaining { get; private set; } = DefaultCommon_Remaining;
		private const string DefaultCommon_Remaining = "remaining";
		public string Common_Extracting { get; private set; } = DefaultCommon_Extracting;
		private const string DefaultCommon_Extracting = "Extracting";
		public string Common_FilesExtracted { get; private set; } = DefaultCommon_FilesExtracted;
		private const string DefaultCommon_FilesExtracted = "files extracted";
		#endregion
	}
}
