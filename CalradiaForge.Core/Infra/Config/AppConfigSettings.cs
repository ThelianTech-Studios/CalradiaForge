namespace CalradiaForge.Core.Infra.Config {
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Text;

	using CalradiaForge.Core.Infra.Paths;

	/// <summary>
	/// A wrapper around AppConfig to provide strongly-typed access to specific configuration settings.
	/// Using string keys directly can lead to typos and makes it harder to understand what each setting is for. This class provides a clear interface for accessing configuration values, improving code readability and maintainability.
	/// </summary>
	public sealed class AppConfigSettings : INotifyPropertyChanged {
		private readonly AppConfig _config;
		public AppConfigSettings(AppConfig config) {
			_config = config;
			InitDefaults();
		}
		#region INotifyPropertyChanged Implementation
		public event PropertyChangedEventHandler? PropertyChanged;
		private void OnPropertyChanged(string configValueChanged) {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(configValueChanged));
		}
		#endregion
		#region Config Settings Management Methods
		private void AddIfMissing(string key,string? defaultValue = null) {
			if (string.IsNullOrEmpty(_config[key])) {
								_config[key] = defaultValue ?? string.Empty;
			}
		}
		#endregion
		#region Config Defaults
		internal void InitDefaults() {
			AddIfMissing("Language", "en-US");
			AddIfMissing("GameFolderPath");
			AddIfMissing("GameLauncherFilePath");
			AddIfMissing("GamePlatform", GameProvider.NotInitialized.ToString());
		}
		#endregion

		#region Config Settings Not Json
		public bool IsGameFromSteam => GameProvider == GameProvider.Steam;

		#endregion
		#region Default Config Json Settings
		public GameProvider GameProvider {
			get => Enum.TryParse(_config["GamePlatform"], out GameProvider p) ? p : GameProvider.NotInitialized;
			set {
				if (_config["GamePlatform"] != value.ToString()) {
					_config["GamePlatform"] = value.ToString();
				}
			}
		}

		public string Language {
			get => _config["Language"];
			set {
				if (_config["Language"] != value) {
					_config["Language"] = value;
					OnPropertyChanged(nameof(Language));
				}
			}
		}
		public string GameFolderPath {
			get => _config["GameFolderPath"];
			set {
				if (_config["GameFolderPath"] != value) {
					_config["GameFolderPath"] = value;
					OnPropertyChanged(nameof(GameFolderPath));
				}
			}
		}
		public string GameLauncherFilePath {
			get => _config["GameLauncherFilePath"];
			set {
				if (_config["GameLauncherFilePath"] != value) {
					_config["GameLauncherFilePath"] = value;
				}
			}
		}
		
		public string SteamWorkshopFolderPath {
			get => _config["SteamWorkshopFolderPath"];
			set {
				if (_config["SteamWorkshopFolderPath"] != value) {
					_config["SteamWorkshopFolderPath"] = value;
					OnPropertyChanged(nameof(SteamWorkshopFolderPath));
				}
			}
		}
		// Add more strongly-typed properties for other configuration settings as needed

		#endregion
	}
}
