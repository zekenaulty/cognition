# Milestone A Inventory — Prompts, Validators, Outputs

## Definition of Done
- Prompt catalog + schema/validator mapping for both Python prototype and Cognition tools remains accurate to source templates (drift is tracked as TODOs).
- Each prompt has a migration decision (reuse vs retire) recorded, and Cognition tooling now references the documented templates/validators.
- Sample run artifacts (story.json, context files, metrics) are archived/linked so template edits can be regression tested.

## 1. Prompt Catalogue (Python Weaver v2)

| Phase | Function | Purpose / Prompt Summary | Output Expectation |
| --- | --- | --- | --- |
| Vision | `get_or_build_author_summary` | "Compress the following author persona into a concise, actionable style guide" | Free-form summary text saved to `_context/author.summary.txt` |
| Vision | `plan_initial_vision` (`user_goals`) | "Define the soul of a new novel... Goals for book/characters, Surface plot, Deeper meaning" | Markdown written to `_context/book_goals.md` |
| Vision | `plan_initial_vision` (`user_plan`) | "Create a comprehensive STORY PLAN... Conceptual Vision, n-act Plot, Character List" | Markdown written to `_context/story.plan.md` |
| World Bible | `plan_world_grimoires` (`characters`) | STRICT JSON roster + knowledge lists based on story plan | Saves to `_context/characters.roster.json` & `_context/characters.knowledge.json` |
| World Bible | `plan_world_grimoires` (`locations`) | STRICT JSON array of locations w/ name, description, status | `_context/locations.json` |
| World Bible | `plan_world_grimoires` (`systems`) | STRICT JSON array describing systems (name, rules/function, state) | `_context/systems.json` |
| Iterative Planning | `perform_iterative_planning_passes` | Iterative brief template (Story Arc Adjustments, Character Priorities, Location Notes, Systems Considerations) | Markdown notes under `_context/plan_cache/pass_*.md` |
| Blueprint | `plan_chapter_blueprint` | STRICT JSON blueprint for chapter (title, summary, structure array w/ goal/obstacle/turn/fallout) | Stored in `_context/blueprints.json` |
| Scroll Refinement | `refine_chapter_plan_for_writing` | STRICT JSON final chapter: section + scene metadata (goal/obstacle/turn/fallout/carry_forward) | Appended to `story.json` (`chapters` array) |
| Transition Planning | `plan_inter_section_transition` | Brief narrative bridge between sections | In-memory, passed to writer |
| Scene Memory | `update_chapter_memory` | "Summarize the key plot points... 2-3 bullet points" | Saved to `_context/{chapter}.memory.json` |
| Chapter Recap | `get_full_chapter_summary` | "Synthesize scene summaries into a 1-2 paragraph chapter summary" | Used when planning next blueprint |
| Writing | `generate_prose_for_subsection` | Structured scene context (book/chapter/section meta, scene beats, transition plan); instructs to "Write only the prose" | Markdown scene written under chapter directory (e.g., `01-chapter/.../001-scene.md`) |
| Grimoire Reflection | `update_character_grimoire_after_chapter` | STRICT JSON roster refresh + knowledge updates referencing chapter sections | Updates character roster/knowledge/timeline |
| Grimoire Reflection | `update_world_grimoires_after_chapter` | STRICT JSON to mutate locations/systems arrays given chapter payload | Updates world bible jsons |

## 2. Schema Validators & Gates

- `validate_blueprint_structure` — ensures chapter blueprint JSON has required keys (`title`, `summary`, `structure` entries with goal/obstacle/turn/fallout). Raises `SchemaValidationError` on failure.
- `validate_refined_chapter_structure` — enforces final chapter schema (sections + subsections with metadata fields, slug generation, synopsis).
- `simple_attentional_gate` — token overlap check between generated text and salient terms derived from roster/book goals to catch off-topic outputs.
- `collect_salient_terms` — builds term set from roster names + book goals to feed the attentional gate.
- JSON extraction helper inside `llm_call` — regex-based extraction to guard against mixed-format responses.
- Blueprint/refine/writing phases all wrap LLM calls in retry loops with exponential backoff (up to 4 attempts) and log schema failures.

## 3. Retry / Backoff Patterns

