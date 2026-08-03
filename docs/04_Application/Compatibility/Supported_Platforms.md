# Supported Platforms

The current source targets Windows WPF and `.NET 10` Windows Desktop for the
UI. The root README and publish settings describe Windows x64 framework-dependent
distribution; release verification remains a separate evidence question.

| Provider | Current behavior |
| --- | --- |
| Steam | Automatic detection path and direct launch path are implemented; Steam readiness is fail-closed when process state is unknown. |
| GOG / Standalone | Manual configuration can identify these provider shapes and direct executable launch is supported when paths validate. |
| Epic Games | Detection is disabled in the automatic resolver; direct launch is blocked and the user is directed to the Epic client. |
| Game Pass | Direct launch is blocked and the user is directed to the Xbox app. |

This table is a source-grounded behavior summary, not manual compatibility
certification. See [Release Verification](../../08_Releases/Release_Verification.md).
