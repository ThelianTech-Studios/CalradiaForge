# Nexus Metadata and Download State

**Status: unimplemented and deferred.**

The current path model contains Nexus-related directory names, but the current
solution does not provide a verified Nexus authentication, metadata, download,
NXM, or update-check workflow. A future data contract must be designed with the
Nexus project boundary, DPAPI CurrentUser credential storage, secret-free logs,
and explicit user-triggered operations. It must not be added to `AppConfig` or
`ModuleModel` merely because this placeholder exists.
