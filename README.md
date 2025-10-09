## Cognition

Agentic LLM orchestration with tools, conversations, planning, a background jobs worker, and a small web console.

This repository is a .NET 9 solution that wires together:
- An ASP.NET Core API with JWT auth, Swagger/OpenAPI, Hangfire Dashboard, and SignalR hub
- A background worker using Hangfire + Rebus (PostgreSQL transport) for event-driven workflows
- A clients library with LLM providers (OpenAI, Gemini, Ollama), image generation, tools, and an agent service
- An EF Core relational data model with migrations for all domains
- A Vite + React console (built into the API’s `wwwroot`)

The code is organized to be pragmatic, composable, and easy to extend for new providers, prompts, tools, and UI features.

---

### Project Structure

- `src/Cognition.Api` — ASP.NET Core API, Swagger, JWT auth, SignalR hub, Hangfire Dashboard
  - Entry point and hosting: `src/Cognition.Api/Program.cs`
  - Controllers for chat, personas, conversations, tools, LLM, images, users, etc.
  - API v2 (agent-centric):
    - POST `/api/chat/ask-v2` (AgentId)
    - POST `/api/chat/ask-with-tools-v2` (AgentId)
    - POST `/api/chat/ask-chat-v2` (ConversationId only)
    - POST `/api/conversations/v2` (create bound to AgentId)
    - v1 personaId endpoints are deprecated and return `Deprecation`/`Sunset` headers with `Link: rel=alternate` to v2
  - Infrastructure: JWT helpers, password hashing, Swagger filters, Hangfire wiring
- `src/Cognition.Jobs` — Background worker (Hangfire + Rebus)
  - Event handlers for planning and tool execution, SignalR notifier
  - Recurring job registration and example jobs (text, images)
- `src/Cognition.Clients` — SDK-style library
  - LLM clients: OpenAI, Gemini, Ollama
  - Images: OpenAI image generation client and persistence service
  - Tools: registry, dispatcher, example tools
  - Agents: agent service (chat, ask-with-tools, plan-and-execute)
- `src/Cognition.Data.Relational` — EF Core DbContext, entity modules, and migrations
  - Modules for LLM, personas, conversations, tools, prompts, images, users, feature flags, knowledge, config
- `src/Cognition.Console` — Vite + React console
  - Dev server proxies to the API; `vite build` outputs to `src/Cognition.Api/wwwroot`
- `src/Cognition.Contracts` — Cross-process contracts for Rebus message events

See the solution file `Cognition.sln` for project references and build order.

---

### Key Features

- Multi-provider LLM support (OpenAI, Gemini, Ollama) with a factory for model/profile-based selection
- Tools framework with database-defined tools, validated parameters, and execution logging
- Agent service that supports:
  - Simple ask (single-shot)
  - Ask with tools (tool index + strict JSON tool call parsing)
  - Conversation chat with history window, optional summaries, and instruction sets
  - CoT v2-style plan-and-execute loop persisted as `ConversationPlan` + `ConversationTask`
- Event-driven workflows via Rebus + Hangfire (e.g., UserMessageAppended → PlanRequested → ToolExecutionRequested → ToolExecutionCompleted)
- Image generation and asset persistence (OpenAI DALLE/GPT-Image with graceful fallback)
- WebSocket-style updates via SignalR hub for assistant messages and token deltas

---

### Architecture Overview

- API (`src/Cognition.Api/Program.cs`)
  - Configures controllers, SignalR (`/hub/chat`), Swagger (`/swagger`), CORS (dev), and static SPA hosting
  - Wires EF Core Postgres (`CognitionDbContext`), clients, tools, and agent service DI
  - JWT auth with development fallback secret. In Production, `JWT__Secret` must be set; the dev fallback is blocked
  - Hangfire Dashboard exposed at `/hangfire` (no auth in dev; local-only restriction in prod)
  - Applies migrations and invokes the startup seeder (ensures default data)

- Data (`src/Cognition.Data.Relational`)
  - DbContext exposes modules for LLM (providers/models/profiles/credentials), Personas (and links), Conversations (messages, versions, plans/tasks, summaries, workflow state), Tools (definitions/parameters/provider support/logs), Prompts, Images, Users (auth + roles + refresh tokens), Feature flags, Knowledge, Config
  - JSONB columns where appropriate (e.g., tool logs, workflow blackboard)

