using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cognition.Api.Infrastructure.Obligations;

internal sealed class PersonaObligationMetadata
{
    private const string ResolutionNotesKey = "resolutionNotes";
    private const string ResolvedSourceKey = "resolvedSource";
    private const string ResolvedBacklogKey = "resolvedBacklogId";
    private const string ResolvedTaskKey = "resolvedTaskId";
    private const string ResolvedConversationKey = "resolvedConversationId";
    private const string VoiceDriftKey = "voiceDrift";

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

    public void SetResolutionContext(string? backlogId, string? taskId, string? conversationId)
    {
        WriteString(ResolvedBacklogKey, backlogId);
        WriteString(ResolvedTaskKey, taskId);
        WriteString(ResolvedConversationKey, conversationId);
    }

    public void SetVoiceDriftFlag(bool? drifted)
    {
        if (drifted is null)
        {
            _root.Remove(VoiceDriftKey);
            return;
        }

        _root[VoiceDriftKey] = drifted.Value;
    }

    public string Serialize()
        => _root.ToJsonString(SerializerOptions);

    private void WriteString(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _root.Remove(key);
            return;
        }

        _root[key] = value;
    }
}
