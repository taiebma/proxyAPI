
using System.Collections.Concurrent;
using ProxyAPI.Infrastructure.Interfaces;

public class SessionStorage : ISessionStorage
{
    private readonly ICacheService<IAuthenticationSession> _sessions;

    /// <summary>
    /// For test unit purposes only. Use the constructor with ICacheService<IAuthenticationSession> parameter in production code.
    /// </summary>
    public SessionStorage()
    {
    }

    public SessionStorage(ICacheService<IAuthenticationSession> sessions)
    {
        _sessions = sessions;
    }

    public void AddSession(IAuthenticationSession session)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        _sessions.Set(session.State, session);
    }

    public IAuthenticationSession? GetSession(string state)
    {
        if (string.IsNullOrWhiteSpace(state)) return null;

        IAuthenticationSession? session = _sessions.Get(state);
        
        if (session is null)
            return null;

        return session;
    }

    public void RemoveSession(string sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
            _sessions.Remove(sessionId);
    }

}