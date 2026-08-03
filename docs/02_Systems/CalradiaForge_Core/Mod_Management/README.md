# Core Mod Management

`ModScanner` discovers and parses module descriptors from configured roots;
`ModParser` reads module metadata; `ModInstaller` and `ModExtractor` own archive
installation mechanics; `BLSEInstaller` handles its supported special case; and
`ModuleUnblocker` owns DLL-unblocking mechanics.

`ModPipelineManager` is the single Core workflow owner. It admits one startup,
refresh, or install operation at a time; tracks operation identity; coordinates
cancellation and quiescence; commits only complete scan results; publishes an
accepted module snapshot; and reconciles install outcomes. It does not replace
the installer or extractor and does not create a generic result hierarchy.

Scan results, install terminal results, progress, warnings, and cache-commit
rules are implementation contracts. User-visible descriptions belong in
[Mod Management](../../../04_Application/Capabilities/Mod_Management.md),
while durable cache data belongs in [Data](../../../03_Data/User_Data/Mod_Inventory.md).
