using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cognition.Api.Infrastructure.Obligations;

internal sealed class PersonaObligationMetadata
{
    private const string ResolutionNotesKey = "resolutionNotes";
    private const string ResolvedSourceKey = "resolvedSource";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly JsonObject _root;

    private PersonaObligationMetadata(JsonObject root)
    {
        _root = root;
    }

    public static PersonaObligationMetadata FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new PersonaObligationMetadata(new JsonObject());
        }

        var node = JsonNode.Parse(json);
        return new PersonaObligationMetadata(node as JsonObject ?? new JsonObject());
    }

    public void AddResolutionNote(string note, DateTime timestampUtc, string? actor)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return;
        }

        if (_root[ResolutionNotesKey] is not JsonArray notes)
        {
            notes = new JsonArray();
            _root[ResolutionNotesKey] = notes;
        }

        var entry = new JsonObject
        {
            ["note"] = note,
            ["timestamp"] = timestampUtc.ToString("O", CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(actor))
        {
            entry["actor"] = actor;
        }

        notes.Add(entry);
    }

    public void SetResolvedSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            _root.Remove(ResolvedSourceKey);
            return;
        }

        _root[ResolvedSourceKey] = source;
    }

    public string Serialize()
        => _root.ToJsonString(SerializerOptions);
}
