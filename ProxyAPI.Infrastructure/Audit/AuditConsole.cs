using Microsoft.Extensions.Logging;
using ProxyAPI.Infrastructure.Interfaces;

namespace ProxyAPI.Infrastructure.Audit;

public class AuditConsole : IAuditService
{
    private readonly ILogger<AuditConsole> _logger;

    public AuditConsole(ILogger<AuditConsole> logger)
    {
        _logger = logger;
    }
    
    public void LogRequest(DateTime timestamp, string UserId, string method, string uri, int statusCode, string? body)
    {
        _logger.LogInformation("Request: {Timestamp} ({UserId}): {Method} {Uri} - Status: {StatusCode}", timestamp, UserId, method, uri, statusCode);
        if (body != null)
        {
            _logger.LogInformation("Body: {Body}", body);
        }
    }
}