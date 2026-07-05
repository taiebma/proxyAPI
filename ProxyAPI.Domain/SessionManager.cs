
namespace ProxyAPI.Domain;
using ProxyAPI.Domain.Entities;
using ProxyAPI.Domain.Interfaces;
using ProxyAPI.Infrastructure.Interfaces;

public class SessionManager : ISessionManager
{
    private readonly ICacheService<IAuthenticationSession> _sessions;

    public SessionManager(ICacheService<IAuthenticationSession> sessions)
    {
        _sessions = sessions;
    }

    public void AddSession(AuthenticationSession session)
    {
        if (session == null
          || string.IsNullOrWhiteSpace(session.State)) 
        {
            throw new ArgumentNullException(nameof(AuthenticationSession));
        }
        _sessions.Set(session.State, session);
    }

    public AuthenticationSession? GetSession(string state)
    {
        if (string.IsNullOrWhiteSpace(state)) return null;

        AuthenticationSession? session = (AuthenticationSession?)_sessions.Get(state);
        
        if (session is null)
            return null;

        if (session.IsExpired)
        {
            _sessions.Remove(session.State);
            return null;
        }

        return session;
    }

    public void RemoveSession(string sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
            _sessions.Remove(sessionId);
    }

}