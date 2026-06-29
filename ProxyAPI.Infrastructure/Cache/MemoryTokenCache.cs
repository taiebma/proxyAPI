namespace ProxyAPI.Infrastructure.Cache;

using ProxyAPI.Infrastructure.Interfaces;
using ProxyAPI.Infrastructure.ValueObjects;
using System.Collections.Concurrent;

public class MemoryTokenCache : ITokenCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache;
    private readonly int _defaultExpirationMinutes;

    private class CacheEntry
    {
        public required TokenValue Token { get; set; }
        public DateTime StoredAt { get; set; }
        public int ExpirationMinutes { get; set; }
    }

    public MemoryTokenCache(int defaultExpirationMinutes = 60)
    {
        _cache = new ConcurrentDictionary<string, CacheEntry>();
        _defaultExpirationMinutes = defaultExpirationMinutes;
    }

    public void Set(ClientId clientId, TokenValue token)
    {
        if (clientId == null) throw new ArgumentNullException(nameof(clientId));
        if (token == null) throw new ArgumentNullException(nameof(token));

        _cache[clientId.Value] = new CacheEntry
        {
            Token = token,
            StoredAt = DateTime.UtcNow,
            ExpirationMinutes = _defaultExpirationMinutes
        };
    }

    public TokenValue? Get(ClientId clientId)
    {
        if (clientId == null) return null;

        if (_cache.TryGetValue(clientId.Value, out var entry))
        {
            if (IsEntryExpired(entry))
            {
                _cache.TryRemove(clientId.Value, out _);
                return null;
            }

            return entry.Token;
        }

        return null;
    }

    public bool Exists(ClientId clientId)
    {
        return Get(clientId) != null;
    }

    public void Remove(ClientId clientId)
    {
        if (clientId != null)
            _cache.TryRemove(clientId.Value, out _);
    }

    public void Clear()
    {
        _cache.Clear();
    }

    private bool IsEntryExpired(CacheEntry entry)
    {
        var expirationTime = entry.StoredAt.AddMinutes(entry.ExpirationMinutes);
        return DateTime.UtcNow > expirationTime;
    }
}
