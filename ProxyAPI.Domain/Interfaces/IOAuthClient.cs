namespace ProxyAPI.Domain.Interfaces;

using ProxyAPI.Domain.ValueObjects;

public interface IOAuthClient
{
    Task<TokenValue> ExchangeCodeForTokenAsync(string code, string redirectUri, string? codeVerifier = null);
    Task<string> GetAuthorizationUrlAsync(string state, string redirectUri, string[]? scopes = null);
    Task<TokenValue> RefreshTokenAsync(string refreshToken);
}
