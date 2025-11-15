using System.Collections.ObjectModel;
using Cognition.Clients.Tools.Fiction.Authoring;

namespace Cognition.Clients.Tools.Planning.Fiction;

internal sealed record AuthorPersonaPromptContext(
    string? PersonaName,
    string? Summary,
    IReadOnlyList<string> Memories,
    IReadOnlyList<string> WorldNotes)
{
    public static AuthorPersonaPromptContext Empty { get; } =
        new(null, null, Array.Empty<string>(), Array.Empty<string>());

    public static AuthorPersonaPromptContext FromConversationState(IReadOnlyDictionary<string, object?>? state)
    {
        if (state is null || state.Count == 0)
        {
            return Empty;
        }

        return new AuthorPersonaPromptContext(
            TryGetString(state, "authorPersonaName"),
            TryGetString(state, "authorPersonaSummary"),
            ExtractList(state, "authorPersonaMemories"),
            ExtractList(state, "authorWorldNotes"));
    }

    public static void ApplyToConversationState(IDictionary<string, object?> state, AuthorPersonaContext? context)
    {
        if (state is null || context is null)
        {
            return;
        }

        state["authorPersonaName"] = context.PersonaName;
        state["authorPersonaSummary"] = context.Summary;
        state["authorPersonaMemories"] = context.Memories;
        state["authorWorldNotes"] = context.WorldNotes;
    }

    public string SummaryText => string.IsNullOrWhiteSpace(Summary)
        ? "(no author persona summary provided)"
        : Summary!;

    public string MemoriesText => FormatLines(Memories, "No recent author persona memories recorded.");
    public string WorldNotesText => FormatLines(WorldNotes, "No recent world-bible updates were captured.");

    private static string FormatLines(IReadOnlyList<string> values, string fallback)
    {
        if (values is null || values.Count == 0)
        {
            return fallback;
        }

        var builder = new System.Text.StringBuilder();
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            builder.AppendLine($"- {value.Trim()}");
        }

        return builder.Length == 0 ? fallback : builder.ToString().TrimEnd();
    }

    private static string? TryGetString(IReadOnlyDictionary<string, object?> state, string key)
    {
        if (!state.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            string s => s,
            _ => value.ToString()
        };
    }

    private static IReadOnlyList<string> ExtractList(IReadOnlyDictionary<string, object?> state, string key)
    {
        if (!state.TryGetValue(key, out var value) || value is null)
        {
            return Array.Empty<string>();
        }

        if (value is IReadOnlyList<string> readOnly)
        {
            return readOnly;
        }

        if (value is string[] array)
        {
            return array;
        }

        if (value is IEnumerable<string> enumerable)
        {
            return new ReadOnlyCollection<string>(enumerable.ToList());
        }

        if (value is IEnumerable<object?> objects)
        {
            var list = objects
                .Select(item => item?.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .ToList();
            return new ReadOnlyCollection<string>(list);
        }

        var single = value.ToString();
        return string.IsNullOrWhiteSpace(single)
            ? Array.Empty<string>()
            : new[] { single };
    }
}
