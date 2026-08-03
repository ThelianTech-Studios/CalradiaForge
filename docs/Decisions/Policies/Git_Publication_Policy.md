# Git Publication Policy

Documentation work may inspect branch, `HEAD`, upstream, and working-tree
state, but must not commit, push, merge, rebase, reset, amend, cherry-pick, or
open a pull request without explicit authorization. Preserve unrelated owner
changes and report scope drift.

Source-level changelog and migration-map updates require an owner-accepted,
committed source endpoint. Documentation-only work does not create a source
migration entry under the current code-only policy. Leave final publication for
owner review unless the user explicitly authorizes Git publication.
