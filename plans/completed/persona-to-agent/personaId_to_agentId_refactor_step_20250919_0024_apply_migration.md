Goal
- Apply migration to add non-null Conversation.AgentId and ConversationMessage.FromAgentId with backfill and constraints.

Commands Executed
- dotnet ef database update -p src/Cognition.Data.Relational

Results
- Migration applied: 20250919051851_AgentScope_AgentOnConversation_FromAgentOnMessage
- Output: Build succeeded; migration lock acquired; migration completed.

Post-Checks (recommended)
- SQL:
  - SELECT COUNT(*) FROM conversations WHERE agent_id IS NULL; -- expect 0
  - SELECT COUNT(*) FROM conversation_messages WHERE from_agent_id IS NULL; -- expect 0
- Index/Constraint presence:
  - agents(persona_id) UNIQUE
  - conversations(agent_id) index, FK -> agents(id)
  - conversation_messages(from_agent_id) index, FK -> agents(id)

Issues
- None observed during update. If extensions (pgcrypto/uuid-ossp) were restricted, migration would have failed earlier.

Decision
- Proceed to service-layer changes (ScopeToken enforcement, RetrievalService, ToolDispatcher dual-write/read window).

Completion
- ✅

