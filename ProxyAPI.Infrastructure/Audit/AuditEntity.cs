using ProxyAPI.Infrastructure.Interfaces;

namespace ProxyAPI.Infrastructure.Audit;

public class AuditEntity: IAudit
{
    public DateTime Timestamp { get; set;} = DateTime.UtcNow;
    public string UserId { get; set;} = string.Empty;
    public string Method { get; set;} = string.Empty;
    public string Uri { get; set;} = string.Empty;
    public int StatusCode { get; set;} = 0;
    public string? Body { get; set;} = null;
}