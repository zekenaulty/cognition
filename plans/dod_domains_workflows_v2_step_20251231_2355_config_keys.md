# DoD Domains + Workflows v2 — Step 2025-12-31 23:55 — config keys

Goal
- Add connection string keys and RavenDB defaults to configs and env templates.

Context
- Separate Postgres databases for DoD/workflows plus RavenDB in Docker.

Commands Executed
- None.

Files Changed
- `.env`
- `.env.example`
- `src/Cognition.Api/appsettings.json`
- `src/Cognition.Jobs/appsettings.json`

Tests / Results
- Not run (config updates only).

Issues
- None.

Decision
- Use `DomainsPostgres` and `WorkflowsPostgres` connection strings plus a `RavenDb` section with `Urls` and `Database`.

Completion
- [x]

Next Actions
- Add DI wiring once the DoD/workflows DbContexts and RavenDB repositories exist.
