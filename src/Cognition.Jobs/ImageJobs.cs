using Cognition.Clients.Images;

namespace Cognition.Jobs;

public class ImageJobs
{
    private readonly IImageService _images;

    public ImageJobs(IImageService images)
    {
        _images = images;
    }

    public async Task<Guid> Generate(Guid? conversationId, Guid? personaId, string prompt, int width, int height, Guid? styleId, string? negativePrompt, string provider, string model, CancellationToken ct = default)
    {
        var p = new ImageParameters(width, height, null, negativePrompt, 30, 7.5f, null, model);
        var asset = await _images.GenerateAndSaveAsync(conversationId, personaId, prompt, p, provider, model);
        return asset.Id;
    }
}

