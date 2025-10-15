using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cognition.Data.Relational.Modules.Fiction;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Cognition.Clients.Tools.Fiction.Weaver;

public static class FictionResponseValidator
{
    private static readonly JSchema VisionSchema = BuildVisionSchema();
    private static readonly JSchema WorldBibleSchema = BuildWorldBibleSchema();
    private static readonly JSchema IterativeSchema = BuildIterativeSchema();
    private static readonly JSchema BlueprintSchema = BuildBlueprintSchema();
    private static readonly JSchema ScrollSchema = BuildScrollSchema();

    public static FictionResponseValidationResult ValidateVisionPayload(string response, FictionPlan plan, FictionPhaseExecutionContext context)
        => ValidateJson(FictionPhase.VisionPlanner, response, VisionSchema, BuildSalientTerms(plan, context));

    public static FictionResponseValidationResult ValidateWorldBiblePayload(string response, FictionPlan plan, FictionPhaseExecutionContext context)
        => ValidateJson(FictionPhase.WorldBibleManager, response, WorldBibleSchema, BuildSalientTerms(plan, context));

    public static FictionResponseValidationResult ValidateIterativePayload(string response, FictionPlan plan, FictionPhaseExecutionContext context)
        => ValidateJson(FictionPhase.IterativePlanner, response, IterativeSchema, BuildSalientTerms(plan, context));

    public static FictionResponseValidationResult ValidateBlueprintPayload(string response, FictionPlan plan, FictionPhaseExecutionContext context)
        => ValidateJson(FictionPhase.ChapterArchitect, response, BlueprintSchema, BuildSalientTerms(plan, context));

    public static FictionResponseValidationResult ValidateScrollPayload(string response, FictionPlan plan, FictionPhaseExecutionContext context)
        => ValidateJson(FictionPhase.ScrollRefiner, response, ScrollSchema, BuildSalientTerms(plan, context));

    public static FictionResponseValidationResult ValidateScenePayload(string response, FictionPlan plan, FictionPhaseExecutionContext context, FictionChapterScene scene)
    {
        var salientTerms = BuildSceneSalientTerms(plan, scene, context);
        var missing = RunAttentionalGate(response, salientTerms);
        if (missing.Count > 0)
        {
            var message = $"Scene attentional gate failed: missing references to {string.Join(", ", missing)}.";
            return new FictionResponseValidationResult(
                FictionPhase.SceneWeaver,
                FictionTranscriptValidationStatus.Failed,
                "Scene attentional gate failed.",
                message,
                missing,
                ParsedPayload: null,
                SalientTerms: salientTerms);
        }

        return new FictionResponseValidationResult(
            FictionPhase.SceneWeaver,
            FictionTranscriptValidationStatus.Passed,
            "Scene attentional gate passed.",
            "Scene response references salient story terms.",
            Array.Empty<string>(),
            ParsedPayload: null,
            SalientTerms: salientTerms);
    }

    private static FictionResponseValidationResult ValidateJson(
        FictionPhase phase,
        string response,
        JSchema schema,
        IReadOnlyList<string> salientTerms)
    {
        JToken token;
        try
        {
            token = JToken.Parse(response);
        }
        catch (Exception ex)
        {
            var message = $"Response for {phase} is not valid JSON: {ex.Message}";
            return new FictionResponseValidationResult(
                phase,
                FictionTranscriptValidationStatus.Failed,
                "JSON parsing failed.",
                message,
                new[] { ex.Message },
                ParsedPayload: null,
                SalientTerms: salientTerms);
        }

        if (!token.IsValid(schema, out IList<ValidationError> errors))
        {
            var details = string.Join("; ", errors.Select(e => e.Message));
            var message = $"Response for {phase} does not match schema: {details}";
            return new FictionResponseValidationResult(
                phase,
                FictionTranscriptValidationStatus.Failed,
                "Schema validation failed.",
                message,
                errors.Select(e => e.Message).ToArray(),
                ParsedPayload: token,
                SalientTerms: salientTerms);
        }

        return new FictionResponseValidationResult(
            phase,
            FictionTranscriptValidationStatus.Passed,
            "Schema validation succeeded.",
            $"Response for {phase} matches expected schema.",
            Array.Empty<string>(),
            ParsedPayload: token,
            SalientTerms: salientTerms);
    }

