# DoD Domains + Workflows v2 — Step 2025-12-31 23:36 — docker data services

Goal
- Add distinct Postgres containers for DoD and Workflows plus RavenDB in docker-compose.

Context
- v2 plan requires separate databases per new data system and RavenDB in Docker.

Commands Executed
- None.

Files Changed
- `docker-compose.yml`
- `plans/dod_domains_workflows_v2.md`

Tests / Results
- Not run (compose edits only).

Issues
- None.

Decision
- Keep existing `postgres` service for legacy data; add `dod-postgres`, `workflows-postgres`, and `ravendb` services with distinct volumes and ports.

Completion
- [x]

Next Actions
- Add connection string keys + DI wiring for the new databases and RavenDB.
