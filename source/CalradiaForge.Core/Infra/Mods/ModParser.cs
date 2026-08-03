namespace CalradiaForge.Core.Infra.Mods {
	using System.Xml.Linq;

	using CalradiaForge.Core.Models;

	using Serilog;

	/// <summary>
	/// Parses Bannerlord SubModule.xml files into module models.
	/// </summary>
	public sealed class ModParser {
		/// <summary>
		/// Parses a SubModule.xml file and returns a populated module model.
		/// </summary>
		public static ModuleModel? Parse(string modXMLPath, string installPath) {
			if (string.IsNullOrWhiteSpace(modXMLPath)) {
				Log.Warning("SubModule.xml not found or path is empty: {XmlPath}.", modXMLPath);
				return null;
			}
			Log.Debug("ModParser: Parsing module from {XmlPath} at {InstallPath}.", modXMLPath, installPath);
			try {
				XDocument doc = XDocument.Load(modXMLPath);
				XElement? moduleElement = doc.Root;
				if (moduleElement is null || moduleElement.Name.LocalName != "Module") {
					Log.Warning("Invalid SubModule.xml format: Root element is missing or not 'Module' in {XmlPath}.", modXMLPath);
					Log.Debug("ModParser: Invalid module XML structure in {XmlPath}.", modXMLPath);
					return null;
				}
				string moduleID = GetValueAttribute(moduleElement, "Id") ?? string.Empty;
				string moduleName = GetValueAttribute(moduleElement, "Name") ?? moduleID;
				string moduleVersion = GetValueAttribute(moduleElement, "Version") ?? "0.0.0";
				string? moduleURL = GetValueAttribute(moduleElement, "Url")
					?? GetValueAttribute(moduleElement, "URL");
				bool isSinglePlayer = ParseSPFlag(moduleElement);
				List<DependenciesModulesModel> dependencies = ParseDependencies(moduleElement);

				Log.Debug(
					"ModParser: Parsed module {ModuleId} ({ModuleName}) version {ModuleVersion}; single-player: {IsSinglePlayer}; dependencies: {DependencyCount}.",
					moduleID,
					moduleName,
					moduleVersion,
					isSinglePlayer,
					dependencies.Count);
				return new ModuleModel {
					ModuleId = moduleID,
					ModuleName = moduleName,
					ModuleVersion = moduleVersion,
					ModuleURL = moduleURL,
					InstallPath = installPath,
					IsSinglePlayerMod = isSinglePlayer,
					DependencyModules = dependencies
				};
			} catch (Exception ex) {
				Log.Error(ex, "Error parsing SubModule.xml with path {XmlPath}.", modXMLPath);
				Log.Debug(ex, "ModParser: Exception while parsing module at {XmlPath}.", modXMLPath);
				return null;
			}
		}


		/// <summary>
		/// Reads a child element value attribute or inner text for the specified name.
		/// </summary>
		private static string? GetValueAttribute(XElement parent, string elementName) {
			XElement? child = parent.Element(elementName);
			if (child is null) {
				return null;
			}
			// Primary: check for "Value" attribute (case-insensitive)
			string? attrValue = child.Attributes()
				.FirstOrDefault(a => string.Equals(a.Name.LocalName, "Value", StringComparison.OrdinalIgnoreCase))
				?.Value;
			if (!string.IsNullOrWhiteSpace(attrValue)) {
				return attrValue;
			}
			// Fallback: use inner text if no Value attribute exists
			string innerText = child.Value.Trim();
			return string.IsNullOrEmpty(innerText) ? null : innerText;
		}
		/// <summary>
		/// Parses the single-player flag, defaulting to true when absent.
		/// </summary>
		private static bool ParseSPFlag(XElement moduleElement) {
			string? spValue = GetValueAttribute(moduleElement, "SingleplayerModule");
			if (!string.IsNullOrWhiteSpace(spValue)) {
				return string.Equals(spValue, "true", StringComparison.OrdinalIgnoreCase);
			}
			Log.Debug("ModParser: Singleplayer flag missing; defaulting to true.");
			return true; // Default to true if not specified 
		}
		/// <summary>
		/// Parses dependency entries from the module metadata.
		/// </summary>
		private static List<DependenciesModulesModel> ParseDependencies(XElement moduleElement) {
			List<DependenciesModulesModel> dependencies = new List<DependenciesModulesModel>();
			HashSet<string> seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			XElement? metadatas = (XElement?)moduleElement.Element("DependedModuleMetadatas");
			if (metadatas is not null) {
				foreach (XElement dep in metadatas.Elements("DependedModuleMetadata")) {
					string depId = dep.Attribute("id")?.Value ?? string.Empty;
					string depVersion = dep.Attribute("version")?.Value ?? "0.0.0";
					bool isOptional = string.Equals(dep.Attribute("optional")?.Value, "true", StringComparison.OrdinalIgnoreCase);
					bool hasVersionRequirement = !string.IsNullOrEmpty(depVersion);
					if (!string.IsNullOrEmpty(depId) && seenIds.Add(depId)) {
						dependencies.Add(new DependenciesModulesModel(depId, depVersion, isOptional, hasVersionRequirement));
					} else {
						Log.Debug("ModParser: Skipping duplicate dependency {DependencyId}.", depId);
					}
				}
			}
			XElement? legacyDeps = (XElement?)moduleElement.Element("DependedModules");
			if (legacyDeps is not null) {
				foreach (XElement dep in legacyDeps.Elements("DependedModule")) {
					string depId = dep.Attribute("Id")?.Value ?? string.Empty;
					string depVersion = dep.Attribute("version")?.Value ?? string.Empty;
					bool hasVersionRequirement = !string.IsNullOrEmpty(depVersion);
					if (!string.IsNullOrEmpty(depId) && seenIds.Add(depId)) {
						dependencies.Add(new DependenciesModulesModel(depId, depVersion, false, hasVersionRequirement));
					} else {
						Log.Debug("ModParser: Skipping duplicate legacy dependency {DependencyId}.", depId);
					}
				}
			}
			Log.Debug("ModParser: Parsed {DependencyCount} dependencies.", dependencies.Count);
			return dependencies;
		}
	}
}
