namespace CalradiaForge.Core.Models {
	/// <summary>
	/// Captures the validated identity and destination of a normal module archive
	/// before the installer performs destructive or destination-writing work.
	/// </summary>
	public sealed class ModuleInstallPreflightResult {
		public bool Success { get; init; }
		public string Message { get; init; } = string.Empty;
		public string ModuleRootPath { get; init; } = string.Empty;
		public string ModuleFolderName { get; init; } = string.Empty;
		public string SubModuleXmlPath { get; init; } = string.Empty;
		public string TargetPath { get; init; } = string.Empty;
		public ModuleModel? Module { get; init; }
		public ModuleModel? ExistingModule { get; init; }
	}
}
