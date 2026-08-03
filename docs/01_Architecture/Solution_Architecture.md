# Solution Architecture

CalradiaForge is a layered desktop solution:

```text
CalradiaForge.UI ───────► CalradiaForge.Core
       │                         ▲
       └──────► CalradiaForge.Nexus ─┘

CalradiaForge.Tests ─────► Core and UI
CalradiaForge.Benchmarks ─► Core
CalradiaForge.ConsoleUtils ─► Core
```

The solution contains six projects under `source/`: UI, Core, Nexus,
ConsoleUtils, Tests, and Benchmarks. `source/CalradiaForge.slnx` is the solution
entry point. The project-reference graph is documented in
[Dependency Direction](Dependency_Direction.md).

## Ownership

- UI is the WPF host and composition/lifecycle owner.
- Core owns runtime infrastructure and shared models without WPF references.
- Nexus is a reserved/optional project that references Core but currently does
  not provide a shipped feature implementation.
- ConsoleUtils provides a developer-facing language-resource utility.
- Tests exercise Core and UI contracts.
- Benchmarks exercise Core parsing, scanning, modpack, and real-installation
  read-only inputs.

The detailed mechanism for each system belongs in
[02_Systems](../02_Systems/README.md), and user-visible behavior belongs in
[04_Application](../04_Application/README.md).
