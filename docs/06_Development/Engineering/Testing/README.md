# Testing

`CalradiaForge.Tests` is the xUnit project covering Core and UI contracts.
Focused areas include configuration recovery, Steam/platform resolution,
launch readiness, localization, logging, persistence, mod parsing/scanning/
installation/pipeline, modpacks, DI composition, lifecycle, dispatching,
notifications, ViewModels, and MainWindow navigation.

## Verification classes

- Build/compilation establishes source compatibility only.
- Automated tests establish the behavior covered by their fixtures.
- Benchmark runs establish measurements for their inputs and environment.
- Manual WPF/Steam/Bannerlord/packaged-runtime checks are separate and must be
  recorded as observed evidence.

See [Testing and Evidence Policy](../../../Decisions/Policies/Testing_and_Evidence_Policy.md),
[Testing Strategy](Testing_Strategy.md), and [Manual Runtime Verification](Manual_Runtime_Verification.md).
