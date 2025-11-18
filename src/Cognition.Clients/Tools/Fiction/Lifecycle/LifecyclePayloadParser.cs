using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Cognition.Data.Relational.Modules.Fiction;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cognition.Clients.Tools.Fiction.Lifecycle;

internal static class LifecyclePayloadParser
{
    public static JToken? TryConvertToToken(object? artifact)
    {
        return artifact switch
        {
            null => null,
            JToken token => token,
            JsonElement element => ParseJson(element.GetRawText()),
            string raw when !string.IsNullOrWhiteSpace(raw) => ParseJson(raw),
            _ => null
        };
    }

    public static IReadOnlyList<CharacterLifecycleDescriptor> ExtractCharacters(JToken? root)
    {
        var results = new List<CharacterLifecycleDescriptor>();
        if (root is null)
        {
            return results;
        }

        CollectCharacters(root["characters"], results);
        CollectCharacters(root["coreCast"], results);
        CollectCharacters(root["supportingCast"], results);
        CollectCharacters(root["cast"], results);

        return results;
    }

    public static IReadOnlyList<LoreRequirementDescriptor> ExtractLoreRequirements(
        JToken? root,
        Guid? defaultScrollId = null,
        Guid? defaultSceneId = null)
    {
        var results = new List<LoreRequirementDescriptor>();
        if (root is null)
        {
            return results;
        }

        CollectLore(root["loreRequirements"], defaultScrollId, defaultSceneId, results);
        CollectLore(root["loreNeeds"], defaultScrollId, defaultSceneId, results);
        CollectLore(root["requirements"], defaultScrollId, defaultSceneId, results);
        CollectLore(root["prerequisites"], defaultScrollId, defaultSceneId, results);

        return results;
    }

    private static void CollectCharacters(JToken? token, ICollection<CharacterLifecycleDescriptor> results)
    {
        if (token is not JArray array)
        {
            return;
        }

        foreach (var obj in array.OfType<JObject>())
        {
            var name = obj.Value<string>("name") ?? obj.Value<string>("title");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var track = obj.Value<bool?>("track") ?? false;
            if (!track)
            {
                continue;
            }

            var descriptor = new CharacterLifecycleDescriptor(
                Name: name!,
                Track: true,
                Slug: NormalizeSlug(obj.Value<string>("slug") ?? obj.Value<string>("id") ?? name!),
                PersonaId: TryReadGuid(obj, "personaId"),
                AgentId: TryReadGuid(obj, "agentId"),
                WorldBibleEntryId: TryReadGuid(obj, "worldBibleEntryId"),
                FirstSceneId: TryReadGuid(obj, "firstSceneId"),
                CreatedByPlanPassId: TryReadGuid(obj, "planPassId"),
                Role: obj.Value<string>("role"),
                Importance: obj.Value<string>("importance") ?? obj.Value<string>("tier"),
                Summary: obj.Value<string>("summary") ?? obj.Value<string>("bio"),
                Notes: obj.Value<string>("notes") ?? obj.Value<string>("continuity"),
                Metadata: BuildMetadata(obj),
                ContinuityHooks: ExtractContinuityHooks(obj));

            results.Add(descriptor);
        }
    }

    private static void CollectLore(
        JToken? token,
        Guid? defaultScrollId,
        Guid? defaultSceneId,
        ICollection<LoreRequirementDescriptor> results)
    {
        if (token is not JArray array)
        {
            return;
        }

        foreach (var obj in array.OfType<JObject>())
        {
            var title = obj.Value<string>("title") ?? obj.Value<string>("name");
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var statusString = obj.Value<string>("status");
            var status = Enum.TryParse<FictionLoreRequirementStatus>(statusString, true, out var parsedStatus)
                ? parsedStatus
                : FictionLoreRequirementStatus.Planned;

            var descriptor = new LoreRequirementDescriptor(
                Title: title!,
                RequirementSlug: NormalizeSlug(obj.Value<string>("requirementSlug") ?? obj.Value<string>("slug") ?? obj.Value<string>("id") ?? title!),
                Status: status,
                ChapterScrollId: TryReadGuid(obj, "chapterScrollId") ?? defaultScrollId,
                ChapterSceneId: TryReadGuid(obj, "chapterSceneId") ?? defaultSceneId,
                WorldBibleEntryId: TryReadGuid(obj, "worldBibleEntryId"),
                CreatedByPlanPassId: TryReadGuid(obj, "planPassId"),
                Description: obj.Value<string>("description"),
                Notes: obj.Value<string>("notes"),
                Metadata: BuildMetadata(obj));

            results.Add(descriptor);
        }
    }

    private static Guid? TryReadGuid(JObject obj, string property)
        => Guid.TryParse(obj.Value<string>(property), out var parsed) ? parsed : null;

    private static string NormalizeSlug(string value)
    {
        var lower = value.ToLowerInvariant();
        var cleaned = new string(lower.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray());
        while (cleaned.Contains("--", StringComparison.Ordinal))
        {
            cleaned = cleaned.Replace("--", "-", StringComparison.Ordinal);
        }

        cleaned = cleaned.Trim('-');
        return string.IsNullOrWhiteSpace(cleaned) ? $"item-{Guid.NewGuid():N}" : cleaned;
    }

    private static IReadOnlyDictionary<string, object?> BuildMetadata(JObject obj)
        => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["raw"] = obj.ToString(Formatting.None)
        };

    private static IReadOnlyList<string>? ExtractContinuityHooks(JObject obj)
    {
        if (!obj.TryGetValue("continuityHooks", out var token) || token is not JArray hooksArray)
        {
            return null;
        }

        var hooks = hooksArray
            .OfType<JToken>()
            .Select(t => t?.ToString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return hooks.Length == 0 ? null : hooks;
    }

    private static JToken? ParseJson(string json)
    {
        try
        {
            return JToken.Parse(json);
        }
        catch (Newtonsoft.Json.JsonException)
        {
            return null;
        }
    }
}
