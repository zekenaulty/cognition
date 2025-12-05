# LLM Default Settings

## Objective
Persist a global default LLM provider/model in the database, expose admin APIs/UI to view/change them, and have the chat flow consume the stored defaults instead of hardcoded fallbacks.

## Scope
- In: Defaults storage, admin GET/PATCH API, UI settings page, chat default resolution order updated (stored default ? agent profile ? heuristic), validation that model belongs to provider, basic tests.
- Out: Per-user defaults, per-agent overrides UI, CI wiring.

## Deliverables
- Schema entry, table with active and priority flags, storing providerId/modelId with audit fields.
- API endpoints: `GET /api/settings/llm-defaults`, `PATCH /api/settings/llm-defaults` (admin-only) with validation.
- Chat resolver uses stored defaults before heuristics.
- Admin settings page to view/change defaults (provider dropdown ? models).
- Tests: resolver preference order, API validation, UI/manual smoke.

## Data/API/Service/UI Changes
- Data: add table/migration (or mark one ClientProfile as GlobalDefault).
- API: controller for defaults with admin auth; validate model belongs to provider.
- Services: `ResolveDefaultLlmAsync` reads stored defaults first, then agent profile, then heuristic fallback.
- UI: new admin page; `useChatProviderModel` loads stored defaults; hardcoded preference only as last resort if no stored default exists.

## Migration/Rollout Order
1) Add schema + EF migration + seed optional initial default (Gemini/Flash).
2) Add service/repo (ResolveDefaultLlmAsync) and wire chat resolver to stored defaults with validation/fallback.
3) Add API GET/PATCH with admin auth + provider/model validation.
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

## Planned Files / Components
- Data: `Modules/LLM/LlmGlobalDefault.cs`, migration, DbContext wiring, seed (StartupDataSeeder or migration seed).
- Service: `ILlmDefaultService` + impl (reads active lowest Priority), wiring into chat resolver (`useChatProviderModel`, API chat endpoints if applicable).
- API: `LlmDefaultsController` with GET/PATCH, validator for provider/model relationship.
- UI: `src/Cognition.Console/src/pages/AdminLlmDefaultsPage.tsx`, hook `useLlmDefaults`, dropdowns fed by providers/models, link in navigation (admin only).
- Tests: unit for resolver/service; API tests for controller; optional UI/manual checklist.

## Turn Plan (execution steps)
1) Schema & service: add entity + migration + DbContext; service to fetch active default; seed Gemini/Flash.
2) API surface: admin GET/PATCH with validation; map to service.
3) Resolver integration: chat default resolution uses stored default ? agent profile ? heuristic; adjust `useChatProviderModel` to fetch defaults.
4) UI admin page: new admin settings page with provider/model selectors; hook up PATCH/GET; add nav entry (admin-only).
5) Cleanup & tests: remove hardcoded defaults except last-resort; add unit/API tests; manual smoke chat + admin page.

## Example POCO
```csharp
using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.LLM;

public sealed class LlmGlobalDefault : BaseEntity
{
    /// <summary>
    /// The selected model that represents the global default.
    /// Provider is reachable via Model.Provider.
    /// </summary>
    public Guid ModelId { get; set; }
    public Model Model { get; set; } = null!;

    /// <summary>
    /// If false, this row is ignored by the resolver.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Lower values win. Resolver takes the active row with the lowest priority.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Audit: who last changed this default (user id from your identity system).
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }
}

```
