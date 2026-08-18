# Security and Secrets Policy

Credentials belong only in an explicit protected secret boundary; never in
`AppConfig`, `ModuleModel`, logs, diagnostics, benchmark artifacts, or release
records. Future Nexus credentials use DPAPI CurrentUser and are decrypted only
inside explicit operation scope. Current logging is neutral and relies on caller
discipline rather than automatic redaction.

Keep Core free of WPF and Nexus networking, validate paths and operation
admission, preserve accepted state on incomplete work, and classify unverified
hardening as deferred. See [Security Model](../../07_Security/Security_Model.md).
