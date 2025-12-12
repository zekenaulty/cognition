# Prompt Construction Refactor

## Objective
Refactor prompt assembly across agents/chat/tools to be composable, testable, and DRY, ensuring provider/model-specific instructions, scope context, and safety rails are applied consistently without duplicative string building.

## Scope
- In: Server-side prompt builders used by chat/ask/ask-with-tools/planners; provider/model overrides; safety/system preambles; unit/regression coverage for prompt composition.
- Out: Frontend prompt editing UI; new planner/tool features; LLM client transport changes.

## Deliverables
- Central prompt construction service/builder with clear inputs (agent/persona context, conversation history/summaries, tools/catalog, safety settings, provider/model options).
- Updated chat/ask/ask-with-tools/planner call sites to use the builder.
- Tests covering provider/model variations, tool inclusion, safety preambles, scope/identity tagging, and history windowing.
- Docs/README snippet describing prompt composition contract.

## Data / API / Service Changes
- Introduce prompt builder service(s) in API/Clients layer; optionally expose configuration (system prompts, safety blocks) via appsettings.
- Adjust agent/chat/planner services to consume the builder instead of ad-hoc string interpolation.

## Migration / Rollout Order
1) Inventory current prompt assembly call sites (chat/ask/ask-with-tools/planners/tools) and shared helpers. **(done)**
2) Add prompt builder abstraction + config; port one path (chat) first. **(done — chat path on builder)**
3) Port tool/planner paths; dedupe safety/system preambles and history handling (next: Ask/AskWithTools/AskWithPlan; then WorldBible/planner prompts).
4) Add snapshot/unit coverage for builder outputs and updated call sites; update docs after ports.

## Testing / Verification
- Unit tests for builder inputs → prompt outputs.
- Regression: chat/ask/ask-with-tools happy paths; planner prompt construction with tools/catalog.
- Optional snapshot tests for key prompt templates.

## Risk / Rollback
- Risk: prompt changes affect model outputs. Mitigation: start with functional equivalence, snapshot key prompts.
- Rollback: retain old helpers; feature-flag new builder if needed.

## Worklog Protocol
- Step notes per `plans/README.md`; one action per note with commands, files, tests, decisions, completion.

## Checklist
- [x] Call site inventory
- [x] Prompt builder abstraction added
- [ ] Chat/ask paths ported (chat ✅; Ask/AskWithTools/AskWithPlan remain)
- [ ] Tool/planner paths ported (WorldBible/planner/tool prompts)
- [ ] Tests added/updated (builder snapshots, Ask*/planner regressions)
- [ ] Docs updated (prompt composition contract)

## Next turns (short)
1) Snapshot current Ask*/AskWithTools/AskWithPlan prompt shapes; extend builder to emit equivalent payloads; port these call sites.
2) Snapshot planner/tool prompt templates (WorldBible, outline/step prompts); move into builder or dedicated prompt helpers; port call sites.
3) Add builder snapshot tests and update docs snippet once ports are stable.
