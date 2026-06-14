# AGENTS.md

## Project Overview
CalradiaForge is a WPF launcher for Mount & Blade II: Bannerlord.
The solution is split into a presentation project, a core runtime library, a Nexus integration project, and a small console utility project.

## Repository Structure
- `source/CalradiaForge.UI`: WPF app and presentation layer.
- `source/CalradiaForge.Core`: application logic, data models, file system helpers, and shared runtime services.
- `source/CalradiaForge.Nexus`: reserved Nexus Mods integration assembly.
- `source/CalradiaForge.ConsoleUtils`: developer-only console utility.
- `source/Languages`: localization manifests and translation files.
- `docs/Architecture`: architecture documentation index and topic guides.

## Source Of Truth Priority
1. Existing source code
2. Existing markdown documentation
3. Architecture documents in `docs/Architecture`
4. User task instructions

If a task conflicts with source code or architecture documents, stop and explain the conflict before changing code.

## Build And Test Commands
- Build the solution: `dotnet build source/CalradiaForge.slnx`
- Build a specific project: `dotnet build source/CalradiaForge.UI/CalradiaForge.UI.csproj`
- Run the console utility: `dotnet run --project source/CalradiaForge.ConsoleUtils/CalradiaForge.ConsoleUtils.csproj`
- There is no test project checked in today; if tests are added later, use `dotnet test source/CalradiaForge.slnx`

## Architecture Rules
- UI decides when.
- Core decides how.
- For Nexus work: UI decides when. Core/Nexus decide how.
- Keep dependency flow one way:
  - `CalradiaForge.UI -> CalradiaForge.Core`
  - `CalradiaForge.UI -> CalradiaForge.Nexus`
  - `CalradiaForge.Nexus -> CalradiaForge.Core`
- Preserve the current layered boundary: presentation in UI, runtime logic in Core, Nexus networking in Nexus.

## Coding Rules
- `AppConfig` is the JSON persistence store.
- `AppConfigSettings` is the typed facade over config.
- Prefer explicit dependency passing over hidden globals.
- Pass dependencies explicitly.
- Keep helpers stateless when they are meant to be pure.
- Keep Core free of WPF references.
- Keep Nexus networking out of Core.
- If a type is a shared data model or metadata shape, put it in `CalradiaForge.Core.Models` unless it is strictly Nexus-internal.
- If something belongs in Core, put it in Core rather than duplicating it in Nexus.
- Keep Nexus auth, `HttpClient`, API calls, and downloader mechanics in `CalradiaForge.Nexus`.
- Keep file I/O in the owning data helper or service.
- Preserve current service ownership and task lifetime patterns.
- Do not bypass authoritative pipeline services such as `ModInstaller` and `ModExtractor`.
- Before changing existing code, double-check whether the change could break behavior or regress another system.
- If a change might break something, or if the risk is unclear, stop, propose a safer path, and ask the user to confirm before editing.

## Forbidden Changes
- Do not move Nexus networking into `CalradiaForge.Core`.
- Do not move Core app systems into `CalradiaForge.Nexus`.
- Do not store credentials in `AppConfig`.
- Do not modify `ModuleModel` to store Nexus metadata.
- Do not add automatic startup update checks.
- Do not add timed Nexus polling.
- Do not bypass `ModExtraction` or `ModInstaller`.
- Do not refactor unrelated systems while implementing Nexus functionality.
- Do not introduce WPF references into Core.

## Nexus Integration Rules
- Nexus integration is optional.
- CalradiaForge must work without Nexus.
- Nexus networking belongs in `CalradiaForge.Nexus`.
- Nexus auth, API, downloader, and transport code belong in `CalradiaForge.Nexus`.
- Shared metadata or DTO-like models belong in `CalradiaForge.Core.Models` when they are used outside Nexus internals.
- `CalradiaForge.Nexus` must use Nexus REST/OpenAPI documentation as the primary implementation reference.
- GraphQL/API v2 is deferred and must not be used as the foundation for initial Nexus download, update-check, or NXM support.
- SwaggerHub V1 docs may be used only as a secondary reference when REST/OpenAPI behavior needs comparison.
- Credentials are stored only through DPAPI CurrentUser.
- Credentials are decrypted only during explicit Nexus operation scope.
- AppConfig must never store secrets.
- NXM handler registration occurs only after authentication.
- NXM links fail if authentication is missing.
- Update checks are manual only.
- No startup update checks.
- No timed polling.
- No silent background scans.
- Logs must never expose credentials or secret URLs.

## Documentation Rules
- When architecture changes, update the relevant markdown document.
- Do not leave architecture decisions only in code comments.
- Prefer focused documents over one large architecture file.
- Mark anything not yet shipped as planned, locked, or deferred instead of describing it as implemented.

## Working Agreement For Agents
- Read the relevant docs and source before editing.
- Keep changes scoped to the requested task.
- Do not modify application source code when the task is documentation-only.
- Stop and explain any architecture conflict before making code changes.
