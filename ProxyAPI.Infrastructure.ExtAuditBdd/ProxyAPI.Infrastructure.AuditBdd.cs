
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProxyAPI.Infrastructure.Interfaces;

namespace ProxyAPI.Infrastructure.ExtAuditBdd;

public class ProxyAPIInfrastructureAuditBdd : IAuditService
{
    public void LogRequest(DateTime timestamp, string UserId, string method, string uri, int statusCode, string? body)
    {
        throw new NotImplementedException();
    }
}