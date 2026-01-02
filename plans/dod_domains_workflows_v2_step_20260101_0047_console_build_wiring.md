# DoD Domains + Workflows v2 — Step 2026-01-01 00:47 — console build wiring

Goal
- Update API build pipeline to use the new DoD console output.

Context
- `Cognition.Api` currently builds `Cognition.Console`; switch to `Cognition.Console.Dod`.

Commands Executed
- None.

Files Changed
- `src/Cognition.Api/Cognition.Api.csproj`

Tests / Results
- Not run (build wiring only).

Issues
- None.

Decision
- Keep legacy console intact but stop building it by default.

Completion
- [x]

Next Actions
- Run npm install/build for the new console and verify output in `wwwroot`.