- Clients (`src/Cognition.Clients`)
  - `ILLMClient` abstraction and concrete clients for OpenAI, Gemini, Ollama
  - `LLMClientFactory` selects per provider/model/profile and resolves API keys from environment
  - `IImageClient` (OpenAI) + `IImageService` for generating and persisting images
  - Tools: `IToolRegistry` (safe type lookup by fully-qualified ClassPath) and `IToolDispatcher` (arg validation, redaction, logging)
  - Agents: `IAgentService` implements ask, ask-with-tools, chat, and staged plan-and-execute

- Jobs (`src/Cognition.Jobs`)
  - Hangfire server with PostgreSQL storage and Rebus configured against the same Postgres
  - Event handlers chain plan and tool execution; SignalR notifier pushes assistant events to the API hub
  - Example jobs for text chat and image generation

- Console (`src/Cognition.Console`)
  - React app that connects to the API, subscribes to the SignalR hub, and provides a chat and image lab experience
  - Dev proxy to API and build emits assets into `src/Cognition.Api/wwwroot`

---

### Data Model Highlights

- Conversations: `Conversation`, `ConversationMessage` (+ versioning), `ConversationPlan`, `ConversationTask`, `ConversationSummary`, `ConversationWorkflowState`
- Personas: system/user-owned, role-play metadata, user links (`UserPersonas`) and primary persona selection
- Tools: catalog (`Tool`) + `ToolParameter` schema with directions and defaults, provider/model support matrix, and `ToolExecutionLog`
- LLM: providers, models (context/window, costs, capabilities), client profiles, and API credentials
- Images: `ImageAsset` (binary + metadata) and `ImageStyle`
- Users: JWT auth, password hashing (PBKDF2), roles, refresh token rotation with hashed storage

---

### Environment Variables

