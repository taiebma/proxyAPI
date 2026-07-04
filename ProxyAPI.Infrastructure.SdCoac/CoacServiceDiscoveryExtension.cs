
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ServiceDiscovery;
using ProxyAPI.Infrastructure.SdExtension;

namespace ProxyAPI.Infrastructure.SdCoac;

public sealed class CoacServiceDiscoveryExtension : IServiceDiscoveryExtension
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {

        services.AddServiceDiscoveryCore();
        services.AddSingleton<IServiceEndpointProviderFactory, CoacServiceEndpointProviderFactory>();
        services.AddConfigurationServiceEndpointProvider(); // fallback interne au plugin
        services.AddPassThroughServiceEndpointProvider();
    }
}