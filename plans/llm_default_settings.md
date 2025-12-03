# LLM Default Settings

## Objective
Persist a global default LLM provider/model in the database, expose admin APIs/UI to view/change them, and have the chat flow consume the stored defaults instead of hardcoded fallbacks.

## Scope
- In: Defaults storage, admin GET/PATCH API, UI settings page, chat default resolution order updated (stored default → agent profile → heuristic), validation that model belongs to provider, basic tests.
- Out: Per-user defaults, per-agent overrides UI, CI wiring.

## Deliverables
- Schema entry (table or GlobalDefault flag) storing providerId/modelId with audit fields.
- API endpoints: `GET /api/settings/llm-defaults`, `PATCH /api/settings/llm-defaults` (admin-only) with validation.
- Chat resolver uses stored defaults before heuristics.
- Admin settings page to view/change defaults (provider dropdown → models).
- Tests: resolver preference order, API validation, UI/manual smoke.

## Data/API/Service/UI Changes
- Data: add table/migration (or mark one ClientProfile as GlobalDefault).
- API: controller for defaults with admin auth; validate model belongs to provider.
- Services: `ResolveDefaultLlmAsync` reads stored defaults first, then agent profile, then heuristic fallback.
- UI: new admin page; `useChatProviderModel` loads stored defaults; hardcoded preference only as last resort if no stored default exists.

## Migration/Rollout Order
1) Add schema + service; seed optional initial default (Gemini/Flash).
2) Add API GET/PATCH with auth + validation.
3) Wire chat resolver to stored defaults.
4) Add admin settings page and hook `useChatProviderModel` to GET defaults.
5) Demote/remove hardcoded fallback (keep only as last-resort if no defaults).

## Testing/Verification
- Unit: resolver picks stored default over heuristics; rejects provider/model mismatch.
- API: GET/PATCH happy path; 400 on mismatch; 401/403 for non-admin.
- UI: Manual smoke—change default, refresh chat, new convo shows selected default.

## Risk/Rollback
- Risk: bad default (missing model) breaks chat; mitigate with validation and fallback heuristic if GET fails.
- Rollback: clear defaults row; fallback resumes.

## Worklog Protocol
- Step notes per `plans/README.md` with Goal, Context, Commands, Files, Tests, Issues, Decision, Completion, Next Actions.

## Checklist
- [ ] Schema/migration added
- [ ] Service/resolver reads stored defaults
- [ ] API GET/PATCH with auth + validation
- [ ] Chat hook consumes stored defaults; hardcoded fallback demoted
- [ ] Admin UI page to set defaults
- [ ] Tests (unit/API/manual UI)
