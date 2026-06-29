namespace ProxyAPI.Infrastructure.OAuth;

using ProxyAPI.Infrastructure.Exceptions;
using ProxyAPI.Infrastructure.Interfaces;
using ProxyAPI.Infrastructure.ValueObjects;
using ProxyAPI.Infrastructure.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

public class OidcClient : IOAuthClient
{
    private readonly HttpClient _httpClient;
    private readonly OIdcAuthSettings _settings;

    public OidcClient(HttpClient httpClient, OIdcAuthSettings settings)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
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
        var token = handler.ReadJwtToken(tokenResponse.AccessToken);

        var expiresAt = token.ValidTo;

        return new TokenValue(tokenResponse.AccessToken, tokenResponse.RefreshToken, expiresAt);
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

            var expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            return new TokenValue(tokenResponse.AccessToken, tokenResponse.RefreshToken, expiresAt);
        }
        catch (HttpRequestException ex)
        {
            throw new OAuthException($"Failed to refresh token: {ex.Message}");
        }
    }

}
