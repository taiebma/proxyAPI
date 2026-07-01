using ProxyAPI.Domain.Interfaces;
using ProxyAPI.Infrastructure.Interfaces;
using ProxyAPI.Infrastructure.ValueObjects;

namespace ProxyAPI.Domain;

public class TokenService: ITokenService
{
    private readonly IOAuthClient _client;

    public TokenService(IOAuthClient client)
    {
        _client = client ;
    }

    public async Task<TokenValue> GetTokenAsync()
    {
        return await _client.GetTokenAsync();
    }

    public async Task<TokenValue> RefreshTokenAsync(string refreshToken)
    {
        return await _client.RefreshTokenAsync(refreshToken);
    }
}