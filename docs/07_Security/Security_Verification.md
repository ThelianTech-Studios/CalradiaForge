# Security Verification

Security verification is evidence-bounded. Useful checks include:

- Core remains free of WPF references;
- project references preserve the allowed dependency direction;
- configuration malformed/read/write failures preserve the appropriate data;
- archive entry paths and extraction cleanup remain contained;
- install and scan commit gates preserve the accepted snapshot;
- unknown Steam process state does not authorize launch;
- logs and benchmark reports contain no intentionally supplied credentials or
  unredacted owner-installation literals;
- shutdown waits for tracked work before provider disposal.

Static inspection and automated tests do not constitute penetration testing,
advisory-database verification, or manual release acceptance. Record unavailable
checks as gaps rather than clean results.
