namespace CalradiaForge.Core.Infra.Modpacks {
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Models;

	/// <summary>
	/// Provides default Bannerlord module load order templates.
	/// Used as starting templates when creating a new modpack.
	/// </summary>
	/// <remarks>
	/// The module IDs and correct load order are sourced from the base game's files.
	/// The IDs must exactly match the values found in the games modules folder (case-sensitive).
	/// When installed mods are available, version data is pulled from the live install.
	/// Otherwise, hardcoded fallback versions are used per entry.
	/// </remarks>
	public static class VanillaModules {
		private static readonly Logger _logger = Logger.Instance;
		/// <summary>
		/// Display name used for the built-in vanilla modpack template.
		/// </summary>
		public const string DefaultModpackName = "Vanilla";

		/// <summary>
		/// Display name used for the built-in ButterLib suite modpack template.
		/// </summary>
		public const string ButterLibModpackName = "ButterLib Default";

		/// <summary>
		/// Display name used for the built-in vanilla + WarSails DLC modpack template.
		/// </summary>
		public const string VanillaWarSailsModpackName = "Vanilla - WarSails";

		/// <summary>
		/// Display name used for the built-in ButterLib + WarSails DLC modpack template.
		/// </summary>
		public const string ButterLibWarSailsModpackName = "ButterLib - WarSails";

		/// <summary>
		/// Core vanilla module template entries.
		/// Shared across all templates to avoid duplication.
		/// </summary>
		private static List<(string Id, string Name, string FallbackVersion)> VanillaEntries => [
			("Native", "Native", "v1.0.2"),
			("Sandbox", "Sandbox", "v1.0.2"),
			("SandBoxCore", "SandBox Core", "v1.0.2"),
			("StoryMode", "StoryMode", "v1.0.2"),
			("CustomBattle", "CustomBattle", "v1.0.2"),
			("BirthAndDeath", "BirthAndDeath", "v1.0.2"),
			];

		/// <summary>
		/// ButterLib framework stack template entries.
		/// Must load before vanilla modules so IL patches hook engine methods before initialization.
		/// </summary>
		private static List<(string Id, string Name, string FallbackVersion)> ButterLibEntries => [
			("Bannerlord.Harmony", "Harmony", "v2.4.2.225"),
			("BetterExceptionWindow", "Better Exception Window", "v8.0.0"),
			("Bannerlord.ButterLib", "ButterLib", "v2.10.3"),
			("Bannerlord.UIExtenderEx", "UIExtender Ex", "v2.13.2"),
			("Bannerlord.MBOptionScreen", "Mod Configuration Menu", "v5.11.3"),
			];

		/// <summary>
		/// WarSails (Naval) DLC template entry.
		/// Loads after all vanilla modules.
		/// </summary>
		private static List<(string Id, string Name, string FallbackVersion)> WarSailsEntries => [
			("NavalDLC", "WarSails", "v1.0.2"),
			];

		#region Default Vanilla

		/// <summary>
		/// Returns the default vanilla module load order as a list of <see cref="ModpackEntryModel"/>.
		/// Each call returns a new list instance to prevent external mutation.
		/// Uses hardcoded fallback versions.
		/// </summary>
		/// <remarks>
		/// Correct Bannerlord vanilla load order:
		/// Native → Sandbox → SandBoxCore → StoryMode → CustomBattle → BirthAndDeath
		/// </remarks>
		public static List<ModpackEntryModel> GetDefaultLoadOrder() {
			return GetDefaultLoadOrder(null);
		}

		/// <summary>
		/// Returns the default vanilla module load order, resolving version data
		/// from installed mods when available. Falls back to per-entry default versions
		/// for modules not found in the installed list.
		/// </summary>
		/// <param name="installedMods">
		/// Currently installed mods from <see cref="Mods.ModService.CurrentMods"/>.
		/// When <c>null</c> or empty, fallback versions are used for all entries.
		/// </param>
		public static List<ModpackEntryModel> GetDefaultLoadOrder(List<ModuleModel>? installedMods) {
			return BuildFromTemplate(VanillaEntries, installedMods);
		}

		#endregion

		#region ButterLib

