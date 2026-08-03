namespace CalradiaForge.Core.Infra.Config;

using System.Security;

using Newtonsoft.Json;

/// <summary>
/// Provides thread-safe, defaults-first key/value configuration persistence backed by JSON.
/// </summary>
public sealed class ConfigFileManager {
	private readonly object _lock = new();
	private readonly string _configFilePath;
	private readonly IConfigFilePersistence _persistence;
	private Dictionary<string, string> _configValues = CreateDefaultsMap();
	private ConfigPersistenceStatus _persistenceStatus = ConfigPersistenceStatus.Available;

	/// <summary>Initializes a configuration store using the specified file path.</summary>
	public ConfigFileManager(string filePath)
		: this(filePath, new ConfigFilePersistence()) { }

	internal ConfigFileManager(string filePath, IConfigFilePersistence persistence) {
		if (string.IsNullOrWhiteSpace(filePath)) {
			throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
		}

		_configFilePath = filePath;
		_persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
		LastLoadResult = new ConfigLoadResult(
			ConfigLoadStatus.MissingFileInitialization,
			ConfigPersistenceStatus.Available,
			ConfigDefaults.Values.Keys.ToArray());
		LastSaveResult = new ConfigSaveResult(ConfigSaveStatus.NoChanges);
	}

	/// <summary>Gets the most recent load result.</summary>
	public ConfigLoadResult LastLoadResult { get; private set; }

	/// <summary>Gets the most recent requested save result.</summary>
	public ConfigSaveResult LastSaveResult { get; private set; }

	/// <summary>Raised once when an attempted write transitions persistence to unavailable.</summary>
	public event EventHandler<ConfigPersistenceUnavailableEventArgs>? PersistenceBecameUnavailable;

	/// <summary>Gets whether the current in-memory configuration can be persisted.</summary>
	public ConfigPersistenceStatus PersistenceStatus {
		get { lock (_lock) { return _persistenceStatus; } }
	}

	/// <summary>Gets or sets a configuration value, retaining changes even when persistence fails.</summary>
	public string this[string key] {
		get {
			ArgumentException.ThrowIfNullOrWhiteSpace(key);
			lock (_lock) {
				return _configValues.TryGetValue(key, out string? value) ? value ?? string.Empty : string.Empty;
			}
		}
		set {
			ArgumentException.ThrowIfNullOrWhiteSpace(key);
			ConfigSaveResult saveResult;
			bool becameUnavailable;
			lock (_lock) {
				ConfigPersistenceStatus previousStatus = _persistenceStatus;
				_configValues[key] = value ?? string.Empty;
				saveResult = SaveLocked();
				LastSaveResult = saveResult;
				becameUnavailable = BecameUnavailable(previousStatus);
			}
			NotifyPersistenceUnavailable(becameUnavailable, saveResult);
		}
	}

	/// <summary>Saves the current configuration values to disk.</summary>
	public ConfigSaveResult Save() {
		ConfigSaveResult saveResult;
		bool becameUnavailable;
		lock (_lock) {
			ConfigPersistenceStatus previousStatus = _persistenceStatus;
			saveResult = SaveLocked();
			LastSaveResult = saveResult;
			becameUnavailable = BecameUnavailable(previousStatus);
		}
		NotifyPersistenceUnavailable(becameUnavailable, saveResult);
		return saveResult;
	}

	/// <summary>Removes a persisted setting when it exists.</summary>
	public bool Remove(string key) {
		ArgumentException.ThrowIfNullOrWhiteSpace(key);
		ConfigSaveResult saveResult;
		bool becameUnavailable;
		lock (_lock) {
			if (!_configValues.Remove(key)) {
				LastSaveResult = new ConfigSaveResult(ConfigSaveStatus.NoChanges);
				return false;
			}

			ConfigPersistenceStatus previousStatus = _persistenceStatus;
			saveResult = SaveLocked();
			LastSaveResult = saveResult;
			becameUnavailable = BecameUnavailable(previousStatus);
		}
		NotifyPersistenceUnavailable(becameUnavailable, saveResult);
		return true;
	}

