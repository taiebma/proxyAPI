
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProxyAPI.Infrastructure.Interfaces;

namespace ProxyAPI.Infrastructure.ExtAuthz;

public class ProxyAPIInfrastructureExtAuthz : IProxyAPIExtension
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register the middleware cache service
        services.AddScoped(typeof(IRoleProvider), typeof(AuthzRoleProvider));
        services.AddHttpClient<IRoleProvider, AuthzRoleProvider>(client =>
        {
            client.BaseAddress = new Uri(configuration["RoleProvider:ApiBaseUrl"]!);
        });        
    }

}