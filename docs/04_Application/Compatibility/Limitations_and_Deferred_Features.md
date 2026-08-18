# Limitations and Deferred Features

- Manual WPF, Steam, Bannerlord, packaged-runtime, and public-release evidence
  is not implied by automated tests or static inspection.
- Automatic Epic/Game Pass detection is gated and their direct launch paths are
  intentionally blocked.
- Nexus authentication, downloads, NXM handling, update checks, timed polling,
  and silent background scans are not implemented.
- Installer rollback and archive resource-limit hardening remain separate
  security work; do not promise them in capability documentation.
- Performance findings and optimization are separate from this documentation
  overhaul and require owner/developer disposition.

Track future work in [Plans](../../Plans/README.md) and evidence in
[Security](../../07_Security/README.md) or [Reviews](../../Reviews/README.md).
