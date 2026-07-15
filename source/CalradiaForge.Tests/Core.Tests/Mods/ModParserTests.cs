namespace CalradiaForge.Tests.Core.Mods;

using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Models;
using CalradiaForge.Tests.Core.Support;

public sealed class ModParserTests {
	[Fact]
	public void Parse_ReadsAttributesInnerTextAndDeduplicatesDependencies() {
		using TestDirectory temp = new();
		string modulePath = temp.CreateDirectory("ParserModule");
		string xmlPath = Path.Combine(modulePath, "SubModule.xml");
		File.WriteAllText(
			xmlPath,
			"""
			<Module>
			  <Name>Parser Module</Name>
			  <Id value="Parser.Module" />
			  <Version value="v2.0.0" />
			  <Url value="https://example.invalid/parser" />
			  <SingleplayerModule value="true" />
			  <DependedModuleMetadatas>
			    <DependedModuleMetadata id="Native" version="v1.0.0" optional="false" />
			    <DependedModuleMetadata id="native" version="v1.1.0" optional="true" />
			  </DependedModuleMetadatas>
			</Module>
			""");

		ModuleModel parsed = Assert.IsType<ModuleModel>(ModParser.Parse(xmlPath, modulePath));

		Assert.Equal("Parser.Module", parsed.ModuleId);
		Assert.Equal("Parser Module", parsed.ModuleName);
		Assert.Equal("v2.0.0", parsed.ModuleVersion);
		Assert.True(parsed.IsSinglePlayerMod);
		Assert.Equal("Native", Assert.Single(parsed.DependencyModules!).DependencyModId);
	}

	[Fact]
	public void Parse_WhenRootIsNotModule_ReturnsNull() {
		using TestDirectory temp = new();
		string xmlPath = temp.GetPath("SubModule.xml");
		File.WriteAllText(xmlPath, "<NotAModule />");

		Assert.Null(ModParser.Parse(xmlPath, temp.RootPath));
	}
}
