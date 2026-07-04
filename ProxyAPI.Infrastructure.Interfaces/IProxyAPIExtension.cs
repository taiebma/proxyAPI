
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyAPI.Infrastructure.Interfaces;

public interface IProxyAPIExtension
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
}