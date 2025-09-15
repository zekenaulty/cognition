using System.Security.Cryptography;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Images;

namespace Cognition.Clients.Images;

public interface IImageService
{
    Task<ImageAsset> GenerateAndSaveAsync(Guid? conversationId, Guid? createdByPersonaId, string prompt, ImageParameters parameters, string provider, string model);
}

public class ImageService : IImageService
{
    private readonly CognitionDbContext _db;
    private readonly IImageClient _client;

    public ImageService(CognitionDbContext db, IImageClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task<ImageAsset> GenerateAndSaveAsync(Guid? conversationId, Guid? createdByPersonaId, string prompt, ImageParameters parameters, string provider, string model)
    {
        var safePrompt = prompt.Length > 2500 ? prompt.Substring(0, 2500) : prompt;
        var res = await _client.GenerateAsync(safePrompt, parameters);
        using var sha = SHA256.Create();
        var hash = Convert.ToHexString(sha.ComputeHash(res.Bytes)).ToLowerInvariant();
        var asset = new ImageAsset
        {
            ConversationId = conversationId,
            CreatedByPersonaId = createdByPersonaId,
            Provider = res.Provider,
            Model = res.Model,
            MimeType = res.MimeType,
            Width = res.Width,
            Height = res.Height,
            Bytes = res.Bytes,
            Sha256 = hash,
            Prompt = prompt,
            NegativePrompt = parameters.NegativePrompt,
            Steps = parameters.Steps,
            Guidance = parameters.Guidance,
            Seed = parameters.Seed,
        };
        _db.ImageAssets.Add(asset);
        await _db.SaveChangesAsync();
        return asset;
    }
}
