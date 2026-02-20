namespace CalradiaForge.Core.Infra.Paths
	{
	using System;
	using System.Reflection;

	public static class AppPaths
		{
		public static string RootDirectory { get; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)??AppDomain.CurrentDomain.BaseDirectory;

		private const string ConfigFolderName = "Config";
		private const string LogsFolderName = "Logs";
		private const string ModpacksFolderName = "Modpacks";
		private const string DataFolderName = "Data";
		private const string LanguagesFolderName = "Languages";

        private const string ModsCurrentFileName = "mods_current.data";
		private const string ModsBackupFileName = "mods_backup.data";
		private const string LastUsedModsFileName = "last_used_mods.data";
		private const string ConfigFileName = "config.json";

		public static string ConfigDirectory => EnsureDirectoryExists(Path.Combine(RootDirectory,ConfigFolderName));
		public static string LogsDirectory => EnsureDirectoryExists(Path.Combine(RootDirectory,LogsFolderName));
		public static string ModpacksDirectory => EnsureDirectoryExists(Path.Combine(RootDirectory,ModpacksFolderName));
		public static string DataDirectory => EnsureDirectoryExists(Path.Combine(RootDirectory,DataFolderName));
		public static string LanguagesDirectory => EnsureDirectoryExists(Path.Combine(RootDirectory,LanguagesFolderName));
        public static string ModsCurrentFilePath => Path.Combine(DataDirectory,ModsCurrentFileName);
		public static string ModsBackupFilePath => Path.Combine(DataDirectory,ModsBackupFileName);
		public static string LastUsedModsFilePath => Path.Combine(DataDirectory,LastUsedModsFileName);
		public static string ConfigFilePath => Path.Combine(ConfigDirectory,ConfigFileName);

		private static string EnsureDirectoryExists(string path) {
			if (!Directory.Exists(path)) {
				Directory.CreateDirectory(path);
				}
			return path;
			}
		}
	}
