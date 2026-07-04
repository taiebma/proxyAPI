
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProxyAPI.Infrastructure.Interfaces;

namespace ProxyAPI.Infrastructure.ExtAuditBdd;

public class ProxyAPIInfrastructureExtAuditBdd : IProxyAPIExtension
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register the BDD audit service
        services.AddSingleton<IAuditService, ProxyAPIInfrastructureAuditBdd>();
    }

}