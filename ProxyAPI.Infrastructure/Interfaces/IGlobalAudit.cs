namespace ProxyAPI.Infrastructure.Interfaces;

public interface IGlobalAudit<T>
{
    Task LogRequest(T audit);
}