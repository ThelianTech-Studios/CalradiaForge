namespace CalradiaForge.Core.Infra.Config;

using CalradiaForge.Core.Infra.Launch;
using CalradiaForge.Core.Infra.Paths;

/// <summary>Owns the complete set of recognized configuration defaults.</summary>
internal static class ConfigDefaults {
	public static IReadOnlyDictionary<string, string> Values { get; } =
		new Dictionary<string, string>(StringComparer.Ordinal) {
			["Language"] = "en-US",
			["GameFolderPath"] = string.Empty,
			["GameLauncherFilePath"] = string.Empty,
			["SteamWorkshopFolderPath"] = string.Empty,
			["GamePlatform"] = GameProvider.NotInitialized.ToString(),
			["LastSelectedModpack"] = "Last Used",
			["ModpackStartupMode"] = ModpackStartupMode.AlwaysAsk.ToString(),
			["LastUnblockRunDate"] = string.Empty,
			["LastUnblockRunResult"] = string.Empty,
			["BLSEExePath"] = string.Empty,
			["DefaultLaunchTarget"] = LaunchTarget.Bannerlord.ToString(),
			["EulaAccepted"] = bool.FalseString,
			["DebugMode"] = bool.FalseString
		};

	public static IReadOnlySet<string> ObsoleteKeys { get; } =
		new HashSet<string>(StringComparer.Ordinal) {
			"LogFileDaysToKeep"
		};

	public static bool IsValidPersistedValue(string key, string value) => key switch {
		"GamePlatform" => IsDefinedEnum<GameProvider>(value),
		"ModpackStartupMode" => IsDefinedEnum<ModpackStartupMode>(value),
		"DefaultLaunchTarget" => IsDefinedEnum<LaunchTarget>(value),
		"EulaAccepted" or "DebugMode" => bool.TryParse(value, out _),
		_ => true
	};

	public static TEnum GetEnum<TEnum>(string key) where TEnum : struct, Enum =>
		Enum.Parse<TEnum>(Values[key]);

	public static bool GetBool(string key) => bool.Parse(Values[key]);

	private static bool IsDefinedEnum<TEnum>(string value) where TEnum : struct, Enum =>
		Enum.TryParse(value, out TEnum parsed) && Enum.IsDefined(parsed);
}
