**Objective**
- Migrate chat flows from personaId to agentId with strict scope isolation to prevent memory bleed. Maintain minimal downtime via dual-write/read and clear rollback.

**Snapshot**
- Tool: `arc.py` at repo root creates a Markdown snapshot of the entire repo tree and text files honoring `.gitignore`.
- Usage:
  - Create snapshot: `python ./arc.py`
  - Output: `<repo-name>.<YYYYMMDD_HHMMSS>.md` in the parent directory of the repo (e.g., `..\cognition.20250919_000348.md`).
  - Labeling: arc.py does not accept labels. Use a Git tag to label the repo state for diff/restore: `git tag persona_to_agent_pre_migration`.
- Diff later:
  - Code/config diff: `git diff persona_to_agent_pre_migration..HEAD`
  - Path-level diff: `git diff --name-status persona_to_agent_pre_migration..HEAD`
  - Snapshot file diff (optional): generate a second snapshot and compare the two Markdown files using your diff tool.
- Restore (surgical): `git restore --source persona_to_agent_pre_migration -- src/** "migrations/**"`
- Restore (full): `git reset --hard persona_to_agent_pre_migration`
- Snapshot metadata: record to `plans/personaId_to_agentId_refactor_step_snapshot.md` (label, timestamp, branch, commit, file count, snapshot file path).

**A. Data Layer**
- Entities (add/extend):
  - `Agents`: `Id uuid PK`, `PersonaId uuid UNIQUE`, `Name nvarchar?`, `IsActive bit`, `ToolProfileId uuid?`, `ModelProfileId uuid?`, `SafetyProfileId uuid?`, `MemoryRootId uuid NOT NULL`.
  - `Conversations`: `Id uuid PK`, `AgentId uuid FK -> Agents(Id)`, `ProjectId uuid?`, `WorldId uuid?`, `MemoryScopeConversation uuid NOT NULL`.
  - `Messages`: `Id uuid PK`, `ConversationId uuid FK -> Conversations(Id)`, `FromAgentId uuid FK -> Agents(Id)`, `Role`, `Content`, `CreatedAt`, `ContentHash`.
  - Vector store metadata for every embedding/write:
    - `{ TenantId, AppId, PersonaId, AgentId, ConversationId, ProjectId?, WorldId?, Source, Tags[], ContentHash }`.
- Indexes:
  - `Messages(ConversationId)`, `Conversations(AgentId)`, `Vectors(AgentId)`, `Vectors(TenantId, AppId, AgentId)`, `Vectors(ContentHash)`.
- Constraints:
  - `UNIQUE(Agents.PersonaId)` to enforce 1:1 Agent:Persona during this migration.
  - All FKs cascade rules unchanged unless noted; do not drop Persona tables yet.
- Idempotency via `ContentHash`:
  - Compute SHA-256 over normalized content + stable scope key (e.g., `TenantId|AppId|AgentId|ConversationId|Source`).
  - Upserts on vector/messages honor `ContentHash` to avoid duplicates per write target.
- Migration steps (DB-agnostic outline):
  1) Add `AgentId` (nullable) to `Conversations`, `Messages`, and vector metadata; add `ContentHash` if missing.
  2) Create `Agents` table if not present; add `UNIQUE(PersonaId)` and `MemoryRootId` (required).
  3) Backfill `Agents` (1 agent per persona) and set `MemoryRootId` for each.
  4) Backfill `Conversations.AgentId` and `Messages.FromAgentId` from `PersonaId` relations.
  5) Create indexes listed above.
  6) Switch services to dual-write/read (see sections C/D) while both PersonaId and AgentId exist.
  7) Drop old PersonaId FKs from `Conversations`/`Messages` after cutover and soak.

**B. Scope & Retrieval Model**
- ScopeToken passed through all layers: `{ tenantId, appId, personaId, agentId, conversationId, projectId?, worldId? }`.
- Default writes: conversation-scoped (short-term) under `ConversationId`.
- Promote/remember: explicitly copy/promote to `AgentId` long-term memory root.
- Default reads: `ConversationId` first → fallback to `AgentId`. Never broaden beyond `Agent` unless explicitly mounted.
- All RAG/tool calls go through a single `IRetrievalService` that requires `ScopeToken` and applies the read policy.

**C. API Surface**
- Drop v1 personaId endpoints; standardize on agent-centric routes (no -v2 suffix):
  - `POST /api/conversations` body: `{ agentId, title?, participantIds[] }`
  - `POST /api/chat/ask` body: `{ agentId, providerId, modelId?, input }`
  - `POST /api/chat/ask-with-tools` body: `{ agentId, providerId, modelId?, input }`
  - `POST /api/chat/ask-chat` body: `{ conversationId, providerId, modelId?, input }`
  - `POST /api/chat/remember` body: `{ conversationId? | agentId?, content, metadata? }`
- Conversations bind to `agentId`; messages resolve scope from `conversationId`.

