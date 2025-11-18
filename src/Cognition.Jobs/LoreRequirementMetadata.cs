using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cognition.Jobs;

internal sealed class LoreRequirementMetadata
{
    private const string AutoRequestedKey = "autoFulfillmentRequestedUtc";
    private const string AutoCompletedKey = "autoFulfillmentCompletedUtc";

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
    }

    public DateTime? AutoFulfillmentRequestedUtc { get; set; }
    public DateTime? AutoFulfillmentCompletedUtc { get; set; }

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
        return _root.ToJsonString(SerializerOptions);
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
}