| Function | Attempts | Backoff Notes |
| --- | --- | --- |
| `plan_chapter_blueprint` | 4 | First retry 0.6–1.2s, subsequent retries exponential up to 30s |
| `refine_chapter_plan_for_writing` | 4 | Same cadence; retries on schema/json/transport failures |
| `update_world_grimoires_after_chapter` | 4 | Retries strict JSON conversion with exponential backoff |
| Scene writing skips rather than retries if markdown already exists (idempotent write guard) |

## 4. File & Directory Outputs

- Root `story.json` — final scroll + metadata.
- `_context/` directory:
  - `book_goals.md`, `story.plan.md`
  - `blueprints.json`
  - `author.summary.txt` + `author.summary.hash`
  - `characters.roster.json`, `characters.knowledge.json`
  - `locations.json`, `systems.json`, `timeline.json`
  - `{chapter_slug}.memory.json`
  - `plan_cache/pass_*.md`
  - `iterate_state.json` (checkpoint manager state)
  - `phase_lock.json` (phase lock toggles)
  - `metrics.csv` (timestamp, phase, chapter/section/scene IDs, event, details)
- Chapter directories (e.g., `01-chapter-slug/`) containing per-section subfolders and numbered markdown scene files.

## 5. Metrics & Checkpoint Artifacts

- **`metrics.csv` columns:** timestamp, phase, chapter_slug, section_slug, sub_slug, event, details (records skips, writes, etc.).
- **`iterate_state.json`:** phase status (`pending`/`in_progress`/`complete`), progress counters, timestamps.
- **`phase_lock.json`:** boolean locks to prevent regeneration of completed phases.

## 6. Provider-Specific Notes

- Prompts assume OpenAI-style chat completion; however, `make_client` supports OpenAI, Gemini, Ollama.
- No provider-specific tokens except persona/style prompts; Scene writer prompt references persona/style summary that may need truncation for different context windows.

## 7. Follow-ups for Milestone A

- Trace remaining prompts embedded directly in C# Fiction tools (Outliner/SceneDraft/etc.)
- Compare Python prompt structures to existing Cognition prompt templates to identify reuse candidates.
- Capture full list of file outputs for a sample run (snapshot already available one directory up).
## 1b. Prompt Catalogue (Cognition Fiction Tools)

| Tool | Method | Prompt Summary | Output/Usage |
| --- | --- | --- | --- |
| OutlinerTool | `BuildBeatsPrompt` | "Return ONLY minified JSON" with promise/progress/payoff beats for a titled node; optional style hints injected from DB | Beats stored in `OutlineNodeVersion.Beats` JSON and surfaced during scene drafting |
| SceneDraftTool | `BuildDraftPrompt` | System reminder + style/canon/glossary context; instructs: "Write a single narrative scene in raw Markdown… Target 1200-2000 words… Return only the scene body" | Result becomes `DraftSegmentVersion.BodyMarkdown`; metrics logged alongside |
| LoreKeeperTool | `BuildExtractionPrompt` | "Extract glossary terms and canon rules. Return ONLY minified JSON…" | Populates `GlossaryTerms` + `CanonRules` (ingest pipeline) |
| WorldbuilderTool | `BuildWorldAssetPrompt` | "Return ONLY minified JSON object… Design a {type} named '{name}'…" | Seeds `WorldAssetVersion.Content` for locations/characters/systems |
| SceneDraftTool | `GenerateDraftFallbackAsync` | Deterministic stub summarizing beats when LLM unavailable | Used only on failure (no external call) |
| FactCheckerTool / NPCDesignerTool | — | No LLM prompt (pure heuristic/db operations) | n/a |

\n_Provider note_: focus templating on OpenAI/Gemini for Phase-001; Ollama support deferred to narrow, explicit-use plays later.
## Legacy Fiction Refactor Actions
1. Catalogue existing POCOs/tools (OutlineNode, DraftSegment, WorldAsset, LoreKeeper, etc.) to determine usage and gaps.
2. Decide keep vs. supersede: map each entity/tool to new plan graph roles or mark for retirement.
3. For retained components, plan polish tasks (schema alignment, prompt updates, telemetry).
4. Remove unused scaffolding once replacements are in place.

