# ConversationsController

## Objective
Keep conversation APIs surface-scoped (public), ensure provider/model defaults come from stored defaults/agent profile, and tighten validation + scope usage.

## Status
Partially updated (LLM defaults integrated). Needs validation/record cleanup and scope guard audit.

## Hotspots / Risks
- DTO record annotations (create/append) may be ignored on properties.
- Rate-limit partitioning logic (agent-first) to confirm.
- ScopePath: ensure participants/messages retrieval uses factory; no manual filters.
- Settings PATCH/GET: validate provider/model belong together.

## Planned Actions
- Convert request records to classes or annotate ctor params; add validation tests.
- Verify default resolution order: stored default → agent profile → heuristic.
- Scope audit: queries and message saves go through ScopePath; add analyzer allowlist if needed.
- Ensure rate-limit partitioning matches agent-first policy.

## Tests / Checks
- API tests for create/append/settings with validation errors.
- Scope analyzer/test covers conversations queries.
- Rate-limit partition test (agent→conversation→persona fallback).
