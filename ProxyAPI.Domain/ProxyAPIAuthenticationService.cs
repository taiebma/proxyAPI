namespace ProxyAPI.Domain;

using ProxyAPI.Domain.DTOs;
using ProxyAPI.Domain.Interfaces;
using ProxyAPI.Domain.Entities;
using ProxyAPI.Infrastructure.Exceptions;
using ProxyAPI.Infrastructure.Interfaces;
using ProxyAPI.Infrastructure.ValueObjects;

public class ProxyAPIAuthenticationService : IProxyAPIAuthenticationService
{
    private readonly ICacheService<TokenValue> _tokenCache;
    private readonly IOidcClient _oauthClient;
    private readonly ISessionManager _sessionManager;

    public ProxyAPIAuthenticationService(ICacheService<TokenValue> tokenCache, IOidcClient oauthClient, ISessionManager sessionManager)
    {
        _tokenCache = tokenCache ?? throw new ArgumentNullException(nameof(tokenCache));
        _oauthClient = oauthClient ?? throw new ArgumentNullException(nameof(oauthClient));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    }

    public async Task<AuthorizationUrlResponse> GetAuthorizationUrlAsync(
        string redirectUri,
        string[]? scopes = null)
    {
        var state = GenerateRandomString(32);
        var session = new AuthenticationSession(state);
        _sessionManager.AddSession(session);

        var url = await _oauthClient.GetAuthorizationUrlAsync(state, redirectUri, scopes);

        return new AuthorizationUrlResponse(url, state, Guid.NewGuid().ToString());
    }

    public async Task<ClientContext> HandleCallbackAsync(AuthorizationCodeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new OAuthException("Authorization code is missing.");

        AuthenticationSession? session = _sessionManager.GetSession(request.State?.Replace(' ','+') ?? "");
        if (session == null || !session.ValidateState(request.State?.Replace(' ','+') ?? ""))
            throw new InvalidStateException("Invalid or expired state parameter.");

        _sessionManager.RemoveSession(request.SessionId ?? "");

        var token = await _oauthClient.ExchangeCodeForTokenAsync(
            request.Code,
            "http://localhost:5000/auth/callback",
            session.CodeVerifier);

        var clientId = Guid.NewGuid().ToString();
        _tokenCache.Set(clientId, token);

        return new ClientContext(
            clientId,
            token.AccessToken,
            token.RefreshToken,
            token.ExpiresAt);
    }

    public async Task<ClientContext?> GetClientContextAsync(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return null;

        var token = _tokenCache.Get(clientId);

        if (token == null || token.IsExpired)
            return null;

        return new ClientContext(
            clientId,
            token.AccessToken,
            token.RefreshToken,
            token.ExpiresAt);
    }

    public async Task<ClientContext?> RefreshClientContextAsync(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return null;

        var token = _tokenCache.Get(clientId);

        if (token?.RefreshToken == null)
            return null;

        try
        {
            var newToken = await _oauthClient.RefreshTokenAsync(token.RefreshToken);
            _tokenCache.Set(clientId, newToken);

            return new ClientContext(
                clientId,
                newToken.AccessToken,
                newToken.RefreshToken,
                newToken.ExpiresAt);
        }
        catch
        {
            _tokenCache.Remove(clientId);
            return null;
        }
    }

    public async Task LogoutAsync(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return;

        _tokenCache.Remove(clientId);
    }

    private static string GenerateRandomString(int length)
        => Convert.ToBase64String(Guid.NewGuid().ToByteArray());
}
