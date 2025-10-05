# Hangfire Job Sketch (Milestone C)

_Source of truth for fiction Hangfire job bindings; referenced by phase runner step notes._

| Phase Runner | Job Name | Responsibilities | Agent Conversation Strategy | Status |
| --- | --- | --- | --- | --- |
| VisionPlannerJob | `fiction:vision:{planId}` | Generate author summary, book goals, story plan; write to `FictionPlan` + transcripts | Create/use `agent_author` conversation; each LLM call records conversation IDs | âœ… wired via `FictionWeaverJobs.RunVisionPlannerAsync` |
| WorldBibleManagerJob | `fiction:world:{planId}` | Seed/refresh world bible tables; update entries per domain | Same agent conversation; log transcripts per grimoire update | âœ… wired via `FictionWeaverJobs.RunWorldBibleManagerAsync` |
| IterativePlannerJob | `fiction:planner:{planId}:{passIndex}` | Produce iterative planning passes, store in `FictionPlanPass` | Uses same conversation; increments attempt metadata | âœ… wired via `FictionWeaverJobs.RunIterativePlannerAsync` |
| ChapterArchitectJob | `fiction:blueprint:{planId}:{chapterIndex}` | Build chapter blueprint, store / update `FictionChapterBlueprint` | Each blueprint attempt recorded with transcript + validation status | ðŸš§ job entry exists; waiting on chapter blueprint seed data/tests |
| ScrollRefinerJob | `fiction:scroll:{scrollId}` | Convert blueprint to scroll, sections, scenes; update plan graph | LLM calls mapped to transcripts, with validation results | ðŸš§ skeleton implementation ready; requires scroll creation flow/tests |
| SceneWeaverJob | `fiction:scene:{sceneId}` | Generate prose, update `DraftSegmentVersion`, metrics, world bible reflection | Agent conversation per author persona; handles retries via Hangfire requeue | ðŸš§ runner ready; pending prose validation + telemetry gating |

All jobs publish progress events (Enqueued, Started, Progress, Completed, Failed) on the bus for UI/agents.

Jobs emit `FictionPhaseProgressed` events and SignalR `FictionPhaseProgressed` notifications on start/progress/completion, with checkpoints supporting branch cancel/resume (checkpoint status now includes `Cancelled`).




\r\nIFictionWeaverJobClient enqueues these jobs and injects provider/model identifiers into the execution metadata so phase runners can resolve LLM bindings without resorting to defaults.\r\n
