# CalradiaForge v1.0 — Remaining Work Checklist

## 🔲 Remaining — in priority order

| # | Feature | Scope | Notes |
|---|---------|-------|-------|
| 1 | Modpack Saving Changes| Core + UI | Implement saving changes to modpacks, ensuring data integrity and proper UI updates and setting to set auto saving modpack on game launching.|
| 2 | Implement some Ideas from Wanning Launcher | Core + UI | Look over certain feauture they provide and implement them.  |
| 3 | NexusModsAPI | Core + UI | Implement API integration for NexusMods functionality. |
| 4 | Epic Games and GamePass Support Backend | Core + UI | Add Functionality and Ship Beta Test Releases for users who have Epic or GamePass to test. |
| 5 | Create post-v1.0 roadmap | App | Outline future features and improvements for post-v1.0 releases |

> Check over Wanning Launcher ultility features to check for things we can add.
> NexusModsAPI Feature is in concept planning phase, and will be implemented for the 1.0 release.

## POST V1.0 Deployment

- Look into adding Game Platform Compatibility with Epic Games and Game pass, which were deferred for v1.0 due to testing access limitations. This would involve re-enabling the existing detection and launch code paths for these platforms, validating them with actual installations, and updating the UI to support platform-specific features or warnings as needed.
- Finish Polishing the project, and then putting the project into a LTS state, where only critical bugs and security issues will be addressed, while new features and non-critical improvements will be deferred to future major releases. This will allow us to focus on maintaining stability and reliability for our users, while also providing a clear roadmap for future development.
