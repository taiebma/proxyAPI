using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;

namespace ProxyAPI.Infrastructure.SdCoac;

public class CoacServiceEndpointProvider : IServiceEndpointProvider
{
    private readonly string _serviceName;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    public CoacServiceEndpointProvider(string serviceName, IHttpClientFactory httpClientFactory, ILogger logger)
    {
        _serviceName = serviceName;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async ValueTask PopulateAsync(IServiceEndpointBuilder builder, CancellationToken cancellationToken)
    {
        // ATTENTION: ce client ne doit surtout pas lui-même utiliser le service discovery,
        // sinon boucle infinie. On donne un nom explicite pour ne pas passer par ConfigureHttpClientDefaults.
        var client = _httpClientFactory.CreateClient("alias-resolver-api");

        try
        {
            /*
            var result = await client.GetFromJsonAsync<AliasResolutionResult>(
                $"/resolve?alias={Uri.EscapeDataString(_serviceName)}",
                cancellationToken);

            if (result is not null)
            {
                foreach (var host in result.Hosts)
                {
                    builder.Endpoints.Add(
                        ServiceEndpoint.Create(new DnsEndPoint(host.Hostname, host.Port)));
                }
            }
            */
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossible de résoudre l'alias {Alias} via l'API", _serviceName);
            // On ne relance pas forcément : ça permet au provider suivant (fallback) de prendre le relais
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}