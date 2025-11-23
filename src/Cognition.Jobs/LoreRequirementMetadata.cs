using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cognition.Jobs;

internal sealed class LoreRequirementMetadata
{
    private const string AutoRequestedKey = "autoFulfillmentRequestedUtc";
    private const string AutoCompletedKey = "autoFulfillmentCompletedUtc";
    private const string BranchSlugKey = "branchSlug";
    private const string BranchLineageKey = "branchLineage";
    private const string AutoConversationKey = "autoFulfillmentConversationId";
    private const string AutoAgentKey = "autoFulfillmentAgentId";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly JsonObject _root;

    private LoreRequirementMetadata(JsonObject root)
    {
        _root = root;
        AutoFulfillmentRequestedUtc = ReadDateTime(root, AutoRequestedKey);
        AutoFulfillmentCompletedUtc = ReadDateTime(root, AutoCompletedKey);
        BranchSlug = ReadString(root, BranchSlugKey);
        BranchLineage = ReadStringArray(root, BranchLineageKey);
        AutoFulfillmentConversationId = ReadGuid(root, AutoConversationKey);
        AutoFulfillmentAgentId = ReadGuid(root, AutoAgentKey);
    }

    public DateTime? AutoFulfillmentRequestedUtc { get; set; }
    public DateTime? AutoFulfillmentCompletedUtc { get; set; }
    public string? BranchSlug { get; set; }
    public IReadOnlyList<string>? BranchLineage { get; set; }
    public Guid? AutoFulfillmentConversationId { get; set; }
    public Guid? AutoFulfillmentAgentId { get; set; }

    public static LoreRequirementMetadata FromJson(string? json)
    {
        JsonObject root;
        if (string.IsNullOrWhiteSpace(json))
        {
            root = new JsonObject();
        }
        else
        {
            var node = JsonNode.Parse(json);
            root = node as JsonObject ?? new JsonObject();
        }

        return new LoreRequirementMetadata(root);
    }

    public string Serialize()
    {
        WriteDateTime(_root, AutoRequestedKey, AutoFulfillmentRequestedUtc);
        WriteDateTime(_root, AutoCompletedKey, AutoFulfillmentCompletedUtc);
        WriteString(_root, BranchSlugKey, BranchSlug);
        WriteStringArray(_root, BranchLineageKey, BranchLineage);
        WriteGuid(_root, AutoConversationKey, AutoFulfillmentConversationId);
        WriteGuid(_root, AutoAgentKey, AutoFulfillmentAgentId);
        return _root.ToJsonString(SerializerOptions);
    }

    public LoreBranchContext ResolveBranchContext(string? defaultBranch, string? fallbackBranch = null)
    {
        var normalizedDefault = string.IsNullOrWhiteSpace(defaultBranch) ? "main" : defaultBranch.Trim();
        var normalizedFallback = string.IsNullOrWhiteSpace(fallbackBranch) ? normalizedDefault : fallbackBranch.Trim();
        var slug = string.IsNullOrWhiteSpace(BranchSlug) ? normalizedFallback : BranchSlug!.Trim();
        IReadOnlyList<string>? lineage = BranchLineage;
        if (lineage is null &&
            !string.Equals(slug, normalizedDefault, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(normalizedDefault))
        {
            lineage = new[] { normalizedDefault, slug };
        }

        return new LoreBranchContext(slug, lineage);
    }

    private static DateTime? ReadDateTime(JsonObject obj, string property)
    {
        if (obj.TryGetPropertyValue(property, out var node) &&
            node is JsonValue value &&
            value.TryGetValue<string>(out var raw) &&
            !string.IsNullOrWhiteSpace(raw) &&
            DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static string? ReadString(JsonObject obj, string property)
    {
        if (obj.TryGetPropertyValue(property, out var node) &&
            node is JsonValue value &&
            value.TryGetValue<string>(out var raw) &&
            !string.IsNullOrWhiteSpace(raw))
        {
            return raw;
        }

        return null;
    }

    private static IReadOnlyList<string>? ReadStringArray(JsonObject obj, string property)
    {
        if (!obj.TryGetPropertyValue(property, out var node))
        {
            return null;
        }

        if (node is JsonArray array && array.Count > 0)
        {
            var values = new List<string>();
            foreach (var item in array)
            {
                if (item is JsonValue value && value.TryGetValue<string>(out var raw) && !string.IsNullOrWhiteSpace(raw))
                {
                    values.Add(raw);
                }
            }

            return values.Count == 0 ? null : values;
        }

        return null;
    }

    private static Guid? ReadGuid(JsonObject obj, string property)
    {
        if (obj.TryGetPropertyValue(property, out var node) &&
            node is JsonValue value &&
            value.TryGetValue<string>(out var raw) &&
            Guid.TryParse(raw, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static void WriteDateTime(JsonObject obj, string property, DateTime? value)
    {
        if (value.HasValue)
        {
            obj[property] = value.Value.ToString("O", CultureInfo.InvariantCulture);
        }
        else
        {
            obj.Remove(property);
        }
    }

    private static void WriteString(JsonObject obj, string property, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            obj[property] = value;
        }
        else
        {
            obj.Remove(property);
        }
    }

    private static void WriteStringArray(JsonObject obj, string property, IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            obj.Remove(property);
            return;
        }

        var array = new JsonArray();
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                array.Add(value);
            }
        }

        if (array.Count == 0)
        {
            obj.Remove(property);
        }
        else
        {
            obj[property] = array;
        }
    }

    private static void WriteGuid(JsonObject obj, string property, Guid? value)
    {
        if (value.HasValue && value != Guid.Empty)
        {
            obj[property] = value.Value.ToString("D", CultureInfo.InvariantCulture);
        }
        else
        {
            obj.Remove(property);
        }
    }
}

internal readonly record struct LoreBranchContext(string Slug, IReadOnlyList<string>? Lineage);
