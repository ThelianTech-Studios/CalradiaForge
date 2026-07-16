namespace CalradiaForge.Core.Infra.GamePlatform.Steam;

using CalradiaForge.Core.Infra.Paths;

/// <summary>
/// Resolves a Steam Bannerlord installation from an explicit Steam client root.
/// </summary>
public interface ISteamInstallationResolver {
	/// <summary>
	/// Resolves Bannerlord and one active Workshop path from registered Steam libraries.
	/// </summary>
	SteamResolutionResult ResolveBannerlord(
		string steamClientRoot,
		SteamResolutionOptions? options = null);
}

/// <summary>
/// Resolves Bannerlord and one active Workshop path from local Steam metadata.
/// </summary>
public sealed class SteamInstallationResolver : ISteamInstallationResolver {
	private const string _bannerlordAppId = "261550";
	private const string _libraryFoldersFileName = "libraryfolders.vdf";
	private const string _bannerlordManifestFileName = "appmanifest_261550.acf";
	private const string _bannerlordWorkshopManifestFileName = "appworkshop_261550.acf";
	private static readonly StringComparer _pathComparer = StringComparer.OrdinalIgnoreCase;

	/// <summary>
	/// Resolves Bannerlord through the Steam libraries registered by the supplied client root.
	/// </summary>
	public SteamResolutionResult ResolveBannerlord(
		string steamClientRoot,
		SteamResolutionOptions? options = null) {
		options ??= new SteamResolutionOptions();
		List<SteamPathDiagnostic> diagnostics = [];

		if (!TryNormalizeDirectory(steamClientRoot, out string normalizedClientRoot)
			|| !Directory.Exists(normalizedClientRoot)) {
			diagnostics.Add(new SteamPathDiagnostic(
				"SteamClientRootUnavailable",
				"The Steam client root was missing, invalid, or inaccessible.",
				steamClientRoot,
				IsWarning: true));
			return Failure(
				SteamResolutionStatus.SteamClientNotFound,
				steamClientRoot,
				[],
				diagnostics);
		}

		List<string> libraryRoots = DiscoverLibraryRoots(normalizedClientRoot, diagnostics);
		if (libraryRoots.Count == 0) {
			return Failure(
				SteamResolutionStatus.SteamLibrariesNotResolved,
				normalizedClientRoot,
				libraryRoots,
				diagnostics);
		}

		ManifestSearchState manifestState = new();
		foreach (string libraryRoot in libraryRoots) {
			if (!TryResolveGameFromLibrary(libraryRoot, diagnostics, manifestState, out string? gameFolderPath)) {
				continue;
			}

			(string? workshopPath, WorkshopPathSource source) = ResolveWorkshopPath(
				libraryRoot,
				libraryRoots,
				options,
				diagnostics);
			SteamResolutionStatus status = workshopPath is null
				? SteamResolutionStatus.SteamGameResolvedWithoutWorkshop
				: SteamResolutionStatus.SteamGameResolved;
			diagnostics.Add(new SteamPathDiagnostic(
				"ResolutionComplete",
				status == SteamResolutionStatus.SteamGameResolved
					? "Steam Bannerlord and an active Workshop path were resolved."
					: "Steam Bannerlord was resolved without Workshop content.",
				gameFolderPath,
				IsWarning: workshopPath is null));

			return new SteamResolutionResult(
				status,
				normalizedClientRoot,
				libraryRoots,
				libraryRoot,
				gameFolderPath,
				workshopPath,
				source,
				diagnostics);
		}

		SteamResolutionStatus failureStatus = manifestState.GamePathInvalid
			? SteamResolutionStatus.BannerlordGamePathInvalid
			: manifestState.ManifestInvalid
				? SteamResolutionStatus.BannerlordManifestInvalid
				: SteamResolutionStatus.BannerlordManifestNotFound;
		return Failure(
			failureStatus,
			normalizedClientRoot,
			libraryRoots,
			diagnostics);
	}

