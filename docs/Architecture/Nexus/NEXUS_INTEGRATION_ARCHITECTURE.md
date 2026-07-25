# CalradiaForge — Nexus Mods Integration Architecture

## Purpose
This document is the locked architecture target for Nexus Mods integration in CalradiaForge.
The current repository contains the dedicated `CalradiaForge.Nexus` project, but no Nexus runtime implementation yet.

## Nexus Integration Philosophy
### Currently Implemented
- Nexus integration is optional.
- CalradiaForge must work without Nexus.

### Planned / Locked Architecture
- Nexus networking belongs in `CalradiaForge.Nexus`.
- Core remains the foundation.
- UI remains presentation and event intent only.
- Existing `ModExtractor` and `ModInstaller` remain authoritative.

## Project Structure
```text
CalradiaForge.UI
CalradiaForge.Core
CalradiaForge.Nexus
```

## Assembly Dependency Direction
```text
CalradiaForge.UI -> CalradiaForge.Core
CalradiaForge.UI -> CalradiaForge.Nexus
CalradiaForge.Nexus -> CalradiaForge.Core
```

## Core Boundary Rules
- Do not move Nexus networking into `CalradiaForge.Core`.
- Do not move Core app systems into `CalradiaForge.Nexus`.
- Do not modify `ModuleModel` to store Nexus metadata.
- Do not refactor unrelated systems while implementing Nexus functionality.
- If a data shape is shared or belongs to the application domain, keep it in `CalradiaForge.Core.Models`.
- Keep Nexus auth, `HttpClient`, API calls, and downloader mechanics in `CalradiaForge.Nexus`.

## Nexus Availability Model
### Planned / Locked Architecture
- Nexus features are opt-in.
- Users must be able to launch and use the app without authenticating to Nexus.
- Offline app workflows remain available.

## Authentication Model
### Planned / Locked Architecture
- Production authentication uses Nexus SSO.
- Development workflows may use a personal API key where needed.
- Credentials are stored only through DPAPI CurrentUser.
- Credentials are decrypted only during explicit Nexus operation scope.

## Credential Storage
- `ConfigFileManager` and `AppSettings` must never store secrets.
- Secrets belong in the dedicated Nexus credential flow, not in shared app settings.

## Credential Lifecycle
- Decrypt only when a Nexus action needs the secret.
- Do not keep credentials decrypted longer than the active operation requires.
- Credential-owning components must not intentionally pass credentials or secret-bearing URLs to the logger; the logger itself performs no automatic redaction.

## Authentication UI Behavior
### Planned / Locked Architecture
- Nexus handler registration occurs only after authentication.
- NXM links fail if authentication is missing.
- The UI should present the authentication state, not own the secret storage model.

## NXM Registration
### Planned / Locked Architecture
- Register NXM handling only after authentication is complete.
- Do not pre-register the handler before the Nexus state is ready.

## NXM Link Handling
### Planned / Locked Architecture
- NXM links require authentication to succeed.
- Link handling should route into the Nexus boundary first, then surface results to UI.

## Nexus Metadata Ownership
### Planned / Locked Architecture
- Nexus metadata is owned outside `ModuleModel`.
- Existing `ModuleModel` stays unchanged.
- Metadata DTOs and shared model shapes belong in `CalradiaForge.Core.Models` when they are consumed beyond Nexus internals.

## Legacy Metadata Behavior
### Planned / Locked Architecture
- Existing module metadata should remain intact for non-Nexus workflows.
- Nexus metadata should layer beside current mod data instead of replacing it.

## Metadata Storage
### Planned / Locked Architecture
- Metadata lives in separate `.metadata` files.
- `.metadata` files contain JSON.
- Metadata filenames use `moduleid-version.metadata`.

## Metadata Contents
### Planned / Locked Architecture
- Metadata should be stored as JSON key/value pairs.
- Metadata contents should align with the data returned or sent by the Nexus Mods API for the supported workflow.
- Keep it separate from the existing mod model contract.

## Metadata Update Scope
### Planned / Locked Architecture
- Metadata updates are action-scoped.
- Do not update metadata on a hidden background schedule.
- Do not treat metadata as a silent indexing cache.

## Update Checking Philosophy
### Planned / Locked Architecture
- Update checks are manual only.
- No startup update checks.
- No timed polling.
- No silent background scans.

## Update Comparison Philosophy
### Planned / Locked Architecture
- Main Check For Updates defaults to all Nexus-linked cached mods.
- Future specific-mod checks belong in context-menu functionality.

## Update Status Model
### Planned / Locked Architecture
- User-facing update states are `Unknown`, `Up To Date`, `Update Available`, and `Error`.

## Status Tooltips
### Planned / Locked Architecture
- Tooltips should provide per-mod contextual detail.
- Tooltip text should explain the state without exposing secrets.

## Download Architecture
### Planned / Locked Architecture
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

