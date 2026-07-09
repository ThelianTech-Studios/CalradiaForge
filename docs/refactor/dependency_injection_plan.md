# Dependency Injection Plan

## Purpose

Replace static application service access over time with explicit composition using `Microsoft.Extensions.DependencyInjection`, then stage legacy logger call-site migration after the DI foundation is in place.

## Current Source Observations

- `App.xaml.cs` initializes services and exposes static properties such as `AppConfig`, `ModService`, `ModInstaller`, `ModpackService`, `GameLauncher`, `Toasts`, and `Translator`.
- Core services already accept important dependencies explicitly, such as `ModService(AppConfigSettings, ModsData)` and `ModInstaller(AppConfigSettings)`.
- Pages currently pull services from `App.*`.
- `CalradiaForge.Nexus` is a reserved boundary and should gain registrations only when Nexus services exist.

## Target Direction

- Register services once during app startup.
- Inject services into ViewModels and workflow coordinators.
- Keep UI-only services in UI.
- Keep Core services free of WPF.
- Add platform adapters for Windows-specific operations.
- Split the work into `Phase 5.A - DI And Platform Adapter Foundation` and `Phase 5.B - Legacy Logger Call-Site Migration`.
- Support test-friendly service replacement.
- Keep `Microsoft.Extensions.Hosting` / Host Builder deferred and not planned unless later justified.

## Steam And Bannerlord Path Adapter Planning

Steam library discovery, Bannerlord install detection, and Steam Workshop path resolution should be planned as explicit platform/path-resolution boundaries before scanner work expands. The known Workshop scanning issue remains unresolved by this plan; it is staged here because the likely investigation area is Steam library discovery and path resolution, not the Bannerlord AppID `261550` string.

Adapter planning should support:

- Steam client install path detection as separate from Steam library root discovery.
- Bannerlord install detection as separate from the selected Workshop content root.
- Multiple Steam library roots.
- Workshop candidate composition under `steamapps/workshop/content/261550`.
- Workshop content under the Bannerlord library root, even when the Steam client is installed elsewhere.
- Manual Workshop path override behavior, if supported, unless a later owner decision removes it.
- Fakeable path providers and fake filesystem roots for tests.
- Windows-specific registry and filesystem probing behind adapters.
- Core remains WPF-free.

The preferred shape is to resolve candidate Steam library roots first, then evaluate valid Bannerlord and Workshop path candidates without assuming that Workshop content lives under the main Steam client install folder.

## Proposed Lifetimes

| Service type | Lifetime |
|---|---|
| `AppConfig`, `AppConfigSettings` | Singleton |
| Data helpers such as `ModsData`, `ModpackData` | Singleton or app-scoped singleton |
| Core services such as `ModService`, `ModInstaller`, `ModpackService`, `GameLauncher` | Singleton while they hold app-wide state |
| Toast and dialog services | Singleton UI services |
| ViewModels | Transient unless navigation requires preserved state |
| Platform adapters | Singleton unless they hold operation state |
| Future Nexus auth/download services | Register in Nexus boundary with explicit lifetimes when implemented |

## Platform Adapter Candidates

- Game path detection.
- Steam client path detection.
- Steam library root discovery.
- Bannerlord install detection.
- Steam Workshop path resolution.
- Registry access.
- External process launching.
- URL and file explorer launching.
- File dialogs.
- Archive extraction implementation details where isolation improves testability.
- File-system operations that need containment checks.
- Future OS-specific Nexus/NXM handler registration.

## Phased Implementation

| Phase | Work | Verification |
|---|---|---|
| 1 | Add DI package and composition root in UI startup. | Build succeeds; service instances match current startup behavior. |
| 2 | Register existing Core services without behavior changes. | Startup, scan, install, modpack load, settings, and launch still work. |
| 3 | Introduce platform adapter interfaces, including Steam/Bannerlord path-resolution adapter targets. | Game path detection, Workshop path candidate resolution, explorer/url launch, file dialogs, and process launch remain functional. |
| 4 | Resolve ViewModels through DI as MVVM extraction begins. | ViewModel tests can replace services. |
| 5.A | DI And Platform Adapter Foundation | Composition root, service lifetimes, and platform adapters are in place without behavior changes. | Build; startup smoke test |
| 5.B | Legacy Logger Call-Site Migration | Convert legacy logger call sites in staged batches after the DI foundation exists, using the approved logging path. | Build; logging smoke test |

## Guardrails

- Do not use DI as a hidden service locator.
- Do not move Nexus networking into Core.
- Do not make Core depend on WPF abstractions.
- Do not convert every class at once.
- Do not add Host Builder complexity without a documented reason.
- Do not treat `ILogger<T>` as the current target for this plan.

## Verification Expectations

- `dotnet build source/CalradiaForge.slnx` succeeds.
- Existing startup initializes the same app services once.
- Singleton services that hold app-wide state are not accidentally duplicated.
- Tests can substitute key services or adapters.
- Core remains WPF-free.

## Future Documentation Cross-References

Accepted platform adapter decisions should later be migrated into future platform/path-detection documentation, especially for Steam library discovery, Bannerlord install detection, Workshop content path policy, manual override behavior, Windows registry probing, and fakeable filesystem/path-provider test boundaries. This plan should remain a staging artifact until those decisions are accepted.

## Open Questions

- Which platform adapters should be first: dialogs, explorer/url launch, registry/game detection, or filesystem?
- Should Workshop scanning check all Steam libraries by default or prefer the Bannerlord install library first?
- Should a manual Workshop path override be exposed or preserved in Settings?

## Out Of Scope

- Full Host Builder migration unless separately approved.
- Rewriting services only to satisfy DI style.
- Moving networking, auth, downloader, or Nexus transport into Core.
