Goal
- Prepare and execute DB migration to support agentId-scoped conversations/messages with dual-write/read window.

TODOs
- [ ] Design migrations for target DB (EF Core/SQL scripts) to:
  - [ ] Create `Agents` table (Id, PersonaId UNIQUE, Name?, IsActive, ToolProfileId?, ModelProfileId?, SafetyProfileId?, MemoryRootId NOT NULL).
  - [ ] Add `AgentId` to `Conversations` (nullable initially) + index.
  - [ ] Add `AgentId` to `Messages` (`FromAgentId`) (nullable initially) + index.
  - [ ] Add/ensure `ContentHash` on messages/vectors + index.
  - [ ] Add vector metadata columns `AgentId`, `ConversationId` if stored in DB.
- [ ] Backfill
  - [ ] Create 1 Agent per Persona and set `MemoryRootId`.
  - [ ] Populate `Conversations.AgentId` from prior Persona mapping.
  - [ ] Populate `Messages.FromAgentId` accordingly.
  - [ ] Vector backfill: stamp `AgentId`/`ConversationId`; tag `Source:"legacy"` when conversation unknown.
- [ ] Dual-write/read
  - [ ] Update services to write both Persona and Agent fields during window.
  - [ ] Read path prefers `ConversationId` → fallback `AgentId`.
- [ ] Constraints cleanup
  - [ ] Enforce `UNIQUE(Agents.PersonaId)`.
  - [ ] Drop PersonaId FKs on conversations/messages after cutover.
  - [ ] Finalize/optimize indexes.

Commands (to be adapted)
- Placeholder EF Core example:
  - `dotnet ef migrations add AgentScope_Migration`
  - `dotnet ef database update`

Files Expected to Change
- `migrations/**`
- ORM entity/configuration files (e.g., `Agents`, `Conversations`, `Messages` models)
- Service layer code adjusting to dual-write/read

Tests / Verification Plan
- Unit tests for retrieval filter order (Conversation → Agent).
- Idempotency via `ContentHash` validated (no duplicates on re-run).
- Data migration dry-run on a copy; measure row counts and durations.

Issues / Unknowns
- Target DB and ORM specifics (SQL Server/Postgres? EF Core?). Adjust migrations accordingly.
- Vector store implementation details (DB vs external service) affect backfill mechanics.

Decision
- Proceed with DB-agnostic plan; specialize once environment details are confirmed.

Completion
- ❌

