namespace ProxyAPI.Tests.Infrastructure.Tests;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;
using ProxyAPI.Infrastructure.Configuration;
using ProxyAPI.Infrastructure.Exceptions;
using ProxyAPI.Infrastructure.OAuth;
using ProxyAPI.Infrastructure.ValueObjects;
using Xunit;

public class OidcClientTests
{
    public OidcClientTests()
    {
    }

    [Fact]
    public async Task GetAuthorizationUrlAsync_WithDefaultScopes_ReturnsCorrectlyEncodedUrl()
    {
        var settings = new OIdcAuthSettings
        {
            AuthorizationEndpoint = "https://idp.example.com/authorize",
            ClientId = "client-123",
            ClientSecret = "secret",
            Scopes = new[] { "openid", "profile" }
        };

        var client = new OidcClient(new HttpClient(), settings);
        var redirectUri = "https://app.example.com/callback";
        var state = "a state with spaces";

        var result = await client.GetAuthorizationUrlAsync(state, redirectUri);

        result.Should().StartWith(settings.AuthorizationEndpoint + "?");
        result.Should().Contain("client_id=client-123");
        result.Should().Contain("redirect_uri=https%3A%2F%2Fapp.example.com%2Fcallback");
        result.Should().Contain("response_type=code");
        result.Should().Contain("scope=openid%20profile");
        result.Should().Contain("state=a%20state%20with%20spaces");
    }

    [Fact]
    public async Task GetAuthorizationUrlAsync_WithExplicitScopes_UsesProvidedScopes()
    {
        var settings = new OIdcAuthSettings 
        {
            AuthorizationEndpoint = "https://idp.example.com/authorize",
            ClientId = "client-123",
            ClientSecret = "secret",
            Scopes = new[] { "openid", "profile" }
        };

        var client = new OidcClient(new HttpClient(), settings);
        var redirectUri = "https://app.example.com/callback";
        var state = "state-xyz";
        var explicitScopes = new[] { "openid", "offline_access" };

        var result = await client.GetAuthorizationUrlAsync(state, redirectUri, explicitScopes);

        result.Should().Contain("scope=openid%20offline_access");
    }

    [Fact]
    public async Task ExchangeCodeForTokenAsync_WithValidResponse_ReturnsTokenValue()
    {
        var jwt = CreateJwtToken(DateTime.UtcNow.AddHours(1));
        var response = new
        {
            access_token = jwt,
            refresh_token = "refresh-123",
            expires_in = 3600
        };

        var httpClient = CreateHttpClient(HttpStatusCode.OK, response);
        var settings = new OIdcAuthSettings
        {
            TokenEndpoint = "https://idp.example.com/token",
            Authority = "https://idp.example.com/",
            ClientId = "client-123",
            ClientSecret = "secret"
        };

        var client = new OidcClient(
            httpClient,
            settings,
            (_, _) => Task.CompletedTask);

        var result = await client.ExchangeCodeForTokenAsync("auth-code", "https://app.example.com/callback");

        result.AccessToken.Should().Be(jwt);
        result.RefreshToken.Should().Be("refresh-123");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task ExchangeCodeForTokenAsync_WithMissingAccessToken_ThrowsOAuthException()
    {
        var response = new
        {
            refresh_token = "refresh-123",
            expires_in = 3600
        };

        var httpClient = CreateHttpClient(HttpStatusCode.OK, response);
        var settings = new OIdcAuthSettings
        {
            TokenEndpoint = "https://idp.example.com/token",
            ClientId = "client-123",
            ClientSecret = "secret"
        };

        var client = new OidcClient(httpClient, settings);

        await client.Invoking(x => x.ExchangeCodeForTokenAsync("auth-code", "https://app.example.com/callback"))
            .Should().ThrowAsync<OAuthException>()
            .WithMessage("Invalid token response from IDP.");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidResponse_ReturnsTokenValue()
    {
        var response = new
        {
            access_token = CreateJwtToken(DateTime.UtcNow.AddMinutes(30)),
            refresh_token = "refresh-456",
            expires_in = 1800
        };

        var httpClient = CreateHttpClient(HttpStatusCode.OK, response);
        var settings = new OIdcAuthSettings
        {
            TokenEndpoint = "https://idp.example.com/token",
            ClientId = "client-123",
            ClientSecret = "secret"
        };

        var client = new OidcClient(
            httpClient,
            settings,
            (_, _) => Task.CompletedTask);

        var result = await client.RefreshTokenAsync("refresh-456");

        result.AccessToken.Should().Be(response.access_token);
        result.RefreshToken.Should().Be("refresh-456");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(29));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RefreshTokenAsync_WithNullOrWhiteSpaceRefreshToken_ThrowsArgumentException(string? refreshToken)
    {
        var settings = new OIdcAuthSettings
        {
            TokenEndpoint = "https://idp.example.com/token",
            ClientId = "client-123",
            ClientSecret = "secret"
        };

        var client = new OidcClient(new HttpClient(), settings);

        await client.Invoking(x => x.RefreshTokenAsync(refreshToken!))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("RefreshToken cannot be empty.*");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithHttpFailure_ThrowsOAuthException()
    {
        var httpClient = CreateHttpClient(HttpStatusCode.BadRequest, new { error = "invalid_request" });
        var settings = new OIdcAuthSettings
        {
            TokenEndpoint = "https://idp.example.com/token",
            ClientId = "client-123",
            ClientSecret = "secret"
        };

        var client = new OidcClient(httpClient, settings);

        await client.Invoking(x => x.RefreshTokenAsync("refresh-789"))
            .Should().ThrowAsync<OAuthException>()
            .WithMessage("Failed to refresh token:*");
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        var settings = new OIdcAuthSettings();

        var act = () => new OidcClient(null!, settings);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullSettings_ThrowsArgumentNullException()
    {
        var httpClient = new HttpClient();

        var act = () => new OidcClient(httpClient, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private static HttpClient CreateHttpClient(HttpStatusCode statusCode, object responseBody)
    {
        var handler = new FakeHttpMessageHandler(statusCode, responseBody);
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://idp.example.com")
        };
    }

    private static string CreateJwtToken(DateTime expiresAt)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new JwtSecurityToken(
            issuer: "https://idp.example.com",
            audience: "https://app.example.com",
            expires: expiresAt,
            claims: Array.Empty<System.Security.Claims.Claim>());

        return tokenHandler.WriteToken(tokenDescriptor);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly object _responseBody;

        public FakeHttpMessageHandler(HttpStatusCode statusCode, object responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = JsonContent.Create(_responseBody, options: new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };

            return Task.FromResult(response);
        }
    }
}
