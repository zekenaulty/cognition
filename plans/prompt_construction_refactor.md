# Prompt Construction Refactor

## Objective
Replace brittle, error-prone string interpolation (`$""`) used for constructing LLM prompt payloads in `AgentService` with a strongly-typed object model and deterministic JSON serialization. This eliminates escaping defects, prevents injection-style structure breakage, and decouples the "Mind's" content from its transport format.

## Scope
- In: Creating strictly typed DTOs (Data Transfer Objects) for prompt components (e.g., `PlanContext`, `GoalDefinition`, `ToolResult`). Refactoring `AgentService.BuildStepPlanPrompt` and similar methods to populate these objects and serialize them.
- Out: Changing the actual planner logic, tool execution flow, or the underlying `IChatClient` implementations. This is purely a refactor of *how* the message payload is assembled.

## Deliverables
- A set of domain-agnostic DTOs in `Cognition.Contracts` (or `Cognition.Clients.prompts`) representing the standard prompt schema.
- A `PromptFactory` or updated service methods that map Domain Entities (Plans, ScopeTokens) to these DTOs.
- Refactored `AgentService` that uses `System.Text.Json` to generate the final prompt string.
- "Golden Master" unit tests verifying that the serialized output matches expected JSON structures.

## Data / Service Changes
- **New Classes:** Introduce `PromptSchema`, `PromptMessage`, `PromptContext` (names TBD based on analysis) to model the JSON structure explicitly.
- **AgentService:** Remove all large interpolated string blocks (e.g., `$"{...}"`). Replace with object instantiation and `JsonSerializer.Serialize`.
- **Serialization:** Configure a specific `JsonSerializerOptions` instance (likely `UnsafeRelaxedJsonEscaping` or strict, depending on LLM tolerance) to ensure clean payload generation.

## Data / Service Changes (Revised)
- **New Classes:** `PromptSchema`, `PromptMessage`, `PromptContext` DTOs.
- **Data Entity Update (`PromptTemplate`):**
    - *Immediate:* Treat the existing `Template` string as the **System Prompt** content only.
    - *Future:* Migration to add `ConfigurationJson` column to `PromptTemplate` to store dynamic flags (e.g., `Temperature`, `ResponseFormat`, `IncludedModules`).
- **Repository Update:** Modify `PlannerTemplateRepository` (or create `IPromptConfigurationSource`) to return a typed `PromptDefinition` instead of a raw string.
    - *Old:* `GetTemplateAsync(id)` -> `string`
    - *New:* `GetPromptConfigAsync(id)` -> `PromptDefinition { SystemInstruction, OutputSchema, Examples }`
- **AgentService:** - Retrieve `PromptDefinition` from DB.
    - Hydrate `PromptSchema` DTO using the definition + runtime variables (`PlanContext`).
    - Serialize to final JSON.

## Data / Service Changes (Revised with Relational Integration)
- **New Classes:** `PromptSchema`, `PromptMessage`, `PromptContext` DTOs in Cognition.Contracts.
- **Data Entity Alignment (`PromptTemplate`):**
  - *Phase 1 (Minimal):* Reuse existing `Template` as `SystemInstruction` string; fetch via repository and inject into DTO.
  - *Phase 2 (Deferred Migration):* Add columns `OutputSchemaJson` (string), `ExamplesJson` (string), `Version` (int). Create migration (e.g., 20251209_AddStructuredPromptFields.cs) with backfill (old Template -> SystemInstruction).
- **Repository Update:** Evolve `PlannerTemplateRepository.GetTemplateAsync` to `GetPromptDefinitionAsync` -> `PromptDefinition { SystemInstruction, OutputSchema, Examples }`.
- **AgentService:** 
  - Fetch `PromptDefinition` from DB/repo.
  - Build `PromptSchema` DTO: Merge definition with runtime vars (e.g., goal, tools).
  - Serialize to JSON for LLM client.

## Testing / Verification (Updated)
- **Unit Tests:** Assert DTO serialization matches legacy string output; test with DB-fetched templates containing special chars.
- **Integration Tests:** End-to-end planner run with mocked DB returning structured vs. legacy templates.
- **Edge Cases:** Invalid JSON in new fields (validation throws); migration backfill preserves old data.

## Migration / Rollout Order
1) **Model Definition:** Create the DTO structure that mirrors the current expected JSON prompt format.
2) **Test Harness:** Create a unit test that takes the *current* string output and asserts it against the *new* serialized output to ensure parity (Snapshot Testing).
3) **Refactor Implementation:** Update `AgentService` to build the object graph instead of the string.
4) **Validation:** Run the snapshot tests. Once parity is confirmed, swap the implementation.
5) **Cleanup:** Remove the old string templates.

- **Migration / Rollout Order (Extended):**
6) **DB Alignment:** Implement Phase 1 (string reuse); defer Phase 2 to post-refactor bandwidth.

## Testing / Verification
- **Unit Tests:** Verify that special characters (quotes, newlines, braces) in `Goal` or `ToolOutput` are correctly escaped by the serializer and do not break the JSON structure.
- **Integration Tests:** Run a `Plan` execution cycle with a mock LLM to ensure the prompt received matches the tokenizer's expectations.
- **Edge Cases:** Test with input data containing intentional JSON-breaking characters (e.g., `user_input": " } break_structure: true, "ignore": "`).

## Risk / Rollback
- **Risk:** The `JsonSerializer` might produce slightly different whitespace or escaping sequences than the manual string (e.g., unicode escaping vs raw characters). Some over-fitted LLM prompts might degrade in performance if the formatting changes drastically.
- **Mitigation:** Use "Golden Master" snapshot tests to verify the output is semantically identical before merging.
- **Rollback:** Revert the changes to `AgentService.cs`; the new DTO classes can remain as unused code until fixed.

## Worklog Protocol
- Step notes per `plans/README.md`, one discrete action each, with commands, paths, tests, decisions, and completion status.

## Checklist
- [ ] Define `Prompt` DTO classes (Model)
- [ ] Create Snapshot/Golden Master tests for current prompt output
- [ ] Refactor `AgentService.BuildStepPlanPrompt` to use `JsonSerializer`
- [ ] Verify handling of special characters and large payloads
- [ ] Remove legacy string interpolation code