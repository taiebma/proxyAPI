using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProxyAPI.Domain;
using ProxyAPI.Infrastructure.Audit;
using ProxyAPI.Infrastructure.Configuration;
using ProxyAPI.Infrastructure.Exceptions;
using ProxyAPI.Infrastructure.Interfaces;
using ProxyAPI.Infrastructure.OAuth;
using ProxyAPI.Infrastructure.ValueObjects;
using Xunit;

namespace ProxyAPI.Tests.Infrastructure.Tests;

public class InfrastructureComponentTests
{
    [Fact]
    public void CacheSettings_DefaultsToSixtyMinutes()
    {
        var settings = new CacheSettings();

        settings.DefaultAbsoluteExpirationMinutes.Should().Be(60);
    }

    [Fact]
    public void OAuthSettings_DefaultsAndProperties_AreSettable()
    {
        var settings = new OAuthSettings
        {
            Authority = "https://idp.example.com",
            ClientId = "client",
            ClientSecret = "secret",
            TokenEndpoint = "https://idp.example.com/token",
            Scopes = ["openid", "profile"],
            HeaderName = "X-Test-Header",
            AdditionalParameters = new Dictionary<string, string> { ["tenant"] = "t1" }
        };

        settings.Authority.Should().Be("https://idp.example.com");
        settings.ClientId.Should().Be("client");
        settings.ClientSecret.Should().Be("secret");
        settings.TokenEndpoint.Should().Be("https://idp.example.com/token");
        settings.Scopes.Should().Equal("openid", "profile");
        settings.HeaderName.Should().Be("X-Test-Header");
        settings.AdditionalParameters!["tenant"].Should().Be("t1");
    }

    [Fact]
    public void AuditConsole_LogsRequestAndBody()
    {
        var logger = new Mock<ILogger<AuditConsole>>();
        var auditConsole = new AuditConsole(logger.Object);

        auditConsole.LogRequest(DateTime.UtcNow, "user-1", "GET", "https://example.test", 200, "payload");

        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Request:")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task TokenService_ForwardsCallsToClient()
    {
        var client = new Mock<IOAuthClient>();
        var token = new TokenValue("access-token", "refresh-token", DateTime.UtcNow.AddMinutes(30));
        client.Setup(x => x.GetTokenAsync()).ReturnsAsync(token);
        client.Setup(x => x.RefreshTokenAsync("refresh-token")).ReturnsAsync(token);

        var service = new TokenService(client.Object);

        var resolved = await service.GetTokenAsync();
        var refreshed = await service.RefreshTokenAsync("refresh-token");

        resolved.AccessToken.Should().Be("access-token");
        refreshed.AccessToken.Should().Be("access-token");
        client.Verify(x => x.GetTokenAsync(), Times.Once);
        client.Verify(x => x.RefreshTokenAsync("refresh-token"), Times.Once);
    }

    [Fact]
    public async Task OAuthClient_RefreshToken_WithInvalidResponse_ThrowsArgumentException()
    {
        var httpClient = new HttpClient(new StubHttpHandler("{}", HttpStatusCode.OK));
        var settings = new OAuthSettings
        {
            Authority = "https://idp.example.com",
            ClientId = "client",
            ClientSecret = "secret",
            TokenEndpoint = "https://idp.example.com/token"
        };

        var client = new OAuthClient(httpClient, settings);

        await FluentActions.Invoking(() => client.RefreshTokenAsync("refresh-token"))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid token response from IDP.");
    }

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly string _content;
        private readonly HttpStatusCode _statusCode;

        public StubHttpHandler(string content, HttpStatusCode statusCode)
        {
            _content = content;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content)
            };
            return Task.FromResult(response);
        }
    }
}
