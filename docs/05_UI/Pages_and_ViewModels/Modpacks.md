# Modpacks Page and ViewModel

`ModpacksPage` and `ModpacksViewModel` present loaded modpacks, create/save/edit
operations, import, refresh, and template selection. They call
`ModpackService` and expose user-facing success/failure state without moving
modpack file I/O into WPF.

The authoritative capability is [Modpacks](../../04_Application/Capabilities/Modpacks.md);
the durable shape is [Modpack Data](../../03_Data/User_Data/Modpacks.md).
