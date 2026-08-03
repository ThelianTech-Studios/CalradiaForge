namespace CalradiaForge.Core.Infra.DependencyInjection;

using CalradiaForge.Core.Infra.Paths;

/// <summary>Supplies application-owned persistence and content paths to Core registrations.</summary>
public sealed class CalradiaForgeCoreOptions {
	public string ConfigFilePath { get; init; } = AppPaths.ConfigFilePath;
	public string LogsDirectory { get; init; } = AppPaths.LogsDirectory;
	public string LogsFilePath { get; init; } = AppPaths.LogsFilePath;
	public string ModsCurrentFilePath { get; init; } = AppPaths.ModsCurrentFilePath;
	public string ModsBackupFilePath { get; init; } = AppPaths.ModsBackupFilePath;
	public string ModpacksDirectory { get; init; } = AppPaths.ModpacksDirectory;
	public string LastUsedModsFilePath { get; init; } = AppPaths.LastUsedModsFilePath;
	public string LanguagesDirectory { get; init; } = AppPaths.LanguagesDirectory;
	public string LanguagesManifestFilePath { get; init; } = AppPaths.LanguagesManifestFilePath;
	public string DefaultLanguageFilePath { get; init; } = AppPaths.DefaultLanguageFilePath;

	internal void Validate() {
		ArgumentException.ThrowIfNullOrWhiteSpace(ConfigFilePath);
		ArgumentException.ThrowIfNullOrWhiteSpace(LogsDirectory);
		ArgumentException.ThrowIfNullOrWhiteSpace(LogsFilePath);
		ArgumentException.ThrowIfNullOrWhiteSpace(ModsCurrentFilePath);
		ArgumentException.ThrowIfNullOrWhiteSpace(ModsBackupFilePath);
		ArgumentException.ThrowIfNullOrWhiteSpace(ModpacksDirectory);
		ArgumentException.ThrowIfNullOrWhiteSpace(LastUsedModsFilePath);
		ArgumentException.ThrowIfNullOrWhiteSpace(LanguagesDirectory);
		ArgumentException.ThrowIfNullOrWhiteSpace(LanguagesManifestFilePath);
		ArgumentException.ThrowIfNullOrWhiteSpace(DefaultLanguageFilePath);
	}
}
