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

		// Folder Names in RootDirectory
		private const string ConfigFolderName = "Config";
		private const string LogsFolderName = "Logs";
		private const string ModpacksFolderName = "Modpacks";
		private const string DataFolderName = "Data";
		private const string LanguagesFolderName = "Languages";
		private const string ResourcesFolderName = "Resources";
		private const string NexusModsFolderName = "Nexus";

		// Folder Names in RootDirectory/Nexus
		private const string ModArchivesFolderName = "Downloads";
		private const string DownloadsMetadataFolderName = "ModsMetadata";
		private const string ExtractionFolderName = "Extraction";

		// File Names
		private const string ModsCurrentFileName = "mods_current.data";
		private const string ModsBackupFileName = "mods_backup.data";
		private const string LastUsedModsFileName = "last_used_mods.data";
		private const string ConfigFileName = "config.json";
		private const string LanguagesManifestFileName = "languages.json";
		private const string DefaulLanguageFileName = "en-US.json";
		private const string EulaFileName = "Eula.txt";

		#region Resolved Paths
		// Resolved Paths
		private static readonly Lazy<ResolvedDirectory> _configDirectory = new(() => ResolveDirectory(Path.Combine(RootDirectory, ConfigFolderName)));
		private static readonly Lazy<ResolvedDirectory> _logsDirectory = new(() => ResolveDirectory(Path.Combine(RootDirectory, LogsFolderName)));
		private static readonly Lazy<ResolvedDirectory> _modpacksDirectory = new(() => ResolveDirectory(Path.Combine(RootDirectory, ModpacksFolderName)));
		private static readonly Lazy<ResolvedDirectory> _dataDirectory = new(() => ResolveDirectory(Path.Combine(RootDirectory, DataFolderName)));
		private static readonly Lazy<ResolvedDirectory> _languagesDirectory = new(() => ResolveDirectory(Path.Combine(RootDirectory, LanguagesFolderName)));
		private static readonly Lazy<ResolvedDirectory> _DownloadsDirectory = new(() => ResolveDirectory(Path.Combine(RootDirectory, NexusModsFolderName, ModArchivesFolderName)));
		private static readonly Lazy<ResolvedDirectory> _DownloadsMetadataDirectory = new(() => ResolveDirectory(Path.Combine(RootDirectory, NexusModsFolderName, DownloadsMetadataFolderName)));

		// Resolved Hidden Paths
		private static readonly Lazy<ResolvedHiddenDirectory> _ExtractionDirectory = new(() => ResolveHiddenDirectory(Path.Combine(RootDirectory, NexusModsFolderName, ExtractionFolderName)));
		#endregion

		#region Directory Properties
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
		/// Gets the path to the downloads directory in LocalAppData.
		/// </summary>
		public static string DownloadsDirectory => _DownloadsDirectory.Value.Path;
		/// <summary>
		/// Gets the path to the temporary extraction directory in LocalAppData.
		/// </summary>
		public static string ExtractionDirectory => _ExtractionDirectory.Value.Path;
		/// <summary>
		/// Gets the path to the downloads metadata directory.
		/// </summary>
		public static string DownloadsMetadataDirectory => _DownloadsMetadataDirectory.Value.Path;
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
		#endregion

		#region ResolvedPaths Logging
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
			LogResolvedPath(logger, _DownloadsDirectory.Value);
			LogResolvedPath(logger, _DownloadsMetadataDirectory.Value);
			LogResolvedHiddenPath(logger, _ExtractionDirectory.Value);

		}

		private static void LogResolvedPath(Logger logger, ResolvedDirectory directory) {
			logger.Debug("AppPaths: Resolved path.", new { directory.Path, directory.Created });
		}
		private static void LogResolvedHiddenPath(Logger logger, ResolvedHiddenDirectory directory) {
			logger.Debug("AppPaths: Resolved hidden path.", new { directory.Path, directory.Created, directory.Hidden });
		}
		#endregion

		#region Resolve Methods
		private static ResolvedDirectory ResolveDirectory(string path) {
			bool existed = Directory.Exists(path);
			if (existed) {
				return new ResolvedDirectory(path, created: false);
			}
			Directory.CreateDirectory(path);
			return new ResolvedDirectory(path, created: true);
		}
		private static ResolvedHiddenDirectory ResolveHiddenDirectory(string path) {
			bool existed = Directory.Exists(path);
			DirectoryInfo info;
			if (existed) {
				info = new DirectoryInfo(path);
				return new ResolvedHiddenDirectory(path, created: false, hidden: info.Attributes.HasFlag(FileAttributes.Hidden));
			}
			info = Directory.CreateDirectory(path);
			info.Attributes |= FileAttributes.Hidden;
			return new ResolvedHiddenDirectory(path, created: true, hidden: info.Attributes.HasFlag(FileAttributes.Hidden));
		}
		#endregion

		#region ResolvedDirectory Classes
		private sealed class ResolvedDirectory {
			public ResolvedDirectory(string path, bool created) {
				Path = path;
				Created = created;
			}

			public string Path { get; }
			public bool Created { get; }
		}
		private sealed class ResolvedHiddenDirectory {
			public ResolvedHiddenDirectory(string path, bool created, bool hidden) {
				Path = path;
				Created = created;
				Hidden = hidden;
			}
			public string Path { get; }
			public bool Created { get; }
			public bool Hidden { get; }
		}
		#endregion
	}
}