	private static List<string> DiscoverLibraryRoots(
		string steamClientRoot,
		List<SteamPathDiagnostic> diagnostics) {
		List<string> libraryRoots = [];
		HashSet<string> seenRoots = new(_pathComparer);
		AddLibraryRoot(steamClientRoot, "SteamClientLibrary", libraryRoots, seenRoots, diagnostics);

		string libraryFoldersPath = Path.Combine(steamClientRoot, "steamapps", _libraryFoldersFileName);
		diagnostics.Add(new SteamPathDiagnostic(
			"LibraryFoldersChecked",
			"Checked the Steam library catalog.",
			libraryFoldersPath));
		if (!File.Exists(libraryFoldersPath)) {
			diagnostics.Add(new SteamPathDiagnostic(
				"LibraryFoldersMissing",
				"The Steam library catalog was not found; the main client root remains available.",
				libraryFoldersPath,
				IsWarning: true));
			return libraryRoots;
		}

		try {
			string content = File.ReadAllText(libraryFoldersPath);
			if (!ValveKeyValuesParser.TryParse(content, out ValveKeyValuesParser.Node parsed, out string error)) {
				diagnostics.Add(new SteamPathDiagnostic(
					"LibraryFoldersParseFailed",
					$"The Steam library catalog could not be parsed: {error}",
					libraryFoldersPath,
					IsWarning: true));
				return libraryRoots;
			}

			ValveKeyValuesParser.Node? libraryFolders = parsed.Child("libraryfolders");
			if (libraryFolders is null) {
				diagnostics.Add(new SteamPathDiagnostic(
					"LibraryFoldersRootMissing",
					"The Steam library catalog does not contain a libraryfolders object.",
					libraryFoldersPath,
					IsWarning: true));
				return libraryRoots;
			}

			foreach (ValveKeyValuesParser.Node libraryEntry in libraryFolders.Children) {
				string? path = libraryEntry.Child("path")?.Value;
				if (string.IsNullOrWhiteSpace(path)) {
					diagnostics.Add(new SteamPathDiagnostic(
						"LibraryPathMissing",
						$"Steam library entry '{libraryEntry.Name}' has no path.",
						libraryFoldersPath,
						IsWarning: true));
					continue;
				}
				AddLibraryRoot(path, "RegisteredSteamLibrary", libraryRoots, seenRoots, diagnostics);
			}
		} catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
			diagnostics.Add(new SteamPathDiagnostic(
				"LibraryFoldersReadFailed",
				$"The Steam library catalog could not be read: {ex.Message}",
				libraryFoldersPath,
				IsWarning: true));
		}

