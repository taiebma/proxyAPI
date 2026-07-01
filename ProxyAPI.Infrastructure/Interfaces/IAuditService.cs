namespace ProxyAPI.Infrastructure.Interfaces;

public interface IAuditService
{
    void LogRequest(DateTime timestamp, string UserId, string method, string uri, int statusCode, string? body);
}