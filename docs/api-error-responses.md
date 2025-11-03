# API Error Responses

The Cognition API now returns structured error payloads for validation, not-found, and conflict conditions. Every controller uses the shared `ApiErrorResponse` helper, producing JSON in the form:

```json
{
  "code": "error_code",
  "message": "Human readable summary",
  "details": { /* optional */ }
}
```

Clients should rely on the `code` field for programmatic handling and surface the `message` field directly to users or logs. The optional `details` property is omitted when no additional context is required.

## Error Codes

The following codes are currently emitted. Controllers may reuse the same code where semantics align.

| Code | Description / Source |
| --- | --- |
| `agent_missing` | Chat remember endpoint requires either `AgentId` or `ConversationId`. |
| `agent_not_found` | Agent lookup failed (chat entry points, agent CRUD). |
| `ask_failed` | Unexpected failure during `/api/chat/ask`. |
| `ask_with_tools_failed` | Unexpected failure during `/api/chat/ask-with-tools`. |
| `ask_chat_v2_failed` | Unexpected failure during `/api/chat/ask-chat-v2`. |
| `chat_failed` | Legacy chat endpoint failure. |
| `client_profile_in_use` | Client profile assigned to tools/agents and cannot be disabled or deleted without `force=true`. |
| `client_profile_not_found` | Client profile lookup failure. |
| `conversation_message_not_found` | Conversation message missing for version operations. |
| `conversation_not_found` | Conversation does not exist or is inaccessible. |
| `conversation_or_agent_not_found` | Conversation and/or backing agent missing. |
| `credential_not_found` | API credential lookup failure. |
| `default_assistant_not_found` | No default assistant persona available. |
| `default_profile_unavailable` | Default profile required for reassignment is missing. |
| `email_conflict` | Email address already assigned to another user. |
| `embedding_pipeline_disabled` | OpenSearch embedding pipeline is disabled. |
| `fiction_plan_not_found` | Planner-bound fiction plan record missing. |
| `image_style_conflict` | Duplicate image style name. |
| `invalid_tool_class_path` | Provided class path does not map to a known `ITool`. |
| `model_not_found` | Model lookup failure (client profiles/tools). |
| `persona_access_link_not_found` | Persona access link target missing. |
| `persona_link_not_found` | User-persona link missing during unlink requests. |
| `persona_not_found` | Persona lookup failure. |
| `persona_type_invalid` | Persona type change violates constraints (e.g., cannot set to `User`). |
| `persona_type_locked` | Existing user persona cannot change type. |
| `primary_persona_not_owned` | Attempt to set a primary persona not owned by the user. |
| `primary_persona_type_invalid` | Primary persona must be a user persona. |
| `provider_not_found` | Provider lookup failure. |
| `tool_execution_failed` | Dispatcher failed to execute requested tool. |
| `tool_not_found` | Tool lookup failure. |
| `user_not_found` | User lookup failure. |
| `user_persona_mismatch` | User-authored message must be sent from the caller's primary persona. |
| `username_conflict` | Username already registered. |
| `version_not_found` | Conversation message version index missing. |

## Client Guidance

- HTTP clients should parse response bodies when requests fail (`status >= 400`). The console application now surfaces the `message` field from these payloads and records the raw JSON for diagnostics.
- External consumers should map known `code` values for contextual messaging or retries. Unknown codes should still be handled generically by displaying the provided `message`.
- When submitting new endpoints, prefer extending `ApiErrorResponse.Create(...)` so the documentation and client behavior remain consistent.
