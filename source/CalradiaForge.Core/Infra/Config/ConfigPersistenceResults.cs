namespace CalradiaForge.Core.Infra.Config;

/// <summary>Describes how the current in-memory configuration was obtained.</summary>
public enum ConfigLoadStatus {
	NormalLoad,
	MissingFileInitialization,
	PartialDefaultCompletion,
	MalformedFileRecovery,
	MalformedFileRetainedNotRepairable,
	ReadUnavailable,
	AccessDenied
}

/// <summary>Describes whether configuration changes can currently be made durable.</summary>
public enum ConfigPersistenceStatus {
	Available,
	Unavailable
}

/// <summary>Describes the outcome of a requested configuration write.</summary>
public enum ConfigSaveStatus {
	Saved,
	NoChanges,
	SkippedPersistenceUnavailable,
	AccessDenied,
	WriteUnavailable
}

/// <summary>Semantic configuration-load result safe to consume outside Core.</summary>
public sealed record ConfigLoadResult(
	ConfigLoadStatus Status,
	ConfigPersistenceStatus PersistenceStatus,
	IReadOnlyList<string> CompletedDefaultKeys,
	string? CorruptionBackupPath = null,
	string? Diagnostic = null);

/// <summary>Semantic configuration-save result safe to consume outside Core.</summary>
public sealed record ConfigSaveResult(
	ConfigSaveStatus Status,
	string? Diagnostic = null) {
	public bool IsDurable => Status is ConfigSaveStatus.Saved or ConfigSaveStatus.NoChanges;
}

/// <summary>Signals the one-way transition from durable to unavailable persistence.</summary>
public sealed class ConfigPersistenceUnavailableEventArgs(ConfigSaveResult saveResult) : EventArgs {
	public ConfigSaveResult SaveResult { get; } = saveResult;
}
