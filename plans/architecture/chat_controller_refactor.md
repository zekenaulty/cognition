# ChatController / ChatHub

## Objective
Tighten chat surface (public vs internal), ensure ScopePath/ScopeToken, and remove any ad-hoc fallbacks; keep controller thin with service orchestration; align DTO validation.

## Status
Pending. Hub + controller currently mix creation/send/remember flows with heuristics for provider/model selection.

## Hotspots / Risks
- Provider/model resolution heuristics; ensure stored defaults + agent profile drive selection.
- DTO validation on record types; avoid ignored annotations.
- Scope discipline: message persistence and tool invocations must use ScopePath builder.
- Duplication between Hub and HTTP fallback (/api/chat/ask-chat).

## Planned Actions
- Surface: document routes as public chat API; move internal helpers behind services.
- Validation: convert record DTOs to classes or annotate ctor params to stop validation warnings.
- Scope: audit message save + tool dispatch for ScopeToken/ScopePath usage; add guard.
- Consolidate send flow: single orchestration service used by Hub and HTTP fallback.

## Tests / Checks
- API tests for send/remember/ask-chat success & validation errors.
- Scope guard test/analyzer passes for chat paths.
- Hub + HTTP paths share service; no divergent behavior.
