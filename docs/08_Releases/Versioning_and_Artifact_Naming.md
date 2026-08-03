# Versioning and Artifact Naming

`source/Directory.Build.props` is the current shared version source. At this
baseline it defines `VersionPrefix=0.14.0`, `VersionSuffix=beta`, application
version `0.14.0-beta`, assembly/file version `0.14.0.0`, and informational
version `0.14.0-beta` without an automatic source-revision suffix.

Do not infer a public release from an internal changelog entry. Major/minor
version changes require owner approval. Documentation-only maintenance does not
automatically require an application-version entry.
