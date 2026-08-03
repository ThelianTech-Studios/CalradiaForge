# Data Privacy

CalradiaForge's current durable data is local application configuration,
module metadata/cache, modpack definitions, logs, language resources, and EULA
state. The application should not treat ordinary paths, module metadata, or
user-authored modpack names as credentials, but logs and benchmark artifacts may
still expose local environment details if callers or tooling include them.

Keep real-installation benchmark artifacts sanitized and avoid committing local
installation paths. Do not add telemetry, cloud sync, or Nexus credential data
to local configuration without an accepted design and explicit source changes.
