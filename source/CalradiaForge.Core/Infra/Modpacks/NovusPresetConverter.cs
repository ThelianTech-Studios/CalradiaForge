namespace CalradiaForge.Core.Infra.Modpacks {
	using System;
	using System.Collections.Generic;
	using System.Xml.Linq;

	using CalradiaForge.Core.Models;

	using Serilog;

	/// <summary>
	/// Pure static converter for Novus Launcher preset XML files.
	/// Parses the Novus <c>&lt;Preset&gt;</c> XML format and converts it
	/// to a CalradiaForge <see cref="ModpackModel"/>.
	///
	/// Novus XML structure:
	/// <code>
	/// &lt;Preset Name="..." CreatedBy="..."&gt;
	///   &lt;PresetModule Id="..." RequiredVersion="..." URL="..." /&gt;
	/// &lt;/Preset&gt;
	/// </code>
	///
	/// Follows the Pure Static Helper pattern — stateless, no global state,
	/// receives all data as parameters.
	/// </summary>
	public static class NovusPresetConverter {
		/// <summary>
		/// Parses a Novus Launcher preset XML file and converts it to a <see cref="ModpackModel"/>.
		/// The <c>LastUpdated</c> field is set to today's date regardless of the XML content.
		/// </summary>
		/// <param name="xmlFilePath">Full path to the Novus preset .xml file.</param>
		/// <returns>
		/// The converted <see cref="ModpackModel"/>, or <c>null</c> if the file
		/// cannot be read, is not valid Novus XML, or contains no module entries.
		/// </returns>
		public static ModpackModel? ConvertFromFile(string xmlFilePath) {
			if (string.IsNullOrWhiteSpace(xmlFilePath) || !File.Exists(xmlFilePath)) {
				Log.Warning("NovusConverter: File not found: {FilePath}.", xmlFilePath);
				return null;
			}

			try {
				string xml = File.ReadAllText(xmlFilePath);
				return ConvertFromXml(xml);
			} catch (Exception ex) {
				Log.Error(ex, "NovusConverter: Failed to read file {FilePath}.", xmlFilePath);
				return null;
			}
		}

		/// <summary>
		/// Parses a Novus Launcher preset from raw XML content and converts it
		/// to a <see cref="ModpackModel"/>.
		/// </summary>
		/// <param name="xmlContent">The raw XML string content.</param>
		/// <returns>
		/// The converted <see cref="ModpackModel"/>, or <c>null</c> if parsing fails.
		/// </returns>
		public static ModpackModel? ConvertFromXml(string xmlContent) {
			if (string.IsNullOrWhiteSpace(xmlContent)) {
				Log.Warning("NovusConverter: XML content is null or empty.");
				return null;
			}

			try {
				XDocument doc = XDocument.Parse(xmlContent);
				XElement? presetElement = doc.Root;

				if (presetElement is null || !string.Equals(presetElement.Name.LocalName, "Preset", StringComparison.OrdinalIgnoreCase)) {
					Log.Warning("NovusConverter: Root element is not <Preset>.");
					return null;
				}

				string name = presetElement.Attribute("Name")?.Value?.Trim() ?? string.Empty;
				string createdBy = presetElement.Attribute("CreatedBy")?.Value?.Trim() ?? "Unknown";

				if (string.IsNullOrWhiteSpace(name)) {
					Log.Warning("NovusConverter: Preset has no Name attribute.");
					return null;
				}

				List<ModpackEntryModel> loadOrder = ParseModuleEntries(presetElement);

				if (loadOrder.Count == 0) {
					Log.Warning("NovusConverter: Preset {PresetName} has no module entries.", name);
					return null;
				}

				ModpackModel modpack = new() {
					ModpackName = name,
					CreatedBy = createdBy,
					LastUpdated = DateTime.Now.ToString("yyyy-MM-dd"),
					LoadOrder = loadOrder
				};

				Log.Information(
					"NovusConverter: Converted preset {PresetName} with {EntryCount} module(s).",
					name,
					loadOrder.Count);
				Log.Debug(
					"NovusConverter: Conversion complete for {PresetName}, created by {CreatedBy}, with {EntryCount} entries.",
					name,
					createdBy,
					loadOrder.Count);
				return modpack;
			} catch (Exception ex) {
				Log.Error(ex, "NovusConverter: Failed to parse XML content.");
				Log.Debug(ex, "NovusConverter: XML parse failed for content length {ContentLength}.", xmlContent.Length);
				return null;
			}
		}

		/// <summary>
		/// Parses all <c>&lt;PresetModule&gt;</c> child elements from the <c>&lt;Preset&gt;</c> root.
		/// </summary>
		/// <param name="presetElement">The root <c>&lt;Preset&gt;</c> element.</param>
		/// <returns>Ordered list of converted <see cref="ModpackEntryModel"/> entries.</returns>
		private static List<ModpackEntryModel> ParseModuleEntries(XElement presetElement) {
			List<ModpackEntryModel> entries = [];

			foreach (XElement module in presetElement.Elements("PresetModule")) {
				string id = module.Attribute("Id")?.Value?.Trim() ?? string.Empty;
				string version = module.Attribute("RequiredVersion")?.Value?.Trim() ?? string.Empty;
				string url = module.Attribute("URL")?.Value?.Trim() ?? string.Empty;

				if (string.IsNullOrWhiteSpace(id)) {
					Log.Debug("NovusConverter: Skipping module with empty Id.");
					continue;
				}

				entries.Add(new ModpackEntryModel {
					ModuleId = id,
					ModuleName = id,
					RequiredVersion = version,
					ModuleURL = string.IsNullOrWhiteSpace(url) ? null : url
				});
			}

			return entries;
		}
	}
}
