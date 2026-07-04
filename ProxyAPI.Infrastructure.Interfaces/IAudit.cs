namespace ProxyAPI.Infrastructure.Interfaces;

public interface IAudit
{
    DateTime Timestamp { get; set;}
    string UserId { get; set;}
    string Method { get; set;}
    string Uri { get; set;}
    int StatusCode { get; set;}
    string? Body { get; set;}
}