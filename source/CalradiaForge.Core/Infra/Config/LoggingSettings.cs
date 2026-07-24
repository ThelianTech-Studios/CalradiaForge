namespace CalradiaForge.Core.Infra.Config;

using System.ComponentModel;

/// <summary>Provides the persisted logging preference required before Serilog construction.</summary>
public sealed class LoggingSettings : INotifyPropertyChanged {
	private readonly ConfigFileManager _config;

	public LoggingSettings(ConfigFileManager config) {
		_config = config ?? throw new ArgumentNullException(nameof(config));
		_config.Remove("LogFileDaysToKeep");
		_config.ApplyDefaults([
			new KeyValuePair<string, string>("DebugMode", "False")
		]);
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>Gets or sets whether the next application process uses Debug-level logging.</summary>
	public bool DebugMode {
		get => _config.GetBool("DebugMode", false);
		set {
			string stringValue = value.ToString();
			if (_config["DebugMode"] == stringValue) {
				return;
			}
			_config["DebugMode"] = stringValue;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DebugMode)));
		}
	}
}
