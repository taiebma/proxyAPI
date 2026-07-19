namespace ProxyAPI.Infrastructure.OAuth;

using ProxyAPI.Infrastructure.Exceptions;
using ProxyAPI.Infrastructure.Interfaces;
using ProxyAPI.Infrastructure.ValueObjects;
using ProxyAPI.Infrastructure.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

public class OAuthClient : IOAuthClient
{
    private readonly HttpClient _httpClient;
    private readonly OAuthSettings _settings;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configManager;
    private OpenIdConnectConfiguration _oauthConfig = null!;

    public OAuthClient(HttpClient httpClient, OAuthSettings settings)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{_settings.Authority}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever());
    }

    public async Task<TokenValue> GetTokenAsync()
    {
        var content = new Dictionary<string, string>
        {
            { "grant_type", "client_credential" },
            { "client_id", _settings.ClientId },
            { "client_secret", _settings.ClientSecret },
            { "scope", string.Join(' ', _settings.Scopes??new string[0]) }
        };
        if (_settings.AdditionalParameters != null)
        {
            foreach (var param in _settings.AdditionalParameters)
            {
                content.Add(param.Key, param.Value);
            }
        }

        var request = new HttpRequestMessage(HttpMethod.Post, _settings.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(content)
        };

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (string.IsNullOrWhiteSpace(tokenResponse?.AccessToken))
            throw new OAuthException("Invalid token response from IDP.");

        var handler = new JwtSecurityTokenHandler();
        CheckTokenValidity(handler, tokenResponse.AccessToken);

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

            if (string.IsNullOrWhiteSpace(tokenResponse?.AccessToken))
                throw new OAuthException("Invalid token response from IDP.");

            var expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            return new TokenValue(tokenResponse.AccessToken, tokenResponse.RefreshToken, expiresAt);
        }
        catch (HttpRequestException ex)
        {
            throw new OAuthException($"Failed to refresh token: {ex.Message}");
        }
    }

    private void CheckTokenValidity(JwtSecurityTokenHandler handler, string accessToken)
    {
        if (_oauthConfig == null)
            _oauthConfig = _configManager.GetConfigurationAsync().GetAwaiter().GetResult();

        var validationParameters = new TokenValidationParameters
        {
            ValidIssuers = new List<string> { $"{_settings.Authority}" },
            IssuerSigningKeys = _oauthConfig.SigningKeys,
            ValidateLifetime = true,
            ValidateAudience = false,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        try
        {
            handler.ValidateToken(accessToken, validationParameters, out SecurityToken validatedToken);
        }
        catch (Exception ex)
        {
            throw new OAuthException($"Invalid token: {ex.Message}");
        }
    }

}
