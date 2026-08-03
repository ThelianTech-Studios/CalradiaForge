# Coding Conventions

- Preserve the UI/Core/Nexus dependency direction.
- Pass dependencies explicitly and avoid hidden globals.
- Keep shared data models in Core when used across project boundaries.
- Keep user-facing localization in translation resources with English fallback.
- Use structured logging with meaningful properties and exception-first calls;
  callers must not intentionally provide secret material.
- Keep changes narrow, avoid unrelated modernization, and retain nullable
  analysis rather than suppressing warnings globally.
- Document future or deferred behavior as such.
