# Secrets and Credentials

The current source does not implement Nexus authentication or credential
storage. The durable rule for future Nexus work is: credentials belong in
DPAPI CurrentUser-protected storage, are decrypted only inside explicit Nexus
operation scope, and never enter `AppConfig`, `ModuleModel`, logs, diagnostics,
or release artifacts.

The active Serilog formatter is neutral rather than an automatic redaction
boundary. Callers are responsible for not passing credentials or secret URLs to
logs. Any future authentication implementation must be documented in Nexus and
reviewed against [Trust Boundaries](Trust_Boundaries.md).
