# Trust Boundaries

| Boundary | Rule |
| --- | --- |
| WPF UI ↔ Core | UI supplies user intent and presentation; Core validates and performs runtime mechanics; Core contains no WPF types. |
| Core ↔ filesystem | Paths, config, logs, module roots, archives, and modpacks are validated by their owning helpers. |
| Application ↔ process | Steam/game process inspection and process start are explicit seams; unknown Steam state cannot authorize a direct launch. |
| Core ↔ Nexus | Nexus is optional/reserved. Future network/auth/download behavior stays in Nexus and must not make the base launcher require Nexus. |
| Logs ↔ local data | Technical diagnostics may contain paths and exceptions; callers must not supply credentials or authentication material. |
| Benchmarks ↔ real installation | Real-installation measurements require explicit consent, are read-only/offline, and must redact paths from artifacts. |

Trust-boundary details are summarized here; implementation ownership remains in
the linked system documentation.
