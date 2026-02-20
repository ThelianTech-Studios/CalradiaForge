namespace CalradiaForge.UI.Pages {
	using System;
	using System.ComponentModel;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.CompilerServices;
	using System.Windows;
	using System.Windows.Controls;

	using CalradiaForge.Core.Infra.Config;
	using CalradiaForge.Core.Infra.Localization;
	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Infra.Mods;
	using CalradiaForge.Core.Infra.Paths;
	using CalradiaForge.Core.Models;

	using CalradiaForge.UI.Toasts;

	using Microsoft.Win32;

	/// <summary>
	/// Settings page with tabbed sections for General, Game Configuration,
	/// Tools, WIP, and About. All settings auto-save immediately on change.
	/// The UI decides *when* actions run; Core services and helpers decide *how*.
	/// </summary>
	public partial class SettingsPage : Page, INotifyPropertyChanged {
		#region Fields
		private readonly AppConfigSettings _config;
		private readonly ModService _modService;
		private readonly Logger _logger = Logger.Instance;
		private string _gameFolderPath = string.Empty;
		private string _gameLauncherFilePath = string.Empty;
		private string _steamWorkshopFolderPath = string.Empty;
		private string _blseExePath = string.Empty;

		/// <summary>
		/// Panel references indexed to match <see cref="SettingsNavBar"/> selection order.
		/// 0 = General, 1 = Game Config, 2 = Tools, 3 = WIP, 4 = About.
		/// </summary>
		private UIElement[] _panels = [];
		#endregion

		#region Bound Properties
		public string GameFolderPath {
			get => _gameFolderPath;
			set { _gameFolderPath = value; OnPropertyChanged(); }
		}

		public string GameLauncherFilePath {
			get => _gameLauncherFilePath;
			set { _gameLauncherFilePath = value; OnPropertyChanged(); }
		}

		public string SteamWorkshopFolderPath {
			get => _steamWorkshopFolderPath;
			set { _steamWorkshopFolderPath = value; OnPropertyChanged(); }
		}

		public string BLSEExePath {
			get => _blseExePath;
			set { _blseExePath = value; OnPropertyChanged(); }
		}
		#endregion

		#region INotifyPropertyChanged
		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = "") {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
		#endregion

		#region Constructor
		public SettingsPage() {
			InitializeComponent();
			DataContext = this;
			_config = App.AppConfig;
			_modService = App.ModService;

			_panels = [PanelGeneral, PanelGameConfig, PanelTools, PanelWip, PanelAbout];

			LoadCurrentValues();
			UpdatePathSectionVisibility();
			UpdateGameFolderValidation();
			UpdateBLSEValidation();
			LoadUnblockStatus();
			SetVersionText();
			PopulateLanguageComboBox();
		}
		#endregion

		#region Settings Nav Bar — Tab Switching

		/// <summary>
		/// Toggles panel visibility based on the selected nav item index.
		/// Collapses all panels, then makes the selected one visible.
		/// </summary>
		private void SettingsNav_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_panels is null || _panels.Length == 0) {
				return;
			}

			int selectedIndex = SettingsNavBar.SelectedIndex;
			if (selectedIndex < 0 || selectedIndex >= _panels.Length) {
				return;
			}

			for (int i = 0; i < _panels.Length; i++) {
				_panels[i].Visibility = (i == selectedIndex)
					? Visibility.Visible
					: Visibility.Collapsed;
			}
		}

		#endregion

		#region Initialization

		/// <summary>
		/// Loads current config values into the UI controls.
		/// </summary>
		private void LoadCurrentValues() {
			// Path display values
			GameFolderPath = _config.GameFolderPath;
			GameLauncherFilePath = _config.GameLauncherFilePath;
			SteamWorkshopFolderPath = _config.SteamWorkshopFolderPath;
			BLSEExePath = _config.BLSEExePath;

			// Modpack startup mode radio buttons
			switch (_config.ModpackStartupMode) {
				case ModpackStartupMode.AlwaysDefault:
					RadioAlwaysDefault.IsChecked = true;
					break;
				case ModpackStartupMode.AlwaysAsk:
					RadioAlwaysAsk.IsChecked = true;
					break;
				case ModpackStartupMode.LastUsed:
				default:
					RadioLastUsed.IsChecked = true;
					break;
			}

			// Debug mode toggle
			DebugModeToggle.IsChecked = _config.DebugMode;
		}

		/// <summary>
		/// Populates the language ComboBox from the <see cref="Core.Infra.Localization.TranslationService"/>
		/// manifest and selects the currently active language.
		/// Wires <see cref="LanguageComboBox_SelectionChanged"/> after selection
		/// to avoid firing during initialization.
		/// </summary>
		private void PopulateLanguageComboBox() {
			LanguageComboBox.SelectionChanged -= LanguageComboBox_SelectionChanged;

			LanguageComboBox.ItemsSource = App.Translator.AvailableLanguages;
			LanguageComboBox.IsEnabled = App.Translator.AvailableLanguages.Count > 1;

			// Select the active language
			string activeCode = App.Translator.ActiveLanguageCode;
			int selectedIndex = App.Translator.AvailableLanguages
				.Select((lang, i) => new { lang, i })
				.FirstOrDefault(x => string.Equals(x.lang.Code, activeCode, StringComparison.OrdinalIgnoreCase))?.i ?? 0;
			LanguageComboBox.SelectedIndex = selectedIndex;

			LanguageComboBox.SelectionChanged += LanguageComboBox_SelectionChanged;
		}

		/// <summary>
		/// Shows or hides path sections based on the current game platform.
		/// Steam: show workshop, hide exe.
		/// Epic/StandAlone: show exe, hide workshop.
		/// NotInitialized: show both so the user can configure manually.
		/// </summary>
		private void UpdatePathSectionVisibility() {
			switch (_config.GameProvider) {
				case GameProvider.Steam:
					ExePathSection.Visibility = Visibility.Collapsed;
					WorkshopPathSection.Visibility = Visibility.Visible;
					break;
				case GameProvider.EpicGames:
				case GameProvider.StandAlone:
					ExePathSection.Visibility = Visibility.Visible;
					WorkshopPathSection.Visibility = Visibility.Collapsed;
					break;
				default:
					ExePathSection.Visibility = Visibility.Visible;
					WorkshopPathSection.Visibility = Visibility.Visible;
					break;
			}
		}

		/// <summary>
		/// Updates the validation indicator below the game folder path.
		/// Delegates validation logic to <see cref="GamePathValidator"/>.
		/// </summary>
		private void UpdateGameFolderValidation() {
			if (GamePathValidator.ValidateGameFolder(_config.GameFolderPath, out string error)) {
				GameFolderValidation.Text = "✓ Valid Bannerlord installation detected.";
				GameFolderValidation.Style = (Style)FindResource("SettingsValidationOk");
			} else {
				GameFolderValidation.Text = $"✗ {error}";
				GameFolderValidation.Style = (Style)FindResource("SettingsValidationError");
			}
		}

		/// <summary>
		/// Updates the validation indicator below the BLSE executable path.
		/// Shows a green check if a valid exe exists, otherwise shows a hint
		/// that BLSE is optional and can be configured later.
		/// </summary>
		private void UpdateBLSEValidation() {
			if (!string.IsNullOrWhiteSpace(_config.BLSEExePath)
				&& GamePathValidator.ValidateGameExecutable(_config.BLSEExePath, out _)) {
				BLSEValidation.Text = "✓ BLSE executable found.";
				BLSEValidation.Style = (Style)FindResource("SettingsValidationOk");
			} else if (string.IsNullOrWhiteSpace(_config.BLSEExePath)) {
				BLSEValidation.Text = "Not configured — optional. Select if you use BLSE mods.";
				BLSEValidation.Style = (Style)FindResource("SettingsValidationOk");
			} else {
				BLSEValidation.Text = "✗ The selected BLSE executable was not found.";
				BLSEValidation.Style = (Style)FindResource("SettingsValidationError");
			}
		}

		/// <summary>
		/// Loads persisted unblock status into the Tools tab display.
		/// </summary>
		private void LoadUnblockStatus() {
			string lastRun = _config.LastUnblockRunDate;
			string lastResult = _config.LastUnblockRunResult;

			UnblockLastRunText.Text = string.IsNullOrWhiteSpace(lastRun)
				? "Last run:  Never"
				: $"Last run:  {lastRun}";

			UnblockResultText.Text = string.IsNullOrWhiteSpace(lastResult)
				? "Result:  —"
				: $"Result:  {lastResult}";
		}

		/// <summary>
		/// Sets the version text from the executing assembly.
		/// </summary>
		private void SetVersionText() {
			Version? version = Assembly.GetExecutingAssembly().GetName().Version;
			VersionText.Text = version is not null
				? $"Version {version.Major}.{version.Minor}.{version.Build}"
				: "Version 1.0.0";
		}

		#endregion

		#region General Tab — Event Handlers

		/// <summary>
		/// Handles language ComboBox selection changes.
		/// Delegates to <see cref="Core.Infra.Localization.TranslationService.SetLanguage"/>
		/// for live hot-reload without restart.
		/// </summary>
		private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (!IsLoaded) {
				return;
			}
			if (LanguageComboBox.SelectedItem is LanguageOption selected) {
				App.Translator.SetLanguage(selected.Code);
				_logger.Info($"SettingsPage: Language changed to '{selected.Code}' ({selected.DisplayName}).");
			}
		}

		/// <summary>
		/// Handles modpack startup mode radio button changes.
		/// Auto-saves the selected mode to config immediately.
		/// </summary>
		private void ModpackStartupMode_Changed(object sender, RoutedEventArgs e) {
			if (!IsLoaded) {
				return;
			}
			if (RadioAlwaysDefault.IsChecked == true) {
				_config.ModpackStartupMode = ModpackStartupMode.AlwaysDefault;
			} else if (RadioAlwaysAsk.IsChecked == true) {
				_config.ModpackStartupMode = ModpackStartupMode.AlwaysAsk;
			} else {
				_config.ModpackStartupMode = ModpackStartupMode.LastUsed;
			}
		}

		/// <summary>
		/// Handles debug mode toggle switch changes.
		/// Auto-saves to config immediately and adjusts log verbosity.
		/// </summary>
		private void DebugMode_Toggled(object sender, RoutedEventArgs e) {
			if (!IsLoaded) {
				return;
			}
			bool enabled = DebugModeToggle.IsChecked == true;
			_config.DebugMode = enabled;
			_logger.MinimumLevel = enabled
				? Logger.LogLevel.Debug
				: Logger.LogLevel.Info;
			_logger.Info($"SettingsPage: DebugMode changed to {enabled}. Log level: {_logger.MinimumLevel}");
		}

		#endregion

		#region Game Configuration Tab — Event Handlers

		/// <summary>
		/// Opens a folder browser dialog for the game folder.
		/// Validates the selection using <see cref="GamePathValidator"/> before saving.
		/// </summary>
		private void SelectGameFolder_Click(object sender, RoutedEventArgs e) {
			string? selectedPath;
			OpenFolderDialog dialogWindow = new() {Title="Select Bannerlord Game Folder"};
				if (dialogWindow.ShowDialog() != true) {
					return;
				}
				selectedPath = dialogWindow.FolderName;

			if (!GamePathValidator.ValidateGameFolder(selectedPath, out string error)) {
				_logger.Warning($"SettingsPage: Game folder validation failed: {error}");
				App.Toasts.Show(new ToastRequest {
					Title = "Invalid Game Folder",
					Message = error,
					Severity = ToastSeverity.Error
				});
				UpdateGameFolderValidation();
				return;
			}

			_config.GameFolderPath = selectedPath;
			GameFolderPath = selectedPath;
			UpdateGameFolderValidation();
			_logger.Info($"SettingsPage: Game folder set to '{selectedPath}'");
		}

		/// <summary>
		/// Opens a file browser dialog for the game executable.
		/// Validates the selection using <see cref="GamePathValidator"/> before saving.
		/// </summary>
		private void SelectGameExe_Click(object sender, RoutedEventArgs e) {
			string? selectedPath;
			OpenFileDialog dialogWindow = new OpenFileDialog() {
				Title = "Select Bannerlord Executable",
				Filter = "Executable Files (*.exe)|*.exe",
				CheckFileExists =true
				};
				if (dialogWindow.ShowDialog() != true) {
					return;
				}
				selectedPath = dialogWindow.FileName;

			if (!GamePathValidator.ValidateGameExecutable(selectedPath, out string error)) {
				_logger.Warning($"SettingsPage: Executable validation failed: {error}");
				App.Toasts.Show(new ToastRequest {
					Title = "Invalid Executable",
					Message = error,
					Severity = ToastSeverity.Error
				});
				return;
			}

			_config.GameLauncherFilePath = selectedPath;
			GameLauncherFilePath = selectedPath;
			_logger.Info($"SettingsPage: Game executable set to '{selectedPath}'");
		}

		/// <summary>
		/// Opens a folder browser dialog for the Steam Workshop folder.
		/// Validates the selection using <see cref="GamePathValidator"/> before saving.
		/// </summary>
		private void SelectWorkshopFolder_Click(object sender, RoutedEventArgs e) {
			string? selectedPath;
			OpenFolderDialog dialogWindow = new() {
				Title="Select Steam Workshop Folder"
				};
				if (dialogWindow.ShowDialog() != true) {
					return;
				}
				selectedPath = dialogWindow.FolderName;

			if (!GamePathValidator.ValidateWorkshopFolder(selectedPath, out string error)) {
				_logger.Warning($"SettingsPage: Workshop folder validation failed: {error}");
				App.Toasts.Show(new ToastRequest {
					Title = "Invalid Workshop Folder",
					Message = error,
					Severity = ToastSeverity.Error
				});
				return;
			}

			_config.SteamWorkshopFolderPath = selectedPath;
			SteamWorkshopFolderPath = selectedPath;
			_logger.Info($"SettingsPage: Workshop folder set to '{selectedPath}'");
		}

		/// <summary>
		/// Opens a file browser dialog for the BLSE Standalone executable.
		/// Validates using <see cref="GamePathValidator.ValidateGameExecutable"/>
		/// (file must exist and be a .exe). Persists to config immediately.
		/// </summary>
		private void SelectBLSEExe_Click(object sender, RoutedEventArgs e) {
			OpenFileDialog dialogWindow = new() {
				Title = "Select BLSE Standalone Executable",
				Filter = "Executable Files (*.exe)|*.exe",
				CheckFileExists = true
			};
			if (dialogWindow.ShowDialog() != true) {
				return;
			}
			string selectedPath = dialogWindow.FileName;

			if (!GamePathValidator.ValidateGameExecutable(selectedPath, out string error)) {
				_logger.Warning($"SettingsPage: BLSE executable validation failed: {error}");
				App.Toasts.Show(new ToastRequest {
					Title = "Invalid BLSE Executable",
					Message = error,
					Severity = ToastSeverity.Error
				});
				UpdateBLSEValidation();
				return;
			}

			_config.BLSEExePath = selectedPath;
			BLSEExePath = selectedPath;
			UpdateBLSEValidation();
			_logger.Info($"SettingsPage: BLSE executable set to '{selectedPath}'");
		}

		/// <summary>
		/// Re-runs the game auto-detection pipeline via <see cref="GamePathsHelper"/>
		/// and refreshes all displayed paths, visibility, and BLSE validation.
		/// </summary>
		private void RedetectGame_Click(object sender, RoutedEventArgs e) {
			_logger.Info("SettingsPage: Re-detecting game installation...");
			GamePathsHelper.TryAutoDetectGameFolder(_config);

			GameFolderPath = _config.GameFolderPath;
			GameLauncherFilePath = _config.GameLauncherFilePath;
			SteamWorkshopFolderPath = _config.SteamWorkshopFolderPath;
			BLSEExePath = _config.BLSEExePath;

			UpdatePathSectionVisibility();
			UpdateGameFolderValidation();
			UpdateBLSEValidation();
			_logger.Info($"SettingsPage: Re-detect complete. Platform: {_config.GameProvider}");

			bool detected = GamePathValidator.ValidateGameFolder(_config.GameFolderPath, out _);
			App.Toasts.Show(new ToastRequest {
				Title = detected ? "Game Detected" : "Detection Failed",
				Message = detected
					? $"Bannerlord found via {_config.GameProvider}."
					: "Could not auto-detect Bannerlord. Please select the game folder manually.",
				Severity = detected ? ToastSeverity.Success : ToastSeverity.Warning
			});
		}

		#endregion

		#region Tools Tab — Event Handlers

		/// <summary>
		/// Runs the DLL unblock operation via <see cref="DLLUnblocker"/>.
		/// Persists the run timestamp and result to config for display and debugging.
		/// </summary>
		private async void UnblockDlls_Click(object sender, RoutedEventArgs e) {
			if (!GamePathValidator.ValidateGameFolder(_config.GameFolderPath, out string folderError)) {
				_logger.Warning($"SettingsPage: Cannot unblock — {folderError}");
				App.Toasts.Show(new ToastRequest {
					Title = "Cannot Unblock DLLs",
					Message = folderError,
					Severity = ToastSeverity.Warning
				});
				return;
			}

			try {
				UnblockLastRunText.Text = "Last run:  Running...";
				UnblockResultText.Text = "Result:  —";

				UnblockResult result = await DLLUnblocker.UnblockAllAsync(_config.ModulesDirectoryPath);

				string timestamp = DateTime.Now.ToString("yyyy-MM-dd  h:mm tt");
				string summary = result.ToSummaryString();

				_config.LastUnblockRunDate = timestamp;
				_config.LastUnblockRunResult = summary;

				UnblockLastRunText.Text = $"Last run:  {timestamp}";
				UnblockResultText.Text = $"Result:  {summary}";

				_logger.Info($"SettingsPage: Unblock complete. {summary}");
				App.Toasts.Show(new ToastRequest {
					Title = "DLL Unblock Complete",
					Message = summary,
					Severity = ToastSeverity.Success
				});
			} catch (Exception ex) {
				_logger.Error(ex, "SettingsPage: Unblock operation failed.");
				UnblockResultText.Text = $"Result:  Error — {ex.Message}";
				App.Toasts.Show(new ToastRequest {
					Title = "Unblock Failed",
					Message = ex.Message,
					Severity = ToastSeverity.Error
				});
			}
		}

		/// <summary>
		/// Clears the mod cache via <see cref="ModService.ClearCache"/>.
		/// Forces a fresh directory scan on next application launch.
		/// </summary>
		private void ClearModCache_Click(object sender, RoutedEventArgs e) {
			bool success = _modService.ClearCache();
			if (success) {
				App.Toasts.Show(new ToastRequest {
					Title = "Cache Cleared",
					Message = "Mod cache has been cleared. A fresh scan will run on next launch.",
					Severity = ToastSeverity.Success
				});
			} else {
				App.Toasts.Show(new ToastRequest {
					Title = "Cache Clear Failed",
					Message = "Could not clear the mod cache. Check logs for details.",
					Severity = ToastSeverity.Error
				});
			}
		}

		/// <summary>
		/// Opens the Config directory via <see cref="ExplorerHelper"/>.
		/// </summary>
		private void OpenConfigFolder_Click(object sender, RoutedEventArgs e) {
			ExplorerHelper.OpenFolder(AppPaths.ConfigDirectory);
		}

		/// <summary>
		/// Opens the Logs directory via <see cref="ExplorerHelper"/>.
		/// </summary>
		private void OpenLogsFolder_Click(object sender, RoutedEventArgs e) {
			ExplorerHelper.OpenFolder(AppPaths.LogsDirectory);
		}

		/// <summary>
		/// Opens the Modpacks directory via <see cref="ExplorerHelper"/>.
		/// </summary>
		private void OpenModpacksFolder_Click(object sender, RoutedEventArgs e) {
			ExplorerHelper.OpenFolder(AppPaths.ModpacksDirectory);
		}

		#endregion

		#region About Tab — Event Handlers

		/// <summary>
		/// Opens the license URL via <see cref="ExplorerHelper"/>.
		/// </summary>
		private void ViewLicense_Click(object sender, RoutedEventArgs e) {
			ExplorerHelper.OpenUrl("https://github.com/ThelianTech/CalradiaForge/blob/master_docs/LICENSE.md");
		}

		/// <summary>
		/// Opens the GitHub repository URL via <see cref="ExplorerHelper"/>.
		/// </summary>
		private void ViewGitHub_Click(object sender, RoutedEventArgs e) {
			ExplorerHelper.OpenUrl("https://github.com/ThelianTech-Studios/CalradiaForge");
		}

		#endregion
	}
}
