# CalradiaForge.Core

`CalradiaForge.Core` is the WPF-free runtime library. It owns configuration,
logging, paths, platform detection, launching, localization, EULA, mod
scanning/installation, modpacks, persistence helpers, and shared models.

It exposes explicit dependency seams and is registered by
`CalradiaForgeCoreServiceCollectionExtensions`. It must not own WPF windows,
dispatcher types, toast rendering, or Nexus transport.

See [Core systems](../../02_Systems/CalradiaForge_Core/README.md),
[Data](../../03_Data/README.md), and [Security](../../07_Security/README.md).
