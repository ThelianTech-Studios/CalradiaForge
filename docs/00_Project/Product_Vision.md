# Product Vision

CalradiaForge aims to make repeatable Bannerlord mod setups easy to install,
organize, save, and launch from a portable desktop application. The durable
product value is a coherent relationship between installed modules, selected
modpacks, platform-aware game launching, and recoverable user data.

The vision is deliberately broader than any one implementation class. Current
capability details belong in [Application](../04_Application/README.md),
presentation details belong in [UI](../05_UI/README.md), and runtime mechanisms
belong in [Systems](../02_Systems/README.md).

## Product principles

- Make the selected load order visible and reproducible.
- Keep installation and scan ownership in Core services rather than page code.
- Preserve user data when a scan, configuration read, or operation is incomplete.
- Keep platform and archive support explicit instead of implying unverified
  compatibility.
- Keep future Nexus functionality optional and separately gated.
