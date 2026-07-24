namespace CalradiaForge.Core.Infra.Config {
	using System.Collections.Generic;

	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Infra.Persistence;

	using Newtonsoft.Json;

	/// <summary>
	/// Provides thread-safe key/value configuration persistence backed by JSON.
	/// </summary>
	public sealed class ConfigFileManager {
		private readonly Logger _logger = Logger.Instance;
		private readonly object _lock = new();
		private Dictionary<string, string> _configValues = new();
		private readonly string _configFilePath;
		/// <summary>
		/// Initializes a new configuration store using the specified file path.
		/// </summary>
		public ConfigFileManager(string filePath) {
			if (string.IsNullOrWhiteSpace(filePath)) {
				throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
			}
			_configFilePath = filePath;
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ConfigFileManager: Initialized.", new { FilePath = _configFilePath });
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
						_logger.Debug("ConfigFileManager: Wrote key.", new {
							Key = key,
							OldValue = string.IsNullOrEmpty(oldValue) ? "<empty>" : oldValue,
							NewValue = string.IsNullOrEmpty(value) ? "<empty>" : value
						});
					}
					SaveLocked();
				}
			}
		}

		/// <summary>
		/// Adds missing or empty default values as one persisted configuration update.
		/// </summary>
		/// <param name="defaults">Default key/value pairs to apply.</param>
		/// <returns>The keys that were added or replaced with defaults.</returns>
		internal IReadOnlyList<string> ApplyDefaults(IEnumerable<KeyValuePair<string, string>> defaults) {
			ArgumentNullException.ThrowIfNull(defaults);

			lock (_lock) {
				List<string> appliedKeys = [];
				foreach (KeyValuePair<string, string> entry in defaults) {
					if (string.IsNullOrWhiteSpace(entry.Key)) {
						throw new ArgumentException("Default configuration keys cannot be null or whitespace.", nameof(defaults));
					}

					string defaultValue = entry.Value ?? string.Empty;
					bool keyIsMissing = !_configValues.TryGetValue(entry.Key, out string? currentValue);
					bool storedValueIsNull = !keyIsMissing && currentValue is null;
					bool requiredDefaultIsEmpty = string.IsNullOrEmpty(currentValue) && !string.IsNullOrEmpty(defaultValue);
					if (keyIsMissing || storedValueIsNull || requiredDefaultIsEmpty) {
						_configValues[entry.Key] = defaultValue;
						appliedKeys.Add(entry.Key);
					}
				}

				if (appliedKeys.Count > 0) {
					SaveLocked();
				}

				return appliedKeys;
			}
		}

		/// <summary>
		/// Saves the current configuration values to disk.
		/// </summary>
		public void Save() {
			lock (_lock) {
				SaveLocked();
			}
		}

		/// <summary>Removes a persisted setting when it exists.</summary>
		public bool Remove(string key) {
			ArgumentException.ThrowIfNullOrWhiteSpace(key);
			lock (_lock) {
				if (!_configValues.Remove(key)) {
					return false;
				}
				SaveLocked();
				return true;
			}
		}

		private void SaveLocked() {
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ConfigFileManager: Saving config.", new { FilePath = _configFilePath, Count = _configValues.Count });
			}
			var json = JsonConvert.SerializeObject(_configValues, Formatting.Indented);
			AtomicFileWriter.WriteAllText(_configFilePath, json);
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("ConfigFileManager: Save complete.", new { FilePath = _configFilePath });
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
		/// Loads configuration values from disk or starts with an empty in-memory store when missing.
		/// </summary>
		public void Load() {
			lock (_lock) {
				if (!File.Exists(_configFilePath)) {
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("ConfigFileManager: Config file missing; starting with empty in-memory config.", new { FilePath = _configFilePath });
					}
					_configValues = new Dictionary<string, string>();
					return;
				}
				try {
					var json = File.ReadAllText(_configFilePath);
					_configValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
				} catch (JsonException ex) {
					_logger.Error(ex, "ConfigFileManager: Config file contains invalid JSON. Falling back to defaults.");
					_configValues = new Dictionary<string, string>();
				}
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("ConfigFileManager: Config loaded.", new { FilePath = _configFilePath, Count = _configValues.Count });
				}
			}
		}
	}
}
