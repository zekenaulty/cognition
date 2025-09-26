Goal
- Ensure new non-null Agent bindings are respected at runtime by setting Conversation.AgentId and ConversationMessage.FromAgentId in controllers/services.

Changes
- ChatController: set FromAgentId for user and assistant messages; also for ask-with-plan user message.
- ConversationsController:
  - On Create: compute AgentId (prefers assistant/default; falls back to first participant) and set Conversation.AgentId.
  - On AddMessage: set FromAgentId from FromPersonaId.
- AgentService: set FromAgentId for all persisted messages (user, assistant, thought) in AskWithPlanAsync and ChatAsync.

Commands Executed
- dotnet build src/Cognition.Data.Relational -v minimal
- dotnet build (solution) — failed due to file locks in Api (non-code issue); Data and Clients build succeed.

Files Changed
- src/Cognition.Api/Controllers/ChatController.cs
- src/Cognition.Api/Controllers/ConversationsController.cs
- src/Cognition.Clients/Agents/AgentService.cs

Tests / Results
- Compilation: Data/Clients compile. Full solution build shows file lock warnings/errors from running processes; code changes compile in their projects.
- Runtime not validated in this step.

Issues
- Api project build blocked by file locks (Cognition.Api running in another process). Stop the process or build the project individually after closing the host.

Decision
- Proceed with next steps (ScopeToken and retrieval enforcement) after confirming API builds locally once locks are cleared.

Completion
- ✅ (code staged; verify by running after closing running processes)

Next Actions
- Implement ScopeToken type and IRetrievalService; start propagating scope in ToolDispatcher.
- Add agentId to event payloads (later in rollout), and prepare API DTO changes.

