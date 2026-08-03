# Security Model

## Principles

- Validate filesystem paths before using them for game, module, configuration,
  archive, or cleanup operations.
- Preserve data when reads, writes, scans, or reconciliation are incomplete.
- Keep Core/UI/Nexus trust boundaries explicit.
- Treat credentials and authentication material as secret even though current
  Nexus behavior is not implemented.
- Do not claim automatic log redaction: the active formatter is neutral and
  callers must not intentionally log secrets.
- Prefer fail-closed authorization for launch/platform checks and install
  admission where the source provides an explicit state.

## Current controls

The source contains typed configuration recovery, atomic persistence seams,
archive-entry containment checks, game/module path validation, bounded operation
admission/cancellation, accepted-snapshot commit gating, explicit Steam process
states, and provider-owned lifecycle disposal. Their detailed behavior belongs
to [Configuration](../02_Systems/CalradiaForge_Core/Configuration/README.md),
[Mod Management](../02_Systems/CalradiaForge_Core/Mod_Management/README.md),
[Game Platforms](../02_Systems/CalradiaForge_Core/Game_Platforms/README.md), and
[Application Lifecycle](../02_Systems/CalradiaForge_UI/Application_Lifecycle/README.md).

These controls do not establish a complete security assessment. See [Deferred
Hardening](Deferred_Hardening.md) and [Security Verification](Security_Verification.md).
