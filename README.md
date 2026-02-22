## Cognition
(in reallity, this solution, is just a learning, testing playground, that I used to actively work though concepts/ideas)

Agent-first LLM orchestration: ASP.NET Core API, Hangfire/Rebus jobs, EF Core Postgres, and a Vite/React console. Supports chat, tools, planners, fiction workflows, and ops telemetry with scoped identities and configurable defaults.

---

### What's Inside
- **API** (`src/Cognition.Api`) - JWT auth, Swagger, SignalR hub (`/hub/chat`), Hangfire dashboard, planners/ops diagnostics, personas/agents, chat/conversations, tools, LLM defaults, images.
- **Jobs** (`src/Cognition.Jobs`) - Hangfire + Rebus workers for planner/backlog and tool execution; pushes hub events.
- **Clients** (`src/Cognition.Clients`) - LLM clients (OpenAI, Gemini, Ollama), tool registry/dispatcher, agent service, image client/service, scope utilities.
- **Data** (`src/Cognition.Data.Relational`) - EF Core DbContext, modules for LLM/providers/models, personas/agents, conversations/messages/plans/tasks, tools, prompts, images, feature flags.
- **Domains** (`src/Cognition.Domains`) - DoD core models and value objects.
- **Domains Relational** (`src/Cognition.Domains.Relational`) - DoD EF Core DbContext + migrations.
- **Domains Documents** (`src/Cognition.Domains.Documents`) - RavenDB document storage for manifests/assets.
- **Workflows** (`src/Cognition.Workflows`) - workflow graph core models.
- **Workflows Relational** (`src/Cognition.Workflows.Relational`) - workflow EF Core persistence + migrations.
- **Console** (`src/Cognition.Console.Dod`) - DoD-first Vite/React SPA built into `src/Cognition.Api/wwwroot`.
- **Contracts** (`src/Cognition.Contracts`) - Rebus events for plan/tool/message flows.

---

### Current Defaults & Behavior
- **Agent-first chat/conversations**: Conversation creation binds to `AgentId`; persona implied from agent. Provider/model defaults resolve in order: conversation settings -> agent profile -> global defaults (admin-set via `/api/llm/defaults`) -> heuristic (Gemini Flash) as last resort.
- **LLM visibility**: Public tool/provider/model surfaces hide inactive/deprecated items. Admin surfaces manage full catalog.
- **Planner telemetry**: Admin-only diagnostics at `/api/diagnostics/planner`; alerts feed Ops webhook if configured.
- **Scope identity**: `ScopePathBuilder`/`ScopeToken` must be used for planner/tool/search contexts; analyzer bans ad-hoc scope construction.
- **Rate limiting**: Fixed-window quotas configurable per user/persona/agent; rate-limit partition prefers agent -> conversation -> persona/user.

---

### Getting Started
Prereqs: .NET 9 SDK, Node 18+, Postgres (Docker optional).

```sh
dotnet restore
dotnet build          # builds API + console (Vite into wwwroot)
dotnet run --project src/Cognition.Api          # applies migrations + seeds defaults
dotnet run --project src/Cognition.Jobs         # background worker

# Console (dev server)
cd src/Cognition.Console.Dod
npm ci
npm run dev
```

API surfaces: Swagger `/swagger`, Hangfire `/hangfire`, SignalR hub `/hub/chat`, SPA `/`.

---

### Configuration (env/appsettings)
- `ConnectionStrings__Postgres` - Postgres connection (legacy data).
- `ConnectionStrings__DomainsPostgres` - DoD Postgres connection.
- `ConnectionStrings__WorkflowsPostgres` - Workflows Postgres connection.
- `RavenDb__Urls__0` / `RavenDb__Database` - RavenDB document store config.
- `JWT__Secret` - required in Production (dev fallback blocked in prod).
- LLM keys: `OPENAI_KEY`/`OPENAI_API_KEY`, `GEMINI_API_KEY`/`GOOGLE_API_KEY`, `OLLAMA_BASE_URL` (optional base overrides).
- `ApiRateLimiting` - permit/window/queue per user/persona/agent; `MaxRequestBodyBytes`.
- `OpsAlerting` - webhook, routing keys, debounce, severity filters (used by planner alerts).
- `PlannerCritique`/`PlannerQuotas` - iteration/token/queue budgets per planner/persona.
- `OpenSearch` - vector model/index/pipeline config for embeddings.

---

### Key Domains
**Chat & Conversations**
- Controllers enforce agent-first defaults, validated provider/model, message versioning, and access filters.
- Conversation services: settings resolver, factory, access filters, message/version service.
- SignalR events stream deltas and final messages; console hydrates from conversation metadata.

**Personas/Agents**
- Personas owned by system or users; links via `UserPersonas`. Access/visibility/ownership now via `PersonaAccessService`.
- Agents link to personas; agent profiles can supply provider/model defaults.

**Tools**
- Admin tool surfaces manage definitions, parameters, provider support; public tools list only active items.
- Execution controller requires agent + scope token context; sandbox hook planned, scope enforcement in place.

**Planners & Fiction**
- Planner health service checks templates, backlog freshness, failures, quotas; alerts surfaced to ops.
- Fiction backlog/resume/obligation flows use planner telemetry and world-bible provenance.

**LLM Defaults**
- `LlmDefaultsController` exposes admin PATCH and anon GET for global defaults. Conversation resolver consumes these first.

---

### Security & Access
- Policies: `AdministratorOnly`, `UserOrHigher`, `ViewerOrHigher`, `AuthenticatedUser`. Planner diagnostics and ops routes are admin-only.
- Rate limits and request cancellation tokens propagate through EF/tool dispatch.
- Tool registry resolves by fully-qualified ClassPath only; dispatcher redacts secret-shaped values in logs.
- Hangfire dashboard unrestricted in Dev, local-only in Prod unless secured upstream.

---

### Testing
- `dotnet test` (uses shared settings/coverage; EF InMemory default).
- Data annotation tests cover DTO validation (users/tools/personas/etc).
- New persona access service tests use EF InMemory to assert owner vs non-owner behavior.

### Verification (dev)
- `docker compose up -d postgres dod-postgres workflows-postgres ravendb`
- `dotnet run --project src/Cognition.Api` (applies migrations + seeds defaults)
- `dotnet run --project src/Cognition.Jobs`

### Rollback (dev)
- Remove DoD/Workflows DI wiring in `src/Cognition.Api/Program.cs` and `src/Cognition.Jobs/Program.cs`.
- Drop the dev data volumes: `cognition-dod-pgdata`, `cognition-workflows-pgdata`, `cognition-ravendbdata`.

---

### Plans & Workstreams (see `plans/`)
- Architecture refactors: chat/conversations/tools controllers, planner diagnostics, scope telemetry.
- UI refactors: planner telemetry page, fiction projects/personas pages.
- LLM defaults plan: data-backed defaults + admin UI (implemented).
- Persona-to-agent refactor: agents primary, personas implied (validated).

Keep new code small: controllers delegate to services; React pages split into hooks + presentational components. Use `ScopePathBuilder` for identity, prefer agent-first defaults, and keep admin/public surfaces clearly separated.
