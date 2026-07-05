
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProxyAPI.Infrastructure.Interfaces;

namespace ProxyAPI.Infrastructure.ExtMidwCache;

public class ProxyAPIInfrastructureExtMidwCache : IProxyAPIExtension
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register the middleware cache service
        services.AddSingleton(typeof(ICacheService<>), typeof(ProxyAPIInfrastructureMidwCache<>));
    }

}