namespace CalradiaForge.Core.Infra.Mods {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using CalradiaForge.Core.Infra.Config;
	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Models;

	/// <summary>
	/// High-level mod management service.
	/// Orchestrates scanning, caching to data files, and change detection.
	/// Provides the mod list for the UI layer to consume.
	/// Receives dependencies explicitly — never accesses global state.
	/// </summary>
	public sealed class ModService {
		private readonly AppConfigSettings _appConfig;
		private readonly ModsData _modsData;
		private readonly Logger _logger = Logger.Instance;
		private readonly SemaphoreSlim _refreshLock = new(1, 1);

		/// <summary>
		/// The current list of all discovered mods.
		/// Updated after each scan or loaded from cache on startup.
		/// The UI layer should copy this into an <c>ObservableCollection</c> on the dispatcher thread.
		/// </summary>
		public List<ModuleModel> CurrentMods { get; private set; } = [];

		/// <summary>
		/// The previous snapshot of mods loaded from <c>mods_backup.data</c>.
		/// Used for change detection (added / removed mods).
		/// </summary>
		public List<ModuleModel> PreviousMods { get; private set; } = [];

		/// <summary>
		/// Mods that were added since the last snapshot.
		/// </summary>
		public List<ModuleModel> AddedMods { get; private set; } = [];

		/// <summary>
		/// Mods that were removed since the last snapshot.
		/// </summary>
		public List<ModuleModel> RemovedMods { get; private set; } = [];

		/// <summary>
		/// Indicates whether a refresh operation is currently in progress.
		/// The UI can bind to this to show a loading indicator.
		/// </summary>
		public bool IsRefreshing { get; private set; }

		public ModService(AppConfigSettings appConfig, ModsData modsData) {
			_appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
			_modsData = modsData ?? throw new ArgumentNullException(nameof(modsData));
		}

		#region Startup — Load From Cache

		/// <summary>
		/// Loads the cached mod list from <c>mods_current.data</c>.
		/// Should be called on application startup to populate the UI quickly
		/// without a full directory scan.
		/// </summary>
		public void LoadFromCache() {
			CurrentMods = _modsData.LoadCurrent();
			PreviousMods = _modsData.LoadBackup();
			_logger.Info($"ModService: Loaded {CurrentMods.Count} mods from cache, {PreviousMods.Count} from backup.");
		}

		#endregion

		#region Refresh — Full Scan

		/// <summary>
		/// Performs a full async scan of all mod directories, updates the cache files,
		/// and detects changes. Thread-safe — only one refresh runs at a time.
		/// </summary>
		/// <param name="token">Cancellation token for cooperative cancellation.</param>
		/// <returns>
		/// <c>true</c> if changes were detected compared to the previous snapshot; otherwise <c>false</c>.
		/// </returns>
		public async Task<bool> RefreshAsync(CancellationToken token = default) {
			bool acquired = await _refreshLock.WaitAsync(0, token);
			if (!acquired) {
				_logger.Warning("ModService: Refresh already in progress. Skipping duplicate request.");
				return false;
			}
			try {
				IsRefreshing = true;

				// Rotate current → backup before scanning
				_modsData.RotateDataFiles();
				PreviousMods = _modsData.LoadBackup();

				// Perform the full directory scan
				List<ModuleModel> scannedMods = await ModScanner.ScanForModsAsync(_appConfig, token);

				// Persist the new scan results
				_modsData.SaveCurrent(scannedMods);
				CurrentMods = scannedMods;

				// Detect changes
				bool hasChanges = DetectChanges();

				_logger.Info(
					$"ModService: Refresh complete. " +
					$"{CurrentMods.Count} mods found, " +
					$"{AddedMods.Count} added, " +
					$"{RemovedMods.Count} removed.");

				return hasChanges;
			} catch (OperationCanceledException) {
				_logger.Info("ModService: Refresh was cancelled.");
				throw;
			} catch (Exception ex) {
				_logger.Error(ex, "ModService: Refresh failed.");
				throw;
			} finally {
				IsRefreshing = false;
				_refreshLock.Release();
			}
		}

		#endregion

		#region Cache Management

		/// <summary>
		/// Clears the mod cache files to force a fresh directory scan on next launch.
		/// Delegates to <see cref="ModsData.ClearCache"/> which owns the file I/O.
		/// </summary>
		/// <returns><c>true</c> when the cache was cleared successfully.</returns>
		public bool ClearCache() {
			bool result = _modsData.ClearCache();
			if (result) {
				CurrentMods = [];
				PreviousMods = [];
				AddedMods = [];
				RemovedMods = [];
			}
			return result;
		}

		#endregion

		#region Change Detection

		/// <summary>
		/// Compares <see cref="CurrentMods"/> against <see cref="PreviousMods"/>
		/// using <c>ModuleId</c> as the key.
		/// Populates <see cref="AddedMods"/> and <see cref="RemovedMods"/>.
		/// </summary>
		private bool DetectChanges() {
			HashSet<string> currentIds = new(
				CurrentMods
					.Where(m => !string.IsNullOrEmpty(m.ModuleId))
					.Select(m => m.ModuleId),
				StringComparer.OrdinalIgnoreCase);

			HashSet<string> previousIds = new(
				PreviousMods
					.Where(m => !string.IsNullOrEmpty(m.ModuleId))
					.Select(m => m.ModuleId),
				StringComparer.OrdinalIgnoreCase);

			AddedMods = CurrentMods
				.Where(m => !string.IsNullOrEmpty(m.ModuleId) && !previousIds.Contains(m.ModuleId))
				.ToList();

			RemovedMods = PreviousMods
				.Where(m => !string.IsNullOrEmpty(m.ModuleId) && !currentIds.Contains(m.ModuleId))
				.ToList();

			return AddedMods.Count > 0 || RemovedMods.Count > 0;
		}

		#endregion
	}
}
