using System.Threading.Tasks;

namespace Cognition.Clients.Images;

public interface IImageClient
{
    Task<ImageResult> GenerateAsync(string prompt, ImageParameters parameters);
}

public record ImageParameters(int Width = 1024, int Height = 1024, string? Style = null, string? NegativePrompt = null, int Steps = 30, float Guidance = 7.5f, int? Seed = null, string? Model = null);

public record ImageResult(byte[] Bytes, string MimeType, int Width, int Height, string Provider, string Model);

