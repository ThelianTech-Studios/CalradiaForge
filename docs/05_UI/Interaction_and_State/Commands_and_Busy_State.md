# Commands and Busy State

ViewModels expose command availability and observable state for busy, status,
error, cancellation, current operation, and last result where relevant. Long
running mod operations are owned by Core and remain correlated across navigation;
pages do not become the task lifetime owner.

User-facing operation semantics belong in [Mod Management](../../04_Application/Capabilities/Mod_Management.md).
