namespace CalradiaForge.Core.Infra.Mods
	{
	using System.Xml.Linq;

	using CalradiaForge.Core.Infra.Logging;
	using CalradiaForge.Core.Models;

	public sealed class ModParser
		{
		private static readonly Logger _logger = Logger.Instance;

		public static ModuleModel? Parse(string modXMLPath,string installPath) {
			if (string.IsNullOrWhiteSpace(modXMLPath)) {
				_logger.Warning($"SubModule.xml not found or path is empty: '{modXMLPath}'");
				return null;
				}
			try {
				XDocument doc = XDocument.Load(modXMLPath);
				XElement moduleElement = doc.Root;
				if (moduleElement is null||moduleElement.Name.LocalName!="Module") {
					_logger.Warning($"Invalid SubModule.xml format: Root element is missing or not 'Module' in '{modXMLPath}'");
					return null;
					}
				string moduleID = GetValueAttribute(moduleElement,"Id")??string.Empty;
				string moduleName = GetValueAttribute(moduleElement,"Name")??moduleID;
				string moduleVersion = GetValueAttribute(moduleElement,"Version")??"0.0.0";
				string? moduleURL = GetValueAttribute(moduleElement,"Url")
					?? GetValueAttribute(moduleElement,"URL");
				bool isSinglePlayer = ParseSPFlag(moduleElement);
				List<DependenciesModulesModel> dependencies = ParseDependencies(moduleElement);

				return new ModuleModel {
					ModuleId=moduleID,
					ModuleName=moduleName,
					ModuleVersion=moduleVersion,
					ModuleURL=moduleURL,
					InstallPath=installPath,
					IsSinglePlayerMod=isSinglePlayer,
					DependencyModules=dependencies
					};
				} catch (Exception ex) {
				_logger.Error($"Error parsing SubModule.xml with path: {modXMLPath}.",ex);
				return null;
				}
			}


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
		private static bool ParseSPFlag(XElement moduleElement) {
			string? spValue = GetValueAttribute(moduleElement,"SingleplayerModule");
			if (!string.IsNullOrWhiteSpace(spValue)) {
				return string.Equals(spValue,"true",StringComparison.OrdinalIgnoreCase);
				}
			return true; // Default to true if not specified 
			}
		private static List<DependenciesModulesModel> ParseDependencies(XElement moduleElement) {
			List<DependenciesModulesModel> dependencies = new List<DependenciesModulesModel>();
			HashSet<string> seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			XElement? metadatas = (XElement?)moduleElement.Element("DependedModuleMetadatas");
			if (metadatas is not null) {
				foreach (XElement dep in metadatas.Elements("DependedModuleMetadata")) {
					string depId = dep.Attribute("id")?.Value??string.Empty;
					string depVersion = dep.Attribute("version")?.Value??"0.0.0";
					bool isOptional = string.Equals(dep.Attribute("optional")?.Value,"true",StringComparison.OrdinalIgnoreCase);
					bool hasVersionRequirement = !string.IsNullOrEmpty(depVersion);
					if (!string.IsNullOrEmpty(depId)&&seenIds.Add(depId)) {
						dependencies.Add(new DependenciesModulesModel(depId,depVersion,isOptional,hasVersionRequirement));
						}
					}
				}
			XElement? legacyDeps = (XElement?)moduleElement.Element("DependedModules");
			if (legacyDeps is not null) {
				foreach (XElement dep in legacyDeps.Elements("DependedModule")) {
					string depId = dep.Attribute("id")?.Value??string.Empty;
					string depVersion = dep.Attribute("version")?.Value??string.Empty;
					bool hasVersionRequirement = !string.IsNullOrEmpty(depVersion);
					if (!string.IsNullOrEmpty(depId)&&seenIds.Add(depId)) {
						dependencies.Add(new DependenciesModulesModel(depId,depVersion,false,hasVersionRequirement));
						}
					}
				}
			return dependencies;
			}
		}
	}
