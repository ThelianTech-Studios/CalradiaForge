# Core EULA

`EulaService` reads the embedded UI resource
`CalradiaForge.Resources.EULA.txt`, checks the persisted acceptance setting,
and records acceptance through `AppSettings`. The UI dialog decides when to
display the content; Core owns the state and persistence mechanism. `AppPaths`
also exposes a `Resources/Eula.txt` path reservation, but the current service
does not read that path; this is a documented source-level discrepancy rather
than a confirmed deployed-file contract. The first-run sequence is documented
in [First Run](../../../04_Application/Workflows/First_Run.md).