    private static List<string> RunAttentionalGate(string response, IReadOnlyList<string> salientTerms)
    {
        var missing = new List<string>();
        foreach (var term in salientTerms)
        {
            if (string.IsNullOrWhiteSpace(term)) continue;
            if (!Regex.IsMatch(response, Regex.Escape(term), RegexOptions.IgnoreCase))
            {
                missing.Add(term);
            }
        }

        if (missing.Count == salientTerms.Count)
        {
            return missing.Take(3).ToList();
        }

        return missing;
    }

    private static IReadOnlyList<string> BuildSalientTerms(FictionPlan plan, FictionPhaseExecutionContext context)
    {
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(plan.Name)) terms.Add(plan.Name);
        if (!string.IsNullOrWhiteSpace(plan.Description)) terms.UnionWith(Tokenize(plan.Description));
        if (!string.IsNullOrWhiteSpace(plan.FictionProject?.Title)) terms.Add(plan.FictionProject.Title);
        if (!string.IsNullOrWhiteSpace(plan.FictionProject?.Logline)) terms.UnionWith(Tokenize(plan.FictionProject.Logline));
        if (!string.IsNullOrWhiteSpace(context.BranchSlug)) terms.Add(context.BranchSlug);
        return terms.Where(t => t.Length > 3).Take(25).ToList();
    }

    private static IReadOnlyList<string> BuildSceneSalientTerms(FictionPlan plan, FictionChapterScene scene, FictionPhaseExecutionContext context)
    {
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            scene.Title,
            scene.SceneSlug
        };

        if (!string.IsNullOrWhiteSpace(scene.Description)) terms.UnionWith(Tokenize(scene.Description));

        var section = scene.FictionChapterSection;
        if (section is not null)
        {
            terms.Add(section.Title);
            terms.Add(section.SectionSlug);
            if (!string.IsNullOrWhiteSpace(section.Description)) terms.UnionWith(Tokenize(section.Description));

            var scroll = section.FictionChapterScroll;
            if (scroll is not null)
            {
                terms.Add(scroll.Title);
                terms.Add(scroll.ScrollSlug);
                if (!string.IsNullOrWhiteSpace(scroll.Synopsis)) terms.UnionWith(Tokenize(scroll.Synopsis));

                var blueprint = scroll.FictionChapterBlueprint;
                if (blueprint is not null)
                {
                    terms.Add(blueprint.Title);
                    terms.Add(blueprint.ChapterSlug);
                    if (!string.IsNullOrWhiteSpace(blueprint.Synopsis)) terms.UnionWith(Tokenize(blueprint.Synopsis));
                }
            }
        }

        terms.UnionWith(BuildSalientTerms(plan, context));
        return terms.Where(t => t.Length > 3).Take(30).ToList();
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        var separators = new[] { ' ', '\n', '\r', '\t', ',', '.', ';', ':', '!', '?' };
        foreach (var token in text.Split(separators, StringSplitOptions.RemoveEmptyEntries))
        {
            yield return token.Trim();
        }
    }

    private static JSchema BuildVisionSchema()
    {
        var backlogItemSchema = new JSchema
        {
            Type = JSchemaType.Object,
            AllowAdditionalProperties = false
        };
        backlogItemSchema.Properties.Add("id", new JSchema { Type = JSchemaType.String, MinimumLength = 1 });
        backlogItemSchema.Properties.Add("description", new JSchema { Type = JSchemaType.String, MinimumLength = 5 });
        backlogItemSchema.Properties.Add("status", new JSchema
        {
            Type = JSchemaType.String,
            Enum = { new JValue("pending"), new JValue("in_progress"), new JValue("complete") }
        });
        backlogItemSchema.Properties.Add("inputs", new JSchema { Type = JSchemaType.Array, Items = { new JSchema { Type = JSchemaType.String, MinimumLength = 1 } } });
        backlogItemSchema.Properties.Add("outputs", new JSchema { Type = JSchemaType.Array, Items = { new JSchema { Type = JSchemaType.String, MinimumLength = 1 } } });
        backlogItemSchema.Required.Add("id");
        backlogItemSchema.Required.Add("description");

        var backlogSchema = new JSchema { Type = JSchemaType.Array, MinimumItems = 1 };
        backlogSchema.Items.Add(backlogItemSchema);

        var goalsSchema = new JSchema { Type = JSchemaType.Array, MinimumItems = 1 };
        goalsSchema.Items.Add(new JSchema { Type = JSchemaType.String, MinimumLength = 3 });

        var optionalStringArray = new JSchema { Type = JSchemaType.Array };
        optionalStringArray.Items.Add(new JSchema { Type = JSchemaType.String, MinimumLength = 3 });

        var schema = new JSchema
        {
            Type = JSchemaType.Object,
            AllowAdditionalProperties = false
        };
        schema.Properties.Add("authorSummary", new JSchema { Type = JSchemaType.String, MinimumLength = 10 });
        schema.Properties.Add("bookGoals", goalsSchema);
        schema.Properties.Add("planningBacklog", backlogSchema);
        schema.Properties.Add("openQuestions", optionalStringArray);
        schema.Properties.Add("worldSeeds", optionalStringArray);
        schema.Required.Add("authorSummary");
        schema.Required.Add("bookGoals");
        schema.Required.Add("planningBacklog");
        return schema;
    }

    private static JSchema BuildWorldBibleSchema()
    {
        var entrySchema = new JSchema
        {
            Type = JSchemaType.Object,
            AllowAdditionalProperties = false
        };
        entrySchema.Properties.Add("name", new JSchema { Type = JSchemaType.String, MinimumLength = 1 });
        entrySchema.Properties.Add("summary", new JSchema { Type = JSchemaType.String, MinimumLength = 5 });
        entrySchema.Properties.Add("status", new JSchema { Type = JSchemaType.String, MinimumLength = 3 });
        var continuitySchema = new JSchema { Type = JSchemaType.Array };
        continuitySchema.Items.Add(new JSchema { Type = JSchemaType.String });
        entrySchema.Properties.Add("continuityNotes", continuitySchema);
        entrySchema.Required.Add("name");
        entrySchema.Required.Add("summary");
        entrySchema.Required.Add("status");
        entrySchema.Required.Add("continuityNotes");

        var collection = new JSchema { Type = JSchemaType.Array };
        collection.Items.Add(entrySchema);

        var schema = new JSchema
        {
            Type = JSchemaType.Object,
            AllowAdditionalProperties = false
        };
        schema.Properties.Add("characters", collection);
        schema.Properties.Add("locations", collection);
        schema.Properties.Add("systems", collection);
        schema.Required.Add("characters");
        schema.Required.Add("locations");
        schema.Required.Add("systems");
        return schema;
    }

    private static JSchema BuildIterativeSchema()
    {
        var stringArray = new JSchema { Type = JSchemaType.Array };
        stringArray.Items.Add(new JSchema { Type = JSchemaType.String, MinimumLength = 3 });

        var schema = new JSchema
        {
            Type = JSchemaType.Object,
            AllowAdditionalProperties = false
        };
        schema.Properties.Add("storyAdjustments", stringArray);
        schema.Properties.Add("characterPriorities", stringArray);
        schema.Properties.Add("locationNotes", stringArray);
        schema.Properties.Add("systemsConsiderations", stringArray);
        schema.Properties.Add("risks", stringArray);
        schema.Required.Add("storyAdjustments");
        schema.Required.Add("characterPriorities");
        schema.Required.Add("locationNotes");
        schema.Required.Add("systemsConsiderations");
        schema.Required.Add("risks");
        return schema;
    }

    private static JSchema BuildBlueprintSchema()
    {
        var structureItem = new JSchema
        {
            Type = JSchemaType.Object,
            AllowAdditionalProperties = false
        };
        structureItem.Properties.Add("slug", new JSchema { Type = JSchemaType.String, MinimumLength = 1 });
        structureItem.Properties.Add("summary", new JSchema { Type = JSchemaType.String, MinimumLength = 10 });
        structureItem.Properties.Add("goal", new JSchema { Type = JSchemaType.String, MinimumLength = 5 });
        structureItem.Properties.Add("obstacle", new JSchema { Type = JSchemaType.String, MinimumLength = 5 });
        structureItem.Properties.Add("turn", new JSchema { Type = JSchemaType.String, MinimumLength = 5 });
        structureItem.Properties.Add("fallout", new JSchema { Type = JSchemaType.String, MinimumLength = 5 });
        var carryForwardSchema = new JSchema { Type = JSchemaType.Array };
        carryForwardSchema.Items.Add(new JSchema { Type = JSchemaType.String });
        structureItem.Properties.Add("carryForward", carryForwardSchema);
        structureItem.Required.Add("slug");
        structureItem.Required.Add("summary");
        structureItem.Required.Add("goal");
        structureItem.Required.Add("obstacle");
        structureItem.Required.Add("turn");
        structureItem.Required.Add("fallout");
        structureItem.Required.Add("carryForward");

        var structureArray = new JSchema { Type = JSchemaType.Array, MinimumItems = 1 };
        structureArray.Items.Add(structureItem);

        var schema = new JSchema
        {
            Type = JSchemaType.Object,
            AllowAdditionalProperties = false
        };
        schema.Properties.Add("title", new JSchema { Type = JSchemaType.String, MinimumLength = 1 });
        schema.Properties.Add("synopsis", new JSchema { Type = JSchemaType.String, MinimumLength = 10 });
        schema.Properties.Add("structure", structureArray);
        schema.Required.Add("title");
        schema.Required.Add("synopsis");
        schema.Required.Add("structure");
        return schema;
    }

    private static JSchema BuildScrollSchema()
    {
        var sceneItem = new JSchema
        {
            Type = JSchemaType.Object,
            AllowAdditionalProperties = false
        };
        sceneItem.Properties.Add("sceneSlug", new JSchema { Type = JSchemaType.String, MinimumLength = 1 });
        sceneItem.Properties.Add("title", new JSchema { Type = JSchemaType.String, MinimumLength = 1 });
        sceneItem.Properties.Add("goal", new JSchema { Type = JSchemaType.String, MinimumLength = 5 });
        sceneItem.Properties.Add("conflict", new JSchema { Type = JSchemaType.String, MinimumLength = 5 });
        sceneItem.Properties.Add("turn", new JSchema { Type = JSchemaType.String, MinimumLength = 5 });
        sceneItem.Properties.Add("fallout", new JSchema { Type = JSchemaType.String, MinimumLength = 5 });
        var carryForwardSchema = new JSchema { Type = JSchemaType.Array };
        carryForwardSchema.Items.Add(new JSchema { Type = JSchemaType.String });
        sceneItem.Properties.Add("carryForward", carryForwardSchema);
        sceneItem.Required.Add("sceneSlug");
        sceneItem.Required.Add("title");
        sceneItem.Required.Add("goal");
        sceneItem.Required.Add("conflict");
        sceneItem.Required.Add("turn");
        sceneItem.Required.Add("fallout");
        sceneItem.Required.Add("carryForward");

        var sectionItem = new JSchema
        {
            Type = JSchemaType.Object,
            AllowAdditionalProperties = false
        };
        sectionItem.Properties.Add("sectionSlug", new JSchema { Type = JSchemaType.String, MinimumLength = 1 });
        sectionItem.Properties.Add("title", new JSchema { Type = JSchemaType.String, MinimumLength = 1 });
        sectionItem.Properties.Add("summary", new JSchema { Type = JSchemaType.String, MinimumLength = 10 });
        var transitionsSchema = new JSchema { Type = JSchemaType.Array };
        transitionsSchema.Items.Add(new JSchema { Type = JSchemaType.String });
        sectionItem.Properties.Add("transitions", transitionsSchema);
        var scenesArray = new JSchema { Type = JSchemaType.Array };
        scenesArray.Items.Add(sceneItem);
        sectionItem.Properties.Add("scenes", scenesArray);
        sectionItem.Required.Add("sectionSlug");
        sectionItem.Required.Add("title");
        sectionItem.Required.Add("summary");
        sectionItem.Required.Add("transitions");
        sectionItem.Required.Add("scenes");

        var sectionsArray = new JSchema { Type = JSchemaType.Array, MinimumItems = 1 };
        sectionsArray.Items.Add(sectionItem);

        var schema = new JSchema
        {
            Type = JSchemaType.Object,
            AllowAdditionalProperties = false
        };
        schema.Properties.Add("scrollSlug", new JSchema { Type = JSchemaType.String, MinimumLength = 1 });
        schema.Properties.Add("title", new JSchema { Type = JSchemaType.String, MinimumLength = 1 });
        schema.Properties.Add("synopsis", new JSchema { Type = JSchemaType.String, MinimumLength = 10 });
        schema.Properties.Add("sections", sectionsArray);
        schema.Required.Add("scrollSlug");
        schema.Required.Add("title");
        schema.Required.Add("synopsis");
        schema.Required.Add("sections");
        return schema;
    }
}
