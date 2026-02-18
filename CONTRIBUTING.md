# Contributing to CalradiaForge

Thank you for your interest in contributing! This document outlines the architecture, coding rules, and design patterns used in CalradiaForge to ensure maintainability, safety, and consistency across the project.

---

## ?? Architecture Overview

CalradiaForge follows a **layered architecture** with strong separation of concerns, making it safe, testable, and reusable.

**Layers:**

| Layer | Responsibility | Notes |
|-------|----------------|------|
| **UI Layer** (WPF App) | User interaction, app lifecycle | Should not contain game logic |
| **Core Infrastructure** (Class Library) | Paths, config, detection, logging | Must be UI-agnostic |
| **Helpers** | Pure logic utilities | Stateless, receives needed data via parameters |
| **Config System** | Persistence & typed access | Handles JSON read/write, thread-safe |

---

## ?? Key Design Patterns

### 1. Repository Pattern — `AppConfig`
- Handles low-level storage of key/value pairs.
- Responsible for JSON read/write and thread safety.
- **Never** contains logic about paths or UI.

### 2. Facade / Typed Adapter — `AppConfigSettings`
- Provides strongly-typed access to config values.
- Exposes properties for easy use and data binding.
- **Never** performs logic, IO, or saves the config directly.

### 3. Singleton Ownership (App Layer) — `App.xaml.cs`
- Creates a single instance of `AppConfigSettings`.
- Passed explicitly into helpers and core classes.
- Ensures a single source of truth for configuration.

### 4. Dependency Injection by Parameter — Helpers
- Helpers like `GamePathsHelper` receive config instances explicitly.
- No helper may access global state.
- Ensures testability and modularity.

### 5. Defensive Design
- Core classes must be safe if used independently of the app.
- Example: `AppConfig` ensures directories exist before saving.
- No assumptions about caller behavior.

### 6. Separation of Concerns
- Each class or module has a single responsibility.
- UI decides *when* actions run.
- Core decides *how* actions are performed.

### 7. Pure Static Helper Pattern — `GamePathsHelper`
- Contains only stateless, pure logic.
- No saving, no UI references.
- Receives all required data as parameters.

---

## ?? Contributor Rules

### ? Core Library
- Must never reference WPF or UI components.
- Must operate safely in isolation.
- Must be fully testable without the application.

### ? Helpers
- Receive `AppConfigSettings` or required data as parameters.
- Never access global or static state.
- Should contain pure logic only.

### ? Config System
- Only `AppConfig` performs JSON read/write.
- `AppConfigSettings` is for typed access and binding.
- Never call `Save()` outside of `AppConfig`.

### ? General
- All classes must be defensively safe if misused.
- Directory or file validation should be in `AppConfig` or helpers, not UI.
- Follow existing naming conventions and project formatting.

### ? Design Changes & Clarifications
- Confirm design changes with a reviewer **before implementation** (especially UI/UX changes).
- Ask clarifying questions when requirements are ambiguous or constraints are unclear.
- Validate changes against this document to stay within project scope.

---

## ?? How to Contribute

1. Fork the repository.
2. Create a feature branch:
   ```bash
   git checkout -b feature/my-feature
   ```
3. Implement your changes, following the architecture rules above.
4. Run any tests you create or update.
5. Submit a pull request with a clear description and reasoning for the changes.

---

## ?? Philosophy

> **UI decides when, core decides how.**  
> This is the guiding principle of the CalradiaForge project. All contributions must respect the separation between the UI and the core logic.

By following these patterns and rules, your contributions will keep the project maintainable, safe, and professional-grade.

---

Thank you for helping CalradiaForge stay clean, modular, and future-proof! ??