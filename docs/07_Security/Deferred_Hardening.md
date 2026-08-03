# Deferred Hardening

The following are explicit follow-up boundaries, not current guarantees:

- rollback/recovery for destructive module replacement;
- archive entry count, size, compression-ratio, timestamp, and resource limits;
- final reparse-point behavior across archive extraction and destination creation;
- complete security/dependency advisory verification when network access is
  available;
- implementation of the future Nexus credential/network boundary;
- any performance-driven production change while findings remain pending owner
  or developer review.

Do not silently convert these items into implementation claims. Track authorized
work under [Plans](../Plans/README.md) and retain evidence under [Reviews](../Reviews/README.md).
