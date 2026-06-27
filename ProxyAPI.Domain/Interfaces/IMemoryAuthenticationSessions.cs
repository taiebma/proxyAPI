
using ProxyAPI.Domain.Entities;

public interface IMemoryAuthenticationSessions
{
    public void AddSession(AuthenticationSession session);
    public AuthenticationSession? GetSession(string state);
    public void RemoveSession(string sessionId);
}