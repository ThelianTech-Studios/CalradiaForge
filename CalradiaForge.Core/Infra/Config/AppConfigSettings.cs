namespace CalradiaForge.Core.Infra.Config {
	using System;
	using System.ComponentModel;
	using System.IO;

	using CalradiaForge.Core.Infra.Launch;
	using CalradiaForge.Core.Infra.Paths;

	/// <summary>
	/// A wrapper around AppConfig to provide strongly-typed access to specific configuration settings.
	/// Using string keys directly can lead to typos and makes it harder to understand what each setting is for. This class provides a clear interface for accessing configuration values, improving code readability and maintainability.
	/// </summary>
	public sealed class AppConfigSettings : INotifyPropertyChanged
		{
		private readonly AppConfig _config;
		public AppConfigSettings(AppConfig config) {
			_config=config;
			InitDefaults();
			}
		#region INotifyPropertyChanged Implementation
		public event PropertyChangedEventHandler? PropertyChanged;
		private void OnPropertyChanged(string configValueChanged) {
			PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(configValueChanged));
			}
		#endregion
		#region Config Settings Management Methods
		private void AddIfMissing(string key,string? defaultValue = null) {
			if (string.IsNullOrEmpty(_config[key])) {
				_config[key]=defaultValue??string.Empty;
				}
			}
		#endregion
		#region Config Defaults
		internal void InitDefaults() {
			AddIfMissing("Language","en-US");
			AddIfMissing("GameFolderPath");
			AddIfMissing("GameLauncherFilePath");
			AddIfMissing("GamePlatform",GameProvider.NotInitialized.ToString());
			AddIfMissing("LastSelectedModpack","Last Used");
			AddIfMissing("ModpackStartupMode",ModpackStartupMode.AlwaysAsk.ToString());
			AddIfMissing("DebugMode","False");
			AddIfMissing("LastUnblockRunDate");
			AddIfMissing("LastUnblockRunResult");
			AddIfMissing("BLSEExePath");
			AddIfMissing("DefaultLaunchTarget",LaunchTarget.Bannerlord.ToString());
			}
		#endregion

		#region Config Settings Not Json
		public bool IsGameFromSteam => GameProvider==GameProvider.Steam;

		public string ModulesDirectoryPath => !string.IsNullOrWhiteSpace(GameFolderPath) ? Path.Combine(GameFolderPath,"Modules") : string.Empty;

		#endregion
		#region Default Config Json Settings
		public GameProvider GameProvider {
			get => Enum.TryParse(_config["GamePlatform"],out GameProvider p) ? p : GameProvider.NotInitialized;
			set {
				if (_config["GamePlatform"]!=value.ToString()) {
					_config["GamePlatform"]=value.ToString();
					OnPropertyChanged(nameof(GameProvider));
					OnPropertyChanged(nameof(IsGameFromSteam));
					}
				}
			}

		public string Language {
			get => _config["Language"];
			set {
				if (_config["Language"]!=value) {
					_config["Language"]=value;
					OnPropertyChanged(nameof(Language));
					}
				}
			}
		public string GameFolderPath {
			get => _config["GameFolderPath"];
			set {
				if (_config["GameFolderPath"]!=value) {
					_config["GameFolderPath"]=value;
					OnPropertyChanged(nameof(GameFolderPath));
					OnPropertyChanged(nameof(ModulesDirectoryPath));
					}
				}
			}
		public string GameLauncherFilePath {
			get => _config["GameLauncherFilePath"];
			set {
				if (_config["GameLauncherFilePath"]!=value) {
					_config["GameLauncherFilePath"]=value;
					OnPropertyChanged(nameof(GameLauncherFilePath));
					}
				}
			}

		public string SteamWorkshopFolderPath {
			get => _config["SteamWorkshopFolderPath"];
			set {
				if (_config["SteamWorkshopFolderPath"]!=value) {
					_config["SteamWorkshopFolderPath"]=value;
					OnPropertyChanged(nameof(SteamWorkshopFolderPath));
					}
				}
			}

		/// <summary>
		/// Persisted name of the last modpack selected in the ModsPage ComboBox.
		/// On startup, the UI uses this to auto-select the modpack.
		/// Defaults to "Last Used" if the referenced modpack file no longer exists.
		/// </summary>
		public string LastSelectedModpack {
			get => _config["LastSelectedModpack"];
			set {
				if (_config["LastSelectedModpack"]!=value) {
					_config["LastSelectedModpack"]=value;
					OnPropertyChanged(nameof(LastSelectedModpack));
					}
				}
			}

		/// <summary>
		/// Controls how the ModsPage ComboBox selects a modpack on application startup.
		/// </summary>
		public ModpackStartupMode ModpackStartupMode {
			get => Enum.TryParse(_config["ModpackStartupMode"],out ModpackStartupMode m) ? m : ModpackStartupMode.LastUsed;
			set {
				if (_config["ModpackStartupMode"]!=value.ToString()) {
					_config["ModpackStartupMode"]=value.ToString();
					OnPropertyChanged(nameof(ModpackStartupMode));
					}
				}
			}

		/// <summary>
		/// When enabled, the application generates more verbose log output
		/// and may produce diagnostic data dumps for troubleshooting.
		/// Should only be enabled when requested for debugging bug reports.
		/// </summary>
		public bool DebugMode {
			get => _config.GetBool("DebugMode",false);
			set {
				string stringValue = value.ToString();
				if (_config["DebugMode"]!=stringValue) {
					_config["DebugMode"]=stringValue;
					OnPropertyChanged(nameof(DebugMode));
					}
				}
			}

		/// <summary>
		/// Persisted timestamp of the last time the DLL Unblock tool was run.
		/// Displayed in the Tools tab for user reference and debugging.
		/// </summary>
		public string LastUnblockRunDate {
			get => _config["LastUnblockRunDate"];
			set {
				if (_config["LastUnblockRunDate"]!=value) {
					_config["LastUnblockRunDate"]=value;
					OnPropertyChanged(nameof(LastUnblockRunDate));
					}
				}
			}

		/// <summary>
		/// Persisted summary result of the last DLL Unblock tool run.
		/// Displayed in the Tools tab for user reference and debugging.
		/// </summary>
		public string LastUnblockRunResult {
			get => _config["LastUnblockRunResult"];
			set {
				if (_config["LastUnblockRunResult"]!=value) {
					_config["LastUnblockRunResult"]=value;
					OnPropertyChanged(nameof(LastUnblockRunResult));
					}
				}
			}

		/// <summary>
		/// Full path to the BLSE Standalone executable (Bannerlord.BLSE.Standalone.exe).
		/// Empty by default — populated by auto-detection in <see cref="GamePathsHelper"/>
		/// or manually via Settings → Game Config.
		/// </summary>
		public string BLSEExePath {
			get => _config["BLSEExePath"];
			set {
				if (_config["BLSEExePath"]!=value) {
					_config["BLSEExePath"]=value;
					OnPropertyChanged(nameof(BLSEExePath));
					}
				}
			}

		/// <summary>
		/// The user's preferred launch target for the Play button.
		/// Persisted across sessions so the split-button selection is restored on startup.
		/// Defaults to <see cref="LaunchTarget.Bannerlord"/>.
		/// </summary>
		public LaunchTarget DefaultLaunchTarget {
			get => Enum.TryParse(_config["DefaultLaunchTarget"],out LaunchTarget t) ? t : LaunchTarget.Bannerlord;
			set {
				if (_config["DefaultLaunchTarget"]!=value.ToString()) {
					_config["DefaultLaunchTarget"]=value.ToString();
					OnPropertyChanged(nameof(DefaultLaunchTarget));
					}
				}
			}

		// Add more strongly-typed properties for other configuration settings as needed

		#endregion
		}
	}
