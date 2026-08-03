# Engineering Practices

Keep dependency passing explicit, helpers stateless where practical, and Core
free of WPF. Preserve service ownership and task lifetime. Prefer workflow-
specific results over a universal result hierarchy. Keep Nexus transport in
Nexus, credentials out of `AppConfig`, and authoritative install mechanics in
`ModInstaller`/`ModExtractor`.

| Practice | Entry |
| --- | --- |
| Coding conventions | [Coding Conventions](Coding_Conventions.md) |
| Dependencies | [Dependency Management](Dependency_Management.md) |
