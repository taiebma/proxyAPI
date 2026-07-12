namespace ProxyAPI.Infrastructure.OAuth;

using ProxyAPI.Infrastructure.Exceptions;
using ProxyAPI.Infrastructure.Interfaces;
using ProxyAPI.Infrastructure.ValueObjects;
using ProxyAPI.Infrastructure.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

public class OidcClient : IOidcClient
{
    private readonly HttpClient _httpClient;
    private readonly OIdcAuthSettings _settings;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configManager;
    private readonly Func<JwtSecurityTokenHandler, string, Task>? _checkTokenValidityOverride;
    private OpenIdConnectConfiguration _oidcConfig = null!;

    public OidcClient(
        HttpClient httpClient,
        OIdcAuthSettings settings,
        Func<JwtSecurityTokenHandler, string, Task>? checkTokenValidityOverride = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _checkTokenValidityOverride = checkTokenValidityOverride;
        _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{_settings.Authority}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever());
    }

    public async Task<string> GetAuthorizationUrlAsync(
        string state,
        string redirectUri,
        string[]? scopes = null)
    {
        var scope = string.Join(" ", scopes ?? _settings.Scopes ?? Array.Empty<string>());

        var authUrl = $"{_settings.AuthorizationEndpoint}?" +
            $"client_id={Uri.EscapeDataString(_settings.ClientId)}&" +
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
            $"response_type=code&" +
            $"scope={Uri.EscapeDataString(scope)}&" +
            $"state={Uri.EscapeDataString(state)}";

        return await Task.FromResult(authUrl);
    }

    public async Task<TokenValue> ExchangeCodeForTokenAsync(
        string code,
        string redirectUri,
        string? codeVerifier = null)
    {
        var content = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", redirectUri },
            { "client_id", _settings.ClientId },
            { "client_secret", _settings.ClientSecret }
        };

        if (!string.IsNullOrWhiteSpace(codeVerifier))
            content["code_verifier"] = codeVerifier;

        var request = new HttpRequestMessage(HttpMethod.Post, _settings.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(content)
        };

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
            throw new OAuthException("Invalid token response from IDP.");

        var handler = new JwtSecurityTokenHandler();
        var validateToken = _checkTokenValidityOverride ?? CheckTokenValidity;
        await validateToken(handler, tokenResponse.AccessToken);

        var token = handler.ReadJwtToken(tokenResponse.AccessToken);

        var expiresAt = token.ValidTo;

        return new TokenValue(tokenResponse.AccessToken, tokenResponse.RefreshToken, expiresAt, token.Subject);
    }

    public async Task<TokenValue> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("RefreshToken cannot be empty.", nameof(refreshToken));

        var content = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken },
            { "client_id", _settings.ClientId },
            { "client_secret", _settings.ClientSecret }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, _settings.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(content)
        };

        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenResponse?.AccessToken == null)
                throw new OAuthException("Invalid token response from IDP.");

            var handler = new JwtSecurityTokenHandler();
            var validateToken = _checkTokenValidityOverride ?? CheckTokenValidity;
            await validateToken(handler, tokenResponse.AccessToken);
            var token = handler.ReadJwtToken(tokenResponse.AccessToken);
            var expiresAt = token.ValidTo;

            return new TokenValue(tokenResponse.AccessToken, tokenResponse.RefreshToken, expiresAt, token.Subject);
        }
        catch (HttpRequestException ex)
        {
            throw new OAuthException($"Failed to refresh token: {ex.Message}");
        }
    }

    private async Task CheckTokenValidity(JwtSecurityTokenHandler handler, string accessToken)
    {
        if (_oidcConfig == null)
            _oidcConfig = await _configManager.GetConfigurationAsync();

        var validationParameters = new TokenValidationParameters
        {
            ValidIssuers = new List<string> { $"{_settings.Authority}" },
            IssuerSigningKeys = _oidcConfig.SigningKeys,
            ValidateLifetime = true,
            ValidateAudience = false,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
        try
        {
            ClaimsPrincipal principal = handler.ValidateToken(accessToken, validationParameters, out var validatedToken);
        }
        catch (SecurityTokenException ex)
        {
            throw new OAuthException($"Token validation failed: {ex.Message}");
        }
    }
}
