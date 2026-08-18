# Launcher Capability

The Launcher surface lets the user inspect the accepted installed-module list,
search it, move modules between active and inactive collections, reorder the
active load order, select a modpack, refresh module discovery, install archives,
and launch Bannerlord when configuration and platform rules permit.

The Core pipeline owns scan/install admission, accepted-state publication,
reconciliation, cancellation, and quiescence. The Launcher ViewModel owns the
presentation state and invokes that pipeline. See [Mod Management](Mod_Management.md),
[Game Launching](Game_Launching.md), and [Launcher UI](../../05_UI/Pages_and_ViewModels/Launcher.md).
