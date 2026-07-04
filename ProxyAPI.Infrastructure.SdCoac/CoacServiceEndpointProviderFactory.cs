
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;

namespace ProxyAPI.Infrastructure.SdCoac;

public class CoacServiceEndpointProviderFactory : IServiceEndpointProviderFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CoacServiceEndpointProviderFactory> _logger;

    public CoacServiceEndpointProviderFactory(
        IHttpClientFactory httpClientFactory,
        ILogger<CoacServiceEndpointProviderFactory> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public bool TryCreateProvider(
        ServiceEndpointQuery query,
        [NotNullWhen(true)] out IServiceEndpointProvider? provider)
    {
        // query contient le nom logique demandé, ex: "v1.apipki.api.dapi"
        provider = new CoacServiceEndpointProvider(query.ToString()!, _httpClientFactory, _logger);
        return true; // true = "je prends en charge ce nom"
    }
}