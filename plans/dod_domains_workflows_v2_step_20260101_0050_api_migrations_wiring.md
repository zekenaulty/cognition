# DoD Domains + Workflows v2 — Step 2026-01-01 00:50 — API migrations wiring

Goal
- Apply DoD and Workflows migrations during API startup.

Context
- `Cognition.Api` already migrates `CognitionDbContext`; add new DbContexts to the startup migration block.

Commands Executed
- None.

Files Changed
- `src/Cognition.Api/Program.cs`

Tests / Results
- Not run.

Issues
- None.

Decision
- Keep migrations guarded by try/catch like the existing data context to avoid blocking startup.

Completion
- [x]

Next Actions
- Run the API once to validate migrations apply to the new databases.
