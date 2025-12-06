# AgentsController

## Objective
Keep agent CRUD/tool-binding APIs lean, with clear admin vs user surfaces; ensure persona/agent invariants and ScopePath enforcement.

## Status
Agent cutover mostly complete; needs validation/DTO review and scope guard audit.

## Hotspots / Risks
- DTO record annotations may be ignored.
- Agent/persona linkage (agent must have persona; images may remain persona-scoped).
- ScopePath for retrieval/tools binding; avoid manual filters.

## Planned Actions
- Convert/annotate DTOs; add validation API tests.
- Extract agent data service to reduce DbContext in controller.
- Verify tool-binding endpoints enforce agent scope and respect banned constructor patterns.

## Tests / Checks
- API tests for create/update/bind tool invalid inputs.
- Scope analyzer/test passes for agent queries.
- Regression on persona/agent invariants.
