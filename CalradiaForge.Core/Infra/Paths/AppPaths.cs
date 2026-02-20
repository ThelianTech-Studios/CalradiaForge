namespace CalradiaForge.Core.Infra.Paths {
	using System;
	using System.Reflection;

	using CalradiaForge.Core.Infra.Logging;

	/// <summary>
	/// Provides resolved application paths and ensures required folders exist.
	/// </summary>
	public static class AppPaths {
		private static readonly Logger _logger = Logger.Instance;
		/// <summary>
		/// Gets the root directory used for all application data folders.
		/// </summary>
		public static string RootDirectory { get; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;

		private const string ConfigFolderName = "Config";
		private const string LogsFolderName = "Logs";
		private const string ModpacksFolderName = "Modpacks";
		private const string DataFolderName = "Data";
		private const string LanguagesFolderName = "Languages";

		private const string ModsCurrentFileName = "mods_current.data";
		private const string ModsBackupFileName = "mods_backup.data";
		private const string LastUsedModsFileName = "last_used_mods.data";
		private const string ConfigFileName = "config.json";

		/// <summary>
		/// Gets the configuration directory path.
		/// </summary>
		public static string ConfigDirectory => EnsureDirectoryExists(Path.Combine(RootDirectory, ConfigFolderName));
		/// <summary>
		/// Gets the logs directory path.
		/// </summary>
		public static string LogsDirectory => EnsureDirectoryExists(Path.Combine(RootDirectory, LogsFolderName));
		/// <summary>
		/// Gets the modpacks directory path.
		/// </summary>
		public static string ModpacksDirectory => EnsureDirectoryExists(Path.Combine(RootDirectory, ModpacksFolderName));
		/// <summary>
		/// Gets the data directory path.
		/// </summary>
		public static string DataDirectory => EnsureDirectoryExists(Path.Combine(RootDirectory, DataFolderName));
		/// <summary>
		/// Gets the languages directory path.
		/// </summary>
		public static string LanguagesDirectory => EnsureDirectoryExists(Path.Combine(RootDirectory, LanguagesFolderName));
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

		/// <summary>
		/// Ensures a directory exists and returns the resolved path.
		/// </summary>
		private static string EnsureDirectoryExists(string path) {
			bool existed = Directory.Exists(path);
			if (!existed) {
				Directory.CreateDirectory(path);
			}
			if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
				_logger.Debug("AppPaths: Resolved path.", new { Path = path, Created = !existed });
			}
			return path;
		}
	}
}
