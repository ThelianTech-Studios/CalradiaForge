# Nexus System Boundary

The Nexus project is optional and reserved. The current solution contains its
project reference boundary but no implemented Nexus networking surface. This
document therefore defines only the boundary: future authentication, REST/API
calls, download mechanics, NXM handling, and transport stay in Nexus; credentials
must use DPAPI CurrentUser and never `AppConfig`; Core/UI must remain usable
without Nexus.

Any future implementation must be documented separately and must not be
presented as current behavior by application, release, or security documents.