**D. Services & Tooling**
- `AgentService` and related signatures switch to `agentId/conversationId` inputs.
- `ToolDispatcher` requires `ScopeToken`; propagates to every tool and embedding call.
- Embedding/write path must stamp full scope metadata including `ContentHash`.
- Eventing/Hub payloads include `AgentId` (never `PersonaId`) and `ConversationId`.
- Introduce `IRetrievalService` entrypoint to enforce scope and consolidation of retrieval logic.

**E. UI**
- Replace Persona picker with Agent picker (1:1; label can show Persona name, value is `agentId`).
- Conversation header shows scope chips: `Agent`, optional `Project`/`World`.
- "Remember this" action promotes to `Agent` memory; default writes remain conversation-only.

**F. Migration Strategy**
- Pre-migration snapshot with `arc.py` and Git tag:
  - `python ./arc.py`
  - `git tag persona_to_agent_pre_migration`
- DB phase:
  - Add new columns and indexes; keep Persona FKs for now. (Done)
  - Backfill `AgentId` using `PersonaId`. Ensure `Agents.PersonaId` is UNIQUE. (Done)
  - Wipe all conversation data to assert new model cleanly in dev: `dotnet ef migrations add WipeConversations` (adds TRUNCATE) and `dotnet ef database update`. (Done)
  - Vector backfill (optional post-wipe): stamp `AgentId`/`ConversationId`; if conversation unknown, attach to Agent root and tag `Source:"legacy"`.
- Rollout order:
  1) DB migrations →
  2) Wipe conversation data (dev only) →
  3) Server/API switch to agent-centric routes (no -v2) →
  4) UI adopts agent picker and scope chips →
  5) Remove legacy persona paths.

**G. Testing/Verification**
- Retrieval unit tests: filter by `ConversationId` then fallback `AgentId` even if caller omits `ConversationId`.
- Isolation tests: two conversations under the same persona never cross-retrieve unless explicitly promoted to Agent.
- Back-compat tests for deprecated endpoints accepting `personaId`.
- Data migration dry run against snapshot and tag: measure time, row counts, and ContentHash duplicate rates; include rollback commands.

**H. Risk & Rollback**
- Risks: missing scope propagation, partial backfill, stale/missing indexes, duplicate embeddings (no ContentHash), UI still sending personaId, event payloads with personaId, tool calls missing ScopeToken.
- Rollback: use Git tag `persona_to_agent_pre_migration`.
  - Surgical: `git restore --source persona_to_agent_pre_migration -- src/** "migrations/**"`
  - Full: `git reset --hard persona_to_agent_pre_migration`
- The Markdown snapshot from `arc.py` provides a human-auditable copy for recovery and review.

**I. Worklog/Execution Protocol**
- Step notes in `plans/personaId_to_agentId_refactor_step_{notes}.md`.
- Filename pattern: `personaId_to_agentId_refactor_step_YYYYMMDD_HHMM_{topic}.md`.
- Each note includes: Step goal, commands executed, files changed, tests/results, issues, decisions, completion (✅/❌), and next actions.

**Checklist**
- Completed: Audit all personaId usages and migrate major chat/navigation/API flows to agentId with passing build/lint.
- Completed: ChatPage migrated to agentId-first state/handlers with validated build.
- Completed: Isolate agentId<->personaId fallback adapter and propagate updates to shared hooks/components.
- Todo: Remove personaId from remaining lagging components/hooks, update documentation, and finalize fallback strategy.

---
**Audit Results (Frontend)**
- Chat surfaces now route, select, and send via agentId; legacy persona references limited to fallback adapter.
- ChatLayout/ChatMenu present agent lists/labels from agentId source of truth.
- useConversationsMessages/useImageGenerator updated to accept agentId primary, carrying personaId only when necessary.
- Fallback adapter encapsulated via useAgentPersonaIndex; legacy callers pull personaId labels through the adapter.
- Image Lab remains persona-scoped for user image browsing/generation until backend contracts change.

**Audit Results (Backend)**
- PersonaId usages cataloged across AgentService, RetrievalService, ToolDispatcher, and event payloads.
- Core service methods accept agentId as the primary identifier with legacy personaId entry points preserved.
- AgentId-to-personaId fallback layer still shared with main flows; needs decoupling before personaId removal.

**Migration Todos (Next Steps)**
- Completed: Navigation shell and drawer rely on useAgentPersonaIndex; no persona-bound state remains in chat flows.
- Todo: Update documentation/migration notes with completed ChatPage refactor and enumerate intentional legacy persona touchpoints (image lab, auth).
- Todo: Re-run build/lint/regression checks after documentation updates.

**Notes on arc.py behavior**
- Honors `.gitignore` via Git plumbing; falls back to `pathspec` if available. Excludes common binary formats. Always ignores `arc.py` itself.
- No `snapshot/diff/restore` subcommands; we wrap with Git tag for labeling and use Git for diff/restore.
