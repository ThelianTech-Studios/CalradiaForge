namespace CalradiaForge.Core.Infra.Config {
	using System;
	using System.ComponentModel;
	using System.IO;

	using CalradiaForge.Core.Infra.Launch;
	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Infra.Paths;

	/// <summary>
	/// A wrapper around AppConfig to provide strongly-typed access to specific configuration settings.
	/// Using string keys directly can lead to typos and makes it harder to understand what each setting is for. This class provides a clear interface for accessing configuration values, improving code readability and maintainability.
	/// </summary>
	public sealed class AppConfigSettings : INotifyPropertyChanged
		{
		private readonly Logger _logger = Logger.Instance;
		private readonly AppConfig _config;
		public AppConfigSettings(AppConfig config) {
			_config=config;
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("AppConfigSettings: Initializing defaults.");
			}
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
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("AppConfigSettings: Added default.", new { Key = key, Value = _config[key] });
				}
			} else if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("AppConfigSettings: Default already present.", new { Key = key, Value = _config[key] });
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
					string oldValue = _config["GamePlatform"];
					_config["GamePlatform"]=value.ToString();
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("AppConfigSettings: GameProvider changed.", new { OldValue = oldValue, NewValue = _config["GamePlatform"] });
					}
					OnPropertyChanged(nameof(GameProvider));
					OnPropertyChanged(nameof(IsGameFromSteam));
					}
				}
			}

		public string Language {
			get => _config["Language"];
			set {
				if (_config["Language"]!=value) {
					string oldValue = _config["Language"];
					_config["Language"]=value;
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("AppConfigSettings: Language changed.", new { OldValue = oldValue, NewValue = value });
					}
					OnPropertyChanged(nameof(Language));
					}
				}
			}
		public string GameFolderPath {
			get => _config["GameFolderPath"];
			set {
				if (_config["GameFolderPath"]!=value) {
					string oldValue = _config["GameFolderPath"];
					_config["GameFolderPath"]=value;
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("AppConfigSettings: GameFolderPath changed.", new { OldValue = oldValue, NewValue = value });
					}
					OnPropertyChanged(nameof(GameFolderPath));
					OnPropertyChanged(nameof(ModulesDirectoryPath));
					}
				}
			}
		public string GameLauncherFilePath {
			get => _config["GameLauncherFilePath"];
			set {
				if (_config["GameLauncherFilePath"]!=value) {
					string oldValue = _config["GameLauncherFilePath"];
					_config["GameLauncherFilePath"]=value;
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("AppConfigSettings: GameLauncherFilePath changed.", new { OldValue = oldValue, NewValue = value });
					}
					OnPropertyChanged(nameof(GameLauncherFilePath));
					}
				}
			}

		public string SteamWorkshopFolderPath {
			get => _config["SteamWorkshopFolderPath"];
			set {
				if (_config["SteamWorkshopFolderPath"]!=value) {
					string oldValue = _config["SteamWorkshopFolderPath"];
					_config["SteamWorkshopFolderPath"]=value;
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("AppConfigSettings: SteamWorkshopFolderPath changed.", new { OldValue = oldValue, NewValue = value });
					}
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
					string oldValue = _config["LastSelectedModpack"];
					_config["LastSelectedModpack"]=value;
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("AppConfigSettings: LastSelectedModpack changed.", new { OldValue = oldValue, NewValue = value });
					}
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
					string oldValue = _config["ModpackStartupMode"];
					_config["ModpackStartupMode"]=value.ToString();
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("AppConfigSettings: ModpackStartupMode changed.", new { OldValue = oldValue, NewValue = _config["ModpackStartupMode"] });
					}
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
					string oldValue = _config["DebugMode"];
					_config["DebugMode"]=stringValue;
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("AppConfigSettings: DebugMode changed.", new { OldValue = oldValue, NewValue = stringValue });
					}
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
					string oldValue = _config["LastUnblockRunDate"];
					_config["LastUnblockRunDate"]=value;
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("AppConfigSettings: LastUnblockRunDate changed.", new { OldValue = oldValue, NewValue = value });
					}
					OnPropertyChanged(nameof(LastUnblockRunDate));
					}
				}
			}

		public string LastUnblockRunResult {
			get => _config["LastUnblockRunResult"];
			set {
				if (_config["LastUnblockRunResult"]!=value) {
					string oldValue = _config["LastUnblockRunResult"];
					_config["LastUnblockRunResult"]=value;
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("AppConfigSettings: LastUnblockRunResult changed.", new { OldValue = oldValue, NewValue = value });
					}
					OnPropertyChanged(nameof(LastUnblockRunResult));
					}
				}
			}

		public string BLSEExePath {
			get => _config["BLSEExePath"];
			set {
				if (_config["BLSEExePath"]!=value) {
					string oldValue = _config["BLSEExePath"];
					_config["BLSEExePath"]=value;
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("AppConfigSettings: BLSEExePath changed.", new { OldValue = oldValue, NewValue = value });
					}
					OnPropertyChanged(nameof(BLSEExePath));
					}
				}
			}

		public LaunchTarget DefaultLaunchTarget {
			get => Enum.TryParse(_config["DefaultLaunchTarget"],out LaunchTarget t) ? t : LaunchTarget.Bannerlord;
			set {
				if (_config["DefaultLaunchTarget"]!=value.ToString()) {
					string oldValue = _config["DefaultLaunchTarget"];
					_config["DefaultLaunchTarget"]=value.ToString();
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("AppConfigSettings: DefaultLaunchTarget changed.", new { OldValue = oldValue, NewValue = _config["DefaultLaunchTarget"] });
					}
					OnPropertyChanged(nameof(DefaultLaunchTarget));
					}
				}
			}

		// Add more strongly-typed properties for other configuration settings as needed

		#endregion
		}
	}
