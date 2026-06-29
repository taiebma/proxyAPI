namespace ProxyAPI.Infrastructure.Interfaces;

public interface ISessionStorage
{
    public void AddSession(IAuthenticationSession session);
    public IAuthenticationSession? GetSession(string state);
    public void RemoveSession(string sessionId);
}