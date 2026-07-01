using ProxyAPI.Infrastructure.Interfaces;

namespace ProxyAPI.Infrastructure.Audit;

public class AuditConsole : IAuditService
{
    public void LogRequest(DateTime timestamp, string UserId, string method, string uri, int statusCode, string? body)
    {
        Console.WriteLine($"Request: {timestamp:dd/MMM/yyyy:HH:mm:ss zzz} ({UserId}): {method} {uri} - Status: {statusCode}");
        if (body != null)
        {
            Console.WriteLine($"Body: {body}");
        }
    }
}