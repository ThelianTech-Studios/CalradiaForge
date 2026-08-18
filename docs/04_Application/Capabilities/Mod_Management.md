# Mod Management Capability

The user can select supported mod archives, install them into the configured
Bannerlord `Modules` directory, receive per-operation progress and terminal
outcomes, and refresh the module inventory. The current result model distinguishes
success, success with warnings, partial failure, cancellation, busy/admission
rejection, validation failure, and failure.

Incomplete, cancelled, invalid, or failed scans preserve the previous accepted
module snapshot. Installation uses [Core Mod Management](../../02_Systems/CalradiaForge_Core/Mod_Management/README.md)
and is presented through [UI Notifications](../../05_UI/Notifications/README.md).

The archive and installer safety limitations are recorded in
[Deferred Hardening](../../07_Security/Deferred_Hardening.md); no unverified
rollback or archive-resource-limit behavior is promised here.
