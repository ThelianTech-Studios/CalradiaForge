namespace CalradiaForge.Tests.Core.GamePlatform;

using System.Text;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.GamePlatform.Steam;
using CalradiaForge.Core.Models;
using CalradiaForge.Tests.Core.Support;

internal static class GamePlatformTestFixture {
	public const string BannerlordAppId = "261550";
	public const string BannerlordInstallDirectory = "Mount & Blade II Bannerlord";

	public static AppSettings CreateSettings(TestDirectory temp) {
		return new AppSettings(new ConfigFileManager(temp.GetPath($"config-{Guid.NewGuid():N}.json")));
	}

	public static string CreateLibrary(TestDirectory temp, params string[] segments) {
		return temp.CreateDirectory(segments);
	}

	public static string CreateValidBannerlordInstall(
		TestDirectory temp,
		string libraryRoot,
		string installDirectory = BannerlordInstallDirectory,
		bool writeManifest = true) {
		string gameRoot = Directory.CreateDirectory(Path.Combine(
			libraryRoot,
			"steamapps",
			"common",
			installDirectory)).FullName;
		CreateGameMarkers(temp, gameRoot);
		if (writeManifest) {
			WriteAppManifest(libraryRoot, BannerlordAppId, installDirectory);
		}
		return gameRoot;
	}

	public static string CreateValidManualGame(TestDirectory temp, params string[] segments) {
		string gameRoot = temp.CreateDirectory(segments);
		CreateGameMarkers(temp, gameRoot);
		return gameRoot;
	}

	public static void CreateGameMarkers(TestDirectory temp, string gameRoot) {
		string modulesRoot = Directory.CreateDirectory(Path.Combine(gameRoot, "Modules")).FullName;
		temp.WriteModule(modulesRoot, "Native", "Native");
		string launcherDirectory = Directory.CreateDirectory(Path.Combine(
			gameRoot,
			"bin",
			"Win64_Shipping_Client")).FullName;
		File.WriteAllText(Path.Combine(launcherDirectory, "Bannerlord.exe"), string.Empty);
	}

	public static string CreateBlseExecutable(string gameRoot) {
		string path = Path.Combine(
			gameRoot,
			"bin",
			"Win64_Shipping_Client",
			"Bannerlord.BLSE.Standalone.exe");
		File.WriteAllText(path, string.Empty);
		return path;
	}

	public static string CreateWorkshopContent(string libraryRoot) {
		return Directory.CreateDirectory(GetWorkshopPath(libraryRoot)).FullName;
	}

	public static string GetWorkshopPath(string libraryRoot) {
		return Path.Combine(libraryRoot, "steamapps", "workshop", "content", BannerlordAppId);
	}

	public static string GetLauncherPath(string gameRoot) {
		return Path.Combine(gameRoot, "bin", "Win64_Shipping_Client", "Bannerlord.exe");
	}

	public static void WriteLibraryFolders(string clientRoot, params string[] registeredRoots) {
		StringBuilder content = new();
		content.AppendLine("\"libraryfolders\"");
		content.AppendLine("{");
		for (int index = 0; index < registeredRoots.Length; index++) {
			content.AppendLine($"    \"{index}\"");
			content.AppendLine("    {");
			content.AppendLine($"        \"path\" \"{EscapeMetadataValue(registeredRoots[index])}\"");
			content.AppendLine("        \"apps\"");
			content.AppendLine("        {");
			content.AppendLine($"            \"{BannerlordAppId}\" \"1\"");
			content.AppendLine("        }");
			content.AppendLine("    }");
		}
		content.AppendLine("}");
		WriteLibraryFoldersContent(clientRoot, content.ToString());
	}

	public static void WriteLibraryFoldersContent(string clientRoot, string content) {
		string steamApps = Directory.CreateDirectory(Path.Combine(clientRoot, "steamapps")).FullName;
		File.WriteAllText(Path.Combine(steamApps, "libraryfolders.vdf"), content);
	}

	public static void WriteAppManifest(string libraryRoot, string appId, string? installDirectory) {
		StringBuilder content = new();
		content.AppendLine("\"AppState\"");
		content.AppendLine("{");
		content.AppendLine($"    \"appid\" \"{EscapeMetadataValue(appId)}\"");
		if (installDirectory is not null) {
			content.AppendLine($"    \"installdir\" \"{EscapeMetadataValue(installDirectory)}\"");
		}
		content.AppendLine("}");
		WriteManifestContent(libraryRoot, content.ToString());
	}

	public static void WriteManifestContent(string libraryRoot, string content) {
		string steamApps = Directory.CreateDirectory(Path.Combine(libraryRoot, "steamapps")).FullName;
		File.WriteAllText(Path.Combine(steamApps, "appmanifest_261550.acf"), content);
	}

	public static void WriteWorkshopManifest(string libraryRoot, string appId = BannerlordAppId) {
		string workshopRoot = Directory.CreateDirectory(Path.Combine(
			libraryRoot,
			"steamapps",
			"workshop")).FullName;
		File.WriteAllText(
			Path.Combine(workshopRoot, "appworkshop_261550.acf"),
			$"\"AppWorkshop\"\n{{\n    \"appid\" \"{EscapeMetadataValue(appId)}\"\n}}");
	}

	private static string EscapeMetadataValue(string value) {
		return value
			.Replace("\\", "\\\\", StringComparison.Ordinal)
			.Replace("\"", "\\\"", StringComparison.Ordinal);
	}

	internal sealed class FakeSteamClientRootProvider(string? steamClientRoot) : ISteamClientRootProvider {
		public int CallCount { get; private set; }

		public string? GetSteamClientRoot() {
			CallCount++;
			return steamClientRoot;
		}
	}

	internal sealed class ThrowingSteamClientRootProvider : ISteamClientRootProvider {
		public string? GetSteamClientRoot() {
			throw new IOException("Synthetic Steam root failure.");
		}
	}

	internal sealed class FakeSteamInstallationResolver(
		Func<string, SteamResolutionOptions?, SteamResolutionResult> resolve) : ISteamInstallationResolver {
		public int CallCount { get; private set; }

		public SteamResolutionResult ResolveBannerlord(
			string steamClientRoot,
			SteamResolutionOptions? options = null) {
			CallCount++;
			return resolve(steamClientRoot, options);
		}
	}

	public static SteamResolutionResult Resolution(
		string clientRoot,
		string? gameRoot,
		string? workshopRoot = null,
		SteamResolutionStatus? status = null) {
		return new SteamResolutionResult(
			status ?? (workshopRoot is null
				? SteamResolutionStatus.SteamGameResolvedWithoutWorkshop
				: SteamResolutionStatus.SteamGameResolved),
			clientRoot,
			[clientRoot],
			clientRoot,
			gameRoot,
			workshopRoot,
			workshopRoot is null ? WorkshopPathSource.None : WorkshopPathSource.BannerlordLibrary,
			Array.Empty<SteamPathDiagnostic>());
	}
}
