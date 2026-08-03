# Module Inventory Data

`ModsData` persists the accepted module inventory in
`Data/mods_current.data` and rotates the prior file to
`Data/mods_backup.data`. The files represent cached module metadata and are not
the source of truth for an incomplete filesystem scan.

`ModPipelineManager` authorizes rotation, save, and accepted-snapshot
publication only after the scan is complete and cancellation has not won the
commit boundary. Failed, partial, cancelled, or invalid operations preserve the
previous accepted state.
