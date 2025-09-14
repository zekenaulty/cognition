namespace Cognition.Clients.Tools;

public class TextTransformTool : ITool
{
    public string Name => "Text Transform";
    public string ClassPath => typeof(TextTransformTool).FullName! + ", " + typeof(TextTransformTool).Assembly.GetName().Name;

    public Task<object?> ExecuteAsync(ToolContext ctx, IDictionary<string, object?> args)
    {
        var text = (args.TryGetValue("text", out var v) ? v as string : null) ?? string.Empty;
        var mode = (args.TryGetValue("mode", out var m) ? m as string : null)?.ToLowerInvariant() ?? "upper";
        var output = mode == "lower" ? text.ToLowerInvariant() : text.ToUpperInvariant();
        return Task.FromResult<object?>(output);
    }
}

