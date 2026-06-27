namespace ProxyAPI.Application.Services;

using ProxyAPI.Application.DTOs;
using ProxyAPI.Application.Interfaces;
using ProxyAPI.Domain.Entities;
using ProxyAPI.Domain.Exceptions;
using ProxyAPI.Domain.Interfaces;
using ProxyAPI.Domain.ValueObjects;

public class AuthenticationService : IAuthenticationService
{
    private readonly ITokenCache _tokenCache;
    private readonly IOAuthClient _oauthClient;
    private readonly IMemoryAuthenticationSessions _memoryAuthenticationSessions;

    public AuthenticationService(ITokenCache tokenCache, IOAuthClient oauthClient, IMemoryAuthenticationSessions memoryAuthenticationSessions)
    {
        _tokenCache = tokenCache ?? throw new ArgumentNullException(nameof(tokenCache));
        _oauthClient = oauthClient ?? throw new ArgumentNullException(nameof(oauthClient));
        _memoryAuthenticationSessions = memoryAuthenticationSessions ?? throw new ArgumentNullException(nameof(memoryAuthenticationSessions));
    }

    public async Task<AuthorizationUrlResponse> GetAuthorizationUrlAsync(
        string redirectUri,
        string[]? scopes = null)
    {
        var state = GenerateRandomString(32);
        var session = new AuthenticationSession(state);
        _memoryAuthenticationSessions.AddSession(session);

        var url = await _oauthClient.GetAuthorizationUrlAsync(state, redirectUri, scopes);

        return new AuthorizationUrlResponse(url, state, session.Id);
    }

    public async Task<ClientContext> HandleCallbackAsync(AuthorizationCodeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new OAuthException("Authorization code is missing.");

        AuthenticationSession? session = _memoryAuthenticationSessions.GetSession(request.State?.Replace(' ','+') ?? "");
        if (session == null || !session.ValidateState(request.State?.Replace(' ','+') ?? ""))
            throw new InvalidStateException("Invalid or expired state parameter.");

        _memoryAuthenticationSessions.RemoveSession(request.SessionId ?? "");

        var clientId = new ClientId(Guid.NewGuid().ToString());
        var token = await _oauthClient.ExchangeCodeForTokenAsync(
            request.Code,
            "http://localhost:5000/auth/callback",
            session.CodeVerifier);

        _tokenCache.Set(clientId, token);

        return new ClientContext(
            clientId.Value,
            token.AccessToken,
            token.RefreshToken,
            token.ExpiresAt);
    }

    public async Task<ClientContext?> GetClientContextAsync(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return null;

        var id = new ClientId(clientId);
        var token = _tokenCache.Get(id);

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

        var id = new ClientId(clientId);
        var token = _tokenCache.Get(id);

        if (token?.RefreshToken == null)
            return null;

        try
        {
            var newToken = await _oauthClient.RefreshTokenAsync(token.RefreshToken);
            _tokenCache.Set(id, newToken);

            return new ClientContext(
                clientId,
                newToken.AccessToken,
                newToken.RefreshToken,
                newToken.ExpiresAt);
        }
        catch
        {
            _tokenCache.Remove(id);
            return null;
        }
    }

    public async Task LogoutAsync(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return;

        _tokenCache.Remove(new ClientId(clientId));
    }

    private static string GenerateRandomString(int length)
        => Convert.ToBase64String(Guid.NewGuid().ToByteArray());
}