		return libraryRoots;
	}

	private static void AddLibraryRoot(
		string candidate,
		string sourceCode,
		List<string> libraryRoots,
		HashSet<string> seenRoots,
		List<SteamPathDiagnostic> diagnostics) {
		if (!TryNormalizeDirectory(candidate, out string normalized)) {
			diagnostics.Add(new SteamPathDiagnostic(
				"LibraryPathInvalid",
				"A Steam library path could not be normalized.",
				candidate,
				IsWarning: true));
			return;
		}
		if (!seenRoots.Add(normalized)) {
			diagnostics.Add(new SteamPathDiagnostic(
				"DuplicateLibraryRemoved",
				"A duplicate Steam library path was ignored.",
				normalized));
			return;
		}

		libraryRoots.Add(normalized);
		diagnostics.Add(new SteamPathDiagnostic(
			sourceCode,
			Directory.Exists(normalized)
				? "Steam library root discovered."
				: "Steam library root was registered but is missing or inaccessible.",
			normalized,
			IsWarning: !Directory.Exists(normalized)));
	}

	private static bool TryResolveGameFromLibrary(
		string libraryRoot,
		List<SteamPathDiagnostic> diagnostics,
		ManifestSearchState state,
		out string? gameFolderPath) {
		gameFolderPath = null;
		if (!Directory.Exists(libraryRoot)) {
			diagnostics.Add(new SteamPathDiagnostic(
				"LibraryUnavailable",
				"The Steam library could not be inspected.",
				libraryRoot,
				IsWarning: true));
			return false;
		}

		string manifestPath = Path.Combine(libraryRoot, "steamapps", _bannerlordManifestFileName);
		diagnostics.Add(new SteamPathDiagnostic(
			"BannerlordManifestChecked",
			"Checked for the Bannerlord app manifest.",
			manifestPath));
		if (!File.Exists(manifestPath)) {
			return false;
		}
		state.ManifestFound = true;

		ValveKeyValuesParser.Node parsed;
		try {
			string content = File.ReadAllText(manifestPath);
			if (!ValveKeyValuesParser.TryParse(content, out parsed, out string error)) {
				state.ManifestInvalid = true;
				diagnostics.Add(new SteamPathDiagnostic(
					"BannerlordManifestParseFailed",
					$"The Bannerlord app manifest could not be parsed: {error}",
					manifestPath,
					IsWarning: true));
				return false;
			}
		} catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
			state.ManifestInvalid = true;
			diagnostics.Add(new SteamPathDiagnostic(
				"BannerlordManifestReadFailed",
				$"The Bannerlord app manifest could not be read: {ex.Message}",
				manifestPath,
				IsWarning: true));
			return false;
		}

		ValveKeyValuesParser.Node? appState = parsed.Child("AppState");
		string? appId = appState?.Child("appid")?.Value;
		if (!string.Equals(appId, _bannerlordAppId, StringComparison.Ordinal)) {
			state.ManifestInvalid = true;
			diagnostics.Add(new SteamPathDiagnostic(
				"BannerlordManifestAppIdMismatch",
				$"The app manifest did not identify Bannerlord App ID {_bannerlordAppId}.",
				manifestPath,
				IsWarning: true));
			return false;
		}

		string? installDirectory = appState?.Child("installdir")?.Value;
		if (string.IsNullOrWhiteSpace(installDirectory)) {
			state.ManifestInvalid = true;
			diagnostics.Add(new SteamPathDiagnostic(
				"BannerlordInstallDirMissing",
				"The Bannerlord app manifest has no installdir value.",
				manifestPath,
				IsWarning: true));
			return false;
		}

		string commonRoot = Path.GetFullPath(Path.Combine(libraryRoot, "steamapps", "common"));
		if (Path.IsPathRooted(installDirectory)) {
			state.ManifestInvalid = true;
			diagnostics.Add(new SteamPathDiagnostic(
				"BannerlordInstallDirUnsafe",
				"The Bannerlord installdir was rooted and was rejected.",
				installDirectory,
				IsWarning: true));
			return false;
		}

		string candidate;
		try {
			candidate = Path.GetFullPath(Path.Combine(commonRoot, installDirectory));
		} catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) {
			state.ManifestInvalid = true;
			diagnostics.Add(new SteamPathDiagnostic(
				"BannerlordInstallDirInvalid",
				$"The Bannerlord installdir could not be resolved: {ex.Message}",
				installDirectory,
				IsWarning: true));
			return false;
		}

		string relativeCandidate = Path.GetRelativePath(commonRoot, candidate);
		if (Path.IsPathRooted(relativeCandidate)
			|| relativeCandidate == ".."
			|| relativeCandidate.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
			|| relativeCandidate.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal)) {
			state.ManifestInvalid = true;
			diagnostics.Add(new SteamPathDiagnostic(
				"BannerlordInstallDirEscapesLibrary",
				"The Bannerlord installdir escaped the Steam common directory and was rejected.",
				candidate,
				IsWarning: true));
			return false;
		}

		if (!GamePathValidator.ValidateGameFolder(candidate)) {
			state.GamePathInvalid = true;
			diagnostics.Add(new SteamPathDiagnostic(
				"BannerlordGamePathInvalid",
				$"The manifest-backed Bannerlord path failed validation",
				candidate,
				IsWarning: true));
			return false;
		}

		gameFolderPath = candidate;
		diagnostics.Add(new SteamPathDiagnostic(
			"BannerlordLibrarySelected",
			"Selected the registered Steam library containing a validated Bannerlord manifest and install.",
			libraryRoot));
		diagnostics.Add(new SteamPathDiagnostic(
			"BannerlordGamePathValidated",
			"Validated the manifest-derived Bannerlord installation path.",
			candidate));
		return true;
	}

	private static (string? Path, WorkshopPathSource Source) ResolveWorkshopPath(
		string bannerlordLibraryRoot,
		IReadOnlyList<string> libraryRoots,
		SteamResolutionOptions options,
		List<SteamPathDiagnostic> diagnostics) {
		if (options.PreserveExistingWorkshopPath && !string.IsNullOrWhiteSpace(options.ExistingWorkshopPath)) {
			if (TryNormalizeDirectory(options.ExistingWorkshopPath, out string existingWorkshopPath)
				&& Directory.Exists(existingWorkshopPath)) {
				diagnostics.Add(new SteamPathDiagnostic(
					"ExistingWorkshopPathSelected",
					"Preserved the existing valid configured Workshop path.",
					existingWorkshopPath));
				return (existingWorkshopPath, WorkshopPathSource.ExistingConfiguration);
			}
			diagnostics.Add(new SteamPathDiagnostic(
				"ExistingWorkshopPathRejected",
				"The configured Workshop path was not a valid existing directory; automatic resolution continued.",
				options.ExistingWorkshopPath,
				IsWarning: true));
		}

		string primaryCandidate = GetWorkshopContentPath(bannerlordLibraryRoot);
		diagnostics.Add(new SteamPathDiagnostic(
			"PrimaryWorkshopCandidateChecked",
			"Checked the Bannerlord library Workshop path.",
			primaryCandidate));
		if (Directory.Exists(primaryCandidate)) {
			diagnostics.Add(new SteamPathDiagnostic(
				"PrimaryWorkshopPathSelected",
				"Selected the Bannerlord library Workshop path.",
				primaryCandidate));
			return (primaryCandidate, WorkshopPathSource.BannerlordLibrary);
		}
		diagnostics.Add(new SteamPathDiagnostic(
			"PrimaryWorkshopPathUnavailable",
			"The Bannerlord library did not contain a Workshop content directory for App ID 261550.",
			primaryCandidate,
			IsWarning: true));

		List<WorkshopCandidate> alternateCandidates = [];
		for (int index = 0; index < libraryRoots.Count; index++) {
			string libraryRoot = libraryRoots[index];
			if (_pathComparer.Equals(libraryRoot, bannerlordLibraryRoot)) {
				continue;
			}

			string contentPath = GetWorkshopContentPath(libraryRoot);
			string manifestPath = Path.Combine(
				libraryRoot,
				"steamapps",
				"workshop",
				_bannerlordWorkshopManifestFileName);
			bool contentExists = Directory.Exists(contentPath);
			bool manifestValid = IsBannerlordWorkshopManifestValid(manifestPath, diagnostics);
			diagnostics.Add(new SteamPathDiagnostic(
				"AlternateWorkshopCandidateChecked",
				$"Checked alternate Workshop candidate; content exists={contentExists}, manifest valid={manifestValid}.",
				contentPath,
				IsWarning: !contentExists));
			if (contentExists) {
				alternateCandidates.Add(new WorkshopCandidate(contentPath, manifestValid, index));
			}
		}

		WorkshopCandidate? selected = alternateCandidates
			.OrderByDescending(candidate => candidate.HasValidManifest)
			.ThenBy(candidate => candidate.DiscoveryIndex)
			.FirstOrDefault();
		if (selected is not null) {
			diagnostics.Add(new SteamPathDiagnostic(
				"AlternateWorkshopPathSelected",
				"Selected one deterministic Workshop fallback outside the Bannerlord library.",
				selected.Path,
				IsWarning: true));
			return (selected.Path, WorkshopPathSource.AlternateSteamLibrary);
		}

		diagnostics.Add(new SteamPathDiagnostic(
			"WorkshopPathUnavailable",
			"Steam Bannerlord was resolved, but no valid Workshop content directory was found.",
			primaryCandidate,
			IsWarning: true));
		return (null, WorkshopPathSource.None);
	}

	private static bool IsBannerlordWorkshopManifestValid(
		string manifestPath,
		List<SteamPathDiagnostic> diagnostics) {
		if (!File.Exists(manifestPath)) {
			return false;
		}
		try {
			string content = File.ReadAllText(manifestPath);
			if (!ValveKeyValuesParser.TryParse(content, out ValveKeyValuesParser.Node parsed, out string error)) {
				diagnostics.Add(new SteamPathDiagnostic(
					"WorkshopManifestParseFailed",
					$"An alternate Workshop manifest could not be parsed: {error}",
					manifestPath,
					IsWarning: true));
				return false;
			}

			ValveKeyValuesParser.Node? appWorkshop = parsed.Child("AppWorkshop");
			string? appId = appWorkshop?.Child("appid")?.Value;
			bool valid = string.Equals(appId, _bannerlordAppId, StringComparison.Ordinal);
			if (!valid) {
				diagnostics.Add(new SteamPathDiagnostic(
					"WorkshopManifestAppIdMismatch",
					$"An alternate Workshop manifest did not identify App ID {_bannerlordAppId}.",
					manifestPath,
					IsWarning: true));
			}
			return valid;
		} catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
			diagnostics.Add(new SteamPathDiagnostic(
				"WorkshopManifestReadFailed",
				$"An alternate Workshop manifest could not be read: {ex.Message}",
				manifestPath,
				IsWarning: true));
			return false;
		}
	}

	private static string GetWorkshopContentPath(string libraryRoot) {
		return Path.Combine(libraryRoot, "steamapps", "workshop", "content", _bannerlordAppId);
	}

	private static bool TryNormalizeDirectory(string? path, out string normalized) {
		normalized = string.Empty;
		if (string.IsNullOrWhiteSpace(path)) {
			return false;
		}
		try {
			normalized = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path.Trim()));
			return true;
		} catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) {
			return false;
		}
	}

	private static SteamResolutionResult Failure(
		SteamResolutionStatus status,
		string? steamClientRoot,
		IReadOnlyList<string> libraryRoots,
		List<SteamPathDiagnostic> diagnostics) {
		diagnostics.Add(new SteamPathDiagnostic(
			"ResolutionFailed",
			$"Steam Bannerlord resolution ended with status {status}.",
			steamClientRoot,
			IsWarning: true));
		return new SteamResolutionResult(
			status,
			steamClientRoot,
			libraryRoots,
			null,
			null,
			null,
			WorkshopPathSource.None,
			diagnostics);
	}

	private sealed class ManifestSearchState {
		public bool ManifestFound { get; set; }
		public bool ManifestInvalid { get; set; }
		public bool GamePathInvalid { get; set; }
	}

	private sealed record WorkshopCandidate(string Path, bool HasValidManifest, int DiscoveryIndex);
}
