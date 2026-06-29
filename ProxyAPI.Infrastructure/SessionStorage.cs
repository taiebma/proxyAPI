
using System.Collections.Concurrent;
using ProxyAPI.Infrastructure.Interfaces;

public class SessionStorage : ISessionStorage
{
    private readonly ConcurrentDictionary<string, IAuthenticationSession> _sessions;

    public SessionStorage()
    {
        _sessions = new ConcurrentDictionary<string, IAuthenticationSession>();
    }

    public void AddSession(IAuthenticationSession session)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        _sessions[session.Id] = session;
    }

    public IAuthenticationSession? GetSession(string state)
    {
        if (string.IsNullOrWhiteSpace(state)) return null;

        IAuthenticationSession? session = _sessions.Values.FirstOrDefault(x => x.State == state);
        
        if (session is null)
            return null;

        return session;
    }

    public void RemoveSession(string sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
            _sessions.TryRemove(sessionId, out _);
    }

}