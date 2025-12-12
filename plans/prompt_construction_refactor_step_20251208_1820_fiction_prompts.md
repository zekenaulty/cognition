# Step Note — prompt_construction_refactor — 2025-12-08 18:20

## Goal
Extend inventory to fiction weaver runners to spot prompt assembly patterns/string interpolation risks.

## Commands Executed
- `rg "Build.*Prompt" src/Cognition.Clients/Tools/Fiction/Weaver -g"*.cs"`
- `Get-Content src/Cognition.Clients/Tools/Fiction/Weaver/WorldBibleManagerRunner.cs -First 260`
- `rg "prompt" src/Cognition.Clients/Tools/Fiction/Weaver | Select-Object -First 50`

## Findings
- WorldBibleManagerRunner has explicit `BuildWorldBiblePrompt` with multi-line interpolated string containing instructions and JSON shape for characters/locations/systems. Risk: manual string formatting, duplicated safety/preamble, no provider/model awareness.
- Other runners (ChapterArchitect, ScrollRefiner, SceneWeaver, VisionPlanner, IterativePlanner) do not build prompts inline; they read `prompt` from `step.Output` (coming from planner/tool pipeline) and log/store it. Prompt construction likely upstream in planner/tool templates, not in runner code.
- FictionPhaseRunnerBase propagates prompt into transcripts; no central builder.

## Decisions
- WorldBibleManager prompt is a candidate to move into the central prompt builder/templates, parameterized by plan name/branch/description/pass summaries.
- Planner/tool prompt generation (that feeds the `prompt` in `step.Output`) will need similar treatment later, but immediate risk is WorldBibleManager’s hand-built prompt.

## Completion
- Status: ○ (inventory extended; no code changes)

## Next Actions
- Sketch builder inputs that cover WorldBibleManager needs (plan name, branch, description, passes summary) and agent chat needs (system/preamble/tools/history).
- Identify where planner/tool prompts are constructed (likely in planner template pipeline) for later porting.