	/// <summary>Determines whether a non-empty value exists for the specified key.</summary>
	public bool IsConfigured(string key) {
		ArgumentException.ThrowIfNullOrWhiteSpace(key);
		lock (_lock) {
			return _configValues.TryGetValue(key, out string? value)
				&& !string.IsNullOrWhiteSpace(value);
		}
	}

	/// <summary>Reads a boolean configuration value with a fallback.</summary>
	public bool GetBool(string key, bool fallback = false) {
		lock (_lock) {
			return _configValues.TryGetValue(key, out string? value)
				&& bool.TryParse(value, out bool result)
				? result
				: fallback;
		}
	}

	/// <summary>Reads an integer configuration value with a fallback.</summary>
	public int GetInt(string key, int fallback = 0) {
		lock (_lock) {
			return _configValues.TryGetValue(key, out string? value)
				&& int.TryParse(value, out int result)
				? result
				: fallback;
		}
	}

	/// <summary>Loads a disk overlay onto a complete in-memory defaults map.</summary>
	public ConfigLoadResult Load() {
		lock (_lock) {
			_configValues = CreateDefaultsMap();
			_persistenceStatus = ConfigPersistenceStatus.Available;

			bool exists;
			try {
				exists = _persistence.FileExists(_configFilePath);
			} catch (UnauthorizedAccessException ex) {
				return SetUnavailableLoadResult(ConfigLoadStatus.AccessDenied, ex);
			} catch (SecurityException ex) {
				return SetUnavailableLoadResult(ConfigLoadStatus.AccessDenied, ex);
			} catch (IOException ex) {
				return SetUnavailableLoadResult(ConfigLoadStatus.ReadUnavailable, ex);
			}

			if (!exists) {
				ConfigSaveResult saveResult = SaveLocked();
				LastSaveResult = saveResult;
				LastLoadResult = new ConfigLoadResult(
					ConfigLoadStatus.MissingFileInitialization,
					_persistenceStatus,
					ConfigDefaults.Values.Keys.ToArray(),
					Diagnostic: saveResult.Diagnostic);
				return LastLoadResult;
			}

			string json;
			try {
				json = _persistence.ReadAllText(_configFilePath);
			} catch (UnauthorizedAccessException ex) {
				return SetUnavailableLoadResult(ConfigLoadStatus.AccessDenied, ex);
			} catch (SecurityException ex) {
				return SetUnavailableLoadResult(ConfigLoadStatus.AccessDenied, ex);
			} catch (IOException ex) {
				return SetUnavailableLoadResult(ConfigLoadStatus.ReadUnavailable, ex);
			}

			Dictionary<string, string?> persisted;
			try {
				persisted = JsonConvert.DeserializeObject<Dictionary<string, string?>>(json)
					?? new Dictionary<string, string?>();
			} catch (JsonException ex) {
				return RecoverMalformedFile(ex);
			}

			List<string> completedDefaults = [];
			bool repairNeeded = false;
			foreach (KeyValuePair<string, string?> entry in persisted) {
				if (ConfigDefaults.ObsoleteKeys.Contains(entry.Key)) {
					repairNeeded = true;
					continue;
				}

				if (ConfigDefaults.Values.ContainsKey(entry.Key)
					&& (entry.Value is null
						|| !ConfigDefaults.IsValidPersistedValue(entry.Key, entry.Value))) {
					completedDefaults.Add(entry.Key);
					repairNeeded = true;
					continue;
				}

				_configValues[entry.Key] = entry.Value ?? string.Empty;
			}

			foreach (KeyValuePair<string, string> entry in ConfigDefaults.Values) {
				if (!persisted.ContainsKey(entry.Key)) {
					completedDefaults.Add(entry.Key);
					repairNeeded = true;
				}
			}

			ConfigSaveResult repairResult = repairNeeded
				? SaveLocked()
				: new ConfigSaveResult(ConfigSaveStatus.NoChanges);
			LastSaveResult = repairResult;
			LastLoadResult = new ConfigLoadResult(
				repairNeeded ? ConfigLoadStatus.PartialDefaultCompletion : ConfigLoadStatus.NormalLoad,
				_persistenceStatus,
				completedDefaults,
				Diagnostic: repairResult.Diagnostic);
			return LastLoadResult;
		}
	}

