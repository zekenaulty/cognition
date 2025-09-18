using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Microsoft.Extensions.Options;
using OpenSearch.Client;

using Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;

namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Utils;

public static class OpenSearchClientFactory
{
    public static IOpenSearchClient Create(OpenSearchVectorsOptions options)
    {
        var uri = new Uri(options.Url);

        var connectionSettings = new ConnectionSettings(uri)
            .DefaultIndex(options.DefaultIndex)
            .EnableDebugMode();

        if (!string.IsNullOrWhiteSpace(options.Username) && options.Password is not null)
            connectionSettings = connectionSettings.BasicAuthentication(options.Username, options.Password);

        if (options.DisableCertValidation)
            connectionSettings = connectionSettings.ServerCertificateValidationCallback((_, _, _, _) => true);

        return new OpenSearchClient(connectionSettings);
    }
}
