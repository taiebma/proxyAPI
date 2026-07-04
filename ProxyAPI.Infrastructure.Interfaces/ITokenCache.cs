namespace ProxyAPI.Infrastructure.Interfaces;

using ProxyAPI.Infrastructure.ValueObjects;

public interface ITokenCache
{
    void Set(ClientId clientId, TokenValue token);
    TokenValue? Get(ClientId clientId);
    bool Exists(ClientId clientId);
    void Remove(ClientId clientId);
    void Clear();
}
