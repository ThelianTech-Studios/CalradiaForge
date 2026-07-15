namespace CalradiaForge.Core.Infra.Config {
	using System.Collections.Generic;

	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Infra.Persistence;

	using Newtonsoft.Json;

	/// <summary>
	/// Provides thread-safe key/value configuration persistence backed by JSON.
	/// </summary>
	public sealed class AppConfig {
		private readonly Logger _logger = Logger.Instance;
		private readonly object _lock = new();
		private Dictionary<string, string> _configValues = new();
		private readonly string _configFilePath;
		/// <summary>
		/// Initializes a new configuration store using the specified file path.
		/// </summary>
		public AppConfig(string filePath) {
			if (string.IsNullOrWhiteSpace(filePath)) {
				throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
			}
			_configFilePath = filePath;
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("AppConfig: Initialized.", new { FilePath = _configFilePath });
			}
		}

		/// <summary>
		/// Gets or sets a configuration value by key, persisting changes immediately.
		/// </summary>
		public string this[string key] {
			get {
				lock (_lock) {
					return _configValues.TryGetValue(key, out string? value) ? value : string.Empty;
				}
			}
			set {
				lock (_lock) {
					_configValues.TryGetValue(key, out var oldValue);
					_configValues[key] = value;
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("AppConfig: Wrote key.", new {
							Key = key,
							OldValue = string.IsNullOrEmpty(oldValue) ? "<empty>" : oldValue,
							NewValue = string.IsNullOrEmpty(value) ? "<empty>" : value
						});
					}
					Save();
				}
			}
		}
		/// <summary>
		/// Saves the current configuration values to disk.
		/// </summary>
		public void Save() {

			lock (_lock) {
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("AppConfig: Saving config.", new { FilePath = _configFilePath, Count = _configValues.Count });
				}
				var json = JsonConvert.SerializeObject(_configValues, Formatting.Indented);
				AtomicFileWriter.WriteAllText(_configFilePath, json);
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("AppConfig: Save complete.", new { FilePath = _configFilePath });
				}
			}
		}

		/// <summary>
		/// Determines whether a non-empty value exists for the specified key.
		/// </summary>
		public bool IsConfigured(string key) {
			lock (_lock) {
				return !string.IsNullOrWhiteSpace(this[key]);
			}
		}

		/// <summary>
		/// Reads a boolean configuration value with a fallback.
		/// </summary>
		public bool GetBool(string key, bool fallback = false) {
			lock (_lock) {
				return bool.TryParse(this[key], out var result) ? result : fallback;
			}
		}

		/// <summary>
		/// Reads an integer configuration value with a fallback.
		/// </summary>
		public int GetInt(string key, int fallback = 0) {
			lock (_lock) {
				return int.TryParse(this[key], out var result) ? result : fallback;
			}
		}

		/// <summary>
		/// Loads configuration values from disk or initializes defaults when missing.
		/// </summary>
		public void Load() {
			lock (_lock) {
				if (!File.Exists(_configFilePath)) {
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("AppConfig: Config file missing; creating default.", new { FilePath = _configFilePath });
					}
					Save();
					return;
				}
				try {
					var json = File.ReadAllText(_configFilePath);
					_configValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
				} catch (JsonException ex) {
					_logger.Error(ex, "AppConfig: Config file contains invalid JSON. Falling back to defaults.");
					_configValues = new Dictionary<string, string>();
				}
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("AppConfig: Config loaded.", new { FilePath = _configFilePath, Count = _configValues.Count });
				}
			}
		}
	}
}
