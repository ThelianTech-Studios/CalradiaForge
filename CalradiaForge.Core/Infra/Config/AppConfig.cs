namespace CalradiaForge.Core.Infra.Config
	{
	using System.Collections.Generic;

	using CalradiaForge.Core.Infra.Logging;

	using Newtonsoft.Json;

	public sealed class AppConfig
		{
		private readonly Logger _logger = Logger.Instance;
		private readonly object _lock = new();
		private Dictionary<string,string> _configValues = new();
		private readonly string _configFilePath;
		public AppConfig(string filePath) {
			if (string.IsNullOrWhiteSpace(filePath)) {
				throw new ArgumentException("File path cannot be null or whitespace.",nameof(filePath));
				}
			_configFilePath=filePath;
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("AppConfig: Initialized.", new { FilePath = _configFilePath });
			}
			}

		public string this[string key] {
			get {
				lock (_lock) {
					bool found = _configValues.TryGetValue(key,out var value);
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("AppConfig: Read key.", new {
							Key = key,
							Value = string.IsNullOrEmpty(value) ? "<empty>" : value,
							Found = found
						});
					}
					return found ? value : string.Empty;
					}
				}
			set {
				lock (_lock) {
					_configValues.TryGetValue(key, out var oldValue);
					_configValues[key]=value;
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
		public void Save() {

			lock (_lock) {
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("AppConfig: Saving config.", new { FilePath = _configFilePath, Count = _configValues.Count });
				}
				var json = JsonConvert.SerializeObject(_configValues,Formatting.Indented);
				File.WriteAllText(_configFilePath,json);
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("AppConfig: Save complete.", new { FilePath = _configFilePath });
				}
				}
			}

		public bool IsConfigured(string key) {
			lock (_lock) {
				return !string.IsNullOrWhiteSpace(this[key]);
				}
			}

		public bool GetBool(string key,bool fallback = false) {
			lock (_lock) {
				return bool.TryParse(this[key],out var result) ? result : fallback;
				}
			}

		public int GetInt(string key,int fallback = 0) {
			lock (_lock) {
				return int.TryParse(this[key],out var result) ? result : fallback;
				}
			}

		public void Load() {
			lock (_lock) {
				if (!File.Exists(_configFilePath)) {
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("AppConfig: Config file missing; creating default.", new { FilePath = _configFilePath });
					}
					Save();
					return;
					}
				var json = File.ReadAllText(_configFilePath);
				_configValues=JsonConvert.DeserializeObject<Dictionary<string,string>>(json)??new Dictionary<string,string>();
				if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
					_logger.Debug("AppConfig: Config loaded.", new { FilePath = _configFilePath, Count = _configValues.Count });
				}
				}
			}
		}
	}
