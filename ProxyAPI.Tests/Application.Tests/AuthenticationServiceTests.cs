namespace ProxyAPI.Tests.Application.Tests;

using FluentAssertions;
using Moq;
using ProxyAPI.Application.DTOs;
using ProxyAPI.Application.Services;
using ProxyAPI.Domain.Exceptions;
using ProxyAPI.Domain.Interfaces;
using ProxyAPI.Domain.ValueObjects;
using Xunit;

public class AuthenticationServiceTests
{
    private readonly Mock<ITokenCache> _mockTokenCache;
    private readonly Mock<IOAuthClient> _mockOAuthClient;
    private readonly Mock<IMemoryAuthenticationSessions> _mockMemoryAuthenticationSessions;
    private readonly AuthenticationService _service;

    public AuthenticationServiceTests()
    {
        _mockTokenCache = new Mock<ITokenCache>();
        _mockOAuthClient = new Mock<IOAuthClient>();
        _mockMemoryAuthenticationSessions = new Mock<IMemoryAuthenticationSessions>();
        _service = new AuthenticationService(_mockTokenCache.Object, _mockOAuthClient.Object, _mockMemoryAuthenticationSessions.Object);
    }

    [Fact]
    public async Task GetAuthorizationUrlAsync_ReturnsValidUrl()
    {
        var redirectUri = "http://localhost/callback";
        _mockOAuthClient
            .Setup(x => x.GetAuthorizationUrlAsync(It.IsAny<string>(), redirectUri, null))
            .ReturnsAsync("http://idp/authorize?state=123");

        var result = await _service.GetAuthorizationUrlAsync(redirectUri);

        result.Url.Should().Contain("authorize");
        result.State.Should().NotBeEmpty();
        result.SessionId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task HandleCallbackAsync_WithInvalidCode_ThrowsOAuthException()
    {
        var request = new AuthorizationCodeRequest("", "state", "session-id");

        var act = () => _service.HandleCallbackAsync(request);
        await act.Should().ThrowAsync<OAuthException>();
    }

    [Fact]
    public async Task HandleCallbackAsync_WithValidCode_ReturnsClientContext()
    {
        var authUrlResponse = await _service.GetAuthorizationUrlAsync("http://localhost/callback");

        var token = new TokenValue("access-token", "refresh-token", DateTime.UtcNow.AddHours(1));
        var fakeOAuthClient = new FakeOAuthClient(token);
        var service = new AuthenticationService(_mockTokenCache.Object, fakeOAuthClient, _mockMemoryAuthenticationSessions.Object);

        var authUrl = await service.GetAuthorizationUrlAsync("http://localhost/callback");
        var request = new AuthorizationCodeRequest("auth-code", authUrl.State, authUrl.SessionId);
        var result = await service.HandleCallbackAsync(request);

        result.AccessToken.Should().Be("access-token");
        result.ClientId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetClientContextAsync_WithValidClientId_ReturnsCachedToken()
    {
        var clientId = new ClientId("client-1");
        var token = new TokenValue("access-token", null, DateTime.UtcNow.AddHours(1));

        _mockTokenCache.Setup(x => x.Get(clientId)).Returns(token);

        var result = await _service.GetClientContextAsync("client-1");

        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("access-token");
    }

    [Fact]
    public async Task GetClientContextAsync_WithExpiredToken_ReturnsNull()
    {
        var clientId = new ClientId("client-1");
        _mockTokenCache.Setup(x => x.Get(clientId)).Returns((TokenValue?)null);

        var result = await _service.GetClientContextAsync("client-1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task RefreshClientContextAsync_WithValidRefreshToken_ReturnsNewToken()
    {
        var clientId = new ClientId("client-1");
        var oldToken = new TokenValue("old-token", "refresh-token", DateTime.UtcNow.AddHours(1));
        var newToken = new TokenValue("new-token", "new-refresh", DateTime.UtcNow.AddHours(1));

        _mockTokenCache.Setup(x => x.Get(clientId)).Returns(oldToken);
        _mockOAuthClient
            .Setup(x => x.RefreshTokenAsync("refresh-token"))
            .ReturnsAsync(newToken);

        var result = await _service.RefreshClientContextAsync("client-1");

        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("new-token");
        _mockTokenCache.Verify(x => x.Set(clientId, newToken), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_RemovesClientFromCache()
    {
        var clientId = "client-1";
        await _service.LogoutAsync(clientId);

        _mockTokenCache.Verify(x => x.Remove(It.IsAny<ClientId>()), Times.Once);
    }
}

internal class FakeOAuthClient : IOAuthClient
{
    private readonly TokenValue _tokenToReturn;

    public FakeOAuthClient(TokenValue tokenToReturn)
    {
        _tokenToReturn = tokenToReturn;
    }

    public Task<string> GetAuthorizationUrlAsync(string state, string redirectUri, string[]? scopes = null)
    {
        return Task.FromResult($"http://idp/auth?state={state}");
    }

    public Task<TokenValue> ExchangeCodeForTokenAsync(string code, string redirectUri, string? codeVerifier = null)
    {
        return Task.FromResult(_tokenToReturn);
    }

    public Task<TokenValue> RefreshTokenAsync(string refreshToken)
    {
        throw new NotImplementedException();
    }
}
