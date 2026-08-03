# Environment and Tooling

The solution uses the .NET SDK and a Windows WPF-capable environment. The
current project files target .NET 10; UI and tests target Windows Desktop.

| Task | Command |
| --- | --- |
| Build solution | `dotnet build source/CalradiaForge.slnx` |
| Build UI | `dotnet build source/CalradiaForge.UI/CalradiaForge.UI.csproj` |
| Run ConsoleUtils | `dotnet run --project source/CalradiaForge.ConsoleUtils/CalradiaForge.ConsoleUtils.csproj` |
| Test solution | `dotnet test source/CalradiaForge.slnx` |

Debug and Release build/test runs should be performed sequentially when both are
needed because WPF generated artifacts can collide during concurrent runs.
