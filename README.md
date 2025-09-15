## Cognition

Agentic LLM orchestration with tools, conversations, and a small console.

### Environment Variables

- OPENAI_API_KEY: API key for OpenAI text and image APIs
- OPENAI_BASE_URL: Optional base URL (default: https://api.openai.com)
- GEMINI_API_KEY or GOOGLE_API_KEY: API key for Google Gemini
- GEMINI_BASE_URL: Optional base URL (default: https://generativelanguage.googleapis.com)
- OLLAMA_BASE_URL: Base URL for Ollama (default: http://localhost:11434)
- ConnectionStrings__Postgres: Postgres connection string (e.g., Host=localhost;Port=5432;Database=cognition;Username=postgres;Password=postgres)
- ASPNETCORE_URLS / ASPNETCORE_HTTPS_PORT: Kestrel configuration
- JWT__Secret: JWT signing secret (required in Production)

### Projects

- src/Cognition.Api: ASP.NET Core API + Swagger + Hangfire dashboard
- src/Cognition.Clients: LLM clients, tools, agent service, DI registration
- src/Cognition.Data.Relational: EF Core models and migrations
- src/Cognition.Console: Vite + React console

### Notes

- Tools are resolved via a registry keyed by fully-qualified type (Namespace.Type[, Assembly]). Plain type names are not allowed.
- Tool execution logs redact secret-like keys (apiKey, token, password, secret).
- Agent service supports conversation mode and will optionally summarize when the message window is dense.

