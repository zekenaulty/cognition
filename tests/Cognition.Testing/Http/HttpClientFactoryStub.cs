using System.Collections.Concurrent;
using Microsoft.Extensions.Http;
using System.Net.Http;

namespace Cognition.Testing.Http;

/// <summary>
/// Minimal <see cref=\"IHttpClientFactory\"/> implementation for unit tests.
/// Allows registering named clients or falling back to a default <see cref=\"HttpClient\"/> instance.
/// </summary>
public sealed class HttpClientFactoryStub : IHttpClientFactory
{
    private readonly ConcurrentDictionary<string, Func<HttpClient>> _registrations = new(StringComparer.OrdinalIgnoreCase);
    private readonly Func<string, HttpClient> _fallbackFactory;

    public HttpClientFactoryStub(HttpClient? defaultClient = null)
    {
        _fallbackFactory = _ => defaultClient ?? new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false });
    }

    public HttpClientFactoryStub(Func<string, HttpClient> fallbackFactory)
    {
        _fallbackFactory = fallbackFactory ?? throw new ArgumentNullException(nameof(fallbackFactory));
    }

    public HttpClient CreateClient(string name)
    {
        if (_registrations.TryGetValue(name, out var createClient))
        {
            return createClient();
        }

        return _fallbackFactory(name);
    }

    public void Register(string name, HttpClient client)
    {
        if (client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        Register(name, () => client);
    }

    public void Register(string name, Func<HttpClient> factory)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Client name must be provided", nameof(name));
        }

        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        _registrations[name] = factory;
    }
}


