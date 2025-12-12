# Step Note — prompt_construction_refactor — 2025-12-08 18:45

## Goal
Sketch prompt builder inputs/outputs and initial implementation targets (chat + WorldBibleManager) to move from ad-hoc interpolation to structured composition.

## Decisions / Plan
- Introduce `IPromptBuilder` (or `PromptComposer`) in Clients with methods like:
  - `BuildChatPrompt(ChatContext ctx, IEnumerable<ToolDescriptor> tools, ProviderModelOptions opts)` → `IReadOnlyList<ChatMessage>`
  - `BuildWorldBiblePrompt(WorldBibleContext ctx)` → string/ChatMessage
- Inputs for chat:
  - Agent/persona text (roleplay/prefix/suffix), safety/system preamble blocks, conversation history window, optional summary, tool index (serialized JSON for tool list), provider/model hints, scope metadata tags.
  - History and tool blocks should use JSON serialization for structured parts (tool index, safety directives), avoiding inline braces.
- Inputs for WorldBible:
  - Plan name, branch slug, plan description, pass summaries; output can remain a single system string but built from template + escaped values; consider JSON shape block serialized (characters/locations/systems) to avoid manual braces.
- Config:
  - Allow appsettings blocks for system/safety templates per provider/model; default to existing text for functional equivalence.
- Implementation order:
  1) Add builder abstraction + models (contexts for chat and world-bible); keep existing AgentService/runner logic intact initially.
  2) Port AgentService chat prompt assembly to builder (system, tool index, history window).
  3) Port WorldBibleManager prompt to builder.
  4) Backfill unit tests for builder outputs; snapshot key prompts for regression.

## Files to Touch (planned)
- New: `src/Cognition.Clients/Prompts/PromptBuilder.cs` (or similar), context/option models, interfaces.
- Wire via DI in API/Clients.
- Update `AgentService` to use the builder for chat/ask/ask-with-tools.
- Update `WorldBibleManagerRunner` to call builder.
- Tests: new prompt builder tests in Clients.Tests.

## Risks / Constraints
- Must preserve functional output initially; snapshot/expected strings should match current behavior.
- JSON serialization in tool index and template blocks to reduce interpolation errors.

## Completion
- Status: ○ (design sketch; no code yet)

## Next Actions
- Add builder abstraction and context models in Clients.
- Begin port: AgentService chat path first, then WorldBibleManagerRunner.
