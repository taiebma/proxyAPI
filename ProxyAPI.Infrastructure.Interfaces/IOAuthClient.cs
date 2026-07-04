namespace ProxyAPI.Infrastructure.Interfaces;

using ProxyAPI.Infrastructure.ValueObjects;

public interface IOAuthClient
{
    Task<TokenValue> GetTokenAsync();
    Task<TokenValue> RefreshTokenAsync(string refreshToken);
}