LLM providers
- `OPENAI_API_KEY` (or `OPENAI_KEY`) — API key for OpenAI
- `OPENAI_BASE_URL` — optional base URL (default: https://api.openai.com)
- `GEMINI_API_KEY` or `GOOGLE_API_KEY` — API key for Google Gemini
- `GEMINI_BASE_URL` — optional base URL (default: https://generativelanguage.googleapis.com)
- `OLLAMA_BASE_URL` — base URL for Ollama (default: http://localhost:11434)

Database and hosting
- `ConnectionStrings__Postgres` — Postgres connection string (e.g., `Host=localhost;Port=5432;Database=cognition;Username=postgres;Password=postgres`)
- `ASPNETCORE_URLS` / `ASPNETCORE_HTTPS_PORT` — optional Kestrel configuration

Auth
- `JWT__Secret` — JWT signing secret. Required in Production. A development fallback is used only when not in Production

Other
- `GITHUB_TOKEN` — optional, surfaced in env status for downstream tooling

---

### Local Development

Prerequisites
- .NET SDK 9.x
- Node.js 18+ (for the console)
- Docker (optional, for Postgres)

Start Postgres (optional if you already have one)
```sh
cp .env.example .env  # optional
docker compose up -d postgres
```

Restore and build the solution
```sh
dotnet restore
dotnet build
```

Run the API (applies migrations and seeds defaults)
```sh
dotnet run --project src/Cognition.Api
```
The API serves Swagger at `/swagger`, SignalR hub at `/hub/chat`, the Hangfire dashboard at `/hangfire`, and (after a console build) the SPA from `/`.

Run the background worker
```sh
dotnet run --project src/Cognition.Jobs
```

Run the console (dev)
```sh
cd src/Cognition.Console
npm ci
npm run dev
```
The Vite dev server proxies API calls. Set `VITE_API_BASE_URL` if your API URL differs.

Build the console (emits to API `wwwroot`)
```sh
cd src/Cognition.Console
npm run build
```

---

### Seeding and Defaults

On first run, the API applies EF Core migrations and invokes a startup seeder:
- Ensures a default system assistant persona exists
- Ensures sane defaults for user primary personas and image styles
- Optionally imports additional personas from `reference/ai.console/personal.agents` if present

You can log in or register via the `UsersController` endpoints. Tokens are issued as JWTs with refresh token rotation and hashed storage.

---

### Agents and Tools

Agent service (`src/Cognition.Clients/Agents/AgentService.cs`) supports:
- `AskAsync` — single-prompt generation with an optional persona system preamble
- `AskWithToolsAsync` — prompts the model with an inline "Tool Index" and expects a strict JSON `{ toolId, args }` selection; parses and dispatches the tool
- `ChatAsync` — conversation with instruction sets + system persona, rolling history window, persisted user/assistant messages and optional summary
- `AskWithPlanAsync` — iterates a plan-and-execute loop and persists each `ConversationTask` with tool, args, observation, status, and final answer

Tools
- Defined in DB (`Tool`, `ToolParameter`), resolved by fully-qualified `ClassPath` only (e.g., `Cognition.Clients.Tools.TextTransformTool, Cognition.Clients`)
- `IToolRegistry` exposes a safe name→type map; plain type names are intentionally not supported
- `IToolDispatcher` validates inputs against parameter schema, performs light type coercion, redacts sensitive values in logs, and writes `ToolExecutionLog`
- Example tools: TextTransform, MemoryWrite (persists a summary), KnowledgeQuery (stub)

Provider support
- `ToolProviderSupport` lets you express support level (Full/Partial/Unsupported) for a tool at provider/model granularity

---

### Images

`IImageClient` (OpenAI) and `IImageService` generate and persist images as `ImageAsset` rows with binary data, hash, and metadata.
- The OpenAI client supports DALLE-3 and `gpt-image-1` with a graceful fallback if org verification is required
- API endpoints let you generate images, stream them by id, or list by conversation/persona

---

### Events and Background Jobs

Rebus contracts (`src/Cognition.Contracts/Events.cs`) define the event flow.
- Example flow: `UserMessageAppended` → `PlanRequested` → `PlanReady` → `ToolExecutionRequested` → `ToolExecutionCompleted` → `AssistantMessageAppended`
- Job handlers (`src/Cognition.Jobs/*Handler.cs`) persist workflow events and forward assistant updates to the SignalR hub
- Hangfire server is configured with PostgreSQL storage; dashboard is reachable via the API

---

---

### Testing & Coverage

- Shared test infrastructure lives under `tests/Directory.Build.props` and the `tests/Cognition.Testing` helper library (DbContext factories, HTTP client stubs, environment guards).
- Run unit suites with `dotnet test` from the repo root; individual projects can be targeted via `dotnet test tests/<Project>/<Project>.csproj`.
- Coverage runs use `coverlet.runsettings`, which excludes EF Core `.Designer.cs` scaffolding. The setting is wired up in `.vscode/settings.json` and as `RunSettingsFilePath` inside each test project, so `dotnet test --collect:"XPlat Code Coverage"` and VS Code's "Run Tests with Coverage" share the same exclusions.
- New fixtures default to the EF InMemory provider for speed; leverage `TestDbContextFactory.CreateSqliteInMemory*` if you need relational behaviors.
### Migrations

The API applies migrations automatically on startup. To manage migrations manually:
```sh
# Install or restore the EF tool
dotnet tool restore

# Add a migration (run from repo root)
dotnet ef migrations add MyChange \
  --project src/Cognition.Data.Relational \
  --startup-project src/Cognition.Api

# Apply migrations
dotnet ef database update \
  --project src/Cognition.Data.Relational \
  --startup-project src/Cognition.Api
```

---

### Security Notes

- Set `JWT__Secret` in Production; the dev fallback secret is rejected in Production
- The Hangfire dashboard is unrestricted in Development and local-only in Production
- Tool execution logs redact common secret-shaped values (apiKey, token, password, secret, authorization)

---

### Troubleshooting

- No LLM output: verify provider API keys and base URLs via `/api/system/env/status`
- 401/403 on endpoints: ensure you’re sending `Authorization: Bearer <access token>` or use `[AllowAnonymous]` endpoints where intended
- Console can’t reach API: set `VITE_API_BASE_URL` in the console environment or align ports (`vite.config.ts` proxies to the API)
- Postgres connection issues: confirm `ConnectionStrings__Postgres` and that Docker container is healthy (`docker ps`, `docker logs cognition-postgres`)

---

### Directory Layout (selected)

```
src/
  Cognition.Api/
  Cognition.Jobs/
  Cognition.Clients/
  Cognition.Data.Relational/
  Cognition.Console/
  Cognition.Contracts/
schemas/
docker-compose.yml
README.md
```

For a full tree, run the snapshot helper `arc.py` (optional) to generate a Markdown directory listing that honors `.gitignore`.


