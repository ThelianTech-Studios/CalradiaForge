# Recovery and Troubleshooting

Use the visible error/warning message first, then inspect the current log under
the portable `Logs` directory. Common recovery paths are:

- configuration read/write failure: continue with the in-memory defaults/current
  session and repair storage permissions or location;
- invalid game detection: select and validate the game folder manually;
- missing Steam Workshop root: select it in Settings while retaining local
  modules;
- incomplete scan: correct the relevant path or access issue and refresh; the
  prior accepted snapshot is preserved;
- install failure or cancellation: read the terminal outcome and reconcile the
  module directory before retrying;
- launch blocked for Epic/Game Pass: launch through the platform client.

Do not infer data loss, rollback guarantees, or universal platform support from a
single log message. See [Diagnostics](../Capabilities/Diagnostics.md) and the
relevant [review evidence](../../Reviews/README.md).
