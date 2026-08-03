# Testing Strategy

Use the smallest deterministic test that proves the affected contract. Prefer
explicit seams for filesystem, process, timing, logging, dispatching, and
provider composition. Keep Core tests WPF-free and isolate test fixtures under
temporary directories.

When a workflow is asynchronous, test admission, cancellation, terminal
classification, cleanup, and quiescence. When a source change affects a
cross-layer boundary, run focused tests first and the complete solution suite
afterward. Do not add timing assertions to ordinary tests merely to make a
performance claim.

Performance and real-installation evidence belongs in the benchmark/review
classes and must remain consent-gated, read-only, and sanitized.
