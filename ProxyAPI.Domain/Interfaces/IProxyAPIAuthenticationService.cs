namespace ProxyAPI.Domain.Interfaces;

using ProxyAPI.Domain.DTOs;

public interface IProxyAPIAuthenticationService
{
    Task<AuthorizationUrlResponse> GetAuthorizationUrlAsync(string redirectUri, string[]? scopes = null);
    Task<ClientContext> HandleCallbackAsync(AuthorizationCodeRequest request);
    Task<ClientContext?> GetClientContextAsync(string clientId);
    Task<ClientContext?> RefreshClientContextAsync(string clientId);
    Task LogoutAsync(string clientId);
}