		/// <summary>
		/// Returns the ButterLib suite module load order as a list of <see cref="ModpackEntryModel"/>.
		/// Each call returns a new list instance to prevent external mutation.
		/// Uses hardcoded fallback versions.
		/// </summary>
		/// <remarks>
		/// Correct ButterLib suite load order:
		/// Harmony → BetterExceptionWindow → ButterLib → UIExtenderEx → MBOptionScreen
		/// → Native → Sandbox → SandBoxCore → StoryMode → CustomBattle → BirthAndDeath
		/// </remarks>
		public static List<ModpackEntryModel> GetButterLibLoadOrder() {
			return GetButterLibLoadOrder(null);
		}

		/// <summary>
		/// Returns the ButterLib suite module load order, resolving version data
		/// from installed mods when available. Falls back to per-entry default versions
		/// for modules not found in the installed list.
		/// </summary>
		/// <param name="installedMods">
		/// Currently installed mods from <see cref="Mods.ModService.CurrentMods"/>.
		/// When <c>null</c> or empty, fallback versions are used for all entries.
		/// </param>
		public static List<ModpackEntryModel> GetButterLibLoadOrder(List<ModuleModel>? installedMods) {
			List<(string Id, string Name, string FallbackVersion)> template = [.. ButterLibEntries, .. VanillaEntries];
			return BuildFromTemplate(template, installedMods);
		}

		#endregion

		#region Vanilla - WarSails

		/// <summary>
		/// Returns the vanilla + WarSails DLC module load order.
		/// Uses hardcoded fallback versions.
		/// </summary>
		/// <remarks>
		/// Load order: Native → Sandbox → SandBoxCore → StoryMode → CustomBattle
		/// → BirthAndDeath → NavalDLC
		/// </remarks>
		public static List<ModpackEntryModel> GetDefaultWarSailsLoadOrder() {
			return GetDefaultWarSailsLoadOrder(null);
		}

		/// <summary>
		/// Returns the vanilla + WarSails DLC module load order, resolving version data
		/// from installed mods when available.
		/// </summary>
		/// <param name="installedMods">
		/// Currently installed mods for version resolution. May be <c>null</c>.
		/// </param>
		public static List<ModpackEntryModel> GetDefaultWarSailsLoadOrder(List<ModuleModel>? installedMods) {
			List<(string Id, string Name, string FallbackVersion)> template = [.. VanillaEntries, .. WarSailsEntries];
			return BuildFromTemplate(template, installedMods);
		}

		#endregion

		#region ButterLib - WarSails

		/// <summary>
		/// Returns the ButterLib + WarSails DLC module load order.
		/// Uses hardcoded fallback versions.
		/// </summary>
		/// <remarks>
		/// Load order: Harmony → BetterExceptionWindow → ButterLib → UIExtenderEx
		/// → MBOptionScreen → Native → Sandbox → SandBoxCore → StoryMode
		/// → CustomBattle → BirthAndDeath → NavalDLC
		/// </remarks>
		public static List<ModpackEntryModel> GetButterLibWarSailsLoadOrder() {
			return GetButterLibWarSailsLoadOrder(null);
		}

		/// <summary>
		/// Returns the ButterLib + WarSails DLC module load order, resolving version data
		/// from installed mods when available.
		/// </summary>
		/// <param name="installedMods">
		/// Currently installed mods for version resolution. May be <c>null</c>.
		/// </param>
		public static List<ModpackEntryModel> GetButterLibWarSailsLoadOrder(List<ModuleModel>? installedMods) {
			List<(string Id, string Name, string FallbackVersion)> template = [.. ButterLibEntries, .. VanillaEntries, .. WarSailsEntries];
			return BuildFromTemplate(template, installedMods);
		}

		#endregion

		#region Template Builder

