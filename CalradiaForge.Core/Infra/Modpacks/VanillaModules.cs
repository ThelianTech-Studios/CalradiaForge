namespace CalradiaForge.Core.Infra.Modpacks
	{
	using System.Collections.Generic;

	using CalradiaForge.Core.Models;

	/// <summary>
	/// Provides default Bannerlord module load order templates.
	/// Used as starting templates when creating a new modpack.
	/// </summary>
	/// <remarks>
	/// The module IDs and correct load order are sourced from the base game's files.
	/// The IDs must exactly match the values found in the games modules folder (case-sensitive).
	/// Versions reflect the baseline Bannerlord release; they are matched against installed mods at runtime.
	/// </remarks>
	public static class VanillaModules
		{
		/// <summary>
		/// Display name used for the built-in vanilla modpack template.
		/// </summary>
		public const string DefaultModpackName = "Vanilla";

		/// <summary>
		/// Display name used for the built-in ButterLib suite modpack template.
		/// </summary>
		public const string ButterLibModpackName = "ButterLib Default";

		/// <summary>
		/// Returns the default vanilla module load order as a list of <see cref="ModpackEntryModel"/>.
		/// Each call returns a new list instance to prevent external mutation.
		/// </summary>
		/// <remarks>
		/// Correct Bannerlord vanilla load order:
		/// Native → Sandbox → SandBoxCore → StoryMode → CustomBattle → BirthAndDeath
		/// </remarks>
		public static List<ModpackEntryModel> GetDefaultLoadOrder() {
			return [
				new ModpackEntryModel("Native","Native","v1.0.2"),
				new ModpackEntryModel("Sandbox","Sandbox","v1.0.2"),
				new ModpackEntryModel("SandBoxCore","SandBox Core","v1.0.2"),
				new ModpackEntryModel("StoryMode","StoryMode","v1.0.2"),
				new ModpackEntryModel("CustomBattle","CustomBattle","v1.0.2"),
				new ModpackEntryModel("BirthAndDeath","BirthAndDeath","v1.0.2"),
				];
			}

		/// <summary>
		/// Returns the ButterLib suite module load order as a list of <see cref="ModpackEntryModel"/>.
		/// Includes the full BUTR framework stack (Harmony, BetterExceptionWindow, ButterLib,
		/// UIExtenderEx, MCM) loaded before the vanilla modules in the correct dependency order.
		/// Each call returns a new list instance to prevent external mutation.
		/// </summary>
		/// <remarks>
		/// Correct ButterLib suite load order:
		/// Harmony → BetterExceptionWindow → ButterLib → UIExtenderEx → MBOptionScreen
		/// → Native → Sandbox → SandBoxCore → StoryMode → CustomBattle → BirthAndDeath
		/// </remarks>
		public static List<ModpackEntryModel> GetButterLibLoadOrder() {
			return [
				new ModpackEntryModel("Bannerlord.Harmony","Harmony","v2.4.2.225"),
				new ModpackEntryModel("BetterExceptionWindow","Better Exception Window","v8.0.0"),
				new ModpackEntryModel("Bannerlord.ButterLib","ButterLib","v2.10.3"),
				new ModpackEntryModel("Bannerlord.UIExtenderEx","UIExtender Ex","v2.13.2"),
				new ModpackEntryModel("Bannerlord.MBOptionScreen","Mod Configuration Menu","v5.11.3"),
				new ModpackEntryModel("Native","Native","v1.0.2"),
				new ModpackEntryModel("Sandbox","Sandbox","v1.0.2"),
				new ModpackEntryModel("SandBoxCore","SandBox Core","v1.0.2"),
				new ModpackEntryModel("StoryMode","StoryMode","v1.0.2"),
				new ModpackEntryModel("CustomBattle","CustomBattle","v1.0.2"),
				new ModpackEntryModel("BirthAndDeath","BirthAndDeath","v1.0.2"),
				];
			}

		/// <summary>
		/// Creates a complete <see cref="ModpackModel"/> pre-populated with the vanilla load order.
		/// </summary>
		/// <param name="createdBy">Author name for the modpack metadata. Defaults to "CalradiaForge".</param>
		/// <returns>A new <see cref="ModpackModel"/> with the vanilla module set.</returns>
		public static ModpackModel CreateDefaultModpack(string createdBy = "CalradiaForge") {
			return new ModpackModel(DefaultModpackName,createdBy,GetDefaultLoadOrder());
			}

		/// <summary>
		/// Creates a complete <see cref="ModpackModel"/> pre-populated with the ButterLib suite load order.
		/// Includes the full BUTR framework stack followed by vanilla modules.
		/// </summary>
		/// <param name="createdBy">Author name for the modpack metadata. Defaults to "CalradiaForge".</param>
		/// <returns>A new <see cref="ModpackModel"/> with the ButterLib suite module set.</returns>
		public static ModpackModel CreateButterLibModpack(string createdBy = "CalradiaForge") {
			return new ModpackModel(ButterLibModpackName,createdBy,GetButterLibLoadOrder());
			}
		}
	}
