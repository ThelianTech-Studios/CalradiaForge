using CalradiaForge.Core.Infra.Localization;
using CalradiaForge.Core.Infra.Paths;

internal class Program {
	private static void Main(string[] args) {
		bool check = true;
		do {
			Console.WriteLine("Select a Program to run: \n 1. Create Default Language File \n 2. Exit \n (Use the number keys to select)");
			var choice = Console.ReadKey().KeyChar;
			switch (choice) {
				case '1':
					CreateDefaultLanguageFile();
					break;
				case '2':
					check = false;
					break;
				default:
					Console.WriteLine("\nInvalid choice. Please select a valid option.");
					break;
			}
		} while (check);

		static void CreateDefaultLanguageFile() {
			var filepath = AppPaths.DefaultLanguageFilePath;
			var manager = new TranslationManager(
				AppPaths.LanguagesDirectory,
				AppPaths.LanguagesManifestFilePath,
				AppPaths.DefaultLanguageFilePath);

			manager.DefaultEnglishLanguageFile(TranslationStrings.GetDefaultTranslations(), filepath);

			var repositoryLanguagesDirectory = Path.GetFullPath(
				Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Languages"));
			var destinationFilepath = Path.Combine(repositoryLanguagesDirectory, Path.GetFileName(filepath));
			Directory.CreateDirectory(repositoryLanguagesDirectory);
			File.Move(filepath, destinationFilepath, overwrite: true);

			Console.Clear();
			Console.WriteLine($"\nDefault English language file created at: {destinationFilepath}");
			Console.WriteLine("\nPress any key to continue...");
			Console.ReadKey();
			Console.Clear();
		}














	}
}
