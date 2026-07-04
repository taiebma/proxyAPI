
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ServiceDiscovery;
using ProxyAPI.Infrastructure.Interfaces;

namespace ProxyAPI.Infrastructure.SdCoac;

public sealed class CoacServiceDiscoveryExtension : IProxyAPIExtension
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {

        services.AddServiceDiscoveryCore();
        services.AddSingleton<IServiceEndpointProviderFactory, CoacServiceEndpointProviderFactory>();
        services.AddConfigurationServiceEndpointProvider(); // fallback interne au plugin
        services.AddPassThroughServiceEndpointProvider();
    }
}