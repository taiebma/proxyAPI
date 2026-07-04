namespace ProxyAPI.Infrastructure.Interfaces;

public interface ICacheService<T>
{
    void Set(string key, T value);
    T? Get(string key);
    bool Exists(string key);
    void Remove(string key);
    void Clear();
}
