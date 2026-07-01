using ProxyAPI.Infrastructure.ValueObjects;

namespace ProxyAPI.Domain.Interfaces;

public interface ITokenService
{
    Task<TokenValue> GetTokenAsync();
    Task<TokenValue> RefreshTokenAsync(string refreshToken);
}