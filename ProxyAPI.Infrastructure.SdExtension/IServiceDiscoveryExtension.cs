
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyAPI.Infrastructure.SdExtension;

public interface IServiceDiscoveryExtension
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
}