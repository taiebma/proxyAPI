
namespace ProxyAPI.Domain;
using ProxyAPI.Domain.Entities;
using ProxyAPI.Domain.Interfaces;
using ProxyAPI.Infrastructure.Interfaces;

public class SessionManager : ISessionManager
{
    private readonly ISessionStorage _sessionStorage;

    public SessionManager(ISessionStorage sessionStorage)
    {
        _sessionStorage = sessionStorage;
    }

    public void AddSession(AuthenticationSession session)
    {
        if (session == null
          || string.IsNullOrWhiteSpace(session.State)) 
        {
            throw new ArgumentNullException(nameof(AuthenticationSession));
        }
        _sessionStorage.AddSession(session);
    }

    public AuthenticationSession? GetSession(string state)
    {
        if (string.IsNullOrWhiteSpace(state)) return null;

        AuthenticationSession? session = (AuthenticationSession?)_sessionStorage.GetSession(state);
        
        if (session is null)
            return null;

        if (session.IsExpired)
        {
            _sessionStorage.RemoveSession(session.State);
            return null;
        }

        return session;
    }

    public void RemoveSession(string sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
            _sessionStorage.RemoveSession(sessionId);
    }

}