# Phase 001 – Session 2025-11-16 Action Plan

## Snapshot
- Backlog + resume APIs are live and metadata contracts are enforced end-to-end (ConversationTask.ProviderId/ModelId/TaskId/ConversationPlanId).
- Roster + lore fulfillment dashboards now surface branch-aware provenance in the console, and manual fulfill calls emit telemetry immediately.
- Planner telemetry already lists backlog drift + recent transitions, but console actions/alerts and lore automation are still pending.
- Persona registry/memories/world notes ship in API + console panes; obligation tagging and drift alerts remain open.

## Priority Work Items

1. **Backlog console widgets & actions**
   - Surface `/api/fiction/plans/{id}/backlog` data on Fiction Projects + Planner Telemetry pages (cards, filters, resume controls).
   - Invoke `/resume` with enforced provider/model/task ids collected from ConversationTask metadata; refresh roster/backlog panes after actions.
   - Emit console event logs + alert banners for blocked/on fire backlog items (missing metadata, repeated failures).

2. **Lore fulfillment automation pipeline**
   - Trigger fulfillment tool runs when requirements stay `Blocked` beyond SLA; capture `conversationId/planPassId/source` metadata from assistants.
   - Persist fulfillment timelines + branch lineage history per requirement; expose in API + console detail views.
   - Publish lifecycle telemetry (request → draft → approval) tied to specific backlog items / Hangfire jobs.

3. **Persona obligation tagging + drift monitoring**
   - Extend persona context payload with outstanding obligations, change history, and Ops alerts when memories drift off prompt baselines.
   - Link obligations back to backlog tasks so follow-up work is trackable; surface alerts in console when obligations age past thresholds.
   - Add API to annotate persona obligations + tag source backlog/resume events.

4. **Telemetry & dashboard enrichments**
   - Pipe backlog vs `FictionPhaseProgressed` drift, resume success/timeout counters, and lore SLA metrics into Planner Telemetry visualizations.
   - Add branch-aware filters and alerting (e.g., “branch alt-beta has 4 blocked lore items for >6h”).
   - Ensure console actions log to telemetry streams for audit trails.

5. **Regression coverage & contracts**
   - Add deterministic tests for backlog resume (API → scheduler → Hangfire) and lore fulfillment propagation (branch lineage, metadata contract).
   - Cover persona obligation workflows and backlog metadata serialization in both API + jobs layers.
   - Extend console unit tests (hooks/components) once backlog widgets + lore timelines are implemented.

## Immediate Next Step
- Start with **Backlog console widgets & actions** to unlock resume workflows for admins; wire APIs + UI state first, then expand alerts/telemetry once data is flowing.
