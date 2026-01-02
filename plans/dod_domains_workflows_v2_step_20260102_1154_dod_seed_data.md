# DoD Domains + Workflows v2 — Step 2026-01-02 11:54 — DoD seed data

Goal
- Add optional seed data for DoD core domain and policy.

Context
- Story 7 requires baseline seed data; seed should be safe to re-run.

Commands Executed
- `New-Item -ItemType Directory -Force src\Cognition.Domains.Relational\Seed`
- `Set-Content -Path src\Cognition.Domains.Relational\Seed\DomainsDataSeeder.cs`
- `Apply patch: src/Cognition.Api/Program.cs`

Files Changed
- `src/Cognition.Domains.Relational/Seed/DomainsDataSeeder.cs`
- `src/Cognition.Api/Program.cs`

Tests / Results
- Not run (seeding only).

Issues
- None.

Decision
- Seed only if the DoD domains table is empty, with a single technical root domain + default policy.

Completion
- [x]

Next Actions
- Add tests for seeding or validate by running the API locally.
