
using System.Collections.Concurrent;
using ProxyAPI.Domain.Entities;

public class MemoryAuthenticationSessions : IMemoryAuthenticationSessions
{
    private readonly ConcurrentDictionary<string, AuthenticationSession> _sessions;

    public MemoryAuthenticationSessions()
    {
        _sessions = new ConcurrentDictionary<string, AuthenticationSession>();
    }

    public void AddSession(AuthenticationSession session)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        _sessions[session.Id] = session;
    }

    public AuthenticationSession? GetSession(string state)
    {
        if (string.IsNullOrWhiteSpace(state)) return null;

        AuthenticationSession? session = _sessions.Values.FirstOrDefault(x => x.State == state);
        
        if (session is null)
            return null;

        if (session.IsExpired)
        {
            _sessions.TryRemove(session.Id, out _);
            return null;
        }

        return session;
    }

    public void RemoveSession(string sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
            _sessions.TryRemove(sessionId, out _);
    }

}