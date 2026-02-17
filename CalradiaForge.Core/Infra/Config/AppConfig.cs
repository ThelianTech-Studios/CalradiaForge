namespace CalradiaForge.Core.Infra.Config
	{
	using System.Collections.Generic;

	using Newtonsoft.Json;

	public sealed class AppConfig
		{
		private readonly object _lock = new();
		private Dictionary<string,string> _configValues = new();
		private readonly string _configFilePath;
		public AppConfig(string filePath) {
			if (string.IsNullOrWhiteSpace(filePath)) {
				throw new ArgumentException("File path cannot be null or whitespace.",nameof(filePath));
				}
			_configFilePath=filePath;
			}

		public string this[string key] {
			get {
				lock (_lock) {
					return _configValues.TryGetValue(key,out var value) ? value : string.Empty;
					}
				}
			set {
				lock (_lock) {
					_configValues[key]=value;
					Save();
					}
				}
			}
		public void Save() {

			lock (_lock) {
				var json = JsonConvert.SerializeObject(_configValues,Formatting.Indented);
				File.WriteAllText(_configFilePath,json);
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
					Save();
					return;
					}
				var json = File.ReadAllText(_configFilePath);
				_configValues=JsonConvert.DeserializeObject<Dictionary<string,string>>(json)??new Dictionary<string,string>();
				}
			}
		}
	}
