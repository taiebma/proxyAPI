namespace ProxyAPI.Infrastructure.Cache;

using ProxyAPI.Infrastructure.Interfaces;
using ProxyAPI.Infrastructure.ValueObjects;
using System.Collections.Concurrent;

public class MemoryCacheService<T> : ICacheService<T>
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache;
    private readonly int _defaultExpirationMinutes;

    private class CacheEntry
    {
        public required T Value { get; set; }
        public DateTime StoredAt { get; set; }
        public int ExpirationMinutes { get; set; }
    }

    public MemoryCacheService(int defaultExpirationMinutes = 60)
    {
        _cache = new ConcurrentDictionary<string, CacheEntry>();
        _defaultExpirationMinutes = defaultExpirationMinutes;
    }

    public void Set(string key, T value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (value == null) throw new ArgumentNullException(nameof(value));

        _cache[key] = new CacheEntry
        {
            Value = value,
            StoredAt = DateTime.UtcNow,
            ExpirationMinutes = _defaultExpirationMinutes
        };
    }

    public T? Get(string key)
    {
        if (key == null) return default(T);

        if (_cache.TryGetValue(key, out var entry))
        {
            if (IsEntryExpired(entry))
            {
                _cache.TryRemove(key, out _);
                return default(T);
            }

            return entry.Value;
        }

        return default(T);
    }

    public bool Exists(string key)
    {
        return Get(key) != null;
    }

    public void Remove(string key)
    {
        if (key != null)
            _cache.TryRemove(key, out _);
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
