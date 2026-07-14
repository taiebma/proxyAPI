using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using ProxyAPI.Infrastructure.Configuration;
using ProxyAPI.Infrastructure.Exceptions;
using ProxyAPI.Infrastructure.OAuth;
using Xunit;

namespace ProxyAPI.Tests.Infrastructure.Tests;

public class OidcClientAdditionalTests
{
    [Fact]
    public async Task ExchangeCodeForTokenAsync_WithMissingAccessToken_ThrowsOAuthException()
    {
        var httpClient = CreateHttpClient(HttpStatusCode.OK, new { refresh_token = "refresh-123" });
        var settings = CreateSettings();

        var client = new OidcClient(httpClient, settings, (_, _) => Task.CompletedTask);

        await client.Invoking(x => x.ExchangeCodeForTokenAsync("code", "https://app.example.com/callback"))
            .Should().ThrowAsync<OAuthException>()
            .WithMessage("Invalid token response from IDP.");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithBadHttpStatus_ThrowsOAuthException()
    {
        var httpClient = CreateHttpClient(HttpStatusCode.BadRequest, new { error = "invalid_request" });
        var settings = CreateSettings();

        var client = new OidcClient(httpClient, settings, (_, _) => Task.CompletedTask);

        await client.Invoking(x => x.RefreshTokenAsync("refresh-123"))
            .Should().ThrowAsync<OAuthException>()
            .WithMessage("Failed to refresh token:*");
    }

    [Fact]
    public async Task GetAuthorizationUrlAsync_WithNoScopes_UsesConfiguredScopes()
    {
        var settings = CreateSettings(scopes: ["openid", "profile"]);
        var client = new OidcClient(new HttpClient(), settings);

        var result = await client.GetAuthorizationUrlAsync("state-123", "https://app.example.com/callback");

        result.Should().Contain("scope=openid%20profile");
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        var settings = CreateSettings();

        var act = () => new OidcClient(null!, settings);

        act.Should().Throw<ArgumentNullException>();
    }

    private static OIdcAuthSettings CreateSettings(string[]? scopes = null)
    {
        return new OIdcAuthSettings
        {
            Authority = "https://idp.example.com",
            AuthorizationEndpoint = "https://idp.example.com/authorize",
            TokenEndpoint = "https://idp.example.com/token",
            ClientId = "client-123",
            ClientSecret = "secret",
            Scopes = scopes
        };
    }

    private static HttpClient CreateHttpClient(HttpStatusCode statusCode, object responseBody)
    {
        var handler = new StubHttpHandler(statusCode, responseBody);
        return new HttpClient(handler);
    }

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly object _responseBody;

        public StubHttpHandler(HttpStatusCode statusCode, object responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(JsonSerializer.Serialize(_responseBody))
            };
            return Task.FromResult(response);
        }
    }
}