	private ConfigLoadResult RecoverMalformedFile(JsonException exception) {
		string backupPath;
		try {
			backupPath = CreateCorruptionBackupPath();
			_persistence.CopyFile(_configFilePath, backupPath);
		} catch (UnauthorizedAccessException ex) {
			return SetMalformedRetainedResult(ex);
		} catch (SecurityException ex) {
			return SetMalformedRetainedResult(ex);
		} catch (IOException ex) {
			return SetMalformedRetainedResult(ex);
		}

		ConfigSaveResult saveResult = SaveLocked();
		LastSaveResult = saveResult;
		LastLoadResult = new ConfigLoadResult(
			saveResult.IsDurable
				? ConfigLoadStatus.MalformedFileRecovery
				: ConfigLoadStatus.MalformedFileRetainedNotRepairable,
			_persistenceStatus,
			ConfigDefaults.Values.Keys.ToArray(),
			backupPath,
			saveResult.Diagnostic ?? exception.Message);
		return LastLoadResult;
	}

	private ConfigLoadResult SetMalformedRetainedResult(Exception exception) {
		_persistenceStatus = ConfigPersistenceStatus.Unavailable;
		LastSaveResult = new ConfigSaveResult(MapSaveFailure(exception), exception.Message);
		LastLoadResult = new ConfigLoadResult(
			ConfigLoadStatus.MalformedFileRetainedNotRepairable,
			_persistenceStatus,
			ConfigDefaults.Values.Keys.ToArray(),
			Diagnostic: exception.Message);
		return LastLoadResult;
	}

	private ConfigLoadResult SetUnavailableLoadResult(ConfigLoadStatus status, Exception exception) {
		_persistenceStatus = ConfigPersistenceStatus.Unavailable;
		LastSaveResult = new ConfigSaveResult(MapSaveFailure(exception), exception.Message);
		LastLoadResult = new ConfigLoadResult(
			status,
			_persistenceStatus,
			ConfigDefaults.Values.Keys.ToArray(),
			Diagnostic: exception.Message);
		return LastLoadResult;
	}

	private ConfigSaveResult SaveLocked() {
		if (_persistenceStatus == ConfigPersistenceStatus.Unavailable) {
			return new ConfigSaveResult(
				ConfigSaveStatus.SkippedPersistenceUnavailable,
				LastSaveResult.Diagnostic);
		}

		try {
			string json = JsonConvert.SerializeObject(_configValues, Formatting.Indented);
			_persistence.WriteAllTextAtomic(_configFilePath, json);
			return new ConfigSaveResult(ConfigSaveStatus.Saved);
		} catch (UnauthorizedAccessException ex) {
			return SetSaveUnavailable(ex);
		} catch (SecurityException ex) {
			return SetSaveUnavailable(ex);
		} catch (IOException ex) {
			return SetSaveUnavailable(ex);
		}
	}

	private ConfigSaveResult SetSaveUnavailable(Exception exception) {
		_persistenceStatus = ConfigPersistenceStatus.Unavailable;
		return new ConfigSaveResult(MapSaveFailure(exception), exception.Message);
	}

	private bool BecameUnavailable(ConfigPersistenceStatus previousStatus) =>
		previousStatus == ConfigPersistenceStatus.Available
		&& _persistenceStatus == ConfigPersistenceStatus.Unavailable;

	private void NotifyPersistenceUnavailable(bool becameUnavailable, ConfigSaveResult saveResult) {
		if (becameUnavailable) {
			PersistenceBecameUnavailable?.Invoke(
				this,
				new ConfigPersistenceUnavailableEventArgs(saveResult));
		}
	}

	private string CreateCorruptionBackupPath() {
		string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
		string candidate = $"{_configFilePath}.corrupt-{timestamp}";
		int suffix = 0;
		while (_persistence.FileExists(candidate)) {
			suffix++;
			candidate = $"{_configFilePath}.corrupt-{timestamp}-{suffix}";
		}
		return candidate;
	}

	private static ConfigSaveStatus MapSaveFailure(Exception exception) =>
		exception is UnauthorizedAccessException or SecurityException
			? ConfigSaveStatus.AccessDenied
			: ConfigSaveStatus.WriteUnavailable;

	private static Dictionary<string, string> CreateDefaultsMap() =>
		new(ConfigDefaults.Values, StringComparer.Ordinal);
}
