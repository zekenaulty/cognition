# Step Note — prompt_construction_refactor — 2025-12-08 17:40

## Goal
Inventory current prompt construction call sites and identify risky string-interpolation patterns to target for a central builder.

## Context
- Need to minimize string interpolation risk (escaping/format bugs) while accepting some templating; prefer structured assembly/JSON where possible.

## Commands Executed
- `rg "prompt" src/Cognition.Clients | Select-Object -First 20`
- `rg "StringBuilder" src/Cognition.Clients/Agents/AgentService.cs`
- `rg "Build.*Prompt" src/Cognition.Clients/Agents/AgentService.cs`
- `rg "PromptTemplates" src` (via PromptTemplatesController reference)

## Findings
- **AgentService** (major hot spot):
  - Uses `StringBuilder` for system message, tool index, and instruction assembly.
  - Helper methods: `BuildOutlinePlanPrompt`, `BuildStepPlanPrompt`, `BuildFinalizePrompt` — heavy string interpolation with inline JSON snippets (e.g., “Return JSON only…”).
  - Chat title/summarization prompts built as `List<ChatMessage>` but with interpolated strings.
  - Tool index construction is freeform text; risk of formatting drift per provider/model.
- **LLM clients**: legacy `GenerateAsync(string prompt)` APIs exist; prompt assembly happens upstream (AgentService, runners). Clients accept `List<ChatMessage>` otherwise.
- **Fiction weaver runners** (not fully enumerated yet): e.g., `WorldBibleManagerRunner.BuildWorldBiblePrompt` and other phase runners build large text prompts directly; likely similar risks.
- **PromptTemplates**: DB-backed prompt templates exist via `PromptTemplatesController`/repo; not currently used to consolidate agent prompts.
- **Images**: prompt strings stored but not composed beyond truncation; lower risk for this refactor.

## Issues / Risk Surface
- Ad-hoc string interpolation in AgentService for plan/step/finalize prompts; no structured JSON builder or provider/model guards.
- Tool index and safety/system preambles duplicated in multiple builders.
- No central config for provider/model-specific blocks or for scope metadata injection.

## Decisions
- Target AgentService first for central builder: system message, tool index, history window, outline/step/finalize prompts should flow through a composable builder with structured JSON fragments (serialized) instead of inline braces.
- Next pass: enumerate fiction phase runners for similar prompt assembly to plan porting order.

## Completion
- Status: ○ (inventory baseline captured; more enumeration needed for fiction runners)

## Next Actions
- Enumerate fiction weaver runner prompts (WorldBibleManager, IterativePlanner, Scene/Scroll/Chapter) for string interpolation patterns.
- Draft builder inputs/outputs covering chat/ask/tool-plan flows and tool index composition.
