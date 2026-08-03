# CalradiaForge Nexus Mods and Future Mod-Management Preservation Notes

**Date:** 2026-07-24  
**Status:** Future-context note — not a Phase 7 implementation specification  
**Purpose:** Preserve owner direction that affects current naming and extensibility decisions without expanding Phase 7 scope

---

## 1. Why this note exists

Phase 7 renames the current `ModsPage` to `LauncherPage`.

This is not only a cosmetic rename. The current page is primarily the application’s load-order, modpack, and game-launch surface. The name `ModsPage` must be reserved for a future dedicated mod-management experience associated with Nexus Mods and broader local mod inventory management.

This note preserves that future intent so later planning does not lose the reason for the Phase 7 rename or accidentally reuse incompatible UI and notification assumptions.

---

## 2. Current page after Phase 7

`LauncherPage` remains the main/home working page for:

- viewing installed modules needed for launch configuration;
- manually arranging load order by drag and drop;
- selecting and applying modpacks;
- preparing the active launch configuration;
- launching Bannerlord.

The current manual archive-install button may remain temporarily during Phase 7 and Phase 8, but it is expected to move later to the dedicated mod-management page.

---

## 3. Future dedicated `ModsPage`

The future `ModsPage` is intended to become the primary mod-management surface for categories such as:

- installed mods;
- uninstalled mods;
- downloaded archives;
- discoverable/available Nexus mods;
- updateable mods;
- Nexus-linked metadata;
- download and installation actions;
- per-mod status, errors, and user-action-required states.

The exact page layout, data model, API workflow, and queue behavior are not locked by this note.

---

## 4. Navigation and layout direction

CalradiaForge should retain its existing left-side primary navigation and one wide main page viewer.

Individual pages may later contain their own page-specific horizontal navigation, tabs, or segmented sections, similar in principle to the current Settings page.

The future mod-management experience may take conceptual inspiration from Mod Organizer 2’s separation of mod-management views and controls, but CalradiaForge should not copy Mod Organizer 2’s dual-panel main-window layout.

The intended direction is:

```text
Left application navigation
        ↓
One wide primary page viewer
        ↓
Optional page-specific tabs/sections inside selected pages
```

---

## 5. Install button relocation

The manual archive-install entry point currently located on the launcher-oriented page is expected eventually to move to the future dedicated `ModsPage`.

This relocation is **not** Phase 7 work and should not occur merely because the page is renamed.

The timing should be decided during the Nexus/mod-management implementation cycle after the new page, workflows, and navigation are defined.

---

## 6. Real-time progress direction

### Current/manual phase

For the current sequential manual archive-install workflow, an application-wide persistent progress toast remains useful because there is no permanent install-management screen.

Phase 7 therefore introduces:

- transient per-archive progress;
- one application-lifetime install notification presenter;
- one correlated notification handle;
- one final launcher-workflow notification.

### Future Nexus/mod-management phase

When downloads and installs are represented as individual mod/archive entries, detailed real-time progress should primarily appear in the dedicated mod-management UI, likely as per-entry state in a list or DataGrid-like view.

Future per-entry progress may include:

- queued;
- downloading;
- validating;
- extracting;
- installing;
- completed;
- skipped;
- failed;
- user action required.

The exact models and UI controls are not locked here.

The Phase 7 progress contract should remain extensible enough to correlate operation/archive identity, but it should not add Nexus-specific IDs or download models prematurely.

---

## 7. Future global notification role

After the dedicated mod-management page exists, the global notification system may transition away from continuous file-level progress updates.

Global notifications should then focus on significant lifecycle events, for example:

- download completed;
- installation completed;
- installation failed;
- queue completed;
- update check completed with actionable results;
- operation blocked;
- user action required.

Detailed continuous progress should remain visible in the dedicated management UI when that UI is available.

---

## 8. Notification architecture evolution

Phase 7 uses a narrow application-lifetime install/mod-pipeline notification presenter.

During Nexus development, repeated workflows may justify evolving that presenter into a broader **typed application notification coordinator**.

Such a future coordinator must:

- preserve Core/UI dependency direction;
- consume semantic typed events/results;
- avoid becoming an untyped global message bus;
- avoid owning scheduling, admission, cancellation, or Core workflow execution;
- avoid duplicating Nexus download/install queue ownership;
- keep `ToastService` focused on rendering and lifecycle mechanics.

This evolution is a future decision, not a Phase 7 implementation requirement.

---

## 9. Future identity and queue considerations

The future Nexus cycle may require identities beyond the Phase 7 manual archive context, such as:

- Nexus mod ID;
- Nexus file ID;
- local archive ID/path identity;
- installed module ID;
- download operation ID;
- install operation ID;
- queue item ID.

The exact identity mapping and concurrency model are intentionally deferred.

Phase 7 should use stable operation/archive correlation without prematurely embedding Nexus-specific identity into the Core manual-install contract.

---

## 10. Explicitly out of scope for Phase 7

This note does not authorize Phase 7 to implement:

- the future `ModsPage`;
- Nexus API access;
- authentication or credentials;
- download/update queues;
- mod discovery;
- per-entry DataGrid progress;
- install-button relocation;
- Nexus-specific IDs in current progress models;
- page-specific tab redesign;
- a generalized notification coordinator;
- Mod Organizer 2-style dual-panel layout.

---

## 11. When to revisit this note

Revisit and convert this context into locked architecture during the dedicated Nexus/mod-management decision cycle, after:

- Phase 7 Core/result/notification foundations are implemented;
- Phase 8 MVVM ownership and navigation are settled;
- the future Nexus source/provider architecture is re-audited;
- download, update, install, and inventory workflows are inventoried;
- the owner reviews concrete page and queue alternatives.

Until then, this document is preservation context and a naming/extensibility constraint, not an implementation contract.