		/// <summary>
		/// Builds a load order entry list from a template, resolving each module's
		/// version and URL from the installed mods list when available.
		/// Falls back to the per-entry fallback version when a module is not installed.
		/// </summary>
		/// <param name="template">Ordered list of (ModuleId, DisplayName, FallbackVersion) tuples.</param>
		/// <param name="installedMods">Installed mods to resolve version data from. May be <c>null</c>.</param>
		/// <returns>A new list of <see cref="ModpackEntryModel"/> with resolved or fallback versions.</returns>
		private static List<ModpackEntryModel> BuildFromTemplate(
			List<(string Id, string Name, string FallbackVersion)> template,
			List<ModuleModel>? installedMods) {

			// Build a lookup by ModuleId for O(1) resolution
			Dictionary<string, ModuleModel>? lookup = installedMods?
				.Where(m => !string.IsNullOrEmpty(m.ModuleId))
				.GroupBy(m => m.ModuleId, StringComparer.OrdinalIgnoreCase)
				.ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

			List<ModpackEntryModel> result = [];

			foreach ((string id, string name, string fallbackVersion) in template) {
				if (lookup is not null && lookup.TryGetValue(id, out ModuleModel? installed)) {
					result.Add(new ModpackEntryModel(
						id,
						installed.ModuleName ?? name,
						installed.ModuleVersion ?? fallbackVersion,
						installed.ModuleURL));
				} else {
					result.Add(new ModpackEntryModel(id, name, fallbackVersion));
					if (_logger.MinimumLevel == Logger.LogLevel.Debug) {
						_logger.Debug("VanillaModules: Using fallback module version.", new { ModuleId = id, ModuleName = name, FallbackVersion = fallbackVersion });
					}
				}
			}

			return result;
		}

		#endregion

		#region Factory Methods

		/// <summary>
		/// Creates a complete <see cref="ModpackModel"/> pre-populated with the vanilla load order.
		/// </summary>
		/// <param name="createdBy">Author name for the modpack metadata. Defaults to "CalradiaForge".</param>
		/// <param name="installedMods">Installed mods for version resolution. May be <c>null</c>.</param>
		/// <returns>A new <see cref="ModpackModel"/> with the vanilla module set.</returns>
		public static ModpackModel CreateDefaultModpack(string createdBy = "CalradiaForge", List<ModuleModel>? installedMods = null) {
			return new ModpackModel(DefaultModpackName, createdBy, GetDefaultLoadOrder(installedMods));
		}

		/// <summary>
		/// Creates a complete <see cref="ModpackModel"/> pre-populated with the ButterLib suite load order.
		/// Includes the full BUTR framework stack followed by vanilla modules.
		/// </summary>
		/// <param name="createdBy">Author name for the modpack metadata. Defaults to "CalradiaForge".</param>
		/// <param name="installedMods">Installed mods for version resolution. May be <c>null</c>.</param>
		/// <returns>A new <see cref="ModpackModel"/> with the ButterLib suite module set.</returns>
		public static ModpackModel CreateButterLibModpack(string createdBy = "CalradiaForge", List<ModuleModel>? installedMods = null) {
			return new ModpackModel(ButterLibModpackName, createdBy, GetButterLibLoadOrder(installedMods));
		}

		/// <summary>
		/// Creates a complete <see cref="ModpackModel"/> pre-populated with the vanilla + WarSails DLC load order.
		/// </summary>
		/// <param name="createdBy">Author name for the modpack metadata. Defaults to "CalradiaForge".</param>
		/// <param name="installedMods">Installed mods for version resolution. May be <c>null</c>.</param>
		/// <returns>A new <see cref="ModpackModel"/> with the vanilla + WarSails module set.</returns>
		public static ModpackModel CreateDefaultWarSailsModpack(string createdBy = "CalradiaForge", List<ModuleModel>? installedMods = null) {
			return new ModpackModel(VanillaWarSailsModpackName, createdBy, GetDefaultWarSailsLoadOrder(installedMods));
		}

		/// <summary>
		/// Creates a complete <see cref="ModpackModel"/> pre-populated with the ButterLib + WarSails DLC load order.
		/// </summary>
		/// <param name="createdBy">Author name for the modpack metadata. Defaults to "CalradiaForge".</param>
		/// <param name="installedMods">Installed mods for version resolution. May be <c>null</c>.</param>
		/// <returns>A new <see cref="ModpackModel"/> with the ButterLib + WarSails module set.</returns>
		public static ModpackModel CreateButterLibWarSailsModpack(string createdBy = "CalradiaForge", List<ModuleModel>? installedMods = null) {
			return new ModpackModel(ButterLibWarSailsModpackName, createdBy, GetButterLibWarSailsLoadOrder(installedMods));
		}

		#endregion
	}
}
