# CalradiaForge — Nexus Mods Integration Architecture Concept Plan

## Purpose

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

