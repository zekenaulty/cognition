# Tools / ToolExecution / LlmController / LlmDefaults

## Objective
Clarify surfaces for tools (runtime vs admin/catalog), enforce ScopePath gating, and keep DTOs validated; ensure LLM provider/model defaults and enablements are consistent.

## Status
LlmDefaults added; provider/model APIs live. Needs surface split and validation/DTO cleanup.

## Hotspots / Risks
- ToolExecution/ToolsController surfaces may mix admin/internal with user calls.
- Scope enforcement for tool dispatch (ensure sandbox/scope policies applied).
- DTO record validation; model/provider mismatch handling.
- LlmDefaults GET now anonymous; ensure PATCH admin-only remains enforced.

## Planned Actions
- Document/partition routes: public resolve vs admin catalog vs internal execution.
- Add validation tests for provider/model consistency and tool exec payloads.
- Scope analyzer for tool dispatch; ensure ToolDispatcher uses ScopePathBuilder and policy.
- Confirm LlmDefaults resolution order consumed by chat/hooks.

## Tests / Checks
- API tests for resolve/enable/exec validation errors and auth.
- Scope analyzer passes for tool dispatch paths.
- LlmDefaults GET/PATCH tests (403 for non-admin on PATCH).
