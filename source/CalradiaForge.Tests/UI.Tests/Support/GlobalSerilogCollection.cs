namespace CalradiaForge.Tests.UI.Support;

/// <summary>
/// Serializes tests that resolve the Core composition root because that action
/// intentionally replaces the process-wide Serilog logger.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GlobalSerilogCollection {
	public const string Name = "Global Serilog composition";
}
