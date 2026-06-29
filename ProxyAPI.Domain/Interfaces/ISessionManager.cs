namespace ProxyAPI.Domain.Interfaces;
using ProxyAPI.Domain.Entities;

public interface ISessionManager
{
    public void AddSession(AuthenticationSession session);
    public AuthenticationSession? GetSession(string state);
    public void RemoveSession(string sessionId);
}