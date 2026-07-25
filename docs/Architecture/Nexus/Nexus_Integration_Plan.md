# CalradiaForge — Nexus Mods Integration Architecture Concept Plan

## Purpose

This is a concise historical/conceptual companion to the canonical
`NEXUS_INTEGRATION_ARCHITECTURE.md`. Phase 7 preserves only naming and
notification context in [the refactor note](../../refactor/nexus_future_mod_management_context.md);
it does not authorize Nexus implementation.

This document defines the authoritative concept architecture for Nexus Mods integration within CalradiaForge.

## Locked Decisions

### Project Structure

```text
CalradiaForge.UI
CalradiaForge.Core
CalradiaForge.Nexus
```

Dependencies:

```text
CalradiaForge.UI -> CalradiaForge.Core
CalradiaForge.UI -> CalradiaForge.Nexus
CalradiaForge.Nexus -> CalradiaForge.Core
```

### Authentication

- Production: Nexus SSO
- Development: Personal API Key
- DPAPI CurrentUser storage
- Scope-of-use decryption only

### Update Checking

- Manual only
- No startup checks
- No polling
- Default: Check All Linked Mods

### Download Architecture

```text
Nexus Integration
    ↓
Download Manager
    ↓
Download Cache
    ↓
Metadata Cache
    ↓
ModExtraction
    ↓
ModInstaller
```

### Metadata

- Separate metadata directory
- .metadata extension
- JSON content
- Action-scoped updates
- Existing ModuleModel unchanged

### Queue Model

- Single active download
- Queue pool
- Pause Download
- Pause Queue
- Retry / Resume

### Recovery

- Preserve .unfinished files
- Resume first
- Quick validation on resume
- Full validation on completion

### NXM

- Auto-register after authentication
- Not registered before authentication
- Immediate queueing when authenticated