### Locked Rules
- Downloads use a single-active-download queue pool.
- Failed downloads do not block the queue.
- Existing `ModExtraction` and `ModInstaller` remain authoritative.

## Download Queue Pool
### Planned / Locked Architecture
- Only one active download should run at a time.
- Queue controls should support pause and retry semantics.

## Queue Controls
### Planned / Locked Architecture
- Pause Download
- Pause Queue
- Retry / Resume

## Semi-Persistent Download Jobs
### Planned / Locked Architecture
- Download jobs should survive enough state loss to resume after interruption when possible.
- Queue persistence should not leak secrets.

## Unfinished File Handling
### Planned / Locked Architecture
- `.unfinished` files are retained for retry and resume.
- Resume failure deletes the partial file and restarts once.

## Download Validation
### Planned / Locked Architecture
- Resume should validate quickly first.
- Completion should perform full validation.

## Download To Install Transition
### Planned / Locked Architecture
- Nexus downloads should hand off into the existing extraction and installation pipeline.
- Do not duplicate install logic inside Nexus when Core already owns it.

## Downloaded Mods View
### Planned / Locked Architecture
- A downloaded mods view may surface cached download and metadata state.
- The UI should read from Nexus-owned state, not invent its own parallel cache.

## Cache Retention And Cleanup
### Planned / Locked Architecture
- Download cache cleanup is manual only.
- Do not auto-purge caches on startup or on a timer.

## Settings Integration
### Planned / Locked Architecture
- Nexus-related settings should live in the settings experience only when the feature exists.
- Credentials must not be placed in `ConfigFileManager` or `AppSettings`.

## Logging And Diagnostics
- Credential-owning components must not intentionally pass credentials or secret URLs to logs; any future export or telemetry feature requires a separate data-handling policy.
- Diagnostics should describe state and failures without leaking sensitive request data.

## Toast And Dialog Integration
### Planned / Locked Architecture
- Nexus status should surface through normal UI messaging patterns.
- Dialogs should request explicit user action for auth or retry flows.

## Rate Limit Handling
### Planned / Locked Architecture
- Respect Nexus API limits.
- Surface throttling as a visible state rather than trying to bypass it.

## Offline Functionality
### Planned / Locked Architecture
- The application should remain usable when offline.
- Cached data should still support browsing and existing install workflows.

## Future Right-Click Menu Placeholder
### Planned / Locked Architecture
- Future context-menu actions may target specific Nexus-linked mods.
- That work is deferred until the core download and metadata model exists.

## Deferred Decisions
- Phase 7/8 naming and notification preservation: the current `ModsPage` is
  planned to become `LauncherPage`, reserving `ModsPage` for future management.
  Phase 7 correlation remains generic manual-archive identity; detailed
  per-entry UI progress and any broader typed coordinator are deferred to the
  Nexus cycle. See the [future mod-management context](../../refactor/nexus_future_mod_management_context.md).
- Specific API response shapes.
- Exact UI placement for Nexus actions.
- Additional cache pruning policy beyond manual cleanup.
- GraphQL/API v2 adoption details.

## API Reference Priority
- `CalradiaForge.Nexus` must use Nexus REST/OpenAPI documentation as the primary implementation reference.
- GraphQL/API v2 is deferred and must not be used as the foundation for initial Nexus download, update-check, or NXM support.
- SwaggerHub V1 docs may be used only as a secondary reference when REST/OpenAPI behavior needs comparison.

## Hard Rules Summary
- Nexus integration is optional.
- CalradiaForge must work without Nexus.
- Nexus networking belongs in `CalradiaForge.Nexus`.
- Core remains the foundation.
- UI remains presentation and event intent only.
- Existing `ModExtractor` and `ModInstaller` remain authoritative.
- Credentials are stored only through DPAPI CurrentUser.
- Credentials are decrypted only during explicit Nexus operation scope.
- `ConfigFileManager` and `AppSettings` must never store secrets.
- NXM handler registration occurs only after authentication.
- NXM links fail if authentication is missing.
- Update checks are manual only.
- Main Check For Updates defaults to all Nexus-linked cached mods.
- Future specific-mod checks belong in context-menu functionality.
- No startup update checks.
- No timed polling.
- No silent background scans.
- Downloads use a single-active-download queue pool.
- Failed downloads do not block the queue.
- `.unfinished` files are retained for retry and resume.
- Resume failure deletes partial and restarts once.
- Metadata lives in separate `.metadata` files.
- `.metadata` files contain JSON.
- Metadata filenames use `moduleid-version.metadata`.
- Metadata updates are action-scoped.
- Existing `ModuleModel` stays unchanged.
- Download cache cleanup is manual only.
- User-facing update states are `Unknown`, `Up To Date`, `Update Available`, `Error`.
- Tooltips provide per-mod contextual detail.
- Credential-owning components must not intentionally pass credentials or secret URLs to logs.
