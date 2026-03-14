namespace CalradiaForge.Core.Infra.Paths {
	using System;
	using System.Reflection;

	using CalradiaForge.Core.Infra.Logging;

	/// <summary>
	/// Provides resolved application paths and ensures required folders exist.
	/// </summary>
	public static class AppPaths {
		/// <summary>
		/// Gets the root directory used for all application data folders.
		/// </summary>
		public static string RootDirectory { get; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;

		private const string ConfigFolderName = "Config";
		private const string LogsFolderName = "Logs";
		private const string ModpacksFolderName = "Modpacks";
		private const string DataFolderName = "Data";
		private const string LanguagesFolderName = "Languages";
		private const string ResourcesFolderName = "Resources";

		private const string ModsCurrentFileName = "mods_current.data";
		private const string ModsBackupFileName = "mods_backup.data";
		private const string LastUsedModsFileName = "last_used_mods.data";
		private const string ConfigFileName = "config.json";
		private const string LanguagesManifestFileName = "languages.json";
		private const string DefaulLanguageFileName = "en-US.json";
		private const string EulaFileName = "Eula.txt";

		private static readonly Lazy<ResolvedDirectory> _configDirectory = new(() => ResolveDirectory(Path.Combine(RootDirectory, ConfigFolderName)));
		private static readonly Lazy<ResolvedDirectory> _logsDirectory = new(() => ResolveDirectory(Path.Combine(RootDirectory, LogsFolderName)));
		private static readonly Lazy<ResolvedDirectory> _modpacksDirectory = new(() => ResolveDirectory(Path.Combine(RootDirectory, ModpacksFolderName)));
		private static readonly Lazy<ResolvedDirectory> _dataDirectory = new(() => ResolveDirectory(Path.Combine(RootDirectory, DataFolderName)));
		private static readonly Lazy<ResolvedDirectory> _languagesDirectory = new(() => ResolveDirectory(Path.Combine(RootDirectory, LanguagesFolderName)));

		/// <summary>
		/// Gets the configuration directory path.
		/// </summary>
		public static string ConfigDirectory => _configDirectory.Value.Path;
		/// <summary>
		/// Gets the logs directory path.
		/// </summary>
		public static string LogsDirectory => _logsDirectory.Value.Path;
		/// <summary>
		/// Gets the modpacks directory path.
		/// </summary>
		public static string ModpacksDirectory => _modpacksDirectory.Value.Path;
		/// <summary>
		/// Gets the data directory path.
		/// </summary>
		public static string DataDirectory => _dataDirectory.Value.Path;
		/// <summary>
		/// Gets the languages directory path.
		/// </summary>
		public static string LanguagesDirectory => _languagesDirectory.Value.Path;
		/// <summary>
		/// Gets the path to the current mods cache file.
		/// </summary>
		public static string ModsCurrentFilePath => Path.Combine(DataDirectory, ModsCurrentFileName);
		/// <summary>
		/// Gets the path to the backup mods cache file.
		/// </summary>
		public static string ModsBackupFilePath => Path.Combine(DataDirectory, ModsBackupFileName);
		/// <summary>
		/// Gets the path to the last-used mods data file.
		/// </summary>
		public static string LastUsedModsFilePath => Path.Combine(DataDirectory, LastUsedModsFileName);
		/// <summary>
		/// Gets the path to the configuration JSON file.
		/// </summary>
		public static string ConfigFilePath => Path.Combine(ConfigDirectory, ConfigFileName);

		public static string LanguagesManifestFilePath => Path.Combine(LanguagesDirectory, LanguagesManifestFileName);
		/// <summary>
		/// Gets the path to the default language JSON file (e.g. <c>en-US.json</c>) in the languages directory.
		/// </summary>
		public static string DefaultLanguageFilePath => Path.Combine(LanguagesDirectory, DefaulLanguageFileName);

		/// <summary>
		/// Gets the path to the EULA text file deployed with the application.
		/// </summary>
		public static string EulaFilePath => Path.Combine(RootDirectory, ResourcesFolderName, EulaFileName);

		/// <summary>
		/// Logs resolved paths with creation metadata.
		/// </summary>
		public static void LogResolvedPaths(Logger logger) {
			if (logger.MinimumLevel != Logger.LogLevel.Debug) {
				return;
			}
			LogResolvedPath(logger, _configDirectory.Value);
			LogResolvedPath(logger, _logsDirectory.Value);
			LogResolvedPath(logger, _modpacksDirectory.Value);
			LogResolvedPath(logger, _dataDirectory.Value);
			LogResolvedPath(logger, _languagesDirectory.Value);
		}

		private static void LogResolvedPath(Logger logger, ResolvedDirectory directory) {
			logger.Debug("AppPaths: Resolved path.", new { directory.Path, directory.Created });
		}

		private static ResolvedDirectory ResolveDirectory(string path) {
			bool existed = Directory.Exists(path);
			if (!existed) {
				Directory.CreateDirectory(path);
			}
			return new ResolvedDirectory(path, created: !existed);
		}

		private sealed class ResolvedDirectory {
			public ResolvedDirectory(string path, bool created) {
				Path = path;
				Created = created;
			}

			public string Path { get; }
			public bool Created { get; }
		}
	}
}
