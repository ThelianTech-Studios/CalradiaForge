# ConsoleUtils System

The developer-only ConsoleUtils executable offers a menu to generate the
default English localization file. It resolves the runtime language path through
Core, creates the repository `source/Languages` directory when needed, and moves
the generated file with overwrite enabled.

This utility is separate from the WPF startup path and does not change runtime
localization behavior by itself. See [CalradiaForge.ConsoleUtils](../../01_Architecture/Projects/CalradiaForge_ConsoleUtils.md).
