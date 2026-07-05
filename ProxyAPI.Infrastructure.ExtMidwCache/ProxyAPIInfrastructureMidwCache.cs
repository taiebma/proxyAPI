
using ProxyAPI.Infrastructure.Interfaces;

namespace ProxyAPI.Infrastructure.ExtMidwCache;

public class ProxyAPIInfrastructureMidwCache<T>: ICacheService<T>
{

    public void Set(string key, T value)
    {
        throw new NotImplementedException();
    }

    T? ICacheService<T>.Get(string key)
    {
        throw new NotImplementedException();
    }

    public bool Exists(string key)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public void Remove(string key)
    {
        throw new NotImplementedException();
    }
}