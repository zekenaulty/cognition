# Phase 001 – Session 2025-11-16 Action Plan

## Definition of Done
- Backlog console widgets consume `/backlog` + `/resume`, enforce metadata requirements, and emit telemetry/audit logs; admins can resume items without CLI/SQL.
- Lore fulfillment automation triggers off SLA breaches, records provenance/timelines, and console users can review/approve history.
- Persona obligations are created/tagged/resolved through API + console flows with alerts for aging items, and regression tests cover backlog → scheduler → lore + obligation loops.

## Status (2025-11-18)
- Backlog + obligation UX is live in Fiction Projects and Planner Telemetry, and an end-to-end regression anchors the resume → fulfill → resolve workflow.
- Persona obligations now include inline persona-memory/world notes, resume dialogs apply default provider/model metadata, and Planner Telemetry exposes the same backlog alerts + dialogs so Ops can triage stale backlog, lore, and obligations in one place.
- Remaining work is explicitly user-facing (plan creation CTA, backlog alert cards, lore auto-fulfillment), so keep the stack focused on features end users can touch.

## Snapshot
- Backlog + resume APIs are live and metadata contracts are enforced end-to-end (ConversationTask.ProviderId/ModelId/TaskId/ConversationPlanId).
- Roster + lore fulfillment dashboards now surface branch-aware provenance in the console, and manual fulfill calls emit telemetry immediately.
- Planner telemetry already lists backlog drift + recent transitions, but console actions/alerts and lore automation are still pending.
- Persona registry/memories/world notes ship in API + console panes; obligation tagging and drift alerts remain open.

## Priority Work Items

1. **Author-facing backlog UX**
   - Add console CTA to create/seed a plan (persona picker, default backlog items).
   - Highlight stale backlog items with inline alerts (age, missing lore, open obligations) linking directly to resume/resolve dialogs.
   - Surface resume/action metadata (provider/model/task) as user-facing hints, not just telemetry.

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
