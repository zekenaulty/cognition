using Cognition.Clients.LLM;

namespace Cognition.Testing.LLM;

public sealed class ScriptedLLM : ILLMClient
{
    private readonly List<Func<string, string?>> _generateHandlers = new();
    private readonly List<Func<IReadOnlyList<ChatMessage>, string?>> _chatHandlers = new();
    private readonly List<Func<string, IEnumerable<string>?>> _generateStreamHandlers = new();
    private readonly List<Func<IReadOnlyList<ChatMessage>, IEnumerable<string>?>> _chatStreamHandlers = new();

    public string DefaultResponse { get; set; } = "noop";
    public IReadOnlyList<string> DefaultStreamResponse { get; set; } = Array.Empty<string>();

    public ScriptedLLM WhenGenerate(Func<string, bool> predicate, string result)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        _generateHandlers.Add(prompt => predicate(prompt) ? result : null);
        return this;
    }

    public ScriptedLLM WhenGenerate(Func<string, bool> predicate, Func<string> resultFactory)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }
        if (resultFactory is null)
        {
            throw new ArgumentNullException(nameof(resultFactory));
        }

        _generateHandlers.Add(prompt => predicate(prompt) ? resultFactory() : null);
        return this;
    }

    public ScriptedLLM WhenChat(Func<IReadOnlyList<ChatMessage>, bool> predicate, string result)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        _chatHandlers.Add(messages => predicate(messages) ? result : null);
        return this;
    }

    public ScriptedLLM WhenChat(Func<IReadOnlyList<ChatMessage>, bool> predicate, Func<string> resultFactory)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }
        if (resultFactory is null)
        {
            throw new ArgumentNullException(nameof(resultFactory));
        }

        _chatHandlers.Add(messages => predicate(messages) ? resultFactory() : null);
        return this;
    }

    public ScriptedLLM WhenGenerateStream(Func<string, bool> predicate, params string[] chunks)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        _generateStreamHandlers.Add(prompt => predicate(prompt) ? chunks : null);
        return this;
    }

    public ScriptedLLM WhenGenerateStream(Func<string, bool> predicate, Func<IEnumerable<string>> chunkFactory)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }
        if (chunkFactory is null)
        {
            throw new ArgumentNullException(nameof(chunkFactory));
        }

        _generateStreamHandlers.Add(prompt => predicate(prompt) ? chunkFactory() : null);
        return this;
    }

    public ScriptedLLM WhenChatStream(Func<IReadOnlyList<ChatMessage>, bool> predicate, params string[] chunks)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        _chatStreamHandlers.Add(messages => predicate(messages) ? chunks : null);
        return this;
    }

    public ScriptedLLM WhenChatStream(Func<IReadOnlyList<ChatMessage>, bool> predicate, Func<IEnumerable<string>> chunkFactory)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }
        if (chunkFactory is null)
        {
            throw new ArgumentNullException(nameof(chunkFactory));
        }

        _chatStreamHandlers.Add(messages => predicate(messages) ? chunkFactory() : null);
        return this;
    }

    public Task<string> GenerateAsync(string prompt, bool track = false)
        => Task.FromResult(ResolveText(_generateHandlers, prompt, DefaultResponse));

    public async IAsyncEnumerable<string> GenerateStreamAsync(string prompt, bool track = false)
    {
        await Task.CompletedTask;
        foreach (var chunk in ResolveStream(_generateStreamHandlers, prompt))
        {
            yield return chunk;
        }
    }

    public Task<string> ChatAsync(IEnumerable<ChatMessage> messages, bool track = false)
    {
        var snapshot = Snapshot(messages);
        var result = ResolveText(_chatHandlers, snapshot, DefaultResponse);
        return Task.FromResult(result);
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(IEnumerable<ChatMessage> messages, bool track = false)
    {
        var snapshot = Snapshot(messages);
        await Task.CompletedTask;
        foreach (var chunk in ResolveStream(_chatStreamHandlers, snapshot))
        {
            yield return chunk;
        }
    }

    private static T ResolveText<TInput, T>(IEnumerable<Func<TInput, T?>> handlers, TInput input, T fallback)
        where T : class
        where TInput : notnull
    {
        foreach (var handler in handlers)
        {
            var candidate = handler(input);
            if (candidate is not null)
            {
                return candidate;
            }
        }

        return fallback;
    }

    private IEnumerable<string> ResolveStream<TInput>(IEnumerable<Func<TInput, IEnumerable<string>?>> handlers, TInput input)
        where TInput : notnull
    {
        foreach (var handler in handlers)
        {
            var candidate = handler(input);
            if (candidate is not null)
            {
                return candidate;
            }
        }

        return DefaultStreamResponse;
    }

    private static IReadOnlyList<ChatMessage> Snapshot(IEnumerable<ChatMessage> messages)
    {
        if (messages is IReadOnlyList<ChatMessage> list)
        {
            return list;
        }

        return messages.ToList();
    }
}
