# Nexus And Future Mod-Management Context

Status: preservation context, not an implementation contract.

## Purpose

Phase 7 reserves naming and notification seams without implementing Nexus API
access, authentication, download queues, mod discovery, or a new UI. It records
the future context from the supplied
[Nexus/mod-management note](../TEMP_Codex_Instructions/CalradiaForge_Nexus_Future_Mod_Management_Notes_2026-07-24.md)
so Phase 7 does not consume names or introduce incompatible ownership.

## Preserved constraints

- Phase 7 plans `ModsPage` -> `LauncherPage`; the reserved future `ModsPage`
  may later manage installed, downloaded, discoverable, updateable, and
  Nexus-associated mods.
- Phase 7 progress correlation is deliberately generic to the current manual
  archive-install pipeline. It does not embed Nexus IDs, queue identities, or
  per-entry DataGrid behavior.
- A Phase 7 install notification presenter may later inform a broader **typed**
  notification coordinator only when future independent Nexus workflows prove
  that need. It must never become a global message bus or duplicate manager
  admission, cancellation, or scheduling.
- Core remains WPF-free and Nexus networking/transport remains in
  `CalradiaForge.Nexus`; credentials remain DPAPI-only and outside `AppConfig`.

## Explicit deferrals

No Phase 7 work authorizes Nexus REST/API implementation, credentials, NXM
handling, update checks, download/install queues, per-entry progress UI,
install-button relocation, a future ModsPage layout, or a visual redesign.
Revisit this context only in a separately locked Nexus/mod-management cycle,
after Phase 7 implementation and Phase 8 MVVM ownership are settled.
