# Fiction Phase 001 Closeout — Step 2025-11-30 10:00 — Bible provenance & lifecycle mint

## Goal
Add provenance fields to world-bible entries and have character lifecycle mint/update entries with agent/persona/branch lineage.

## Context
- DoD calls for branch/agent/persona provenance in world-bible/roster. Entries previously lacked provenance columns and lifecycle didn’t mint entries.

## Commands Executed
- `dotnet ef migrations add FictionWorldBibleProvenance --project src/Cognition.Data.Relational --startup-project src/Cognition.Api`
- `dotnet ef database update --project src/Cognition.Data.Relational --startup-project src/Cognition.Api`
- `dotnet test tests/Cognition.Clients.Tests/Cognition.Clients.Tests.csproj --filter "CharacterLifecycleServiceTests"`
- `dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj --filter "AbuseHeadersAndRateLimitE2ETests|ScopePathProjectionTests"`

## Files Changed
- `src/Cognition.Data.Relational/Modules/Fiction/FictionWorldBibleEntry.cs` (provenance fields: agent/persona/branch/source pass/backlog/convo)
- `src/Cognition.Data.Relational/Modules/Fiction/Config.cs` (column mappings + fks)
- `src/Cognition.Clients/Tools/Fiction/Lifecycle/CharacterLifecycleService.cs` (mint/update bible entries with provenance; ensure bible per plan/branch)
- `tests/Cognition.Clients.Tests/Fiction/CharacterLifecycleServiceTests.cs` (new provenance mint test; adjusted existing flow)
- Migration: `20251130094707_FictionWorldBibleProvenance` (+snapshot)
- Added projection sanity test: `tests/Cognition.Api.Tests/Infrastructure/ScopePathProjectionTests.cs`

## Tests / Results
- ✅ `dotnet test tests/Cognition.Clients.Tests/Cognition.Clients.Tests.csproj --filter "CharacterLifecycleServiceTests"`
- ✅ `dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj --filter "AbuseHeadersAndRateLimitE2ETests|ScopePathProjectionTests"`

## Issues
- Existing world-bible entries are left null for provenance (additive fields); no backfill beyond optional update path in lifecycle.
- UI/roster still needs to surface provenance; not addressed in this step.

## Decision
- Provenance fields landed and lifecycle now mints/updates bible entries with agent/persona/branch and source metadata.

## Completion
- ✅

## Next Actions
- Surface provenance in roster/bible API/console views; add regression to assert payload fields.
- Add webhook/telemetry coverage for lore/backlog drift (separate workstream).
